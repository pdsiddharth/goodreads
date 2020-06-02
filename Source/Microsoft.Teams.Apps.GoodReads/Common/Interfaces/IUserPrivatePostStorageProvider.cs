﻿// <copyright file="IUserPrivatePostStorageProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.GoodReads.Common.Interfaces
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.Teams.Apps.GoodReads.Models;

    /// <summary>
    /// Interface for provider which helps in storing, updating or deleting user's private list of posts details in Microsoft Azure Table storage.
    /// </summary>
    public interface IUserPrivatePostStorageProvider
    {
        /// <summary>
        /// Get user's private list of posts data from Microsoft Azure Table storage.
        /// </summary>
        /// <param name="userId">User id for which need to fetch data.</param>
        /// <returns>A task that represent collection to hold user's private list of posts data.</returns>
        Task<IEnumerable<string>> GetUserPrivatePostsIdsAsync(string userId);

        /// <summary>
        /// Stores or update post data in user's private list in Microsoft Azure Table storage.
        /// </summary>
        /// <param name="entity">Holds private post detail entity data.</param>
        /// <returns>A task that represents private post entity data is saved or updated.</returns>
        Task<bool> UpsertPostAsPrivateAsync(UserPrivatePostEntity entity);

        /// <summary>
        /// Delete private post from user's private list in Microsoft Azure Table storage.
        /// </summary>
        /// <param name="postId">Holds private post id.</param>
        /// <param name="userId">Azure Active Directory id of user.</param>
        /// <returns>A task that represents private post is deleted.</returns>
        Task<bool> DeletePrivatePostAsync(string postId, string userId);
    }
}
