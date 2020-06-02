// <copyright file="UserPrivatePostStorageHelper.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.GoodReads.Helpers
{
    using System;
    using Microsoft.Extensions.Logging;
    using Microsoft.Teams.Apps.GoodReads.Common.Interfaces;
    using Microsoft.Teams.Apps.GoodReads.Models;

    /// <summary>
    /// Implements storage helper which is responsible for get, add and delete user private posts data in Microsoft Azure Table storage.
    /// </summary>
    public class UserPrivatePostStorageHelper : IUserPrivatePostStorageHelper
    {
        /// <summary>
        /// Instance to send logs to the Application Insights service.
        /// </summary>
        private readonly ILogger<UserPrivatePostStorageHelper> logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserPrivatePostStorageHelper"/> class.
        /// </summary>
        /// <param name="logger">Instance to send logs to the Application Insights service.</param>
        public UserPrivatePostStorageHelper(
            ILogger<UserPrivatePostStorageHelper> logger)
        {
            this.logger = logger;
        }

        /// <summary>
        /// Get user private post details model.
        /// </summary>
        /// <param name="userPrivatePostEntity">User private post entity model.</param>
        /// <param name="userId">Azure Active Directory id of the user.</param>
        /// <param name="userName">The user name.</param>
        /// <returns>Represents private post entity model.</returns>
        public UserPrivatePostEntity GetNewUserPrivatePostModel(UserPrivatePostEntity userPrivatePostEntity, string userId, string userName)
        {
            try
            {
                userPrivatePostEntity = userPrivatePostEntity ?? throw new ArgumentNullException(nameof(userPrivatePostEntity));
                userPrivatePostEntity.UserId = userId;
                userPrivatePostEntity.CreatedByName = userName;
                userPrivatePostEntity.CreatedDate = DateTime.UtcNow;
                return userPrivatePostEntity;
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Exception occurred while creating the user's private post model.");
                throw;
            }
        }
    }
}
