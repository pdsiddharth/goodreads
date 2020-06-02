// <copyright file="GoodReadsActivityHandler.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.GoodReads.Bot
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.Bot.Builder;
    using Microsoft.Bot.Builder.Teams;
    using Microsoft.Bot.Schema;
    using Microsoft.Bot.Schema.Teams;
    using Microsoft.Extensions.Localization;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Microsoft.Teams.Apps.GoodReads;
    using Microsoft.Teams.Apps.GoodReads.Cards;
    using Microsoft.Teams.Apps.GoodReads.Common;
    using Microsoft.Teams.Apps.GoodReads.Common.Interfaces;
    using Microsoft.Teams.Apps.GoodReads.Models;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// This class is responsible for reacting to incoming events from Microsoft Teams sent from BotFramework.
    /// </summary>
    public sealed class GoodReadsActivityHandler : TeamsActivityHandler
    {
        /// <summary>
        /// Sets the height of the task module.
        /// </summary>
        private const int TaskModuleHeight = 460;

        /// <summary>
        /// Sets the width of the task module.
        /// </summary>
        private const int TaskModuleWidth = 600;

        /// <summary>
        /// Represents the conversation type as personal.
        /// </summary>
        private const string Personal = "PERSONAL";

        /// <summary>
        /// Represents the conversation type as channel.
        /// </summary>
        private const string Channel = "CHANNEL";

        /// <summary>
        /// Represents the close command for task module.
        /// </summary>
        private const string CloseCommand = "close";

        /// <summary>
        /// State management object for maintaining user conversation state.
        /// </summary>
        private readonly BotState userState;

        /// <summary>
        /// A set of key/value application configuration properties for Activity settings.
        /// </summary>
        private readonly IOptions<BotSetting> botOptions;

        /// <summary>
        /// Instance to send logs to the Application Insights service.
        /// </summary>
        private readonly ILogger<GoodReadsActivityHandler> logger;

        /// <summary>
        /// The current cultures' string localizer.
        /// </summary>
        private readonly IStringLocalizer<Strings> localizer;

        /// <summary>
        /// Instance of Application Insights Telemetry client.
        /// </summary>
        private readonly TelemetryClient telemetryClient;

        /// <summary>
        /// Messaging Extension search helper for working with team posts data in Microsoft Azure Table storage.
        /// </summary>
        private readonly IMessagingExtensionHelper messagingExtensionHelper;

        /// <summary>
        /// Instance to work with user team membership data.
        /// </summary>
        private readonly IUserTeamMembershipProvider userTeamMembershipProvider;

        /// <summary>
        /// Instance of team preference storage helper.
        /// </summary>
        private readonly ITeamPreferenceStorageHelper teamPreferenceStorageHelper;

        /// <summary>
        /// Instance of team preference storage provider for team preferences.
        /// </summary>
        private readonly ITeamPreferenceStorageProvider teamPreferenceStorageProvider;

        /// <summary>
        /// Instance of team tags storage provider to configure team tags.
        /// </summary>
        private readonly ITeamTagStorageProvider teamTagStorageProvider;

        /// <summary>
        /// Represents the Application base Uri.
        /// </summary>
        private readonly string appBaseUri;

        /// <summary>
        /// Entity id of the static discover tab.
        /// </summary>
        private readonly string discoverTabEntityId;

        /// <summary>
        /// Initializes a new instance of the <see cref="GoodReadsActivityHandler"/> class.
        /// </summary>
        /// <param name="logger">Instance to send logs to the Application Insights service.</param>
        /// <param name="localizer">The current cultures' string localizer.</param>
        /// <param name="telemetryClient">The Application Insights telemetry client.</param>
        /// <param name="options">>A set of key/value application configuration properties for activity handler.</param>
        /// <param name="messagingExtensionHelper">Messaging Extension helper dependency injection.</param>
        /// <param name="userState">State management object for maintaining user conversation state.</param>
        /// <param name="userTeamMembershipProvider">Provider instance to work with user team membership data.</param>
        /// <param name="teamPreferenceStorageHelper">Team preference storage helper dependency injection.</param>
        /// <param name="teamPreferenceStorageProvider">Team preference storage provider dependency injection.</param>
        /// <param name="teamTagStorageProvider">Team tags storage provider dependency injection.</param>
        /// <param name="botOptions">A set of key/value application configuration properties for activity handler.</param>
        public GoodReadsActivityHandler(
            ILogger<GoodReadsActivityHandler> logger,
            IStringLocalizer<Strings> localizer,
            TelemetryClient telemetryClient,
            IOptions<GoodReadsActivityHandlerOptions> options,
            IMessagingExtensionHelper messagingExtensionHelper,
            UserState userState,
            IUserTeamMembershipProvider userTeamMembershipProvider,
            ITeamPreferenceStorageHelper teamPreferenceStorageHelper,
            ITeamPreferenceStorageProvider teamPreferenceStorageProvider,
            ITeamTagStorageProvider teamTagStorageProvider,
            IOptions<BotSetting> botOptions)
        {
            this.logger = logger;
            this.localizer = localizer;
            this.telemetryClient = telemetryClient;
            options = options ?? throw new ArgumentNullException(nameof(options));
            this.messagingExtensionHelper = messagingExtensionHelper;
            this.userState = userState;
            this.userTeamMembershipProvider = userTeamMembershipProvider;
            this.teamPreferenceStorageHelper = teamPreferenceStorageHelper;
            this.teamPreferenceStorageProvider = teamPreferenceStorageProvider;
            this.teamTagStorageProvider = teamTagStorageProvider;
            this.botOptions = botOptions ?? throw new ArgumentNullException(nameof(botOptions));
            this.appBaseUri = options.Value.AppBaseUri;
            this.discoverTabEntityId = options.Value.DiscoverTabEntityId;
        }

        /// <summary>
        /// Handles an incoming activity.
        /// </summary>
        /// <param name="turnContext">Context object containing information cached for a single turn of conversation with a user.</param>
        /// <param name="cancellationToken">Propagates notification that operations should be canceled.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        /// <remarks>
        /// Reference link: https://docs.microsoft.com/en-us/dotnet/api/microsoft.bot.builder.activityhandler.onturnasync?view=botbuilder-dotnet-stable.
        /// </remarks>
        public override Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default)
        {
            try
            {
                turnContext = turnContext ?? throw new ArgumentNullException(nameof(turnContext));
                this.RecordEvent(nameof(this.OnTurnAsync), turnContext);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, $"Error at OnTurnAsync(): {ex.Message}", SeverityLevel.Error);
            }

            return base.OnTurnAsync(turnContext, cancellationToken);
        }

        /// <summary>
        /// Invoked when members other than this bot (like a user) are removed from the conversation.
        /// </summary>
        /// <param name="turnContext">Context object containing information cached for a single turn of conversation with a user.</param>
        /// <param name="cancellationToken">Propagates notification that operations should be canceled.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        protected override async Task OnConversationUpdateActivityAsync(ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            turnContext = turnContext ?? throw new ArgumentNullException(nameof(turnContext));

            try
            {
                this.RecordEvent(nameof(this.OnConversationUpdateActivityAsync), turnContext);

                var activity = turnContext.Activity;
                this.logger.LogInformation($"conversationType: {activity.Conversation.ConversationType}, membersAdded: {activity.MembersAdded?.Count}, membersRemoved: {activity.MembersRemoved?.Count}");

                if (activity.Conversation.ConversationType.Equals(Personal, StringComparison.OrdinalIgnoreCase))
                {
                    if (activity.MembersAdded != null && activity.MembersAdded.Any(member => member.Id != activity.Recipient.Id))
                    {
                        await this.HandleMemberAddedinPersonalScopeAsync(turnContext);
                    }
                    else if (activity.MembersRemoved != null && activity.MembersRemoved.Any(member => member.Id != activity.Recipient.Id))
                    {
                        await this.HandleMemberRemovedinPersonalScopeAsync(turnContext);
                    }
                }
                else if (activity.Conversation.ConversationType.Equals(Channel, StringComparison.OrdinalIgnoreCase))
                {
                    if (activity.MembersAdded != null && activity.MembersAdded.Any(member => member.Id == activity.Recipient.Id))
                    {
                        await this.HandleMemberAddedInTeamAsync(turnContext);
                    }
                    else if (activity.MembersRemoved != null && activity.MembersRemoved.Any(member => member.Id == activity.Recipient.Id))
                    {
                        await this.HandleMemberRemovedInTeamScopeAsync(turnContext);
                    }
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Exception occurred while bot conversation update event.");
                throw;
            }
        }

        /// <summary>
        /// Invoked when the user opens the Messaging Extension or searching any content in it.
        /// </summary>
        /// <param name="turnContext">Context object containing information cached for a single turn of conversation with a user.</param>
        /// <param name="query">Contains Messaging Extension query keywords.</param>
        /// <param name="cancellationToken">Propagates notification that operations should be canceled.</param>
        /// <returns>Messaging extension response object to fill compose extension section.</returns>
        /// <remarks>
        /// https://docs.microsoft.com/en-us/dotnet/api/microsoft.bot.builder.teams.teamsactivityhandler.onteamsmessagingextensionqueryasync?view=botbuilder-dotnet-stable.
        /// </remarks>
        protected override async Task<MessagingExtensionResponse> OnTeamsMessagingExtensionQueryAsync(
            ITurnContext<IInvokeActivity> turnContext,
            MessagingExtensionQuery query,
            CancellationToken cancellationToken)
        {
            turnContext = turnContext ?? throw new ArgumentNullException(nameof(turnContext));
            this.RecordEvent(nameof(this.OnTeamsMessagingExtensionQueryAsync), turnContext);

            var activity = turnContext.Activity;

            try
            {
                var messagingExtensionQuery = JsonConvert.DeserializeObject<MessagingExtensionQuery>(activity.Value.ToString());
                var searchQuery = this.messagingExtensionHelper.GetSearchQueryString(messagingExtensionQuery);

                return new MessagingExtensionResponse
                {
                    ComposeExtension = await this.messagingExtensionHelper.GetTeamPostSearchResultAsync(searchQuery, messagingExtensionQuery.CommandId, activity.From.AadObjectId, messagingExtensionQuery.QueryOptions.Count, messagingExtensionQuery.QueryOptions.Skip),
                };
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, $"Failed to handle the Messaging Extension command {activity.Name}: {ex.Message}", SeverityLevel.Error);
                throw;
            }
        }

        /// <summary>
        /// Invoked when task module fetch event is received from the bot.
        /// </summary>
        /// <param name="turnContext">Context object containing information cached for a single turn of conversation with a user.</param>
        /// <param name="taskModuleRequest">Task module invoke request value payload.</param>
        /// <param name="cancellationToken">Propagates notification that operations should be canceled.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        protected override async Task<TaskModuleResponse> OnTeamsTaskModuleFetchAsync(ITurnContext<IInvokeActivity> turnContext, TaskModuleRequest taskModuleRequest, CancellationToken cancellationToken)
        {
            try
            {
                turnContext = turnContext ?? throw new ArgumentNullException(nameof(turnContext));
                taskModuleRequest = taskModuleRequest ?? throw new ArgumentNullException(nameof(taskModuleRequest));

                this.RecordEvent(nameof(this.OnTeamsTaskModuleFetchAsync), turnContext);

                var activity = turnContext.Activity;
                if (taskModuleRequest.Data == null)
                {
                    this.telemetryClient.TrackTrace("Request data obtained on task module fetch action is null.");
                    await turnContext.SendActivityAsync(this.localizer.GetString("WelcomeCardContent")).ConfigureAwait(false);
                    return default;
                }

                var postedValues = JsonConvert.DeserializeObject<GoodReadsViewModel>(JObject.Parse(taskModuleRequest.Data.ToString()).SelectToken("data").ToString());
                var command = postedValues.Text;

                switch (command.ToUpperInvariant())
                {
                    case Constants.Preferences: // Preference command to set the tags in a team.
                        return this.GetTaskModuleResponse();
                    default:
                        return default;
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error while fetching task module received by the bot.");
                throw;
            }
        }

        /// <summary>
        /// Invoked when a message activity is received from the bot.
        /// </summary>
        /// <param name="turnContext">Context object containing information cached for a single turn of conversation with a user.</param>
        /// <param name="cancellationToken">Propagates notification that operations should be canceled.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            try
            {
                turnContext = turnContext ?? throw new ArgumentNullException(nameof(turnContext));
                var message = turnContext.Activity;
                var command = message?.RemoveRecipientMention()?.Trim();

                switch (command?.ToUpperInvariant())
                {
                    case Constants.HelpCommand: // Help command to get the information about the bot.
                        this.logger.LogInformation("Sending user help card");
                        var userHelpCards = GetCarouselCard.GetUserHelpCards(this.appBaseUri);
                        await turnContext.SendActivityAsync(MessageFactory.Carousel(userHelpCards)).ConfigureAwait(false);
                        break;
                    case Constants.Preferences: // Preference command to get the card to setup the tags preference of a team.
                        await turnContext.SendActivityAsync(MessageFactory.Attachment(WelcomeCard.GetPreferenceCard(localizer: this.localizer)), cancellationToken).ConfigureAwait(false);
                        break;
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error while message activity is received from the bot.");
                throw;
            }
        }

        /// <summary>
        /// When OnTurn method receives a submit invoke activity on bot turn, it calls this method.
        /// </summary>
        /// <param name="turnContext">Provides context for a turn of a bot.</param>
        /// <param name="taskModuleRequest">Task module invoke request value payload.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>A task that represents a task module response.</returns>
        protected override async Task<TaskModuleResponse> OnTeamsTaskModuleSubmitAsync(ITurnContext<IInvokeActivity> turnContext, TaskModuleRequest taskModuleRequest, CancellationToken cancellationToken)
        {
            try
            {
                turnContext = turnContext ?? throw new ArgumentNullException(nameof(turnContext));
                taskModuleRequest = taskModuleRequest ?? throw new ArgumentNullException(nameof(taskModuleRequest));

                var valuesFromTaskModule = JsonConvert.DeserializeObject<SubmitPreferencesEntity>(taskModuleRequest.Data?.ToString());

                if (valuesFromTaskModule == null)
                {
                    this.logger.LogInformation($"Request data obtained on task module submit action is null.");
                    await turnContext.SendActivityAsync(Strings.ErrorMessage).ConfigureAwait(false);
                    return default;
                }

                if (valuesFromTaskModule.Command == CloseCommand)
                {
                    return default;
                }
                else
                {
                    var teamPreferenceDetail = this.teamPreferenceStorageHelper.GetTeamPreferenceModel(valuesFromTaskModule.ConfigureDetails);
                    await this.teamPreferenceStorageProvider.UpsertTeamPreferenceAsync(teamPreferenceDetail);
                }

                return default;
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error in submit action of task module.");
                return default;
            }
        }

        /// <summary>
        /// Get task module response object.
        /// </summary>
        /// <returns>TaskModuleResponse object.</returns>
        private TaskModuleResponse GetTaskModuleResponse()
        {
            string url = $"{this.appBaseUri}/configurepreferences";

            return new TaskModuleResponse
            {
                Task = new TaskModuleContinueResponse
                {
                    Type = "continue",
                    Value = new TaskModuleTaskInfo()
                    {
                        Url = url,
                        Height = TaskModuleHeight,
                        Width = TaskModuleWidth,
                        Title = this.localizer.GetString("TaskModuleTitleText"),
                    },
                },
            };
        }

        /// <summary>
        /// Records event data to Application Insights telemetry client
        /// </summary>
        /// <param name="eventName">Name of the event.</param>
        /// <param name="turnContext">Provides context for a turn in a bot.</param>
        private void RecordEvent(string eventName, ITurnContext turnContext)
        {
            this.telemetryClient.TrackEvent(eventName, new Dictionary<string, string>
            {
                { "userId", turnContext.Activity.From.AadObjectId },
                { "tenantId", turnContext.Activity.Conversation.TenantId },
            });
        }

        private async Task<IEnumerable<TeamsChannelAccount>> GetTeamMembersAsync(ITurnContext<IConversationUpdateActivity> turnContext)
        {
            var teamInfo = turnContext.Activity.TeamsGetTeamInfo();
            var teamId = teamInfo.Id;
            var teamsChannelAccounts = await TeamsInfo.GetTeamMembersAsync(turnContext, teamId);

            return teamsChannelAccounts;
        }

        /// <summary>
        /// Get Azure Active Directory id of user.
        /// </summary>
        /// <param name="channelAccount">Channel account object.</param>
        /// <returns>Azure Active Directory id of user.</returns>
        private string GetUserAadObjectId(ChannelAccount channelAccount)
        {
            if (!string.IsNullOrWhiteSpace(channelAccount.AadObjectId))
            {
                return channelAccount.AadObjectId;
            }

            return channelAccount.Properties["objectId"].ToString();
        }

        /// <summary>
        /// Sent welcome card to personal chat.
        /// </summary>
        /// <param name="turnContext">Provides context for a turn in a bot.</param>
        /// <returns>A task that represents a response.</returns>
        private async Task HandleMemberAddedinPersonalScopeAsync(ITurnContext<IConversationUpdateActivity> turnContext)
        {
            this.logger.LogInformation($"Bot added in personal {turnContext.Activity.Conversation.Id}");
            var userStateAccessors = this.userState.CreateProperty<UserConversationState>(nameof(UserConversationState));
            var userConversationState = await userStateAccessors.GetAsync(turnContext, () => new UserConversationState());

            userConversationState = userConversationState ?? throw new NullReferenceException(nameof(userConversationState));

            if (userConversationState.IsWelcomeCardSent == null || userConversationState.IsWelcomeCardSent == false)
            {
                userConversationState.IsWelcomeCardSent = true;
                await userStateAccessors.SetAsync(turnContext, userConversationState);

                var userWelcomeCardAttachment = WelcomeCard.GetWelcomeCardAttachmentForPersonal(
                    this.appBaseUri,
                    localizer: this.localizer,
                    this.botOptions.Value.ManifestId,
                    this.discoverTabEntityId);

                await turnContext.SendActivityAsync(MessageFactory.Attachment(userWelcomeCardAttachment));
            }
        }

        /// <summary>
        /// Set user conversation state to  new if bot is removed from personal scope.
        /// </summary>
        /// <param name="turnContext">Provides context for a turn in a bot.</param>
        /// <returns>>A task that represents a response.</returns>
        private async Task HandleMemberRemovedinPersonalScopeAsync(ITurnContext<IConversationUpdateActivity> turnContext)
        {
            this.logger.LogInformation($"Bot removed from personal {turnContext.Activity.Conversation.Id}");
            var userStateAccessors = this.userState.CreateProperty<UserConversationState>(nameof(UserConversationState));
            var userdata = await userStateAccessors.GetAsync(turnContext, () => new UserConversationState());
            userdata.IsWelcomeCardSent = false;
            await userStateAccessors.SetAsync(turnContext, userdata).ConfigureAwait(false);
        }

        /// <summary>
        /// Add user membership to storage if bot is installed in Team scope.
        /// </summary>
        /// <param name="turnContext">Provides context for a turn in a bot.</param>
        /// <returns>A task that represents a response.</returns>
        private async Task HandleMemberAddedInTeamAsync(ITurnContext<IConversationUpdateActivity> turnContext)
        {
            this.logger.LogInformation($"Bot added in team {turnContext.Activity.Conversation.Id}");
            var teamMembers = await this.GetTeamMembersAsync(turnContext);
            var channelData = turnContext.Activity.GetChannelData<TeamsChannelData>();
            var userWelcomeCardAttachment = WelcomeCard.GetWelcomeCardAttachmentForTeam(this.appBaseUri, localizer: this.localizer);
            await turnContext.SendActivityAsync(MessageFactory.Attachment(userWelcomeCardAttachment));

            foreach (var teamMember in teamMembers)
            {
                var userAadObjectId = this.GetUserAadObjectId(teamMember);
                await this.userTeamMembershipProvider.AddUserTeamMembershipAsync(channelData.Team.Id, userAadObjectId, new Uri(turnContext.Activity.ServiceUrl));
            }
        }

        /// <summary>
        /// Remove user membership from storage if bot is uninstalled from Team scope.
        /// </summary>
        /// <param name="turnContext">Provides context for a turn in a bot.</param>
        /// <returns>A task that represents a response.</returns>
        private async Task HandleMemberRemovedInTeamScopeAsync(ITurnContext<IConversationUpdateActivity> turnContext)
        {
            this.logger.LogInformation($"Bot removed from team {turnContext.Activity.Conversation.Id}");
            var teamsChannelData = turnContext.Activity.GetChannelData<TeamsChannelData>();
            var teamId = teamsChannelData.Team.Id;
            await this.userTeamMembershipProvider.DeleteUserTeamMembershipByTeamIdAsync(teamId);
            await this.teamTagStorageProvider.DeleteTeamTagsEntryDataAsync(teamId);
        }
    }
}