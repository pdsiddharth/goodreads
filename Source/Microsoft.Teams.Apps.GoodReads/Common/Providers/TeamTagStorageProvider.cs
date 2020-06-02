﻿// <copyright file="TeamTagStorageProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.GoodReads.Common.Providers
{
    using System;
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
    /// Implements storage provider which stores team tags data in Microsoft Azure Table storage.
    /// </summary>
    public class TeamTagStorageProvider : BaseStorageProvider, ITeamTagStorageProvider
    {
        /// <summary>
        /// Represents team tag entity name.
        /// </summary>
        private const string TeamTagEntityName = "TeamTagEntity";

        /// <summary>
        /// Represents row key string.
        /// </summary>
        private const string RowKey = "RowKey";

        /// <summary>
        /// Initializes a new instance of the <see cref="TeamTagStorageProvider"/> class.
        /// Handles Microsoft Azure Table storage read write operations.
        /// </summary>
        /// <param name="options">A set of key/value application configuration properties for Microsoft Azure Table storage.</param>
        /// <param name="logger">Sends logs to the Application Insights service.</param>
        public TeamTagStorageProvider(
            IOptions<StorageSetting> options,
            ILogger<BaseStorageProvider> logger)
            : base(options?.Value.ConnectionString, TeamTagEntityName, logger)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }
        }

        /// <summary>
        /// Get team tags data from Microsoft Azure Table storage.
        /// </summary>
        /// <param name="teamId">Team id for which need to fetch data.</param>
        /// <returns>A task that represents an object to hold team tags data.</returns>
        public async Task<TeamTagEntity> GetTeamTagsDataAsync(string teamId)
        {
            teamId = teamId ?? throw new ArgumentNullException(nameof(teamId));
            await this.EnsureInitializedAsync();

            string partitionKeyCondition = TableQuery.GenerateFilterCondition(Constants.PartitionKey, QueryComparisons.Equal, TeamTagEntityName);
            string teamIdCondition = TableQuery.GenerateFilterCondition(RowKey, QueryComparisons.Equal, teamId);
            var combinedTeamFilter = TableQuery.CombineFilters(partitionKeyCondition, TableOperators.And, teamIdCondition);

            TableQuery<TeamTagEntity> query = new TableQuery<TeamTagEntity>().Where(combinedTeamFilter);
            var queryResult = await this.GoodReadsCloudTable.ExecuteQuerySegmentedAsync(query, null);

            return queryResult?.Results.FirstOrDefault();
        }

        /// <summary>
        /// Delete configured tags for a team if Bot is uninstalled from Microsoft Azure Table storage.
        /// </summary>
        /// <param name="teamId">Holds team id.</param>
        /// <returns>A task that represents team tags data is deleted.</returns>
        public async Task<bool> DeleteTeamTagsEntryDataAsync(string teamId)
        {
            teamId = teamId ?? throw new ArgumentNullException(nameof(teamId));
            await this.EnsureInitializedAsync();

            string partitionKeyCondition = TableQuery.GenerateFilterCondition(Constants.PartitionKey, QueryComparisons.Equal, TeamTagEntityName);
            string teamIdCondition = TableQuery.GenerateFilterCondition(RowKey, QueryComparisons.Equal, teamId);
            var combinedTeamFilter = TableQuery.CombineFilters(partitionKeyCondition, TableOperators.And, teamIdCondition);

            TableQuery<TeamTagEntity> query = new TableQuery<TeamTagEntity>().Where(combinedTeamFilter);
            var queryResult = await this.GoodReadsCloudTable.ExecuteQuerySegmentedAsync(query, null);
            TableOperation deleteOperation = TableOperation.Delete(queryResult?.FirstOrDefault());
            await this.GoodReadsCloudTable.ExecuteAsync(deleteOperation);

            return true;
        }

        /// <summary>
        /// Stores or update team tags data in Microsoft Azure Table storage.
        /// </summary>
        /// <param name="teamTagEntity">Represents team tag entity object.</param>
        /// <returns>A task that represents team tags entity data is saved or updated.</returns>
        public async Task<bool> UpsertTeamTagsAsync(TeamTagEntity teamTagEntity)
        {
            var result = await this.StoreOrUpdateEntityAsync(teamTagEntity);
            return result.HttpStatusCode == (int)HttpStatusCode.NoContent;
        }

        /// <summary>
        /// Stores or update team tags data in Microsoft Azure Table storage.
        /// </summary>
        /// <param name="teamTagEntity">Represents team tag entity object.</param>
        /// <returns>A task that represents team tags entity data is saved or updated.</returns>
        private async Task<TableResult> StoreOrUpdateEntityAsync(TeamTagEntity teamTagEntity)
        {
            await this.EnsureInitializedAsync();
            TableOperation addOrUpdateOperation = TableOperation.InsertOrReplace(teamTagEntity);
            return await this.GoodReadsCloudTable.ExecuteAsync(addOrUpdateOperation);
        }
    }
}