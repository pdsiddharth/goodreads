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
        /// Create user private post model data.
        /// </summary>
        /// <param name="userPrivatePostEntity">User private post entity model.</param>
        /// <param name="userId">Azure Active Directory id of the user.</param>
        /// <param name="userName">Name of user who added the post in private list.</param>
        /// <returns>Represents private post entity model.</returns>
        UserPrivatePostEntity CreateUserPrivatePostModel(UserPrivatePostEntity userPrivatePostEntity, string userId, string userName);
    }
}
