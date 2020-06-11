// <copyright file="TeamPostStorageHelper.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.GoodReads.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Microsoft.Extensions.Logging;
    using Microsoft.Teams.Apps.GoodReads.Common.Interfaces;
    using Microsoft.Teams.Apps.GoodReads.Models;

    /// <summary>
    /// Implements team post storage helper which helps to construct the model, create search query for team post.
    /// </summary>
    public class TeamPostStorageHelper : ITeamPostStorageHelper
    {
        /// <summary>
        /// Instance to send logs to the Application Insights service.
        /// </summary>
        private readonly ILogger<TeamPostStorageHelper> logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="TeamPostStorageHelper"/> class.
        /// </summary>
        /// <param name="logger">Instance to send logs to the Application Insights service.</param>
        public TeamPostStorageHelper(
            ILogger<TeamPostStorageHelper> logger)
        {
            this.logger = logger;
        }

        /// <summary>
        /// Create team post model data.
        /// </summary>
        /// <param name="teamPostEntity">Team post detail.</param>
        /// <param name="userId">User Azure active directory id.</param>
        /// <param name="userName">Author who created the post.</param>
        /// <returns>A task that represents team post entity data.</returns>
        public TeamPostEntity CreateTeamPostModel(TeamPostEntity teamPostEntity, string userId, string userName)
        {
            teamPostEntity = teamPostEntity ?? throw new ArgumentNullException(nameof(teamPostEntity));

            try
            {
                teamPostEntity.PostId = Guid.NewGuid().ToString();
                teamPostEntity.UserId = userId;
                teamPostEntity.CreatedByName = userName;
                teamPostEntity.CreatedDate = DateTime.UtcNow;
                teamPostEntity.UpdatedDate = DateTime.UtcNow;
                teamPostEntity.IsRemoved = false;

                return teamPostEntity;
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Exception occurred while creating the team post model data.");
                throw;
            }
        }

        /// <summary>
        /// Create updated team post model data for Microsoft Azure Table storage.
        /// </summary>
        /// <param name="teamPostEntity">Team post detail.</param>
        /// <returns>A task that represents team post entity updated data.</returns>
        public TeamPostEntity CreateUpdatedTeamPostModel(TeamPostEntity teamPostEntity)
        {
            teamPostEntity = teamPostEntity ?? throw new ArgumentNullException(nameof(teamPostEntity));

            try
            {
                teamPostEntity.UpdatedDate = DateTime.UtcNow;
                teamPostEntity.IsRemoved = false;

                return teamPostEntity;
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Exception occurred while getting the team post model data");
                throw;
            }
        }

        /// <summary>
        /// Get filtered team posts as per the configured tags.
        /// </summary>
        /// <param name="teamPosts">Team post entities.</param>
        /// <param name="searchText">Search text for tags.</param>
        /// <returns>Represents team posts.</returns>
        public IEnumerable<TeamPostEntity> GetFilteredTeamPostsAsPerTags(IEnumerable<TeamPostEntity> teamPosts, string searchText)
        {
            try
            {
                teamPosts = teamPosts ?? throw new ArgumentNullException(nameof(teamPosts));
                searchText = searchText ?? throw new ArgumentNullException(nameof(searchText));
                var filteredTeamPosts = new List<TeamPostEntity>();

                foreach (var teamPost in teamPosts)
                {
                    foreach (var tag in searchText.Split(";"))
                    {
                        if (Array.Exists(teamPost.Tags?.Split(";"), tagText => tagText.Equals(tag.Trim(), StringComparison.InvariantCultureIgnoreCase)))
                        {
                            filteredTeamPosts.Add(teamPost);
                            break;
                        }
                    }
                }

                return filteredTeamPosts;
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Exception occurred while preparing the team preference entities list.");
                throw;
            }
        }

        /// <summary>
        /// Get tags to fetch team posts as per the configured tags.
        /// </summary>
        /// <param name="tags">Tags of a configured team post.</param>
        /// <returns>Represents tags to fetch team posts.</returns>
        public string GetTags(string tags)
        {
            try
            {
                tags = tags ?? throw new ArgumentNullException(nameof(tags));
                var postTags = tags.Split(';').Where(postType => !string.IsNullOrWhiteSpace(postType)).ToList();

                return string.Join(" ", postTags);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Exception occurred while preparing the query for tags to get team posts as per the configured tags.");
                throw;
            }
        }

        /// <summary>
        /// Get filtered team posts as per the date range from Microsoft Azure Table storage.
        /// </summary>
        /// <param name="teamPosts">Team posts data.</param>
        /// <param name="fromDate">Start date from which data should fetch.</param>
        /// <param name="toDate">End date till when data should fetch.</param>
        /// <returns>A task that represent collection to hold team posts data.</returns>
        public IEnumerable<TeamPostEntity> GetTeamPostsInDateRangeAsync(IEnumerable<TeamPostEntity> teamPosts, DateTime fromDate, DateTime toDate)
        {
            return teamPosts.Where(post => post.UpdatedDate >= fromDate && post.UpdatedDate <= toDate);
        }

        /// <summary>
        /// Get filtered user names from team posts data.
        /// </summary>
        /// <param name="teamPosts">Represents a collection of team posts.</param>
        /// <returns>Represents team posts.</returns>
        public IEnumerable<string> GetAuthorNamesAsync(IEnumerable<TeamPostEntity> teamPosts)
        {
            try
            {
                teamPosts = teamPosts ?? throw new ArgumentNullException(nameof(teamPosts));

                return teamPosts.Select(post => post.CreatedByName).Distinct().OrderBy(createdByName => createdByName);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Exception occurred while preparing the unique user names list.");
                throw;
            }
        }

        /// <summary>
        /// Get combined query to fetch team posts as per the selected filter.
        /// </summary>
        /// <param name="postTypes">Post type like: Blog post or Other.</param>
        /// <param name="sharedByNames">User names selected in filter.</param>
        /// <returns>Represents user names query to filter team posts.</returns>
        public string GetFilterSearchQuery(string postTypes, string sharedByNames)
        {
            try
            {
                var typesQuery = this.GetPostTypesQuery(postTypes);
                var sharedByNamesQuery = this.GetSharedByNamesQuery(sharedByNames);
                string combinedQuery = string.Empty;

                if (string.IsNullOrEmpty(typesQuery) && string.IsNullOrEmpty(sharedByNamesQuery))
                {
                    return null;
                }

                if (!string.IsNullOrEmpty(typesQuery) && !string.IsNullOrEmpty(sharedByNamesQuery))
                {
                    return $"({typesQuery}) and ({sharedByNamesQuery})";
                }

                if (!string.IsNullOrEmpty(typesQuery))
                {
                    return $"({typesQuery})";
                }

                if (!string.IsNullOrEmpty(sharedByNamesQuery))
                {
                    return $"({sharedByNamesQuery})";
                }

                return null;
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Exception occurred while preparing the query to get filter bar search result for team posts.");
                throw;
            }
        }

        /// <summary>
        /// Get post type query to fetch team posts as per the selected filter.
        /// </summary>
        /// <param name="postTypes">Post type like: Blog post or Other.</param>
        /// <returns>Represents post type query to filter team posts.</returns>
        private string GetPostTypesQuery(string postTypes)
        {
            try
            {
                if (string.IsNullOrEmpty(postTypes))
                {
                    return null;
                }

                StringBuilder postTypesQuery = new StringBuilder();
                var postTypesData = postTypes.Split(';').Where(postType => !string.IsNullOrWhiteSpace(postType)).Select(postType => postType.Trim()).ToList();

                if (postTypesData.Count > 1)
                {
                    var posts = postTypesData.Take(postTypesData.Count - 1).ToList();
                    posts.ForEach(postType =>
                    {
                        postTypesQuery.Append($"Type eq '{postType}' or ");
                    });

                    postTypesQuery.Append($"Type eq '{postTypesData.Last()}'");
                }
                else
                {
                    postTypesQuery.Append($"Type eq '{postTypesData.Last()}'");
                }

                return postTypesQuery.ToString();
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Exception occurred while preparing the query for post types to get team posts as per the selected types.");
                throw;
            }
        }

        /// <summary>
        /// Get user names query to fetch team posts as per the selected filter.
        /// </summary>
        /// <param name="sharedByNames">User names selected in filter.</param>
        /// <returns>Represents user names query to filter team posts.</returns>
        private string GetSharedByNamesQuery(string sharedByNames)
        {
            try
            {
                if (string.IsNullOrEmpty(sharedByNames))
                {
                    return null;
                }

                StringBuilder sharedByNamesQuery = new StringBuilder();
                var sharedByNamesData = sharedByNames.Split(';').Where(name => !string.IsNullOrWhiteSpace(name)).Select(name => name.Trim()).ToList();

                if (sharedByNamesData.Count > 1)
                {
                    var users = sharedByNamesData.Take(sharedByNamesData.Count - 1).ToList();
                    users.ForEach(user =>
                    {
                        sharedByNamesQuery.Append($"CreatedByName eq '{user}' or ");
                    });

                    sharedByNamesQuery.Append($"CreatedByName eq '{sharedByNamesData.Last()}'");
                }
                else
                {
                    sharedByNamesQuery.Append($"CreatedByName eq '{sharedByNamesData.Last()}'");
                }

                return sharedByNamesQuery.ToString();
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Exception occurred while preparing the query for shared by names to get team posts as per the selected names.");
                throw;
            }
        }
    }
}
