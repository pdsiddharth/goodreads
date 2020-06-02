// <copyright file="GetCarouselCard.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.GoodReads.Cards
{
    using System.Collections.Generic;
    using Microsoft.Bot.Schema;

    /// <summary>
    /// Carousal card.
    /// </summary>
    public static class GetCarouselCard
    {
        /// <summary>
        /// Create the set of cards that comprise the user help carousel.
        /// </summary>
        /// <param name="applicationBasePath">Application base path to get the logo of the application.</param>
        /// <returns>The cards that comprise the user tour.</returns>
        public static IEnumerable<Attachment> GetUserHelpCards(string applicationBasePath)
        {
            return new List<Attachment>()
            {
                GetCarouselCards(Strings.WelcomeCardTitle, Strings.WelcomeCardContent, applicationBasePath + "/Artifacts/applicationLogo.png"),
                GetCarouselCards(Strings.WelcomeCardTitle, Strings.WelcomeCardSuggestText, applicationBasePath + "/Artifacts/applicationLogo.png"),
                GetCarouselCards(Strings.WelcomeCardTitle, Strings.WelcomeCardDiscoverText, applicationBasePath + "/Artifacts/applicationLogo.png"),
            };
        }

        private static Attachment GetCarouselCards(string title, string text, string imageUri)
        {
            HeroCard userHelpCarouselCard = new HeroCard()
            {
                Title = title,
                Text = text,
                Images = new List<CardImage>()
                {
                    new CardImage(imageUri),
                },
            };

            return userHelpCarouselCard.ToAttachment();
        }
    }
}
