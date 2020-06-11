// <copyright file="UserTeamMembershipProvider.cs" company="Microsoft">
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
    /// Implements storage provider which helps to create, get, update or delete user team membership data in Microsoft Azure Table storage.
    /// </summary>
    public class UserTeamMembershipProvider : BaseStorageProvider, IUserTeamMembershipProvider
    {
        /// <summary>
        /// Represents user vote entity name.
        /// </summary>
        private const string UserTeamMembershipEntityName = "UserTeamMembershipEntity";

        /// <summary>
        /// Represents team id property name for an entity.
        /// </summary>
        private const string TeamIdPropertyName = "TeamId";

        /// <summary>
        /// Initializes a new instance of the <see cref="UserTeamMembershipProvider"/> class.
        /// Handles Microsoft Azure Table storage read write operations.
        /// </summary>
        /// <param name="options">A set of key/value application configuration properties for Microsoft Azure Table storage.</param>
        /// <param name="logger">Sends logs to the Application Insights service.</param>
        public UserTeamMembershipProvider(
            IOptions<StorageSetting> options,
            ILogger<BaseStorageProvider> logger)
            : base(options?.Value.ConnectionString, UserTeamMembershipEntityName, logger)
        {
            options = options ?? throw new ArgumentNullException(nameof(options));
        }

        /// <summary>
        /// Adds a user team membership entity in DB.
        /// </summary>
        /// <param name="teamId">The team id.</param>
        /// <param name="userAadObjectId">Azure Active Directory id of the user.</param>
        /// <param name="serviceUri">Team service Uri.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task AddUserTeamMembershipAsync(string teamId, string userAadObjectId, Uri serviceUri)
        {
            var teamIdCondition = TableQuery.GenerateFilterCondition(nameof(UserTeamMembershipEntity.TeamId), QueryComparisons.Equal, teamId);
            var userIdCondition = TableQuery.GenerateFilterCondition(nameof(UserTeamMembershipEntity.UserAadObjectId), QueryComparisons.Equal, userAadObjectId);
            var combinedFilter = TableQuery.CombineFilters(teamIdCondition, TableOperators.And, userIdCondition);

            var userTeamMembershipEntities = await this.GetWithFilterAsync(combinedFilter);

            if (userTeamMembershipEntities == null || !userTeamMembershipEntities.Any())
            {
                var userTeamMembershipEntity = new UserTeamMembershipEntity
                {
                    TeamId = teamId,
                    UserAadObjectId = userAadObjectId,
                    ServiceUrl = serviceUri?.ToString(),
                };

                await this.UpserUsertTeamMembershipPostAsync(userTeamMembershipEntity);
            }
        }

        /// <summary>
        /// Deletes all memberships belonging to a team.
        /// </summary>
        /// <param name="teamId">The team id.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task DeleteUserTeamMembershipByTeamIdAsync(string teamId)
        {
            var partitionKeyFilter = this.GetPartitionKeyFilter();

            var teamIdCondition = TableQuery.GenerateFilterCondition(
                nameof(UserTeamMembershipEntity.TeamId),
                QueryComparisons.Equal,
                teamId);

            string combinedFilter = TableQuery.CombineFilters(partitionKeyFilter, TableOperators.And, teamIdCondition);
            var userTeamMembershipEntities = await this.GetWithFilterAsync(combinedFilter);

            if (userTeamMembershipEntities != null)
            {
                foreach (var userTeamMembershipEntity in userTeamMembershipEntities)
                {
                    await this.DeleteEntityAsync(userTeamMembershipEntity);
                }
            }
        }

        /// <summary>
        /// Gets all memberships by a user's Active Directory id of user and team id.
        /// </summary>
        /// <param name="teamId">The team id.</param>
        /// <param name="userAadObjectId">Azure Active Directory id of the user.</param>
        /// <returns>The memberships meet the search criteria.</returns>
        public async Task<IEnumerable<UserTeamMembershipEntity>> GetUserTeamMembershipByUserAadObjectIdAsync(string teamId, string userAadObjectId)
        {
            var teamIdCondition = TableQuery.GenerateFilterCondition(
                nameof(UserTeamMembershipEntity.TeamId),
                QueryComparisons.Equal,
                teamId);

            var userIdCondition = TableQuery.GenerateFilterCondition(
                nameof(UserTeamMembershipEntity.UserAadObjectId),
                QueryComparisons.Equal,
                userAadObjectId);

            var combinedTeamFilter = TableQuery.CombineFilters(teamIdCondition, TableOperators.And, userIdCondition);
            var partitionKeyFilter = this.GetPartitionKeyFilter();
            var combinedFilter = TableQuery.CombineFilters(partitionKeyFilter, TableOperators.And, combinedTeamFilter);

            var userTeamMembershipEntities = await this.GetWithFilterAsync(combinedFilter);

            return userTeamMembershipEntities;
        }

        /// <summary>
        /// Get user team membership data from Microsoft Azure Table storage.
        /// </summary>
        /// <param name="teamId">Team id for which need to fetch data.</param>
        /// <returns>A task that represents an object to hold team preference data.</returns>
        public async Task<UserTeamMembershipEntity> GetUserTeamMembershipDataAsync(string teamId)
        {
            teamId = teamId ?? throw new ArgumentNullException(nameof(teamId));
            await this.EnsureInitializedAsync();

            var partitionKeyFilter = this.GetPartitionKeyFilter();

            string teamIdCondition = TableQuery.GenerateFilterCondition(
                TeamIdPropertyName,
                QueryComparisons.Equal,
                teamId);

            string combinedFilter = TableQuery.CombineFilters(partitionKeyFilter, TableOperators.And, teamIdCondition);

            TableQuery<UserTeamMembershipEntity> query = new TableQuery<UserTeamMembershipEntity>().Where(combinedFilter);
            var queryResult = await this.GoodReadsCloudTable.ExecuteQuerySegmentedAsync(query, null);

            if (queryResult.Any())
            {
                return queryResult.First();
            }

            return null;
        }

        /// <summary>
        /// Delete user team membership data in Microsoft Azure Table storage.
        /// </summary>
        /// <param name="entity">Represents user team membership entity object.</param>
        /// <returns>A boolean that represents user team membership data is successfully deleted or not.</returns>
        private async Task<bool> DeleteEntityAsync(UserTeamMembershipEntity entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            var operation = TableOperation.Delete(entity);
            var result = await this.GoodReadsCloudTable.ExecuteAsync(operation);

            return result.HttpStatusCode == (int)HttpStatusCode.NoContent;
        }

        /// <summary>
        /// Stores or update user team membership details data in Microsoft Azure Table storage.
        /// </summary>
        /// <param name="entity">Holds user team membership entity data.</param>
        /// <returns>A boolean that represents user team membership entity is successfully saved/updated or not.</returns>
        private async Task<bool> UpserUsertTeamMembershipPostAsync(UserTeamMembershipEntity entity)
        {
            var result = await this.StoreOrUpdateEntityAsync(entity);
            return result.HttpStatusCode == (int)HttpStatusCode.NoContent;
        }

        /// <summary>
        /// Stores or update user team membership detail in Microsoft Azure Table storage.
        /// </summary>
        /// <param name="entity">Holds team post detail entity data.</param>
        /// <returns>A task that represents user team membership entity data is saved or updated.</returns>
        private async Task<TableResult> StoreOrUpdateEntityAsync(UserTeamMembershipEntity entity)
        {
            await this.EnsureInitializedAsync();
            TableOperation addOrUpdateOperation = TableOperation.InsertOrReplace(entity);
            return await this.GoodReadsCloudTable.ExecuteAsync(addOrUpdateOperation);
        }

        /// <summary>
        /// Get entities from the table storage in a partition with a filter.
        /// </summary>
        /// <param name="teamIdCondition">Filter to the result.</param>
        /// <returns>All data entities.</returns>
        private async Task<IEnumerable<UserTeamMembershipEntity>> GetWithFilterAsync(string teamIdCondition)
        {
            await this.EnsureInitializedAsync();

            var partitionKeyFilter = this.GetPartitionKeyFilter();
            var combinedFilter = this.CombineFilters(teamIdCondition, partitionKeyFilter);
            var query = new TableQuery<UserTeamMembershipEntity>().Where(combinedFilter);

            TableContinuationToken continuationToken = null;
            var entities = new List<UserTeamMembershipEntity>();

            do
            {
                var queryResult = await this.GoodReadsCloudTable.ExecuteQuerySegmentedAsync(query, continuationToken);
                if (queryResult?.Results != null)
                {
                    entities.AddRange(queryResult.Results);
                    continuationToken = queryResult.ContinuationToken;
                }
            }
            while (continuationToken != null);

            return entities;
        }

        /// <summary>
        /// Get partition key filter based user team membership partition key.
        /// </summary>
        /// <returns>Returns user team membership filter query.</returns>
        private string GetPartitionKeyFilter()
        {
            var userMembershipFilter = TableQuery.GenerateFilterCondition(
                nameof(TableEntity.PartitionKey),
                QueryComparisons.Equal,
                Constants.UserTeamMembershipPartitionKey);

            return userMembershipFilter;
        }
    }
}
