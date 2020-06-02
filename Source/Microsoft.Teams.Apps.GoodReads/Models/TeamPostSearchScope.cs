// <copyright file="TeamPostSearchScope.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.GoodReads.Models
{
    /// <summary>
    /// Team post search scope.
    /// </summary>
    public enum TeamPostSearchScope
    {
        /// <summary>
        /// All items for team post.
        /// </summary>
        AllItems,

        /// <summary>
        /// Posted by me team posts.
        /// </summary>
        PostedByMe,

        /// <summary>
        /// Popular team posts.
        /// </summary>
        Popular,

        /// <summary>
        /// Get tags while configuring team preference.
        /// </summary>
        TeamPreferenceTags,

        /// <summary>
        /// Get team posts as per the configured tags in a particular team.
        /// </summary>
        FilterAsPerTeamTags,

        /// <summary>
        /// Get team posts based on the updated date.
        /// </summary>
        FilterPostsAsPerDateRange,

        /// <summary>
        /// Get unique user names who created the posts to show on filter bar drop-down list.
        /// </summary>
        UniqueUserNames,

        /// <summary>
        /// Get team posts as per the search text for title field.
        /// </summary>
        SearchTeamPostsForTitleText,

        /// <summary>
        /// Get team posts as per the applied filters.
        /// </summary>
        FilterTeamPosts,
    }
}
