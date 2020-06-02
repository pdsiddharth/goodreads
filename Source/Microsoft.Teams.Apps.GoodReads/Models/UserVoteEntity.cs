// <copyright file="UserVoteEntity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.GoodReads.Models
{
    using System.ComponentModel.DataAnnotations;
    using Microsoft.WindowsAzure.Storage.Table;

    /// <summary>
    /// Class which represents user like/vote model.
    /// </summary>
    public class UserVoteEntity : TableEntity
    {
        /// <summary>
        /// Gets or sets unique value for each user.
        /// </summary>
        public string UserId
        {
            get { return this.PartitionKey; }
            set { this.PartitionKey = value; }
        }

        /// <summary>
        /// Gets or sets unique identifier for each created post.
        /// </summary>
        [Key]
        public string PostId
        {
            get { return this.RowKey; }
            set { this.RowKey = value; }
        }
    }
}
