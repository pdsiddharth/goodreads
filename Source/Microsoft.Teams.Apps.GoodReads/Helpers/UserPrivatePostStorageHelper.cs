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
    /// Implements user private post storage helper which helps to construct the model for user private post.
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
        /// Create user private post model data.
        /// </summary>
        /// <param name="userPrivatePostEntity">User private post entity model.</param>
        /// <param name="userId">Azure Active Directory id of the user.</param>
        /// <param name="userName">Name of user who added the post in private list.</param>
        /// <returns>Represents private post entity model.</returns>
        public UserPrivatePostEntity CreateUserPrivatePostModel(UserPrivatePostEntity userPrivatePostEntity, string userId, string userName)
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
