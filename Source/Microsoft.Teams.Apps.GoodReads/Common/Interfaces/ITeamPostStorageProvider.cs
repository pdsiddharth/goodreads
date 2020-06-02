// <copyright file="ITeamPostStorageProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>
namespace Microsoft.Teams.Apps.GoodReads.Common.Interfaces
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.Teams.Apps.GoodReads.Models;

    /// <summary>
    /// Interface for provider which helps in retrieving, storing, updating and deleting team post details in Microsoft Azure Table storage.
    /// </summary>
    public interface ITeamPostStorageProvider
    {
        /// <summary>
        /// Get team posts data from Microsoft Azure Table storage.
        /// </summary>
        /// <param name="isRemoved">Represent whether a team post is deleted or not.</param>
        /// <returns>A task that represent collection to hold team posts ids.</returns>
        Task<IEnumerable<string>> GetTeamPostsIdsAsync(bool isRemoved);

        /// <summary>
        /// Stores or update team post details data in Microsoft Azure Table storage.
        /// </summary>
        /// <param name="teamPostEntity">Holds team post detail entity data.</param>
        /// <returns>A task that represents team post entity data is saved or updated.</returns>
        Task<bool> UpsertTeamPostAsync(TeamPostEntity teamPostEntity);

        /// <summary>
        /// Get team post data from Microsoft Azure Table storage.
        /// </summary>
        /// <param name="postId">Post id to fetch the post details.</param>
        /// <returns>A task that represent a object to hold team post data.</returns>
        Task<TeamPostEntity> GetTeamPostEntityAsync(string postId);

        /// <summary>
        /// Get team posts as per the user's private list of post from Microsoft Azure Table storage.
        /// </summary>
        /// <param name="postIds">A collection of user private post id's.</param>
        /// <returns>A task that represent collection to hold team posts data.</returns>
        Task<IEnumerable<TeamPostEntity>> GetFilteredUserPrivatePostsAsync(IEnumerable<string> postIds);

        /// <summary>
        /// Delete team posts which is soft deleted and isRemoved = true in Microsoft Azure Table storage.
        /// </summary>
        /// <param name="teamPostsIds">A collection of team post ids.</param>
        /// <returns>A task that represents team posts are deleted.</returns>
        Task<bool> DeleteTeamPostEntitiesAsync(IEnumerable<string> teamPostsIds);
    }
}