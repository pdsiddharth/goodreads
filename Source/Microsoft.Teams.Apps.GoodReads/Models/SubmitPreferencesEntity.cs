// <copyright file="SubmitPreferencesEntity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.GoodReads.Models
{
    /// <summary>
    /// Class which represents take data after submit entity.
    /// </summary>
    public class SubmitPreferencesEntity
    {
        /// <summary>
        /// Gets or sets TeamPreferenceEntity Model.
        /// </summary>
        public TeamPreferenceEntity ConfigureDetails { get; set; }

        /// <summary>
        /// Gets or sets Command showing submit or cancel taskmodule.
        /// </summary>
        public string Command { get; set; }
    }
}
