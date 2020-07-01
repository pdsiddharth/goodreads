// <copyright file="UserNotificationCard.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.Grow.Cards
{
    using System;
    using System.Collections.Generic;
    using AdaptiveCards;
    using Microsoft.Bot.Schema;
    using Microsoft.Bot.Schema.Teams;
    using Microsoft.Extensions.Localization;
    using Microsoft.Extensions.Options;
    using Microsoft.Teams.Apps.Grow.Bot;
    using Microsoft.Teams.Apps.Grow.Common;
    using Microsoft.Teams.Apps.Grow.Models.Card;
    using Newtonsoft.Json;

    /// <summary>
    /// Class that helps to create notification card for user's personal scope.
    /// </summary>
    public static class UserNotificationCard
    {
        /// <summary>
        /// Send project closure card to team members.
        /// </summary>
        /// <param name="projectTitle">Title of project to be closed.</param>
        /// <param name="ownerName">Owner of project to be closed.</param>
        /// <param name="tabOptions">Activity handler options object for getting discover tab ID and app url.</param>
        /// <param name="applicationManifestId">Tab's manifest Id.</param>
        /// <param name="feedback">Feedback of participant.</param>
        /// <param name="acquiredSkills">Skills acquired by participant.</param>
        /// <param name="localizer">The current cultures' string localizer.</param>
        /// <returns>Adaptive card with feedback and acquired skills.</returns>
        public static Attachment SendProjectClosureCard(
            string projectTitle,
            string ownerName,
            IOptions<GrowActivityHandlerOptions> tabOptions,
            string applicationManifestId,
            string feedback,
            List<string> acquiredSkills,
            IStringLocalizer<Strings> localizer)
        {
            AdaptiveCard projectClosureCard = new AdaptiveCard(Constants.AdaptiveCardVersion)
            {
                Body = new List<AdaptiveElement>
                {
                    new AdaptiveTextBlock
                    {
                        HorizontalAlignment = AdaptiveHorizontalAlignment.Left,
                        Wrap = true,
                        Text = localizer.GetString("CongratulationsMessage"),
                        Weight = AdaptiveTextWeight.Bolder,
                        Size = AdaptiveTextSize.Large,
                    },
                    new AdaptiveTextBlock
                    {
                        HorizontalAlignment = AdaptiveHorizontalAlignment.Left,
                        Wrap = true,
                        Text = localizer.GetString("ProjectClosedMessage", ownerName, projectTitle),
                    },
                    new AdaptiveTextBlock
                    {
                        HorizontalAlignment = AdaptiveHorizontalAlignment.Left,
                        Wrap = true,
                        Text = string.IsNullOrEmpty(feedback) ? string.Empty : $"\"{feedback}\" **{ownerName}**",
                    },
                },
            };

            if (acquiredSkills.Count > 0)
            {
                projectClosureCard.Body.Add(new AdaptiveTextBlock
                {
                    HorizontalAlignment = AdaptiveHorizontalAlignment.Left,
                    Wrap = true,
                    Text = localizer.GetString("SkillsEndorsedMessage", ownerName),
                    Spacing = AdaptiveSpacing.Medium,
                });
                List<AdaptiveTextBlock> skillsTextBlocks = new List<AdaptiveTextBlock>();
                for (int i = 0; i < acquiredSkills.Count; i++)
                {
                    skillsTextBlocks.Add(new AdaptiveTextBlock
                    {
                        Text = $"- {acquiredSkills[i]}",
                        HorizontalAlignment = AdaptiveHorizontalAlignment.Left,
                        Wrap = true,
                        Spacing = AdaptiveSpacing.Small,
                    });
                }

                projectClosureCard.Body.AddRange(skillsTextBlocks);
            }

            projectClosureCard.Actions = new List<AdaptiveAction>
            {
                new AdaptiveOpenUrlAction
                {
                    Title = localizer.GetString("GoToSkillsCardButton"),
                    Url = new Uri($"https://teams.microsoft.com/l/entity/{applicationManifestId}/AcquiredSkillsTab"),
                },
            };

            return new Attachment
            {
                ContentType = AdaptiveCard.ContentType,
                Content = projectClosureCard,
            };
        }

        /// <summary>
        /// Send project deletion card to team members.
        /// </summary>
        /// <param name="projectTitle">Title of project to be deleted.</param>
        /// <param name="ownerName">Owner of project to be deleted.</param>
        /// <param name="tabOptions">Activity handler options object for getting discover tab ID and app url.</param>
        /// <param name="applicationManifestId">Tab's manifest Id.</param>
        /// <param name="localizer">The current cultures' string localizer.</param>
        /// <returns>Adaptive card with deletion message.</returns>
        public static Attachment SendProjectDeletionCard(
            string projectTitle,
            string ownerName,
            IOptions<GrowActivityHandlerOptions> tabOptions,
            string applicationManifestId,
            IStringLocalizer<Strings> localizer)
        {
            AdaptiveCard projectDeletionCard = new AdaptiveCard(Constants.AdaptiveCardVersion)
            {
                Body = new List<AdaptiveElement>
                {
                    new AdaptiveTextBlock
                    {
                        HorizontalAlignment = AdaptiveHorizontalAlignment.Left,
                        Wrap = true,
                        Text = localizer.GetString("SorryMessage"),
                        Weight = AdaptiveTextWeight.Bolder,
                        Size = AdaptiveTextSize.Large,
                    },
                    new AdaptiveTextBlock
                    {
                        HorizontalAlignment = AdaptiveHorizontalAlignment.Left,
                        Wrap = true,
                        Text = localizer.GetString("ProjectDeletedMessage", ownerName, projectTitle),
                    },
                    new AdaptiveTextBlock
                    {
                        HorizontalAlignment = AdaptiveHorizontalAlignment.Left,
                        Wrap = true,
                        Text = localizer.GetString("TryNewProjectsMessage"),
                    },
                },
            };

            projectDeletionCard.Actions = new List<AdaptiveAction>
            {
                new AdaptiveOpenUrlAction
                {
                    Title = localizer.GetString("TabName"),
                    Url = new Uri($"https://teams.microsoft.com/l/entity/{applicationManifestId}/{tabOptions.Value.DiscoverTabEntityId}"),
                },
            };

            return new Attachment
            {
                ContentType = AdaptiveCard.ContentType,
                Content = projectDeletionCard,
            };
        }

        /// <summary>
        /// Send project deletion card to team members.
        /// </summary>
        /// <param name="projectTitle">Title of project to be deleted.</param>
        /// <param name="ownerName">Owner of project to be deleted.</param>
        /// <param name="tabOptions">Activity handler options object for getting discover tab ID and app url.</param>
        /// <param name="applicationManifestId">Tab's manifest Id.</param>
        /// <param name="localizer">The current cultures' string localizer.</param>
        /// <returns>Adaptive card with deletion message.</returns>
        public static Attachment SendProjectRemovalCard(
            string projectTitle,
            string ownerName,
            IOptions<GrowActivityHandlerOptions> tabOptions,
            string applicationManifestId,
            IStringLocalizer<Strings> localizer)
        {
            AdaptiveCard projectDeletionCard = new AdaptiveCard(Constants.AdaptiveCardVersion)
            {
                Body = new List<AdaptiveElement>
                {
                    new AdaptiveTextBlock
                    {
                        HorizontalAlignment = AdaptiveHorizontalAlignment.Left,
                        Wrap = true,
                        Text = localizer.GetString("SorryMessage"),
                        Weight = AdaptiveTextWeight.Bolder,
                        Size = AdaptiveTextSize.Large,
                    },
                    new AdaptiveTextBlock
                    {
                        HorizontalAlignment = AdaptiveHorizontalAlignment.Left,
                        Wrap = true,
                        Text = localizer.GetString("ProjectRemovalMessage", ownerName, projectTitle),
                    },
                    new AdaptiveTextBlock
                    {
                        HorizontalAlignment = AdaptiveHorizontalAlignment.Left,
                        Wrap = true,
                        Text = localizer.GetString("TryNewProjectsMessage"),
                    },
                },
            };

            projectDeletionCard.Actions = new List<AdaptiveAction>
            {
                new AdaptiveOpenUrlAction
                {
                    Title = localizer.GetString("TabName"),
                    Url = new Uri($"https://teams.microsoft.com/l/entity/{applicationManifestId}/{tabOptions.Value.DiscoverTabEntityId}"),
                },
            };

            return new Attachment
            {
                ContentType = AdaptiveCard.ContentType,
                Content = projectDeletionCard,
            };
        }

        /// <summary>
        /// Send project joined card to team members.
        /// </summary>
        /// <param name="projectId">Id of joined project.</param>
        /// <param name="projectTitle">Title of joined project.</param>
        /// <param name="userName">Owner of joined project.</param>
        /// <param name="userPrincipalName">User principal name.</param>
        /// <param name="createdByUserId">User Id who created project.</param>
        /// <param name="localizer">The current cultures' string localizer.</param>
        /// <returns>Adaptive card with deletion message.</returns>
        public static Attachment SendProjectJoinedCard(
            string projectId,
            string projectTitle,
            string userName,
            string userPrincipalName,
            string createdByUserId,
            IStringLocalizer<Strings> localizer)
        {
            AdaptiveCard projectJoinedCard = new AdaptiveCard(Constants.AdaptiveCardVersion)
            {
                Body = new List<AdaptiveElement>
                {
                    new AdaptiveTextBlock
                    {
                        HorizontalAlignment = AdaptiveHorizontalAlignment.Left,
                        Wrap = true,
                        Text = localizer.GetString("CongratulationsMessage"),
                        Weight = AdaptiveTextWeight.Bolder,
                        Size = AdaptiveTextSize.Large,
                    },
                    new AdaptiveTextBlock
                    {
                        HorizontalAlignment = AdaptiveHorizontalAlignment.Left,
                        Wrap = true,
                        Text = localizer.GetString("ProjectJoinedMessage", userName, projectTitle.Trim()),
                    },
                    new AdaptiveTextBlock
                    {
                        HorizontalAlignment = AdaptiveHorizontalAlignment.Left,
                        Wrap = true,
                        Text = localizer.GetString("ConnectWithUserMessage", userName),
                    },
                },
            };

            projectJoinedCard.Actions = new List<AdaptiveAction>
            {
                new AdaptiveOpenUrlAction
                {
                    Title = localizer.GetString("ChatButton"),
                    Url = new Uri($"https://teams.microsoft.com/l/chat/0/0?users={userPrincipalName}"),
                },
                new AdaptiveSubmitAction
                {
                    Title = localizer.GetString("ProjectDetails"),
                    Data = new AdaptiveSubmitActionData
                    {
                        Msteams = new TaskModuleAction(Constants.ViewProjectDetail, new { data = JsonConvert.SerializeObject(new AdaptiveTaskModuleCardAction { Text = Constants.ViewProjectDetail, ProjectId = projectId, CreatedByUserId = createdByUserId }) }),
                    },
                },
            };

            return new Attachment
            {
                ContentType = AdaptiveCard.ContentType,
                Content = projectJoinedCard,
            };
        }
    }
}
