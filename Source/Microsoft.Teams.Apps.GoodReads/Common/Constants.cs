// <copyright file="Constants.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.GoodReads.Common
{
    /// <summary>
    /// A class that holds application constants that are used in multiple files.
    /// </summary>
    public static class Constants
    {
        /// <summary>
        /// Partition key for team post entity table.
        /// </summary>
        public const string TeamPostEntityPartitionKey = "TeamPostEntity";

        /// <summary>
        /// Partition key for team preference entity table.
        /// </summary>
        public const string TeamPreferenceEntityPartitionKey = "TeamPreferenceEntity";

        /// <summary>
        /// Partition key for user team membership entity table.
        /// </summary>
        public const string UserTeamMembershipPartitionKey = "UserTeamMembershipEntity";

        /// <summary>
        /// All items post command id in the manifest file.
        /// </summary>
        public const string AllItemsPostCommandId = "allItems";

        /// <summary>
        ///  Posted by me post command id in the manifest file.
        /// </summary>
        public const string PostedByMePostCommandId = "postedByMe";

        /// <summary>
        ///  Popular post command id in the manifest file.
        /// </summary>
        public const string PopularPostCommandId = "popularReads";

        /// <summary>
        /// Bot prefrences command to set preference for sending Weekly/Monthly notifications.
        /// </summary>
        public const string Preferences = "PREFERENCES";

        /// <summary>
        /// Partition key for team tag entity table.
        /// </summary>
        public const string TeamTagEntityPartitionKey = "TeamTagEntity";

        /// <summary>
        /// Bot Help command in personal scope.
        /// </summary>
        public const string HelpCommand = "HELP";

        /// <summary>
        /// Per page post count for lazy loading (max 50).
        /// </summary>
        public const int LazyLoadPerPagePostCount = 50;
    }
}
