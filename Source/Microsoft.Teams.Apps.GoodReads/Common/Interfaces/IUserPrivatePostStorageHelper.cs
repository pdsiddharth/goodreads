// <copyright file="IUserPrivatePostStorageHelper.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.GoodReads.Common.Interfaces
{
    using Microsoft.Teams.Apps.GoodReads.Models;

    /// <summary>
    /// Interface for storage helper which helps in preparing model data for user private post.
    /// </summary>
    public interface IUserPrivatePostStorageHelper
    {
        /// <summary>
        /// Get user private post details model.
        /// </summary>
        /// <param name="userPrivatePostEntity">User private post entity model.</param>
        /// <param name="userId">Azure Active Directory id of the user.</param>
        /// <param name="userName">The user name.</param>
        /// <returns>Represents private post entity model.</returns>
        UserPrivatePostEntity GetNewUserPrivatePostModel(UserPrivatePostEntity userPrivatePostEntity, string userId, string userName);
    }
}
