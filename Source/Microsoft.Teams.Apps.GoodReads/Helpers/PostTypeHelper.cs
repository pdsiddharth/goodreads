// <copyright file="PostTypeHelper.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.GoodReads.Helpers
{
    using System.Collections.Generic;

    /// <summary>
    ///  Class that handles the post type.
    /// </summary>
    public static class PostTypeHelper
    {
        /// <summary>
        /// Dictionary for team post types.
        /// </summary>
        private static readonly Dictionary<int, string> PostType = new Dictionary<int, string>()
        {
            { 1, "Blog post" },
            { 2, "Other" },
            { 3, "Pod-cast" },
            { 4, "Video" },
            { 5, "Book" },
        };

        /// <summary>
        /// Get the post type using it's id.
        /// </summary>
        /// <param name="key">Post type id value.</param>
        /// <returns>Returns a post type from the id value.</returns>
        public static string GetPostType(int key)
        {
            string postType;
            if (PostType.TryGetValue(key, out postType))
            {
                return postType;
            }

            return null;
        }
    }
}
