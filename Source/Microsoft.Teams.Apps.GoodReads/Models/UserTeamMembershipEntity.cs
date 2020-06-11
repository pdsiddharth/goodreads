// <copyright file="UserTeamMembershipEntity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.GoodReads.Models
{
    using System;
    using Microsoft.Teams.Apps.GoodReads.Common;
    using Microsoft.WindowsAzure.Storage.Table;

    /// <summary>
    /// A class that represents user team membership entity.
    /// </summary>
    public class UserTeamMembershipEntity : TableEntity
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UserTeamMembershipEntity"/> class.
        /// Holds team posts data.
        /// </summary>
        public UserTeamMembershipEntity()
        {
            this.PartitionKey = Constants.UserTeamMembershipPartitionKey;
            this.RowKey = Guid.NewGuid().ToString();
        }

        /// <summary>
        /// Gets or sets Azure Active Directory id of user.
        /// </summary>
        public string UserAadObjectId { get; set; }

        /// <summary>
        /// Gets or sets id of the team.
        /// </summary>
        public string TeamId { get; set; }

        /// <summary>
        /// Gets or sets service URL where responses to this activity should be sent.
        /// </summary>
        public string ServiceUrl { get; set; }
    }
}
