// <copyright file="UserVoteController.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.GoodReads.Controllers
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;
    using Microsoft.Teams.Apps.GoodReads.Common.Interfaces;
    using Microsoft.Teams.Apps.GoodReads.Models;

    /// <summary>
    /// Controller to handle user vote operations.
    /// </summary>
    [ApiController]
    [Route("api/uservote")]
    [Authorize]
    public class UserVoteController : BaseGoodReadsController
    {
        /// <summary>
        /// Event name for user vote HTTP get call.
        /// </summary>
        private const string RecordUserVoteHTTPGetCall = "User votes - HTTP Get call succeeded.";

        /// <summary>
        /// Event name for user vote HTTP post call.
        /// </summary>
        private const string RecordUserVoteHTTPPostCall = "User votes - HTTP Post call succeeded.";

        /// <summary>
        /// Event name for user vote HTTP delete call.
        /// </summary>
        private const string RecordUserVoteHTTPDeleteCall = "User votes - HTTP Delete call succeeded.";

        /// <summary>
        /// Sends logs to the Application Insights service.
        /// </summary>
        private readonly ILogger logger;

        /// <summary>
        /// Instance of user vote storage helper to add and delete user vote.
        /// </summary>
        private readonly IUserVoteStorageHelper userVoteStorageHelper;

        /// <summary>
        /// Instance of team post storage provider.
        /// </summary>
        private readonly ITeamPostStorageProvider teamPostStorageProvider;

        /// <summary>
        /// Instance of Search service for working with Microsoft Azure Table storage.
        /// </summary>
        private readonly ITeamPostSearchService teamPostSearchService;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserVoteController"/> class.
        /// </summary>
        /// <param name="logger">Sends logs to the Application Insights service.</param>
        /// <param name="telemetryClient">The Application Insights telemetry client.</param>
        /// <param name="userVoteStorageHelper">Instance of user vote storage helper to add and delete user vote.</param>
        /// <param name="teamPostStorageProvider">Instance of team post storage provider to update post and get information of posts.</param>
        /// <param name="teamPostSearchService">The team post search service dependency injection.</param>
        public UserVoteController(
            ILogger<TeamPostController> logger,
            TelemetryClient telemetryClient,
            IUserVoteStorageHelper userVoteStorageHelper,
            ITeamPostStorageProvider teamPostStorageProvider,
            ITeamPostSearchService teamPostSearchService)
            : base(telemetryClient)
        {
            this.logger = logger;
            this.userVoteStorageHelper = userVoteStorageHelper;
            this.teamPostStorageProvider = teamPostStorageProvider;
            this.teamPostSearchService = teamPostSearchService;
        }

        /// <summary>
        /// Get call to retrieve list of votes for user.
        /// </summary>
        /// <returns>List of team posts.</returns>
        [HttpGet("votes")]
        public async Task<IActionResult> GetVotesAsync()
        {
            try
            {
                this.logger.LogInformation("call to retrieve list of votes for user.");

                var userVotes = await this.userVoteStorageHelper.GetVotesAsync(this.UserAadId);
                this.RecordEvent(RecordUserVoteHTTPGetCall);

                return this.Ok(userVotes);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error while making call to team post service.");
                throw;
            }
        }

        /// <summary>
        /// Post call to store user vote in Microsoft Azure Table storage.
        /// </summary>
        /// <param name="userVote">Holds vote entity data.</param>
        /// <returns>Returns true for successful operation.</returns>
        [HttpPost("vote")]
        public async Task<IActionResult> AddVoteAsync([FromBody] UserVoteEntity userVote)
        {
            try
            {
                this.logger.LogInformation("call to add user vote.");

                userVote = userVote ?? throw new ArgumentNullException(nameof(userVote));

                userVote.UserId = this.UserAadId;
                var addResult = await this.userVoteStorageHelper.AddUserVoteDetailsAsync(userVote);

                if (addResult)
                {
                    var teamPostEntity = await this.teamPostStorageProvider.GetTeamPostEntityAsync(userVote.PostId);
                    teamPostEntity.TotalVotes++;
                    var result = await this.teamPostStorageProvider.UpsertTeamPostAsync(teamPostEntity);

                    if (result)
                    {
                        this.RecordEvent(RecordUserVoteHTTPPostCall);
                        await this.teamPostSearchService.RunIndexerOnDemandAsync();
                    }

                    return this.Ok(result);
                }

                return this.Ok(false);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error while making call to team post service.");
                throw;
            }
        }

        /// <summary>
        /// Delete call to delete user vote details from Microsoft Azure Table storage.
        /// </summary>
        /// <param name="postId">Id of the post to delete vote.</param>
        /// <returns>Returns true for successful operation.</returns>
        [HttpDelete]
        public async Task<IActionResult> DeleteVoteAsync(string postId)
        {
            try
            {
                this.logger.LogInformation("call to delete user vote.");

                postId = postId ?? throw new ArgumentNullException(nameof(postId));
                var deleteResult = await this.userVoteStorageHelper.DeleteUserVoteDetailsAsync(postId, this.UserAadId);

                if (deleteResult)
                {
                    var teamPostEntity = await this.teamPostStorageProvider.GetTeamPostEntityAsync(postId);
                    teamPostEntity.TotalVotes--;
                    var result = await this.teamPostStorageProvider.UpsertTeamPostAsync(teamPostEntity);

                    if (result)
                    {
                        this.RecordEvent(RecordUserVoteHTTPDeleteCall);
                        await this.teamPostSearchService.RunIndexerOnDemandAsync();
                    }

                    return this.Ok(result);
                }

                return this.Ok(false);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error while making call to team post service.");
                throw;
            }
        }
    }
}