// <copyright file="TeamPostDataRefreshService.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.GoodReads.Common.BackgroundService
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using Microsoft.Teams.Apps.GoodReads.Common.Interfaces;

    /// <summary>
    /// This class inherits IHostedService and implements the methods related to background tasks to
    /// re-create Azure Search Service related resources like: Indexes and Indexer and remove the soft deleted data,
    /// if timer matched(runs two times a day/every 12 Hours).
    /// </summary>
    public class TeamPostDataRefreshService : BackgroundService
    {
        /// <summary>
        /// Instance to send logs to the Application Insights service.
        /// </summary>
        private readonly ILogger<TeamPostDataRefreshService> logger;

        /// <summary>
        /// Instance of Search service for working with Microsoft Azure Table storage.
        /// </summary>
        private readonly ITeamPostSearchService teamPostSearchService;

        /// <summary>
        /// Instance of team post storage provider to update post and get information of posts.
        /// </summary>
        private readonly ITeamPostStorageProvider teamPostStorageProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="TeamPostDataRefreshService"/> class.
        /// BackgroundService class that inherits IHostedService and implements the methods related to re-create Azure Search service related resources like: Indexes and Indexer tasks.
        /// </summary>
        /// <param name="logger">Instance to send logs to the Application Insights service.</param>
        /// <param name="teamPostSearchService">The team post search service dependency injection.</param>
        /// <param name="teamPostStorageProvider">Team post storage provider dependency injection.</param>
        public TeamPostDataRefreshService(
            ILogger<TeamPostDataRefreshService> logger,
            ITeamPostSearchService teamPostSearchService,
            ITeamPostStorageProvider teamPostStorageProvider)
        {
            this.logger = logger;
            this.teamPostSearchService = teamPostSearchService;
            this.teamPostStorageProvider = teamPostStorageProvider;
        }

        /// <summary>
        ///  This method is called when the Microsoft.Extensions.Hosting.IHostedService starts.
        ///  The implementation should return a task that represents the lifetime of the long
        ///  running operation(s) being performed.
        /// </summary>
        /// <param name="stoppingToken">Triggered when Microsoft.Extensions.Hosting.IHostedService.StopAsync(System.Threading.CancellationToken) is called.</param>
        /// <returns>A System.Threading.Tasks.Task that represents the long running operations.</returns>
        protected async override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    this.logger.LogInformation($"Notification Hosted Service is running at: {DateTimeOffset.UtcNow}.");
                    this.logger.LogInformation($"Timer matched to re-create Search service indexes, indexer at: {DateTimeOffset.UtcNow}");

                    // Re-create Search service indexes, indexer and data source.
                    await this.RecreateAzureSearchResourcesAsync();
                }
                catch (Exception ex)
                {
                    this.logger.LogError(ex, $"Error while running the background service to refresh team posts and remove soft deleted posts): {ex.Message}", SeverityLevel.Error);
                }

                await Task.Delay(TimeSpan.FromHours(12), stoppingToken);
            }
        }

        /// <summary>
        /// Method invokes task to re-create the Search service indexes, indexer.
        /// </summary>
        /// <returns>A task that create Search service indexes, indexer.</returns>
        private async Task RecreateAzureSearchResourcesAsync()
        {
            this.logger.LogInformation("Search service indexes, indexer re-creation task queued.");

            IEnumerable<string> teamPostsIds = await this.teamPostStorageProvider.GetTeamPostsIdsAsync(isRemoved: true);

            if (teamPostsIds.Any())
            {
                await this.teamPostStorageProvider.DeleteTeamPostEntitiesAsync(teamPostsIds); // Delete the team post entities.
                await this.teamPostSearchService.RecreateSearchServiceIndexAsync();  // re-create the Search service indexes, indexer.
            }
        }
    }
}
