// <copyright file="UserPrivatePostController.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.GoodReads.Controllers
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;
    using Microsoft.Teams.Apps.GoodReads.Common.Interfaces;
    using Microsoft.Teams.Apps.GoodReads.Models;

    /// <summary>
    /// Controller to handle user's private posts API operations.
    /// </summary>
    [Route("api/userprivatepost")]
    [ApiController]
    [Authorize]
    public class UserPrivatePostController : BaseGoodReadsController
    {
        /// <summary>
        /// Event name for private post HTTP get call.
        /// </summary>
        private const string RecordPrivatePostHTTPGetCall = "Private posts - HTTP Get call succeeded";

        /// <summary>
        /// Event name for private post HTTP post call.
        /// </summary>
        private const string RecordPrivatePostHTTPPostCall = "Private posts - HTTP Post call succeeded";

        /// <summary>
        /// Event name for private post HTTP delete call.
        /// </summary>
        private const string RecordPrivatePostHTTPDeleteCall = "Private posts - HTTP Delete call succeeded";

        /// <summary>
        /// Sends logs to the Application Insights service.
        /// </summary>
        private readonly ILogger logger;

        /// <summary>
        /// Instance of private post storage helper to update post and get information of posts.
        /// </summary>
        private readonly IUserPrivatePostStorageHelper userPrivatePostStorageHelper;

        /// <summary>
        /// Instance of user private post storage provider for private posts.
        /// </summary>
        private readonly IUserPrivatePostStorageProvider userPrivatePostStorageProvider;

        /// <summary>
        /// Instance of team post storage provider.
        /// </summary>
        private readonly ITeamPostStorageProvider teamPostStorageProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserPrivatePostController"/> class.
        /// </summary>
        /// <param name="logger">Sends logs to the Application Insights service.</param>
        /// <param name="telemetryClient">The Application Insights telemetry client.</param>
        /// <param name="userPrivatePostStorageHelper">Private post storage helper dependency injection.</param>
        /// <param name="userPrivatePostStorageProvider">User private post storage provider dependency injection.</param>
        /// <param name="teamPostStorageProvider">Storage provider for team posts.</param>
        public UserPrivatePostController(
            ILogger<UserPrivatePostController> logger,
            TelemetryClient telemetryClient,
            IUserPrivatePostStorageHelper userPrivatePostStorageHelper,
            IUserPrivatePostStorageProvider userPrivatePostStorageProvider,
            ITeamPostStorageProvider teamPostStorageProvider)
            : base(telemetryClient)
        {
            this.logger = logger;
            this.userPrivatePostStorageHelper = userPrivatePostStorageHelper;
            this.userPrivatePostStorageProvider = userPrivatePostStorageProvider;
            this.teamPostStorageProvider = teamPostStorageProvider;
        }

        /// <summary>
        /// Get call to retrieve list of private posts.
        /// </summary>
        /// <returns>List of private posts.</returns>
        [HttpGet]
        public async Task<IActionResult> GetAsync()
        {
            try
            {
                var postIds = await this.userPrivatePostStorageProvider.GetUserPrivatePostsIdsAsync(this.UserAadId);

                if (postIds == null || !postIds.Any())
                {
                    return this.Ok(null);
                }

                if (postIds.Any())
                {
                    var teamPostsData = await this.teamPostStorageProvider.GetFilteredUserPrivatePostsAsync(postIds.Take(50));
                    this.RecordEvent(RecordPrivatePostHTTPGetCall);

                    return this.Ok(teamPostsData?.OrderByDescending(post => post.CreatedDate));
                }
                else
                {
                    return null;
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error while making call to private post service.");
                throw;
            }
        }

        /// <summary>
        /// Post call to store private posts details data in Microsoft Azure Table storage.
        /// </summary>
        /// <param name="userPrivatePostEntity">Represents user private post entity object.</param>
        /// <returns>Returns true for successful operation.</returns>
        [HttpPost]
        public async Task<IActionResult> PostAsync([FromBody] UserPrivatePostEntity userPrivatePostEntity)
        {
            try
            {
                if (string.IsNullOrEmpty(userPrivatePostEntity?.PostId))
                {
                    this.logger.LogError("Error while adding post in user's private list.");
                    return this.GetErrorResponse(StatusCodes.Status400BadRequest, "Error while adding post in user's private list.");
                }

                var updatedPrivatePostEntity = this.userPrivatePostStorageHelper.GetNewUserPrivatePostModel(userPrivatePostEntity, this.UserAadId, this.UserName);
                var result = await this.userPrivatePostStorageProvider.UpsertPostAsPrivateAsync(updatedPrivatePostEntity);

                if (result)
                {
                    this.RecordEvent(RecordPrivatePostHTTPPostCall);
                }

                return this.Ok(result);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error while making call to private post service.");
                throw;
            }
        }

        /// <summary>
        /// Delete call to delete private post details data in Microsoft Azure Table storage.
        /// </summary>
        /// <param name="postId">Id of the post to be deleted.</param>
        /// <param name="userId">Azure Active Directory id of user.</param>
        /// <returns>Returns true for successful operation.</returns>
        [HttpDelete]
        public async Task<IActionResult> DeleteAsync(string postId, string userId)
        {
            try
            {
                if (string.IsNullOrEmpty(postId))
                {
                    this.logger.LogError("Error while deleting private post details data in Microsoft Azure Table storage.");
                    return this.GetErrorResponse(StatusCodes.Status400BadRequest, "Error while deleting private post details data in Microsoft Azure Table storage.");
                }

                this.RecordEvent(RecordPrivatePostHTTPDeleteCall);
                return this.Ok(await this.userPrivatePostStorageProvider.DeletePrivatePostAsync(postId, userId));
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error while making call to private post service.");
                throw;
            }
        }
    }
}