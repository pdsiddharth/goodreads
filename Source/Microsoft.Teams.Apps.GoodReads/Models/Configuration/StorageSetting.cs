// <copyright file="StorageSetting.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.GoodReads.Models.Configuration
{
    /// <summary>
    /// A class which helps to provide Microsoft Azure Table storage settings for Share Now app.
    /// </summary>
    public class StorageSetting : BotSetting
    {
        /// <summary>
        /// Gets or sets Microsoft Azure Table storage connection string.
        /// </summary>
        public string ConnectionString { get; set; }
    }
}
