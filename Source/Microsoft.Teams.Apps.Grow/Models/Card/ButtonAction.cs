// <copyright file="ButtonAction.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.Grow.Models.Card
{
    using Newtonsoft.Json;

    /// <summary>
    /// Button action class for list card.
    /// </summary>
    public class ButtonAction
    {
        /// <summary>
        /// Gets or sets type of button action.
        /// </summary>
        [JsonProperty("type")]
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets title of button.
        /// </summary>
        [JsonProperty("title")]
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets value of button.
        /// </summary>
        [JsonProperty("value")]
        public string Value { get; set; }
    }
}
