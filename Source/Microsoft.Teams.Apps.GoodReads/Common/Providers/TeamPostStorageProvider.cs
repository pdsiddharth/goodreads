// <copyright file="TeamPostStorageProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.GoodReads.Common.Providers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Microsoft.Teams.Apps.GoodReads.Common.Interfaces;
    using Microsoft.Teams.Apps.GoodReads.Models;
    using Microsoft.Teams.Apps.GoodReads.Models.Configuration;
    using Microsoft.WindowsAzure.Storage.Table;

    /// <summary>
    /// Implements storage provider which helps to create, get, update or delete team posts data in Microsoft Azure Table storage.
    /// </summary>
    public class TeamPostStorageProvider : BaseStorageProvider, ITeamPostStorageProvider
    {
        /// <summary>
        /// Represents team post entity name.
        /// </summary>
        private const string TeamPostEntityName = "TeamPostEntity";

        /// <summary>
        /// Represent a column name.
        /// </summary>
        private const string IsRemovedColumnName = "IsRemoved";

        /// <summary>
        /// Initializes a new instance of the <see cref="TeamPostStorageProvider"/> class.
        /// Handles Microsoft Azure Table storage read write operations.
        /// </summary>
        /// <param name="options">A set of key/value application configuration properties for Microsoft Azure Table storage.</param>
        /// <param name="logger">Sends logs to the Application Insights service.</param>
        public TeamPostStorageProvider(
            IOptions<StorageSetting> options,
            ILogger<BaseStorageProvider> logger)
            : base(options?.Value.ConnectionString, TeamPostEntityName, logger)
        {
            options = options ?? throw new ArgumentNullException(nameof(options));
        }

        /// <summary>
        /// Get team posts data from Microsoft Azure Table storage.
        /// </summary>
        /// <param name="isRemoved">Represent a team post is deleted or not.</param>
        /// <returns>A task that represent collection to hold team posts ids.</returns>
        public async Task<IEnumerable<string>> GetTeamPostsIdsAsync(bool isRemoved)
        {
            await this.EnsureInitializedAsync();

            string teamPostCondition = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, TeamPostEntityName);
            string isRemovedCondition = TableQuery.GenerateFilterConditionForBool(IsRemovedColumnName, QueryComparisons.Equal, isRemoved);
            var combinedFilter = TableQuery.CombineFilters(teamPostCondition, TableOperators.And, isRemovedCondition);

            TableQuery<TeamPostEntity> query = new TableQuery<TeamPostEntity>().Where(combinedFilter);
            TableContinuationToken continuationToken = null;
            var teamPostCollection = new List<TeamPostEntity>();

            do
            {
                var queryResult = await this.GoodReadsCloudTable.ExecuteQuerySegmentedAsync(query, continuationToken);
                if (queryResult?.Results != null)
                {
                    teamPostCollection.AddRange(queryResult.Results);
                    continuationToken = queryResult.ContinuationToken;
                }
            }
            while (continuationToken != null);

            return teamPostCollection.Select(teamPost => teamPost.PostId);
        }

        /// <summary>
        /// Get team post data from Microsoft Azure Table storage.
        /// </summary>
        /// <param name="postId">Post id to fetch the post details.</param>
        /// <returns>A task that represent a object to hold team post data.</returns>
        public async Task<TeamPostEntity> GetTeamPostEntityAsync(string postId)
        {
            // When there is no team post created by user and Messaging Extension is open, table initialization is required here before creating search index or data source or indexer.
            await this.EnsureInitializedAsync();

            if (string.IsNullOrEmpty(postId))
            {
                return null;
            }

            string partitionKeyCondition = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, TeamPostEntityName);
            string postIdCondition = TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, postId);
            var combinedPartitionFilter = TableQuery.CombineFilters(partitionKeyCondition, TableOperators.And, postIdCondition);

            string isRemovedCondition = TableQuery.GenerateFilterConditionForBool(IsRemovedColumnName, QueryComparisons.Equal, false);
            var combinedFilter = TableQuery.CombineFilters(combinedPartitionFilter, TableOperators.And, isRemovedCondition);

            TableQuery<TeamPostEntity> query = new TableQuery<TeamPostEntity>().Where(combinedFilter);
            var queryResult = await this.GoodReadsCloudTable.ExecuteQuerySegmentedAsync(query, null);

            return queryResult?.FirstOrDefault();
        }

        /// <summary>
        /// Stores or update team post details data in Microsoft Azure Table storage.
        /// </summary>
        /// <param name="teamPostEntity">Holds team post detail entity data.</param>
        /// <returns>A boolean that represents team post entity data is successfully saved/updated or not.</returns>
        public async Task<bool> UpsertTeamPostAsync(TeamPostEntity teamPostEntity)
        {
            var result = await this.StoreOrUpdateEntityAsync(teamPostEntity);
            return result.HttpStatusCode == (int)HttpStatusCode.NoContent;
        }

        /// <summary>
        /// Get team posts as per the user's private list of post from Microsoft Azure Table storage.
        /// </summary>
        /// <param name="postIds">A collection of user private post id's.</param>
        /// <returns>A task that represent collection to hold team posts data.</returns>
        public async Task<IEnumerable<TeamPostEntity>> GetFilteredUserPrivatePostsAsync(IEnumerable<string> postIds)
        {
            postIds = postIds ?? throw new ArgumentNullException(nameof(postIds));
            await this.EnsureInitializedAsync();

            string teamPostCondition = this.CreateUserPrivatePostsFilter(postIds);
            string isRemovedCondition = TableQuery.GenerateFilterConditionForBool(IsRemovedColumnName, QueryComparisons.Equal, false);
            var combinedFilter = TableQuery.CombineFilters(teamPostCondition, TableOperators.And, isRemovedCondition);

            TableQuery<TeamPostEntity> query = new TableQuery<TeamPostEntity>().Where(combinedFilter);
            TableContinuationToken continuationToken = null;
            var teamPostCollection = new List<TeamPostEntity>();

            do
            {
                var queryResult = await this.GoodReadsCloudTable.ExecuteQuerySegmentedAsync(query, continuationToken);

                if (queryResult?.Results != null)
                {
                    teamPostCollection.AddRange(queryResult.Results);
                    continuationToken = queryResult.ContinuationToken;
                }
            }
            while (continuationToken != null);

            return teamPostCollection;
        }

        /// <summary>
        /// Delete team posts which is soft deleted and isRemoved = true in Microsoft Azure Table storage.
        /// </summary>
        /// <param name="teamPostsIds">A collection of team post ids.</param>
        /// <returns>A boolean that represents team posts are deleted successfully or not.</returns>
        public async Task<bool> DeleteTeamPostEntitiesAsync(IEnumerable<string> teamPostsIds)
        {
            teamPostsIds = teamPostsIds ?? throw new ArgumentNullException(nameof(teamPostsIds));
            await this.EnsureInitializedAsync();

            foreach (var postId in teamPostsIds)
            {
                var partitionKeyCondition = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, TeamPostEntityName);
                string postIdCondition = TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, postId);
                var combinedFilter = TableQuery.CombineFilters(partitionKeyCondition, TableOperators.And, postIdCondition);

                TableQuery<TeamPostEntity> query = new TableQuery<TeamPostEntity>().Where(combinedFilter);
                var queryResult = await this.GoodReadsCloudTable.ExecuteQuerySegmentedAsync(query, null);
                TableOperation deleteOperation = TableOperation.Delete(queryResult?.FirstOrDefault());
                await this.GoodReadsCloudTable.ExecuteAsync(deleteOperation);
            }

            return true;
        }

        /// <summary>
        /// Get combined filter condition for user private posts data.
        /// </summary>
        /// <param name="postIds">List of user private posts id.</param>
        /// <returns>Returns combined filter for user private posts.</returns>
        private string CreateUserPrivatePostsFilter(IEnumerable<string> postIds)
        {
            var postIdConditions = new List<string>();
            StringBuilder combinedPostIdFilter = new StringBuilder();

            postIds = postIds.Where(s => !string.IsNullOrWhiteSpace(s)).Distinct();

            foreach (var postId in postIds)
            {
                postIdConditions.Add("(" + TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, postId) + ")");
            }

            if (postIdConditions.Count >= 2)
            {
                var posts = postIdConditions.Take(postIdConditions.Count - 1).ToList();

                posts.ForEach(postCondition =>
                {
                    combinedPostIdFilter.Append($"{postCondition} {"or"} ");
                });

                combinedPostIdFilter.Append($"{postIdConditions.Last()}");

                return combinedPostIdFilter.ToString();
            }
            else
            {
                return TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, postIds.FirstOrDefault());
            }
        }

        /// <summary>
        /// Stores or update team post details data in Microsoft Azure Table storage.
        /// </summary>
        /// <param name="entity">Holds team post detail entity data.</param>
        /// <returns>A task that represents team post entity data is saved or updated.</returns>
        private async Task<TableResult> StoreOrUpdateEntityAsync(TeamPostEntity entity)
        {
            await this.EnsureInitializedAsync();
            TableOperation addOrUpdateOperation = TableOperation.InsertOrReplace(entity);
            return await this.GoodReadsCloudTable.ExecuteAsync(addOrUpdateOperation);
        }
    }
}
