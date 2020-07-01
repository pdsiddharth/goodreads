// <copyright file="ListItem.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.Grow.Models.Card
{
    using Newtonsoft.Json;

    /// <summary>
    /// List card Item class.
    /// </summary>
    public class ListItem
    {
        /// <summary>
        /// Gets or sets type of item.
        /// </summary>
        [JsonProperty("type")]
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets id of the list card item.
        /// </summary>
        [JsonProperty("id")]
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets title of list card item.
        /// </summary>
        [JsonProperty("title")]
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets subtitle of list card item.
        /// </summary>
        [JsonProperty("subtitle")]
        public string Subtitle { get; set; }

        /// <summary>
        /// Gets or sets icon for list card item.
        /// </summary>
        [JsonProperty("icon")]
        public string Icon { get; set; }
    }
}
