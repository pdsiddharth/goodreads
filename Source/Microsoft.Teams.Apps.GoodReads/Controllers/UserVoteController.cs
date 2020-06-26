// <copyright file="UserVoteController.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.GoodReads.Controllers
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;
    using Microsoft.Teams.Apps.GoodReads.Common.Interfaces;
    using Microsoft.Teams.Apps.GoodReads.Models;
    using Microsoft.WindowsAzure.Storage;
    using Polly;
    using Polly.Contrib.WaitAndRetry;
    using Polly.Retry;

    /// <summary>
    /// Controller to handle user vote operations.
    /// </summary>
    [ApiController]
    [Route("api/uservote")]
    [Authorize]
    public class UserVoteController : BaseGoodReadsController
    {
        /// <summary>
        /// Retry policy with jitter.
        /// </summary>
        /// <remarks>
        /// Reference: https://github.com/Polly-Contrib/Polly.Contrib.WaitAndRetry#new-jitter-recommendation.
        /// </remarks>
        private readonly AsyncRetryPolicy retryPolicy;

        /// <summary>
        /// Used to perform logging of errors and information.
        /// </summary>
        private readonly ILogger logger;

        /// <summary>
        /// Provider for working with user vote data in storage.
        /// </summary>
        private readonly IUserVoteStorageProvider userVoteStorageProvider;

        /// <summary>
        /// Provider to fetch posts from storage.
        /// </summary>
        private readonly IPostStorageProvider postStorageProvider;

        /// <summary>
        /// Search service instance for fetching posts using filters and search queries.
        /// </summary>
        private readonly IPostSearchService postSearchService;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserVoteController"/> class.
        /// </summary>
        /// <param name="logger">Used to perform logging of errors and information.</param>
        /// <param name="telemetryClient">The Application Insights telemetry client.</param>
        /// <param name="userVoteStorageProvider">Provider for working with user vote data in storage.</param>
        /// <param name="postStorageProvider">Provider to fetch posts from storage.</param>
        /// <param name="postSearchService">Search service instance for fetching posts using filters and search queries.</param>
        public UserVoteController(
            ILogger<TeamPostController> logger,
            TelemetryClient telemetryClient,
            IUserVoteStorageProvider userVoteStorageProvider,
            IPostStorageProvider postStorageProvider,
            IPostSearchService postSearchService)
            : base(telemetryClient)
        {
            this.logger = logger;
            this.userVoteStorageProvider = userVoteStorageProvider;
            this.postStorageProvider = postStorageProvider;
            this.postSearchService = postSearchService;
            this.retryPolicy = Policy.Handle<StorageException>(ex => ex.RequestInformation.HttpStatusCode == StatusCodes.Status412PreconditionFailed)
                .WaitAndRetryAsync(Backoff.LinearBackoff(TimeSpan.FromMilliseconds(1000), 3));
        }

        /// <summary>
        /// Retrieves list of votes for user.
        /// </summary>
        /// <returns>List of posts.</returns>
        [HttpGet("user-votes")]
        public async Task<IActionResult> GetVotesAsync()
        {
            try
            {
                this.logger.LogInformation("call to retrieve list of votes for user.");

                var userVotes = await this.userVoteStorageProvider.GetUserVotesAsync(this.UserAadId);
                this.RecordEvent("User votes - HTTP Get call succeeded.");

                return this.Ok(userVotes);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error while making call to post service.");
                throw;
            }
        }

        /// <summary>
        /// Stores user vote for a post.
        /// </summary>
        /// <param name="postCreatedByUserId">AAD user Id of user who created post.</param>
        /// <param name="postId">Id of the post to delete vote.</param>
        /// <returns>Returns true for successful operation.</returns>
        [HttpGet("vote")]
        public async Task<IActionResult> AddVoteAsync(string postCreatedByUserId, string postId)
        {
            this.logger.LogInformation("call to add user vote.");

#pragma warning disable CA1062 // post details are validated by model validations for null check and is responded with bad request status
            var userVoteForPost = await this.userVoteStorageProvider.GetUserVoteForPostAsync(this.UserAadId, postId);
#pragma warning restore CA1062 // post details are validated by model validations for null check and is responded with bad request status

            if (userVoteForPost == null)
            {
                UserVoteEntity userVote = new UserVoteEntity
                {
                    UserId = this.UserAadId,
                    PostId = postId,
                };

                PostEntity postEntity = null;
                bool isPostSavedSuccessful = false;

                // Retry if storage operation conflict occurs during updating user vote count.
                await this.retryPolicy.ExecuteAsync(async () =>
                {
                    try
                    {
                        postEntity = await this.postStorageProvider.GetPostAsync(postCreatedByUserId, userVote.PostId);

                        // increment the vote count
                        // if the execution is retried, then get the latest vote count and increase it by 1
                        postEntity.TotalVotes += 1;

                        isPostSavedSuccessful = await this.postStorageProvider.UpsertPostAsync(postEntity);
                    }
                    catch (StorageException ex)
                    {
                        if (ex.RequestInformation.HttpStatusCode == StatusCodes.Status412PreconditionFailed)
                        {
                            this.logger.LogError("Optimistic concurrency violation – entity has changed since it was retrieved.");
                            throw;
                        }
                    }
#pragma warning disable CA1031 // catching generic exception to trace log error in telemetry and continue the execution
                    catch (Exception ex)
#pragma warning restore CA1031 // catching generic exception to trace log error in telemetry and continue the execution
                    {
                        // log exception details to telemetry
                        // but do not attempt to retry in order to avoid multiple vote count increment
                        this.logger.LogError(ex, "Exception occurred while reading post details.");
                    }
                });

                if (!isPostSavedSuccessful)
                {
                    this.logger.LogError($"Vote is not updated successfully for post {postId} by {this.UserAadId} ");
                    return this.StatusCode(StatusCodes.Status500InternalServerError, "Vote is not updated successfully.");
                }

                bool isUserVoteSavedSuccessful = false;

                this.logger.LogInformation($"Post vote count updated for PostId:{postId}");
                isUserVoteSavedSuccessful = await this.userVoteStorageProvider.UpsertUserVoteAsync(userVote);

                // if user vote is not saved successfully
                // revert back the total post count
                if (!isUserVoteSavedSuccessful)
                {
                    await this.retryPolicy.ExecuteAsync(async () =>
                    {
                        try
                        {
                            postEntity = await this.postStorageProvider.GetPostAsync(postCreatedByUserId, userVote.PostId);
                            postEntity.TotalVotes -= 1;

                            // Update operation will throw exception if the column has already been updated
                            // or if there is a transient error (handled by an Azure storage)
                            await this.postStorageProvider.UpsertPostAsync(postEntity);
                            await this.postSearchService.RunIndexerOnDemandAsync();
                        }
                        catch (StorageException ex)
                        {
                            if (ex.RequestInformation.HttpStatusCode == StatusCodes.Status412PreconditionFailed)
                            {
                                this.logger.LogError("Optimistic concurrency violation – entity has changed since it was retrieved.");
                                throw;
                            }
                        }
#pragma warning disable CA1031 // catching generic exception to trace log error in telemetry and continue the execution
                        catch (Exception ex)
#pragma warning restore CA1031 // catching generic exception to trace log error in telemetry and continue the execution
                        {
                            // log exception details to telemetry
                            // but do not attempt to retry in order to avoid multiple vote count decrement
                            this.logger.LogError(ex, "Exception occurred while reading post details.");
                        }
                    });
                }
                else
                {
                    this.logger.LogInformation($"User vote added for user{this.UserAadId} for PostId:{postId}");
                    await this.postSearchService.RunIndexerOnDemandAsync();
                    return this.Ok(true);
                }
            }

            return this.Ok(false);
        }

        /// <summary>
        /// Deletes user vote for a post.
        /// </summary>
        /// <param name="postCreatedByUserId">AAD user Id of user who created post.</param>
        /// <param name="postId">Id of the post to delete vote.</param>
        /// <returns>Returns true for successful operation.</returns>
        [HttpDelete]
        public async Task<IActionResult> DeleteVoteAsync(string postCreatedByUserId, string postId)
        {
            this.logger.LogInformation("call to delete user vote.");

            if (string.IsNullOrEmpty(postCreatedByUserId))
            {
                this.logger.LogError("Error while deleting vote. Parameter postCreatedByuserId is either null or empty.");
                return this.GetErrorResponse(StatusCodes.Status400BadRequest, "Error while adding vote. Parameter postCreatedByuserId is either null or empty.");
            }

            if (string.IsNullOrEmpty(postId))
            {
                this.logger.LogError("Error while deleting vote. PostId is either null or empty.");
                return this.GetErrorResponse(StatusCodes.Status400BadRequest, "Error while adding vote. PostId is either null or empty.");
            }

#pragma warning disable CA1062 // post details are validated by model validations for null check and is responded with bad request status
            var userVoteForPost = await this.userVoteStorageProvider.GetUserVoteForPostAsync(this.UserAadId, postId);
#pragma warning restore CA1062 // post details are validated by model validations for null check and is responded with bad request status

            if (userVoteForPost != null)
            {
                PostEntity postEntity = null;
                bool isPostSavedSuccessful = false;

                // Retry if storage operation conflict occurs during updating user vote count.
                await this.retryPolicy.ExecuteAsync(async () =>
                {
                    try
                    {
                        postEntity = await this.postStorageProvider.GetPostAsync(postCreatedByUserId, postId);

                        // increment the vote count
                        // if the execution is retried, then get the latest vote count and increase it by 1
                        postEntity.TotalVotes -= 1;

                        isPostSavedSuccessful = await this.postStorageProvider.UpsertPostAsync(postEntity);
                    }
                    catch (StorageException ex)
                    {
                        if (ex.RequestInformation.HttpStatusCode == StatusCodes.Status412PreconditionFailed)
                        {
                            this.logger.LogError("Optimistic concurrency violation – entity has changed since it was retrieved.");
                            throw;
                        }
                    }
#pragma warning disable CA1031 // catching generic exception to trace log error in telemetry and continue the execution
                    catch (Exception ex)
#pragma warning restore CA1031 // catching generic exception to trace log error in telemetry and continue the execution
                    {
                        // log exception details to telemetry
                        // but do not attempt to retry in order to avoid multiple vote count increment
                        this.logger.LogError(ex, "Exception occurred while reading post details.");
                    }
                });

                if (!isPostSavedSuccessful)
                {
                    this.logger.LogError($"Vote is not updated successfully for post {postId} by {postCreatedByUserId} ");
                    return this.StatusCode(StatusCodes.Status500InternalServerError, "Vote is not updated successfully.");
                }

                bool isUserVotDeletedSuccessful = false;

                this.logger.LogInformation($"Post vote count updated for PostId:{postId}");
                isUserVotDeletedSuccessful = await this.userVoteStorageProvider.DeleteUserVoteAsync(postId, postCreatedByUserId);

                // if user vote is not saved successfully
                // revert back the total post count
                if (!isUserVotDeletedSuccessful)
                {
                    await this.retryPolicy.ExecuteAsync(async () =>
                    {
                        try
                        {
                            postEntity = await this.postStorageProvider.GetPostAsync(postCreatedByUserId, postId);
                            postEntity.TotalVotes += 1;

                            // Update operation will throw exception if the column has already been updated
                            // or if there is a transient error (handled by an Azure storage)
                            await this.postStorageProvider.UpsertPostAsync(postEntity);
                            await this.postSearchService.RunIndexerOnDemandAsync();
                        }
                        catch (StorageException ex)
                        {
                            if (ex.RequestInformation.HttpStatusCode == StatusCodes.Status412PreconditionFailed)
                            {
                                this.logger.LogError("Optimistic concurrency violation – entity has changed since it was retrieved.");
                                throw;
                            }
                        }
#pragma warning disable CA1031 // catching generic exception to trace log error in telemetry and continue the execution
                        catch (Exception ex)
#pragma warning restore CA1031 // catching generic exception to trace log error in telemetry and continue the execution
                        {
                            // log exception details to telemetry
                            // but do not attempt to retry in order to avoid multiple vote count decrement
                            this.logger.LogError(ex, "Exception occurred while reading post details.");
                        }
                    });
                }
                else
                {
                    this.logger.LogInformation($"User vote deleted for user{this.UserAadId} for PostId:{postId}");
                    await this.postSearchService.RunIndexerOnDemandAsync();
                    return this.Ok(true);
                }
            }

            return this.Ok(false);
        }
    }
}