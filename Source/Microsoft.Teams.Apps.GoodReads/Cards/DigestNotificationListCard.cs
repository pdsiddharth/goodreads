﻿// <copyright file="DigestNotificationListCard.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.GoodReads.Cards
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using AdaptiveCards;
    using Microsoft.Bot.Schema;
    using Microsoft.Extensions.Localization;
    using Microsoft.Teams.Apps.GoodReads.Models;
    using Microsoft.Teams.Apps.GoodReads.Models.Card;

    /// <summary>
    /// Class that helps to create notification list card for channel.
    /// </summary>
    public static class DigestNotificationListCard
    {
        /// <summary>
        /// Get list card for channel notification.
        /// </summary>
        /// <param name="teamPosts">Team post list object.</param>
        /// <param name="localizer">The current cultures' string localizer.</param>
        /// <param name="cardTitle">Notification list card title.</param>
        /// <param name="cardPostTypePair">Post type of card.</param>
        /// <param name="applicationManifestId">Application manifest id.</param>
        /// <param name="discoverTabEntityId">Discover tab entity id for personal Bot.</param>
        /// <param name="applicationBasePath">Application bath path.</param>
        /// <returns>An attachment card for channel notification.</returns>
        public static Attachment GetNotificationListCard(
            IEnumerable<TeamPostEntity> teamPosts,
            IStringLocalizer<Strings> localizer,
            string cardTitle,
            Dictionary<int, string> cardPostTypePair,
            string applicationManifestId,
            string discoverTabEntityId,
            string applicationBasePath)
        {
            teamPosts = teamPosts ?? throw new ArgumentNullException(nameof(teamPosts));

            ListCard card = new ListCard
            {
                Title = cardTitle,
                Items = new List<ListItem>(),
                Buttons = new List<ButtonAction>(),
            };

            var voteIcon = $"<img src='{applicationBasePath}/Artifacts/userVoteIcon.png' alt='vote logo' width='18' height='18'";

            foreach (var teamPostEntity in teamPosts)
            {
                string imagePath = string.Empty;
                cardPostTypePair?.TryGetValue(Convert.ToInt32(teamPostEntity.Type, CultureInfo.InvariantCulture), out imagePath);

                card.Items.Add(new ListItem
                {
                    Type = "resultItem",
                    Id = Guid.NewGuid().ToString(),
                    Title = teamPostEntity.Title,
                    Subtitle = $"{teamPostEntity.CreatedByName} | {teamPostEntity.TotalVotes} {voteIcon}",
                    Icon = imagePath,
                });
            }

            var buttonAction = new ButtonAction()
            {
                Title = localizer.GetString("NotificationListCardViewMoreButtonText"),
                Type = "openUrl",
                Value = $"https://teams.microsoft.com/l/entity/{applicationManifestId}/{discoverTabEntityId}?webUrl={applicationBasePath}&label=Discover",
            };

            card.Buttons.Add(buttonAction);

            var attachment = new Attachment()
            {
                ContentType = "application/vnd.microsoft.teams.card.list",
                Content = card,
            };

            return attachment;
        }

        /// <summary>
        /// Get container for team post.
        /// </summary>
        /// <param name="teamPost">Team post entity object.</param>
        /// <param name="cardPostTypePair">Post type of card.</param>
        /// <returns>Return a container for team post.</returns>
        private static AdaptiveContainer GetPostTypeContainer(
             TeamPostEntity teamPost,
             Dictionary<int, string> cardPostTypePair)
        {
            string imagePath = string.Empty;
            cardPostTypePair?.TryGetValue(Convert.ToInt32(teamPost.Type, CultureInfo.InvariantCulture), out imagePath);

            var postTypeContainer = new AdaptiveContainer
            {
                Items = new List<AdaptiveElement>
                {
                    new AdaptiveColumnSet
                    {
                        Columns = new List<AdaptiveColumn>
                        {
                            new AdaptiveColumn
                            {
                                Width = AdaptiveColumnWidth.Auto,
                                Items = new List<AdaptiveElement>
                                {
                                    new AdaptiveImage
                                    {
                                        Url = new Uri(imagePath),
                                        Size = AdaptiveImageSize.Small,
                                    },
                                },
                            },
                            new AdaptiveColumn
                            {
                                Items = new List<AdaptiveElement>
                                {
                                    new AdaptiveTextBlock
                                    {
                                        Weight = AdaptiveTextWeight.Bolder,
                                        Spacing = AdaptiveSpacing.None,
                                        Text = teamPost.Title.Length > 45 ? $"{teamPost.Title.Substring(0, 42)} {"..."}" : teamPost.Title,
                                        Wrap = true,
                                    },
                                    new AdaptiveTextBlock
                                    {
                                        Spacing = AdaptiveSpacing.None,
                                        Text = $"{teamPost.CreatedByName} | {teamPost.TotalVotes} {"upvotes"}",
                                        Wrap = true,
                                        IsSubtle = true,
                                    },
                                },
                                Spacing = AdaptiveSpacing.Small,
                            },
                        },
                    },
                },
            };

            return postTypeContainer;
        }
    }
}
