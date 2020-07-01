// <copyright file="TeamSkillEntity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.Grow.Models
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using Microsoft.Teams.Apps.Grow.Helpers.CustomValidations;
    using Microsoft.WindowsAzure.Storage.Table;

    /// <summary>
    /// A class that represents team skill entity model.
    /// </summary>
    public class TeamSkillEntity : TableEntity
    {
        /// <summary>
        /// Gets or sets unique value for each Team where skills has configured.
        /// </summary>
        [Required]
        public string TeamId
        {
            get { return this.PartitionKey; }
            set { this.PartitionKey = value; }
        }

        /// <summary>
        /// Gets or sets semicolon separated skills selected by user.
        /// </summary>
        [TeamSkillsValidation(5)]
        public string Skills { get; set; }

        /// <summary>
        /// Gets or sets user Id who installed and configured skills for team.
        /// </summary>
        public string CreatedByUserId { get; set; }

        /// <summary>
        /// Gets or sets user Id who updated skills for team.
        /// </summary>
        public string UpdatedByUserId { get; set; }

        /// <summary>
        /// Gets or sets the date time when the application is installed.
        /// </summary>
        public DateTime BotInstalledOn { get; set; }

        /// <summary>
        /// Gets or sets service URL.
        /// </summary>
        public string ServiceUrl { get; set; }
    }
}
