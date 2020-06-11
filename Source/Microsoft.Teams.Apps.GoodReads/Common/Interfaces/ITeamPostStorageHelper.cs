// <copyright file="ITeamPostStorageHelper.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.GoodReads.Common.Interfaces
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Teams.Apps.GoodReads.Models;

    /// <summary>
    /// Interface for storage helper which helps in preparing model data for team post.
    /// </summary>
    public interface ITeamPostStorageHelper
    {
        /// <summary>
        /// Create team post details model.
        /// </summary>
        /// <param name="teamPostEntity">Team post object.</param>
        /// <param name="userId">Azure Active directory id of user.</param>
        /// <param name="userName">Author who created the post.</param>
        /// <returns>A task that represents team post entity data.</returns>
        TeamPostEntity CreateTeamPostModel(TeamPostEntity teamPostEntity, string userId, string userName);

        /// <summary>
        /// Create updated team post model to save in Microsoft Azure Table storage.
        /// </summary>
        /// <param name="teamPostEntity">Team post detail.</param>
        /// <returns>A task that represents team post entity updated data.</returns>
        TeamPostEntity CreateUpdatedTeamPostModel(TeamPostEntity teamPostEntity);

        /// <summary>
        /// Get filtered team posts as per the configured tags.
        /// </summary>
        /// <param name="teamPosts">Team post entities.</param>
        /// <param name="searchText">Search text for tags.</param>
        /// <returns>Represents team posts.</returns>
        IEnumerable<TeamPostEntity> GetFilteredTeamPostsAsPerTags(IEnumerable<TeamPostEntity> teamPosts, string searchText);

        /// <summary>
        /// Get tags query to fetch team posts as per the configured tags.
        /// </summary>
        /// <param name="tags">Tags of a configured team post.</param>
        /// <returns>Represents tags query to fetch team posts.</returns>
        string GetTags(string tags);

        /// <summary>
        /// Get filtered team posts as per the date range from Microsoft Azure Table storage.
        /// </summary>
        /// <param name="teamPosts">Team posts data.</param>
        /// <param name="fromDate">Start date from which data should fetch.</param>
        /// <param name="toDate">End date till when data should fetch.</param>
        /// <returns>A task that represent collection to hold team posts data.</returns>
        IEnumerable<TeamPostEntity> GetTeamPostsInDateRangeAsync(IEnumerable<TeamPostEntity> teamPosts, DateTime fromDate, DateTime toDate);

        /// <summary>
        /// Get filtered unique user names.
        /// </summary>
        /// <param name="teamPosts">Team post entities.</param>
        /// <returns>Represents team posts.</returns>
        IEnumerable<string> GetAuthorNamesAsync(IEnumerable<TeamPostEntity> teamPosts);

        /// <summary>
        /// Get combined query to fetch team posts as per the selected filter.
        /// </summary>
        /// <param name="postTypes">Post type like: Blog post or Other.</param>
        /// <param name="sharedByNames">User names selected in filter.</param>
        /// <returns>Represents user names query to filter team posts.</returns>
        string GetFilterSearchQuery(string postTypes, string sharedByNames);
    }
}
