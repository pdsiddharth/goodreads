﻿// <copyright file="UserVoteStorageProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.GoodReads.Common.Providers
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Microsoft.Teams.Apps.GoodReads.Common.Interfaces;
    using Microsoft.Teams.Apps.GoodReads.Models;
    using Microsoft.Teams.Apps.GoodReads.Models.Configuration;
    using Microsoft.WindowsAzure.Storage.Table;

    /// <summary>
    /// Implements storage provider which stores user vote data in Microsoft Azure Table storage.
    /// </summary>
    public class UserVoteStorageProvider : BaseStorageProvider, IUserVoteStorageProvider
    {
        /// <summary>
        /// Represents user vote entity name.
        /// </summary>
        private const string UserVoteEntityName = "UserVoteEntity";

        /// <summary>
        /// Represents row key string.
        /// </summary>
        private const string RowKey = "RowKey";

        /// <summary>
        /// Initializes a new instance of the <see cref="UserVoteStorageProvider"/> class.
        /// Handles Microsoft Azure Table storage read write operations.
        /// </summary>
        /// <param name="options">A set of key/value application configuration properties for Microsoft Azure Table storage.</param>
        /// <param name="logger">Sends logs to the Application Insights service.</param>
        public UserVoteStorageProvider(
            IOptions<StorageSetting> options,
            ILogger<BaseStorageProvider> logger)
            : base(options?.Value.ConnectionString, UserVoteEntityName, logger)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }
        }

        /// <summary>
        /// Get all user votes from Microsoft Azure Table storage.
        /// </summary>
        /// <param name="userId">Represent Azure Active Directory id of user.</param>
        /// <returns>A task that represents a collection of user votes.</returns>
        public async Task<List<UserVoteEntity>> GetVotesAsync(string userId)
        {
            await this.EnsureInitializedAsync();

            string partitionKeyCondition = TableQuery.GenerateFilterCondition(Constants.PartitionKey, QueryComparisons.Equal, userId);
            string userIdCondition = TableQuery.GenerateFilterCondition(nameof(UserVoteEntity.UserId), QueryComparisons.Equal, userId);
            string combinedFilter = TableQuery.CombineFilters(partitionKeyCondition, TableOperators.And, userIdCondition);

            List<UserVoteEntity> userVotes = new List<UserVoteEntity>();
            TableContinuationToken continuationToken = null;
            TableQuery<UserVoteEntity> query = new TableQuery<UserVoteEntity>().Where(combinedFilter);

            do
            {
                var queryResult = await this.GoodReadsCloudTable.ExecuteQuerySegmentedAsync(query, null);
                if (queryResult?.Results != null)
                {
                    userVotes.AddRange(queryResult.Results);
                    continuationToken = queryResult.ContinuationToken;
                }
            }
            while (continuationToken != null);

            return userVotes;
        }

        /// <summary>
        /// Stores or update user votes data in Microsoft Azure Table storage.
        /// </summary>
        /// <param name="voteEntity">Holds user vote entity data.</param>
        /// <returns>A task that represents user vote entity data is saved or updated.</returns>
        public async Task<bool> UpsertUserVoteAsync(UserVoteEntity voteEntity)
        {
            var result = await this.StoreOrUpdateEntityAsync(voteEntity);
            return result.HttpStatusCode == (int)HttpStatusCode.NoContent;
        }

        /// <summary>
        /// Delete user vote data from Microsoft Azure Table storage.
        /// </summary>
        /// <param name="postId">Represents post id.</param>
        /// <param name="userId">Represent Azure Active Directory id of user.</param>
        /// <returns>A task that represents user vote data is deleted.</returns>
        public async Task<bool> DeleteEntityAsync(string postId, string userId)
        {
            postId = postId ?? throw new ArgumentNullException(nameof(postId));
            await this.EnsureInitializedAsync();

            string postIdCondition = TableQuery.GenerateFilterCondition(RowKey, QueryComparisons.Equal, postId);
            string userIdCondition = TableQuery.GenerateFilterCondition(Constants.PartitionKey, QueryComparisons.Equal, userId);
            string combinedFilter = TableQuery.CombineFilters(postIdCondition, TableOperators.And, userIdCondition);

            TableQuery<UserVoteEntity> query = new TableQuery<UserVoteEntity>().Where(combinedFilter);
            var queryResult = await this.GoodReadsCloudTable.ExecuteQuerySegmentedAsync(query, null);
            TableOperation deleteOperation = TableOperation.Delete(queryResult?.Results[0]);
            await this.GoodReadsCloudTable.ExecuteAsync(deleteOperation);

            return true;
        }

        /// <summary>
        /// Stores or update user votes data in Microsoft Azure Table storage.
        /// </summary>
        /// <param name="voteEntity">Holds user vote entity data.</param>
        /// <returns>A task that represents user vote entity data is saved or updated.</returns>
        private async Task<TableResult> StoreOrUpdateEntityAsync(UserVoteEntity voteEntity)
        {
            await this.EnsureInitializedAsync();
            TableOperation addOrUpdateOperation = TableOperation.InsertOrReplace(voteEntity);
            return await this.GoodReadsCloudTable.ExecuteAsync(addOrUpdateOperation);
        }
    }
}