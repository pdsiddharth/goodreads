// <copyright file="PostResponse.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.GoodReads.Models
{
    using System.Collections.Generic;

    /// <summary>
    /// Class which represents team post response model.
    /// </summary>
    public class PostResponse
    {
        /// <summary>
        /// gets or sets team posts data.
        /// </summary>
#pragma warning disable CA2227 // Getting error to make collection property as read only but needs to assign values.
        public List<TeamPostEntity> Posts { get; set; }
#pragma warning restore CA2227

        /// <summary>
        /// Gets or sets the continuation token.
        /// </summary>
        public string ContinuationToken { get; set; }
    }
}
