// <copyright file="UserMembershipProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.Grow.Common.Providers
{
    using System;
    using System.Net;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Microsoft.Teams.Apps.Grow.Common.Interfaces;
    using Microsoft.Teams.Apps.Grow.Models;
    using Microsoft.Teams.Apps.Grow.Models.Configuration;
    using Microsoft.WindowsAzure.Storage.Table;

    /// <summary>
    /// Implements storage provider which stores user membership data in storage.
    /// </summary>
    public class UserMembershipProvider : BaseStorageProvider, IUserMembershipProvider
    {
        /// <summary>
        /// Represents user membership entity name.
        /// </summary>
        private const string UserMembershipEntityName = "UserMembershipEntity";

        /// <summary>
        /// Initializes a new instance of the <see cref="UserMembershipProvider"/> class.
        /// Handles storage read write operations.
        /// </summary>
        /// <param name="options">A set of key/value application configuration properties for Microsoft Azure Table storage.</param>
        /// <param name="logger">Sends logs to the Application Insights service.</param>
        public UserMembershipProvider(
            IOptions<StorageSetting> options,
            ILogger<BaseStorageProvider> logger)
            : base(options?.Value.ConnectionString, UserMembershipEntityName, logger)
        {
        }

        /// <summary>
        /// Adds a user membership entity in storage.
        /// </summary>
        /// <param name="userConversationId">User conversation id.</param>
        /// <param name="userAadObjectId">Azure Active Directory id of the user.</param>
        /// <param name="servicePath">Service URL for a tenant.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task AddUserMembershipAsync(
            string userConversationId,
            string userAadObjectId,
            string servicePath)
        {
            var userMembershipEntity = new UserMembershipEntity
            {
                UserAadObjectId = userAadObjectId,
                RowKey = userAadObjectId,
                UserConversationId = userConversationId,
                ServiceUrl = servicePath,
            };

            await this.UpsertUserMembershipAsync(userMembershipEntity);
        }

        /// <summary>
        /// Get user membership data from storage.
        /// </summary>
        /// <param name="userAadObjectId">Azure Active Directory id of the user.</param>
        /// <returns>A task that represents an object to hold user membership data.</returns>
        public async Task<UserMembershipEntity> GetUserMembershipDataAsync(string userAadObjectId)
        {
            userAadObjectId = userAadObjectId ?? throw new ArgumentNullException(nameof(userAadObjectId));

            await this.EnsureInitializedAsync();
            var retrieveOperation = TableOperation.Retrieve<UserMembershipEntity>(userAadObjectId, userAadObjectId);
            var queryResult = await this.GrowCloudTable.ExecuteAsync(retrieveOperation);

            if (queryResult?.Result != null)
            {
                return (UserMembershipEntity)queryResult.Result;
            }

            return null;
        }

        /// <summary>
        /// Stores or update user membership details data in storage.
        /// </summary>
        /// <param name="entity">Holds user membership entity data.</param>
        /// <returns>A task that represents user membership entity data is saved or updated.</returns>
        private async Task<bool> UpsertUserMembershipAsync(UserMembershipEntity entity)
        {
            var result = await this.StoreOrUpdateEntityAsync(entity);
            return result.HttpStatusCode == (int)HttpStatusCode.NoContent;
        }

        /// <summary>
        /// Stores or update user membership detail in storage.
        /// </summary>
        /// <param name="entity">Holds user membership entity data.</param>
        /// <returns>A task that represents user membership entity data is saved or updated.</returns>
        private async Task<TableResult> StoreOrUpdateEntityAsync(UserMembershipEntity entity)
        {
            await this.EnsureInitializedAsync();
            TableOperation addOrUpdateOperation = TableOperation.InsertOrReplace(entity);
            return await this.GrowCloudTable.ExecuteAsync(addOrUpdateOperation);
        }
    }
}
