// <copyright file="AzureActiveDirectorySettings.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.GoodReads.Models.Configuration
{
    /// <summary>
    /// A class which helps to provide Azure Active Directlry settings for Share Now application.
    /// </summary>
    public class AzureActiveDirectorySettings
    {
        /// <summary>
        /// Gets or sets tenant id of application.
        /// </summary>
        public string TenantId { get; set; }

        /// <summary>
        /// Gets or sets client id of application.
        /// </summary>
        public string ClientId { get; set; }
    }
}
