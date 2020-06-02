// <copyright file="UserPrivatePostStorageProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.GoodReads.Common.Providers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Microsoft.Teams.Apps.GoodReads.Common.Interfaces;
    using Microsoft.Teams.Apps.GoodReads.Models;
    using Microsoft.Teams.Apps.GoodReads.Models.Configuration;
    using Microsoft.WindowsAzure.Storage.Table;

    /// <summary>
    /// Implements storage provider which stores team post data in user's private list in Microsoft Azure Table storage.
    /// </summary>
    public class UserPrivatePostStorageProvider : BaseStorageProvider, IUserPrivatePostStorageProvider
    {
        /// <summary>
        /// Represents user's private post entity name.
        /// </summary>
        private const string UserPrivatePostEntityName = "UserPrivatePostEntity";

        /// <summary>
        /// Represents row key string.
        /// </summary>
        private const string RowKey = "RowKey";

        /// <summary>
        /// Initializes a new instance of the <see cref="UserPrivatePostStorageProvider"/> class.
        /// Handles Microsoft Azure Table storage read write operations.
        /// </summary>
        /// <param name="options">A set of key/value application configuration properties for Microsoft Azure Table storage.</param>
        /// <param name="logger">Sends logs to the Application Insights service.</param>
        public UserPrivatePostStorageProvider(
            IOptions<StorageSetting> options,
            ILogger<BaseStorageProvider> logger)
            : base(options?.Value.ConnectionString, UserPrivatePostEntityName, logger)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }
        }

        /// <summary>
        /// Get user's private list of posts data from Microsoft Azure Table storage.
        /// </summary>
        /// <param name="userId">User id for which need to fetch data.</param>
        /// <returns>A task that represent collection to hold user's private list of posts data.</returns>
        public async Task<IEnumerable<string>> GetUserPrivatePostsIdsAsync(string userId)
        {
            userId = userId ?? throw new ArgumentNullException(nameof(userId));
            await this.EnsureInitializedAsync();

            var partitionKeyCondition = TableQuery.GenerateFilterCondition(Constants.PartitionKey, QueryComparisons.Equal, userId);
            var userIdCondition = TableQuery.GenerateFilterCondition(nameof(UserPrivatePostEntity.UserId), QueryComparisons.Equal, userId);
            var combinedTeamFilter = TableQuery.CombineFilters(partitionKeyCondition, TableOperators.And, userIdCondition);

            TableQuery<UserPrivatePostEntity> query = new TableQuery<UserPrivatePostEntity>().Where(combinedTeamFilter);
            TableContinuationToken continuationToken = null;
            var userPrivatePostCollection = new List<UserPrivatePostEntity>();

            do
            {
                var queryResult = await this.GoodReadsCloudTable.ExecuteQuerySegmentedAsync(query, continuationToken);
                if (queryResult?.Results != null)
                {
                    userPrivatePostCollection.AddRange(queryResult.Results);
                    continuationToken = queryResult.ContinuationToken;
                }
            }
            while (continuationToken != null);

            return userPrivatePostCollection.OrderByDescending(post => post.CreatedDate).Select(privatePost => privatePost.PostId);
        }

        /// <summary>
        /// Delete private post from user's private list in Microsoft Azure Table storage.
        /// </summary>
        /// <param name="postId">Holds private post id.</param>
        /// <param name="userId">Azure Active Directory id of user.</param>
        /// <returns>A task that represents private post is deleted.</returns>
        public async Task<bool> DeletePrivatePostAsync(string postId, string userId)
        {
            postId = postId ?? throw new ArgumentNullException(nameof(postId));
            await this.EnsureInitializedAsync();

            string partitionKeyCondition = TableQuery.GenerateFilterCondition(Constants.PartitionKey, QueryComparisons.Equal, userId);
            string rowKeyCondition = TableQuery.GenerateFilterCondition(RowKey, QueryComparisons.Equal, postId);
            var combinedFilter = TableQuery.CombineFilters(partitionKeyCondition, TableOperators.And, rowKeyCondition);

            TableQuery<UserPrivatePostEntity> query = new TableQuery<UserPrivatePostEntity>().Where(combinedFilter);
            var queryResult = await this.GoodReadsCloudTable.ExecuteQuerySegmentedAsync(query, null);
            if (queryResult?.Count() > 0)
            {
                TableOperation deleteOperation = TableOperation.Delete(queryResult?.FirstOrDefault());
                await this.GoodReadsCloudTable.ExecuteAsync(deleteOperation);
            }

            return true;
        }

        /// <summary>
        /// Stores or update post data in user's private list in Microsoft Azure Table storage.
        /// </summary>
        /// <param name="entity">Holds user post detail.</param>
        /// <returns>A task that represents user private post is saved or updated.</returns>
        public async Task<bool> UpsertPostAsPrivateAsync(UserPrivatePostEntity entity)
        {
            var result = await this.StoreOrUpdateEntityAsync(entity);
            return result.HttpStatusCode == (int)HttpStatusCode.NoContent;
        }

        /// <summary>
        /// Stores or update post data in user's private list in Microsoft Azure Table storage.
        /// </summary>
        /// <param name="entity">Represents user private post entity object.</param>
        /// <returns>A task that represents user private post is saved or updated.</returns>
        private async Task<TableResult> StoreOrUpdateEntityAsync(UserPrivatePostEntity entity)
        {
            await this.EnsureInitializedAsync();
            TableOperation addOrUpdateOperation = TableOperation.InsertOrReplace(entity);
            return await this.GoodReadsCloudTable.ExecuteAsync(addOrUpdateOperation);
        }
    }
}
