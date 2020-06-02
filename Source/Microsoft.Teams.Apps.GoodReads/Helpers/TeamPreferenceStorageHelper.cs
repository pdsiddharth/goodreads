// <copyright file="TeamPreferenceStorageHelper.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.GoodReads.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Extensions.Logging;
    using Microsoft.Teams.Apps.GoodReads.Common.Interfaces;
    using Microsoft.Teams.Apps.GoodReads.Models;

    /// <summary>
    /// Implements user storage helper which is responsible for storing, updating and deleting team preference data in Microsoft Azure Table storage.
    /// </summary>
    public class TeamPreferenceStorageHelper : ITeamPreferenceStorageHelper
    {
        /// <summary>
        /// Instance to send logs to the Application Insights service.
        /// </summary>
        private readonly ILogger<TeamPreferenceStorageHelper> logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="TeamPreferenceStorageHelper"/> class.
        /// </summary>
        /// <param name="logger">Instance to send logs to the Application Insights service.</param>
        public TeamPreferenceStorageHelper(
            ILogger<TeamPreferenceStorageHelper> logger)
        {
            this.logger = logger;
        }

        /// <summary>
        /// Get team preference details.
        /// </summary>
        /// <param name="entity">Represents team preference entity object.</param>
        /// <returns>Represents team preference entity model.</returns>
        public TeamPreferenceEntity GetTeamPreferenceModel(TeamPreferenceEntity entity)
        {
            try
            {
                entity = entity ?? throw new ArgumentNullException(nameof(entity));

                entity.PreferenceId = Guid.NewGuid().ToString();
                entity.CreatedDate = DateTime.UtcNow;
                entity.UpdatedDate = DateTime.UtcNow;

                return entity;
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Exception occurred while preparing the team preference entity model data");
                throw;
            }
        }

        /// <summary>
        /// Get posts unique tags.
        /// </summary>
        /// <param name="teamPosts">Team post entities.</param>
        /// <param name="searchText">Search text for tags.</param>
        /// <returns>Represents team tags.</returns>
        public IEnumerable<string> GetUniqueTags(IEnumerable<TeamPostEntity> teamPosts, string searchText)
        {
            try
            {
                teamPosts = teamPosts ?? throw new ArgumentNullException(nameof(teamPosts));
                var tags = new List<string>();

                if (searchText == "*")
                {
                    foreach (var teamPost in teamPosts)
                    {
                        tags.AddRange(teamPost.Tags?.Split(";"));
                    }
                }
                else
                {
                    foreach (var teamPost in teamPosts)
                    {
                        tags.AddRange(teamPost.Tags?.Split(";").Where(tag => tag.Contains(searchText, StringComparison.InvariantCultureIgnoreCase)));
                    }
                }

                return tags.Distinct().OrderBy(tag => tag);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Exception occurred while preparing the team preference entity model data");
                throw;
            }
        }
    }
}
