﻿// <copyright file="UserPrivatePostEntity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.GoodReads.Models
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using Microsoft.WindowsAzure.Storage.Table;

    /// <summary>
    /// Class which represents user private post model.
    /// </summary>
    public class UserPrivatePostEntity : TableEntity
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

        /// <summary>
        /// Gets or sets date time when entry is created.
        /// </summary>
        public DateTime CreatedDate { get; set; }

        /// <summary>
        /// Gets or sets name of user who created the post.
        /// </summary>
        public string CreatedByName { get; set; }
    }
}