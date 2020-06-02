// <copyright file="DigestNotificationHelper.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.GoodReads.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Bot.Builder;
    using Microsoft.Bot.Builder.Integration.AspNet.Core;
    using Microsoft.Bot.Connector.Authentication;
    using Microsoft.Bot.Schema;
    using Microsoft.Extensions.Localization;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Microsoft.Teams.Apps.GoodReads.Bot;
    using Microsoft.Teams.Apps.GoodReads.Cards;
    using Microsoft.Teams.Apps.GoodReads.Common.Interfaces;
    using Microsoft.Teams.Apps.GoodReads.Models;
    using Polly;
    using Polly.Contrib.WaitAndRetry;
    using Polly.Retry;

    /// <summary>
    /// Class handles sending notification to channels.
    /// </summary>
    public class DigestNotificationHelper : IDigestNotificationHelper
    {
        /// <summary>
        /// default value for channel activity to send notifications.
        /// </summary>
        private const string Channel = "msteams";

        /// <summary>
        /// Channel conversation type to send notification.
        /// </summary>
        private const string ChannelConversationType = "channel";

        /// <summary>
        /// Weekly digest for checking the digest notification type.
        /// </summary>
        private const string WeeklyDigest = "Weekly";

        /// <summary>
        /// Retry policy with jitter.
        /// </summary>
        /// <remarks>
        /// Reference: https://github.com/Polly-Contrib/Polly.Contrib.WaitAndRetry#new-jitter-recommendation.
        /// </remarks>
        private readonly AsyncRetryPolicy retryPolicy;

        /// <summary>
        /// Helper for storing channel details to azure table storage for sending notification.
        /// </summary>
        private readonly ITeamPreferenceStorageProvider teamPreferenceStorageProvider;

        /// <summary>
        /// Sends logs to the Application Insights service.
        /// </summary>
        private readonly ILogger<DigestNotificationHelper> logger;

        /// <summary>
        /// The current cultures' string localizer.
        /// </summary>
        private readonly IStringLocalizer<Strings> localizer;

        /// <summary>
        /// Bot adapter.
        /// </summary>
        private readonly IBotFrameworkHttpAdapter adapter;

        /// <summary>
        /// Tenant id.
        /// </summary>
        private readonly string tenantId;

        /// <summary>
        /// Represents a set of key/value application configuration properties for Good reads bot.
        /// </summary>
        private readonly IOptions<BotSetting> botOptions;

        /// <summary>
        /// A set of key/value application configuration properties for Activity settings.
        /// </summary>
        private readonly IOptions<GoodReadsActivityHandlerOptions> options;

        /// <summary>
        /// Instance of Search service for working with Microsoft Azure Table storage.
        /// </summary>
        private readonly ITeamPostSearchService teamPostSearchService;

        /// <summary>
        /// Instance of team post storage helper to update post and get information of posts.
        /// </summary>
        private readonly ITeamPostStorageHelper teamPostStorageHelper;

        /// <summary>
        /// Instance of user team membership provider to get information.
        /// </summary>
        private readonly IUserTeamMembershipProvider userTeamMembershipProvider;

        /// <summary>
        /// Card post type images pair.
        /// </summary>
        private readonly Dictionary<int, string> cardPostTypePair = new Dictionary<int, string>();

        /// <summary>
        /// Microsoft Application ID.
        /// </summary>
        private readonly string appId;

        /// <summary>
        /// Initializes a new instance of the <see cref="DigestNotificationHelper"/> class.
        /// </summary>
        /// <param name="logger">Instance to send logs to the Application Insights service.</param>
        /// <param name="localizer">The current cultures' string localizer.</param>
        /// <param name="botOptions">A set of key/value application configuration properties for Good reads bot.</param>
        /// <param name="adapter">Bot adapter.</param>
        /// <param name="microsoftAppCredentials">MicrosoftAppCredentials of bot.</param>
        /// <param name="teamPreferenceStorageProvider">Storage provider for team preference.</param>
        /// <param name="teamPostSearchService">The team post search service dependency injection.</param>
        /// <param name="teamPostStorageHelper">Team post storage helper dependency injection.</param>
        /// <param name="userTeamMembershipProvider">User team membership storage provider dependency injection.</param>
        /// <param name="options">A set of key/value application configuration properties.</param>
        public DigestNotificationHelper(
            ILogger<DigestNotificationHelper> logger,
            IStringLocalizer<Strings> localizer,
            IOptions<BotSetting> botOptions,
            IBotFrameworkHttpAdapter adapter,
            MicrosoftAppCredentials microsoftAppCredentials,
            ITeamPreferenceStorageProvider teamPreferenceStorageProvider,
            ITeamPostSearchService teamPostSearchService,
            ITeamPostStorageHelper teamPostStorageHelper,
            IUserTeamMembershipProvider userTeamMembershipProvider,
            IOptions<GoodReadsActivityHandlerOptions> options)
        {
            this.logger = logger;
            this.localizer = localizer;
            this.botOptions = botOptions ?? throw new ArgumentNullException(nameof(botOptions));
            this.adapter = adapter;
            this.appId = microsoftAppCredentials != null ? microsoftAppCredentials.MicrosoftAppId : throw new ArgumentNullException(nameof(microsoftAppCredentials));
            this.teamPreferenceStorageProvider = teamPreferenceStorageProvider;
            this.tenantId = this.botOptions.Value.TenantId;
            this.teamPostSearchService = teamPostSearchService;
            this.teamPostStorageHelper = teamPostStorageHelper;
            this.userTeamMembershipProvider = userTeamMembershipProvider;
            this.cardPostTypePair = this.InitializePostTypeImages(this.botOptions.Value.AppBaseUri);
            this.options = options;
            this.retryPolicy = Policy.Handle<Exception>()
                .WaitAndRetryAsync(Backoff.DecorrelatedJitterBackoffV2(TimeSpan.FromMilliseconds(this.botOptions.Value.MedianFirstRetryDelay), this.botOptions.Value.RetryCount));
        }

        /// <summary>
        /// Send notification in channels on weekly or monthly basis as per the configured preference in different channels.
        /// </summary>
        /// <param name="fromDate">Start date from which data should fetch.</param>
        /// <param name="toDate">End date till when data should fetch.</param>
        /// <param name="digestFrequency">Digest frequency text for notification like Monthly/Weekly.</param>
        /// <returns>A task that sends notification in channel.</returns>
        public async Task SendNotificationInChannelAsync(DateTime fromDate, DateTime toDate, string digestFrequency)
        {
            this.logger.LogInformation($"Send notification Timer trigger function executed at: {DateTime.UtcNow}");
            try
            {
                digestFrequency = digestFrequency ?? throw new ArgumentNullException(nameof(digestFrequency));

                var teamPosts = await this.teamPostSearchService.GetSearchTeamPostsAsync(TeamPostSearchScope.FilterPostsAsPerDateRange, searchQuery: null, userObjectId: null);
                var filteredTeamPosts = this.teamPostStorageHelper.GetTeamPostsInDateRangeAsync(teamPosts, fromDate, toDate);

                if (filteredTeamPosts.Any())
                {
                    var teamPreferences = await this.teamPreferenceStorageProvider.GetTeamPreferencesAsync(digestFrequency);
                    var notificationCardTitle = digestFrequency.Equals(WeeklyDigest, StringComparison.InvariantCultureIgnoreCase)
                        ? this.localizer.GetString("NotificationCardWeeklyTitleText")
                        : this.localizer.GetString("NotificationCardMonthlyTitleText");

                    foreach (var teamPreference in teamPreferences)
                    {
                        var tagsFilteredData = this.GetDataAsPerTagsAsync(teamPreference, filteredTeamPosts);

                        if (tagsFilteredData.Any())
                        {
                            var notificationCard = DigestNotificationListCard.GetNotificationListCard(
                                tagsFilteredData,
                                this.localizer,
                                notificationCardTitle,
                                this.cardPostTypePair,
                                this.botOptions.Value.ManifestId,
                                this.options.Value.DiscoverTabEntityId,
                                this.options.Value.AppBaseUri);

                            var userTeamMembershipEntity = await this.userTeamMembershipProvider.GetUserTeamMembershipDataAsync(teamPreference.TeamId);
                            if (userTeamMembershipEntity != null)
                            {
                                await this.SendCardToTeamAsync(teamPreference, notificationCard, userTeamMembershipEntity.ServiceUrl);
                            }
                        }
                    }
                }
                else
                {
                    this.logger.LogInformation($"There is no digest data available to send at this time range from: {0} till {1}", fromDate, toDate);
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error while sending digest notifications.");
            }
        }

        /// <summary>
        /// Send the given attachment to the specified team.
        /// </summary>
        /// <param name="teamPreferenceEntity">Team preference model object.</param>
        /// <param name="cardToSend">The attachment card to send.</param>
        /// <param name="serviceUrl">Service url for a particular team.</param>
        /// <returns>A task that sends notification card in channel.</returns>
        private async Task SendCardToTeamAsync(
            TeamPreferenceEntity teamPreferenceEntity,
            Attachment cardToSend,
            string serviceUrl)
        {
            try
            {
                MicrosoftAppCredentials.TrustServiceUrl(serviceUrl);
                string teamsChannelId = teamPreferenceEntity.TeamId;

                var conversationReference = new ConversationReference()
                {
                    ChannelId = Channel,
                    Bot = new ChannelAccount() { Id = $"28:{this.appId}" },
                    ServiceUrl = serviceUrl,
                    Conversation = new ConversationAccount() { ConversationType = ChannelConversationType, IsGroup = true, Id = teamsChannelId, TenantId = this.tenantId },
                };

                this.logger.LogInformation($"sending notification to channelId- {teamsChannelId}");

                await this.retryPolicy.ExecuteAsync(async () =>
                {
                    try
                    {
                        await ((BotFrameworkAdapter)this.adapter).ContinueConversationAsync(
                        this.appId,
                        conversationReference,
                        async (conversationTurnContext, conversationCancellationToken) =>
                        {
                            await conversationTurnContext.SendActivityAsync(MessageFactory.Attachment(cardToSend));
                        },
                        CancellationToken.None);
                    }
                    catch (Exception ex)
                    {
                        this.logger.LogError(ex, "Error while performing retry logic to send digest notification to channel.");
                        throw;
                    }
                });
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error while sending digest notification to channel from background service.");
            }
        }

        /// <summary>
        /// Get team posts as per configured tags for preference.
        /// </summary>
        /// <param name="teamPreferenceEntity">Team preference model object.</param>
        /// <param name="teamPosts">List of team posts.</param>
        /// <returns>List of team posts as per preference tags.</returns>
        private IEnumerable<TeamPostEntity> GetDataAsPerTagsAsync(
            TeamPreferenceEntity teamPreferenceEntity,
            IEnumerable<TeamPostEntity> teamPosts)
        {
            try
            {
                var filteredPosts = new List<TeamPostEntity>();
                var preferenceTagList = teamPreferenceEntity.Tags.Split(";").Where(tag => !string.IsNullOrWhiteSpace(tag)).ToList();
                bool isTagMatched = false;
                teamPosts = teamPosts.OrderByDescending(c => c.UpdatedDate);

                // Loop through the list of filtered posts.
                foreach (var teamPost in teamPosts)
                {
                    // Split the comma separated post tags.
                    var postTags = teamPost.Tags.Split(";").Where(tag => !string.IsNullOrWhiteSpace(tag)).ToList();
                    isTagMatched = false;

                    // Loop through the list of preference tags.
                    foreach (var preferenceTag in preferenceTagList)
                    {
                        // Loop through the post tags.
                        foreach (var postTag in postTags)
                        {
                            // Check if the post tag and preference tag is same.
                            if (postTag.Trim().Equals(preferenceTag.Trim(), StringComparison.InvariantCultureIgnoreCase))
                            {
                                // Set the flag to check the preference tag is present in post tag.
                                isTagMatched = true;
                                break; // break the loop to check for next preference tag with post tag.
                            }
                        }

                        if (isTagMatched && filteredPosts.Count < 15)
                        {
                            // If preference tag is present in post tag then add it in the list.
                            filteredPosts.Add(teamPost);
                            break; // break the inner loop to check for next post.
                        }
                    }

                    // Break the entire loop after getting top 15 posts.
                    if (filteredPosts.Count >= 15)
                    {
                        break;
                    }
                }

                return filteredPosts.Take(15);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error while filtering the team posts as per the configured preference tags.");
                throw;
            }
        }

        /// <summary>
        /// Initialize post type images path.
        /// </summary>
        /// <param name="applicationBasePath">Application base url.</param>
        /// <returns>A dictionary to represent the post type images collection.</returns>
        private Dictionary<int, string> InitializePostTypeImages(string applicationBasePath)
        {
            var cardPostTypePair = new Dictionary<int, string>()
            {
                { 1, $"{applicationBasePath}/Artifacts/blogIcon.png" },
                { 2, $"{applicationBasePath}/Artifacts/otherIcon.png" },
                { 3, $"{applicationBasePath}/Artifacts/podcastIcon.png" },
                { 4, $"{applicationBasePath}/Artifacts/videoIcon.png" },
                { 5, $"{applicationBasePath}/Artifacts/bookIcon.png" },
            };

            return cardPostTypePair;
        }
    }
}
