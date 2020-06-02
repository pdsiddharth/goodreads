// <copyright file="MessagingExtensionHelper.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.GoodReads.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Web;
    using AdaptiveCards;
    using Microsoft.Bot.Schema;
    using Microsoft.Bot.Schema.Teams;
    using Microsoft.Extensions.Localization;
    using Microsoft.Extensions.Options;
    using Microsoft.Teams.Apps.GoodReads;
    using Microsoft.Teams.Apps.GoodReads.Bot;
    using Microsoft.Teams.Apps.GoodReads.Common;
    using Microsoft.Teams.Apps.GoodReads.Common.Interfaces;
    using Microsoft.Teams.Apps.GoodReads.Models;

    /// <summary>
    /// Class that handles the search activities for Messaging Extension.
    /// </summary>
    public class MessagingExtensionHelper : IMessagingExtensionHelper
    {
        /// <summary>
        /// Search text parameter name in the manifest file.
        /// </summary>
        private const string SearchTextParameterName = "searchText";

        /// <summary>
        /// Instance of Search service for working with Microsoft Azure Table storage.
        /// </summary>
        private readonly ITeamPostSearchService teamPostSearchService;

        /// <summary>
        /// The current cultures' string localizer.
        /// </summary>
        private readonly IStringLocalizer<Strings> localizer;

        /// <summary>
        /// A set of key/value application configuration properties for Activity settings.
        /// </summary>
        private readonly IOptions<GoodReadsActivityHandlerOptions> options;

        /// <summary>
        /// Card post type images pair.
        /// </summary>
        private readonly Dictionary<string, string> cardPostTypePair = new Dictionary<string, string>();

        /// <summary>
        /// Initializes a new instance of the <see cref="MessagingExtensionHelper"/> class.
        /// </summary>
        /// <param name="localizer">The current cultures' string localizer.</param>
        /// <param name="teamPostSearchService">The team post search service dependency injection.</param>
        /// <param name="options">>A set of key/value application configuration properties for activity handler.</param>
        public MessagingExtensionHelper(
            IStringLocalizer<Strings> localizer,
            ITeamPostSearchService teamPostSearchService,
            IOptions<GoodReadsActivityHandlerOptions> options)
        {
            this.localizer = localizer;
            this.teamPostSearchService = teamPostSearchService;
            this.options = options ?? throw new ArgumentNullException(nameof(options));
            this.cardPostTypePair = new Dictionary<string, string>()
            {
                { "1", $"{this.options.Value.AppBaseUri}/Artifacts/blogTypeDot.png" },
                { "2", $"{this.options.Value.AppBaseUri}/Artifacts/otherTypeDot.png" },
                { "3", $"{this.options.Value.AppBaseUri}/Artifacts/podcastTypeDot.png" },
                { "4", $"{this.options.Value.AppBaseUri}/Artifacts/videoTypeDot.png" },
                { "5", $"{this.options.Value.AppBaseUri}/Artifacts/bookTypeDot.png" },
            };
        }

        /// <summary>
        /// Get the results from Azure Search service and populate the result (card + preview).
        /// </summary>
        /// <param name="query">Query which the user had typed in Messaging Extension search field.</param>
        /// <param name="commandId">Command id to determine which tab in Messaging Extension has been invoked.</param>
        /// <param name="userObjectId">Azure Active Directory id of the user.</param>
        /// <param name="count">Number of search results to return.</param>
        /// <param name="skip">Number of search results to skip.</param>
        /// <returns><see cref="Task"/>Returns Messaging Extension result object, which will be used for providing the card.</returns>
        public async Task<MessagingExtensionResult> GetTeamPostSearchResultAsync(
            string query,
            string commandId,
            string userObjectId,
            int? count,
            int? skip)
        {
            MessagingExtensionResult composeExtensionResult = new MessagingExtensionResult
            {
                Type = "result",
                AttachmentLayout = AttachmentLayoutTypes.List,
                Attachments = new List<MessagingExtensionAttachment>(),
            };

            IEnumerable<TeamPostEntity> teamPostResults;

            // commandId should be equal to Id mentioned in Manifest file under composeExtensions section.
            switch (commandId)
            {
                case Constants.AllItemsPostCommandId:
                    teamPostResults = await this.teamPostSearchService.GetSearchTeamPostsAsync(TeamPostSearchScope.AllItems, query, userObjectId, count, skip);
                    composeExtensionResult = this.GetTeamPostResult(teamPostResults);
                    break;

                case Constants.PostedByMePostCommandId:
                    teamPostResults = await this.teamPostSearchService.GetSearchTeamPostsAsync(TeamPostSearchScope.PostedByMe, query, userObjectId, count, skip);
                    composeExtensionResult = this.GetTeamPostResult(teamPostResults);
                    break;

                case Constants.PopularPostCommandId:
                    teamPostResults = await this.teamPostSearchService.GetSearchTeamPostsAsync(TeamPostSearchScope.Popular, query, userObjectId, count, skip);
                    composeExtensionResult = this.GetTeamPostResult(teamPostResults);
                    break;
            }

            return composeExtensionResult;
        }

        /// <summary>
        /// Get the value of the searchText parameter in the Messaging Extension query.
        /// </summary>
        /// <param name="query">Contains Messaging Extension query keywords.</param>
        /// <returns>A value of the searchText parameter.</returns>
        public string GetSearchQueryString(MessagingExtensionQuery query)
        {
            return query?.Parameters.FirstOrDefault(parameter => parameter.Name.Equals(SearchTextParameterName, StringComparison.OrdinalIgnoreCase))?.Value?.ToString();
        }

        /// <summary>
        /// Get team posts result for Messaging Extension.
        /// </summary>
        /// <param name="teamPostResults">List of user search result.</param>
        /// <returns><see cref="Task"/>Returns Messaging Extension result object, which will be used for providing the card.</returns>
        private MessagingExtensionResult GetTeamPostResult(IEnumerable<TeamPostEntity> teamPostResults)
        {
            MessagingExtensionResult composeExtensionResult = new MessagingExtensionResult
            {
                Type = "result",
                AttachmentLayout = AttachmentLayoutTypes.List,
                Attachments = new List<MessagingExtensionAttachment>(),
            };

            if (teamPostResults == null)
            {
                return composeExtensionResult;
            }

            foreach (var teamPost in teamPostResults)
            {
                var card = new AdaptiveCard(new AdaptiveSchemaVersion(1, 2))
                {
                    Body = new List<AdaptiveElement>
                    {
                        new AdaptiveTextBlock
                        {
                            Text = teamPost.Title,
                            Wrap = true,
                            Weight = AdaptiveTextWeight.Bolder,
                        },
                        new AdaptiveTextBlock
                        {
                            Text = teamPost.Description,
                            Wrap = true,
                        },
                    },
                };

                card.Body.Add(this.GetPostTypeContainer(teamPost));
                card.Body.Add(this.GetTagsContainer(teamPost));

                card.Actions.Add(
                    new AdaptiveOpenUrlAction
                    {
                        Title = this.localizer.GetString("OpenItem"),
                        Url = new Uri(teamPost.ContentUrl),
                    });

                var voteIcon = $"<img src='{this.options.Value.AppBaseUri}/Artifacts/userVoteIcon.png' alt='vote logo' width='18' height='18'";
                var nameString = teamPost.CreatedByName.Length < 25 ? HttpUtility.HtmlEncode(teamPost.CreatedByName) :
                    $"{HttpUtility.HtmlEncode(teamPost.CreatedByName.Substring(0, 24))} {"..."}";
                ThumbnailCard previewCard = new ThumbnailCard
                {
                    Title = $"<p style='font-weight: 600;'>{teamPost.Title}</p>",
                    Text = $"{nameString} {"|"} {PostTypeHelper.GetPostType(Convert.ToInt32(teamPost.Type, CultureInfo.InvariantCulture))} {"|"} {teamPost.TotalVotes} {voteIcon}",
                };

                composeExtensionResult.Attachments.Add(new Attachment
                {
                    ContentType = AdaptiveCard.ContentType,
                    Content = card,
                }.ToMessagingExtensionAttachment(previewCard.ToAttachment()));
            }

            return composeExtensionResult;
        }

        /// <summary>
        /// Get container for team post.
        /// </summary>
        /// <param name="teamPost">Team post entity object.</param>
        /// <returns>Return a container for team post.</returns>
        private AdaptiveContainer GetPostTypeContainer(TeamPostEntity teamPost)
        {
            string applicationBasePath = this.options.Value.AppBaseUri;
            this.cardPostTypePair.TryGetValue(teamPost.Type, out string typeIconUrl);

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
                                                    Url = new Uri($"{applicationBasePath}/Artifacts/PeopleAvatar.png"),
                                                    Size = AdaptiveImageSize.Small,
                                                    Style = AdaptiveImageStyle.Person,
                                                    AltText = "User Image",
                                                 },
                                            },
                                        },
                                        new AdaptiveColumn
                                        {
                                            Width = AdaptiveColumnWidth.Stretch,
                                            VerticalContentAlignment = AdaptiveVerticalContentAlignment.Center,
                                            Items = new List<AdaptiveElement>
                                            {
                                                new AdaptiveTextBlock
                                                {
                                                    Text = teamPost.CreatedByName.Length > 19 ? $"{teamPost.CreatedByName.Substring(0, 18)} {"..."}" : teamPost.CreatedByName,
                                                    Wrap = true,
                                                },
                                            },
                                            Spacing = AdaptiveSpacing.Medium,
                                        },
                                        new AdaptiveColumn
                                        {
                                            Width = AdaptiveColumnWidth.Auto,
                                            VerticalContentAlignment = AdaptiveVerticalContentAlignment.Center,
                                            Items = new List<AdaptiveElement>
                                            {
                                                new AdaptiveImage
                                                {
                                                    Url = new Uri(typeIconUrl),
                                                    Size = AdaptiveImageSize.Stretch,
                                                    Style = AdaptiveImageStyle.Default,
                                                    Height = AdaptiveHeight.Auto,
                                                },
                                            },
                                            Spacing = AdaptiveSpacing.Small,
                                        },
                                        new AdaptiveColumn
                                        {
                                            Width = AdaptiveColumnWidth.Stretch,
                                            VerticalContentAlignment = AdaptiveVerticalContentAlignment.Center,
                                            Items = new List<AdaptiveElement>
                                            {
                                                new AdaptiveTextBlock
                                                {
                                                    Text = $"{PostTypeHelper.GetPostType(Convert.ToInt32(teamPost.Type, CultureInfo.InvariantCulture))}",
                                                    Spacing = AdaptiveSpacing.None,
                                                    IsSubtle = true,
                                                    Wrap = true,
                                                    Weight = AdaptiveTextWeight.Bolder,
                                                },
                                            },
                                        },
                                        new AdaptiveColumn
                                        {
                                            Width = AdaptiveColumnWidth.Auto,
                                            VerticalContentAlignment = AdaptiveVerticalContentAlignment.Center,
                                            Items = new List<AdaptiveElement>
                                            {
                                                new AdaptiveTextBlock
                                                {
                                                    Text = $"{teamPost.TotalVotes} ",
                                                    Wrap = true,
                                                },
                                            },
                                            Spacing = AdaptiveSpacing.Small,
                                        },
                                        new AdaptiveColumn
                                        {
                                            Width = AdaptiveColumnWidth.Auto,
                                            VerticalContentAlignment = AdaptiveVerticalContentAlignment.Center,
                                            Items = new List<AdaptiveElement>
                                            {
                                                new AdaptiveImage
                                                {
                                                    Url = new Uri($"{applicationBasePath}/Artifacts/userVoteIcon.png"),
                                                    Size = AdaptiveImageSize.Stretch,
                                                    Style = AdaptiveImageStyle.Default,
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

        /// <summary>
        /// Get tags container for team post.
        /// </summary>
        /// <param name="teamPost">Team post entity object.</param>
        /// <returns>Return a container for team post tags.</returns>
        private AdaptiveContainer GetTagsContainer(TeamPostEntity teamPost)
        {
            var tagsContainer = new AdaptiveContainer
            {
                Items = new List<AdaptiveElement>
                {
                    new AdaptiveColumnSet
                    {
                        Columns = new List<AdaptiveColumn>
                        {
                            new AdaptiveColumn
                            {
                                Width = AdaptiveColumnWidth.Stretch,
                                Items = new List<AdaptiveElement>
                                {
                                    new AdaptiveTextBlock
                                    {
                                        Text = $"{this.localizer.GetString("TagsText")} {teamPost.Tags?.Replace(";", ", ", false, CultureInfo.InvariantCulture)}",
                                        Wrap = true,
                                    },
                                },
                            },
                        },
                    },
                },
            };

            return tagsContainer;
        }
    }
}
