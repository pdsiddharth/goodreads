// <copyright file="TeamPostController.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.GoodReads.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;
    using Microsoft.Teams.Apps.GoodReads.Authentication;
    using Microsoft.Teams.Apps.GoodReads.Common;
    using Microsoft.Teams.Apps.GoodReads.Common.Interfaces;
    using Microsoft.Teams.Apps.GoodReads.Models;

    /// <summary>
    /// Controller to handle team post API operations.
    /// </summary>
    [ApiController]
    [Route("api/teampost")]
    public class TeamPostController : BaseGoodReadsController
    {
        /// <summary>
        /// Event name for team post HTTP get call.
        /// </summary>
        private const string RecordTeamPostHTTPGetCall = "Team post - HTTP Get call succeeded";

        /// <summary>
        /// Event name for filtered team post HTTP get call.
        /// </summary>
        private const string RecordFilteredTeamPostsHTTPGetCall = "Filtered team post - HTTP Get call succeeded";

        /// <summary>
        /// Event name for team post unique names HTTP get call.
        /// </summary>
        private const string RecordUniqueUserNamesHTTPGetCall = "Team post unique user names - HTTP Get call succeeded";

        /// <summary>
        /// Event name for searched team post for filter HTTP get call.
        /// </summary>
        private const string RecordSearchedTeamPostsForTitleHTTPGetCall = "Team post title search - HTTP Get call succeeded";

        /// <summary>
        /// Event name for team post applied filters HTTP get call.
        /// </summary>
        private const string RecordAppliedFiltersTeamPostsHTTPGetCall = "Team post applied filters - HTTP Get call succeeded";

        /// <summary>
        /// Event name for team post search HTTP get call.
        /// </summary>
        private const string RecordSearchPostsHTTPGetCall = "Team post search result - HTTP Get call succeeded";

        /// <summary>
        /// Event name for team post unique author names HTTP get call.
        /// </summary>
        private const string RecordAuthorNamesHTTPGetCall = "Team post unique author names - HTTP Get call succeeded";

        /// <summary>
        /// Event name for team post HTTP put call.
        /// </summary>
        private const string RecordTeamPostHTTPPutCall = "Team post - HTTP Put call succeeded";

        /// <summary>
        /// Event name for team post HTTP post call.
        /// </summary>
        private const string RecordTeamPostHTTPPostCall = "Team post - HTTP Post call succeeded";

        /// <summary>
        /// Event name for team post HTTP delete call.
        /// </summary>
        private const string RecordTeamPostHTTPDeleteCall = "Team post - HTTP Delete call succeeded";

        /// <summary>
        /// Sends logs to the Application Insights service.
        /// </summary>
        private readonly ILogger logger;

        /// <summary>
        /// Instance of team post storage helper to update post and get information of posts.
        /// </summary>
        private readonly ITeamPostStorageHelper teamPostStorageHelper;

        /// <summary>
        /// Instance of team post storage provider to update post and get information of posts.
        /// </summary>
        private readonly ITeamPostStorageProvider teamPostStorageProvider;

        /// <summary>
        /// Instance of user private post storage provider for private posts.
        /// </summary>
        private readonly IUserPrivatePostStorageProvider userPrivatePostStorageProvider;

        /// <summary>
        /// Instance of Search service for working with Microsoft Azure Table storage.
        /// </summary>
        private readonly ITeamPostSearchService teamPostSearchService;

        /// <summary>
        /// Instance of user validator to check whether is valid team user or not.
        /// </summary>
        private readonly UserValidator userValidator;

        /// <summary>
        /// Instance of team tags storage provider for team's discover posts.
        /// </summary>
        private readonly ITeamTagStorageProvider teamTagStorageProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="TeamPostController"/> class.
        /// </summary>
        /// <param name="logger">Sends logs to the Application Insights service.</param>
        /// <param name="telemetryClient">The Application Insights telemetry client.</param>
        /// <param name="teamPostStorageHelper">Team post storage helper dependency injection.</param>
        /// <param name="teamPostStorageProvider">Team post storage provider dependency injection.</param>
        /// <param name="userPrivatePostStorageProvider">User private post storage provider dependency injection.</param>
        /// <param name="teamPostSearchService">The team post search service dependency injection.</param>
        /// <param name="userValidator">User validator to check whether is valid team user or not.</param>
        /// <param name="teamTagStorageProvider">Team tags storage provider dependency injection.</param>
        public TeamPostController(
            ILogger<TeamPostController> logger,
            TelemetryClient telemetryClient,
            ITeamPostStorageHelper teamPostStorageHelper,
            ITeamPostStorageProvider teamPostStorageProvider,
            IUserPrivatePostStorageProvider userPrivatePostStorageProvider,
            ITeamPostSearchService teamPostSearchService,
            UserValidator userValidator,
            ITeamTagStorageProvider teamTagStorageProvider)
            : base(telemetryClient)
        {
            this.logger = logger;
            this.teamPostStorageHelper = teamPostStorageHelper;
            this.teamPostStorageProvider = teamPostStorageProvider;
            this.userPrivatePostStorageProvider = userPrivatePostStorageProvider;
            this.teamPostSearchService = teamPostSearchService;
            this.userValidator = userValidator;
            this.teamTagStorageProvider = teamTagStorageProvider;
        }

        /// <summary>
        /// Get call to retrieve list of team posts.
        /// </summary>
        /// <param name="pageCount">Page number to get search data from Azure Search service.</param>
        /// <returns>List of team posts.</returns>
        [HttpGet]
        public async Task<IActionResult> GetAsync(int pageCount)
        {
            var skipRecords = pageCount * Constants.LazyLoadPerPagePostCount;

            try
            {
                this.logger.LogInformation("Call to retrieve list of team posts.");
                var teamPosts = await this.teamPostSearchService.GetTeamPostsAsync(TeamPostSearchScope.AllItems, searchQuery: null, userObjectId: null, count: Constants.LazyLoadPerPagePostCount, skip: skipRecords);
                this.RecordEvent(RecordTeamPostHTTPGetCall);

                return this.Ok(teamPosts);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error while making call to team post service.");
                throw;
            }
        }

        /// <summary>
        /// Post call to store team posts details data in Microsoft Azure Table storage.
        /// </summary>
        /// <param name="teamPostEntity">Holds team post detail entity data.</param>
        /// <returns>Returns true for successful operation.</returns>
        [HttpPost]
        public async Task<IActionResult> PostAsync([FromBody] TeamPostEntity teamPostEntity)
        {
            try
            {
                this.logger.LogInformation("Call to add team posts details.");
                var updatedTeamPostEntity = this.teamPostStorageHelper.CreateTeamPostModel(teamPostEntity, this.UserAadId, this.UserName);
                var result = await this.teamPostStorageProvider.UpsertTeamPostAsync(updatedTeamPostEntity);

                if (result)
                {
                    this.RecordEvent(RecordTeamPostHTTPPostCall);
                    await this.teamPostSearchService.RunIndexerOnDemandAsync();

                    return this.Ok(updatedTeamPostEntity);
                }

                return this.Ok(result);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error while making call to team post service.");
                throw;
            }
        }

        /// <summary>
        /// Put call to update team post details data in Microsoft Azure Table storage.
        /// </summary>
        /// <param name="teamPostEntity">Holds team post detail entity data.</param>
        /// <returns>Returns true for successful operation.</returns>
        [HttpPut]
        public async Task<IActionResult> PutAsync([FromBody] TeamPostEntity teamPostEntity)
        {
            teamPostEntity = teamPostEntity ?? throw new ArgumentNullException(nameof(teamPostEntity));

            try
            {
                this.logger.LogInformation("Call to update team post details.");

                if (string.IsNullOrEmpty(teamPostEntity.PostId))
                {
                    this.logger.LogError("Error while updating team post details data in Microsoft Azure Table storage.");
                    return this.GetErrorResponse(StatusCodes.Status400BadRequest, "Error while updating team post details data");
                }

                var updatedTeamPostEntity = this.teamPostStorageHelper.CreateUpdatedTeamPostModel(teamPostEntity);
                var result = await this.teamPostStorageProvider.UpsertTeamPostAsync(updatedTeamPostEntity);

                if (result)
                {
                    this.RecordEvent(RecordTeamPostHTTPPutCall);
                    await this.teamPostSearchService.RunIndexerOnDemandAsync();
                }

                return this.Ok(result);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error while making call to team post service.");
                throw;
            }
        }

        /// <summary>
        /// Delete call to delete team post details data in Microsoft Azure Table storage.
        /// </summary>
        /// <param name="postId">Id of the post to be deleted.</param>
        /// <param name="userId">Azure Active Directory id of user.</param>
        /// <returns>Returns true for successful operation.</returns>
        [HttpDelete]
        public async Task<IActionResult> DeleteAsync(string postId, string userId)
        {
            try
            {
                this.logger.LogInformation("Call to delete team post details.");

                postId = postId ?? throw new ArgumentNullException(nameof(postId));

                var teamPostEntity = await this.teamPostStorageProvider.GetTeamPostEntityAsync(postId);
                teamPostEntity.IsRemoved = true;
                var result = await this.teamPostStorageProvider.UpsertTeamPostAsync(teamPostEntity);

                if (result)
                {
                    await this.teamPostSearchService.RunIndexerOnDemandAsync();
                    await this.userPrivatePostStorageProvider.DeletePrivatePostAsync(postId, userId);
                    this.RecordEvent(RecordTeamPostHTTPDeleteCall);
                }

                return this.Ok(result);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error while making call to team post service.");
                throw;
            }
        }

        /// <summary>
        /// Get filtered team posts for particular team as per the configured tags in Microsoft Azure Table storage.
        /// </summary>
        /// <param name="teamId">Team id for which data will fetch.</param>
        /// <param name="pageCount">Page number to get search data from Azure Search service.</param>
        /// <returns>Returns filtered list of team posts as per the configured tags.</returns>
        [HttpGet("team-discover-posts")]
        public async Task<IActionResult> GetFilteredTeamPostsAsync(string teamId, int pageCount)
        {
            var skipRescords = pageCount * Constants.LazyLoadPerPagePostCount;

            try
            {
                this.logger.LogInformation("Call to get filtered team post details.");

                if (string.IsNullOrEmpty(teamId))
                {
                    this.logger.LogError("Error while fetching filtered team posts as per the configured tags from Microsoft Azure Table storage.");
                    return this.GetErrorResponse(StatusCodes.Status400BadRequest, "Error while fetching filtered team posts as per the configured tags from Microsoft Azure Table storage.");
                }

                var isUserValid = await this.userValidator.ValidateAsync(teamId, this.UserAadId);
                if (!isUserValid)
                {
                    return this.Forbid();
                }

                IEnumerable<TeamPostEntity> teamPosts = new List<TeamPostEntity>();

                // Get tags based on the teamid for which tags has configured.
                var teamTagEntity = await this.teamTagStorageProvider.GetTeamTagsDataAsync(teamId);

                if (teamTagEntity == null || string.IsNullOrEmpty(teamTagEntity.Tags))
                {
                    return this.Ok(teamPosts);
                }

                // Prepare query based on the tags and get the data using search service.
                var tagsQuery = this.teamPostStorageHelper.GetTags(teamTagEntity.Tags);
                teamPosts = await this.teamPostSearchService.GetTeamPostsAsync(TeamPostSearchScope.FilterAsPerTeamTags, tagsQuery, userObjectId: null, count: Constants.LazyLoadPerPagePostCount, skip: skipRescords);

                // Filter the data based on the configured tags.
                var filteredTeamPosts = this.teamPostStorageHelper.GetFilteredTeamPostsAsPerTags(teamPosts, teamTagEntity.Tags);
                this.RecordEvent(RecordFilteredTeamPostsHTTPGetCall);

                return this.Ok(filteredTeamPosts);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error while making call to team post service.");
                throw;
            }
        }

        /// <summary>
        /// Get unique user names from Microsoft Azure Table storage.
        /// </summary>
        /// <returns>Returns unique user names.</returns>
        [HttpGet("unique-user-names")]
        public async Task<IActionResult> GetUniqueUserNamesAsync()
        {
            try
            {
                this.logger.LogInformation("Call to get unique names.");

                var teamPosts = await this.teamPostSearchService.GetTeamPostsAsync(TeamPostSearchScope.UniqueUserNames, searchQuery: null, userObjectId: null);
                var authorNames = this.teamPostStorageHelper.GetAuthorNamesAsync(teamPosts);

                this.RecordEvent(RecordUniqueUserNamesHTTPGetCall);

                return this.Ok(authorNames);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error while making call to get unique user names.");
                throw;
            }
        }

        /// <summary>
        /// Get list of team posts as per the title text.
        /// </summary>
        /// <param name="searchText">Search text represents the title field to find and get team posts.</param>
        /// <param name="pageCount">Page number to get search data from Azure Search service.</param>
        /// <returns>List of filtered team posts as per the search text for title.</returns>
        [HttpGet("search-team-posts")]
        public async Task<IActionResult> GetSearchedTeamPostsForTitleAsync(string searchText, int pageCount)
        {
            var skipRescords = pageCount * Constants.LazyLoadPerPagePostCount;

            try
            {
                this.logger.LogInformation("Call to get list of team posts.");
                var teamPosts = await this.teamPostSearchService.GetTeamPostsAsync(TeamPostSearchScope.SearchTeamPostsForTitleText, searchText, userObjectId: null, skip: skipRescords, count: Constants.LazyLoadPerPagePostCount);
                this.RecordEvent(RecordSearchedTeamPostsForTitleHTTPGetCall);

                return this.Ok(teamPosts);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error while making call to get team posts for search title text.");
                throw;
            }
        }

        /// <summary>
        /// Get team posts as per the applied filters from Microsoft Azure Table storage.
        /// </summary>
        /// <param name="postTypes">Semicolon separated types of posts like Blog post or Other.</param>
        /// /// <param name="sharedByNames">Semicolon separated User names to filter the posts.</param>
        /// /// <param name="tags">Semicolon separated tags to match the post tags for which data will fetch.</param>
        /// /// <param name="sortBy">Represents sorting type like: Popularity or Newest.</param>
        /// <param name="teamId">Team id to get configured tags for a team.</param>
        /// <param name="pageCount">Page count for which post needs to be fetched.</param>
        /// <returns>Returns filtered list of team posts as per the selected filters.</returns>
        [HttpGet("applied-filtered-team-posts")]
        public async Task<IActionResult> GetAppliedFiltersTeamPostsAsync(string postTypes, string sharedByNames, string tags, string sortBy, string teamId, int pageCount)
        {
            var skipRecords = pageCount * Constants.LazyLoadPerPagePostCount;

            TeamTagEntity teamTagEntity = new TeamTagEntity();

            try
            {
                this.logger.LogInformation("Call to get team posts as per the applied filters.");

                // Team id will be empty when called from personal scope Discover tab.
                if (!string.IsNullOrEmpty(teamId))
                {
                    teamTagEntity = await this.teamTagStorageProvider.GetTeamTagsDataAsync(teamId);
                    var savedTags = teamTagEntity?.Tags?.Split(";").Where(tag => tag.Trim().Length > 0);
                    var tagsList = tags?.Split(';').Where(tag => tag.Trim().Length > 0).Intersect(savedTags);
                    tags = tagsList != null && tagsList.Any() ? string.Join(';', tagsList) : teamTagEntity?.Tags;
                }

                var tagsQuery = string.IsNullOrEmpty(tags) ? "*" : this.teamPostStorageHelper.GetTags(tags);
                var filterQuery = this.teamPostStorageHelper.GetFilterSearchQuery(postTypes, sharedByNames);
                var teamPosts = await this.teamPostSearchService.GetTeamPostsAsync(TeamPostSearchScope.FilterTeamPosts, tagsQuery, userObjectId: null, sortBy: sortBy, filterQuery: filterQuery, count: Constants.LazyLoadPerPagePostCount, skip: skipRecords);

                this.RecordEvent(RecordAppliedFiltersTeamPostsHTTPGetCall);

                return this.Ok(teamPosts);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error while making call to get team posts as per the applied filters service.");
                throw;
            }
        }

        /// <summary>
        /// Get list of posts for team's discover tab, as per the configured tags and title of posts.
        /// </summary>
        /// <param name="searchText">Search text represents the title of the posts.</param>
        /// <param name="teamId">Team id to get configured tags for a team.</param>
        /// <param name="pageCount">Page count for which post needs to be fetched.</param>
        /// <returns>List of posts as per the title and configured tags.</returns>
        [HttpGet("team-search-posts")]
        public async Task<IActionResult> GetTeamDiscoverSearchPostsAsync(string searchText, string teamId, int pageCount)
        {
            var skipRecords = pageCount * Constants.LazyLoadPerPagePostCount;

            try
            {
                this.logger.LogInformation("Call to get list of posts as per the configured tags and title.");

                if (string.IsNullOrEmpty(teamId))
                {
                    this.logger.LogError("Error while fetching search posts as per the title and configured tags from Microsoft Azure Table storage.");
                    return this.GetErrorResponse(StatusCodes.Status400BadRequest, "Error while fetching search posts as per the title and configured tags from Microsoft Azure Table storage.");
                }

                var teamTagEntity = await this.teamTagStorageProvider.GetTeamTagsDataAsync(teamId);
                var tagsQuery = string.IsNullOrEmpty(teamTagEntity?.Tags) ? "*" : this.teamPostStorageHelper.GetTags(teamTagEntity.Tags);
                var filterQuery = $"search.ismatch('{tagsQuery}', 'Tags')";
                var teamPosts = await this.teamPostSearchService.GetTeamPostsAsync(TeamPostSearchScope.SearchTeamPostsForTitleText, searchText, userObjectId: null, count: Constants.LazyLoadPerPagePostCount, skip: skipRecords, filterQuery: filterQuery);
                this.RecordEvent(RecordSearchPostsHTTPGetCall);

                return this.Ok(teamPosts);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error while making call to get team posts for search title text.");
                throw;
            }
        }

        /// <summary>
        /// Get unique author names from Microsoft Azure Table storage.
        /// </summary>
        /// <param name="teamId">Team id to get the configured tags for a team.</param>
        /// <returns>Returns unique user names.</returns>
        [HttpGet("authors-for-tags")]
        public async Task<IActionResult> GetAuthorNamesAsync(string teamId)
        {
            try
            {
                this.logger.LogInformation("Call to get unique author names.");

                var names = new List<string>();

                // Get tags based on the teamid for which tags has configured.
                var teamTagEntity = await this.teamTagStorageProvider.GetTeamTagsDataAsync(teamId);

                if (teamTagEntity == null || string.IsNullOrEmpty(teamTagEntity.Tags))
                {
                    return this.Ok(names);
                }

                var tagsQuery = this.teamPostStorageHelper.GetTags(teamTagEntity.Tags);
                var teamPosts = await this.teamPostSearchService.GetTeamPostsAsync(TeamPostSearchScope.FilterAsPerTeamTags, tagsQuery, null, null);
                var authorNames = this.teamPostStorageHelper.GetAuthorNamesAsync(teamPosts);
                this.RecordEvent(RecordAuthorNamesHTTPGetCall);

                return this.Ok(authorNames);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error while making call to get unique user names.");
                throw;
            }
        }
    }
}