// <copyright file="ITeamPreferenceStorageHelper.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.GoodReads.Common.Interfaces
{
    using System.Collections.Generic;
    using Microsoft.Teams.Apps.GoodReads.Models;

    /// <summary>
    /// Interface for storage helper which helps in preparing model data for team preference.
    /// </summary>
    public interface ITeamPreferenceStorageHelper
    {
        /// <summary>
        /// Get team preference details.
        /// </summary>
        /// <param name="entity">Team preference detail.</param>
        /// <returns>Represents team preference entity model.</returns>
        TeamPreferenceEntity GetTeamPreferenceModel(TeamPreferenceEntity entity);

        /// <summary>
        /// Get posts unique tags.
        /// </summary>
        /// <param name="teamPosts">Team post entities.</param>
        /// <param name="searchText">Input tag as search text.</param>
        /// <returns>Represents team tags.</returns>
        IEnumerable<string> GetUniqueTags(IEnumerable<TeamPostEntity> teamPosts, string searchText);
    }
}
