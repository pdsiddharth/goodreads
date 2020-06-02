// <copyright file="TeamTagStorageHelper.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.GoodReads.Helpers
{
    using System;
    using Microsoft.Extensions.Logging;
    using Microsoft.Teams.Apps.GoodReads.Common.Interfaces;
    using Microsoft.Teams.Apps.GoodReads.Models;

    /// <summary>
    /// Implements user storage helper which is responsible for preparing the model data for team tags to store in Microsoft Azure Table storage.
    /// </summary>
    public class TeamTagStorageHelper : ITeamTagStorageHelper
    {
        /// <summary>
        /// Instance to send logs to the Application Insights service.
        /// </summary>
        private readonly ILogger<TeamTagStorageHelper> logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="TeamTagStorageHelper"/> class.
        /// </summary>
        /// <param name="logger">Instance to send logs to the Application Insights service.</param>
        public TeamTagStorageHelper(
            ILogger<TeamTagStorageHelper> logger)
        {
            this.logger = logger;
        }

        /// <summary>
        /// Get team tags details.
        /// </summary>
        /// <param name="teamTagEntity">Represents team tag entity object.</param>
        /// <param name="userName">User name who has configured the tags in team.</param>
        /// <param name="userAadId">Azure Active Directory id of the user.</param>
        /// <returns>Represents team tags entity model.</returns>
        public TeamTagEntity GetTeamTagModel(TeamTagEntity teamTagEntity, string userName, string userAadId)
        {
            try
            {
                teamTagEntity = teamTagEntity ?? throw new ArgumentNullException(nameof(teamTagEntity));

                teamTagEntity.CreatedByName = userName;
                teamTagEntity.UserAadId = userAadId;
                teamTagEntity.CreatedDate = DateTime.UtcNow;

                return teamTagEntity;
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Exception occurred while preparing the team tags entity model data");
                throw;
            }
        }
    }
}
