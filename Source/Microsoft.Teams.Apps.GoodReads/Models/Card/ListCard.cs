﻿// <copyright file="ListCard.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.GoodReads.Models.Card
{
    using System.Collections.Generic;
    using Newtonsoft.Json;

    /// <summary>
    /// List card root class.
    /// </summary>
    public class ListCard
    {
        /// <summary>
        /// Gets or sets title of list card.
        /// </summary>
        [JsonProperty("title")]
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets list items.
        /// </summary>
        [JsonProperty("items")]
#pragma warning disable CA2227 // Getting error to make collection property as read only but needs to assign values.
        public List<ListItem> Items { get; set; }
#pragma warning restore CA2227

        /// <summary>
        /// Gets or sets buttons.
        /// </summary>
        [JsonProperty("buttons")]
#pragma warning disable CA2227 // Getting error to make collection property as read only but needs to assign values.
        public List<ButtonAction> Buttons { get; set; }
#pragma warning restore CA2227
    }
}