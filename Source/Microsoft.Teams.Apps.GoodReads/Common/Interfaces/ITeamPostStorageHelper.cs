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
        /// Get team post details model.
        /// </summary>
        /// <param name="teamPostEntity">Team post object.</param>
        /// <param name="userId">Azure Active directory id of user.</param>
        /// <param name="userName">The user name.</param>
        /// <returns>A task that represents team post entity data.</returns>
        TeamPostEntity GetTeamPostModel(TeamPostEntity teamPostEntity, string userId, string userName);

        /// <summary>
        /// Get updated team post details to Microsoft Azure Table storage.
        /// </summary>
        /// <param name="teamPostEntity">Team post detail.</param>
        /// <returns>A task that represents team post entity updated data.</returns>
        TeamPostEntity GetUpdatedTeamPostModel(TeamPostEntity teamPostEntity);

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
        string GetTagsQuery(string tags);

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
        IEnumerable<string> GetFilteredUserNames(IEnumerable<TeamPostEntity> teamPosts);

        /// <summary>
        /// Get user names and post types query to fetch team posts as per the selected filters.
        /// </summary>
        /// <param name="postTypes">Post type like: Blog post or Other.</param>
        /// <param name="sharedByNames">User names selected in filter.</param>
        /// <returns>Represents user names query to filter team posts.</returns>
        string GetFilterSearchQuery(string postTypes, string sharedByNames);
    }
}
