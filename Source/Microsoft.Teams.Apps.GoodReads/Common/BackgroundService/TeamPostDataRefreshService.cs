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
    using Cronos;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using Microsoft.Teams.Apps.GoodReads.Common.Interfaces;

    /// <summary>
    /// This class inherits IHostedService and implements the methods related to background tasks to re-create Azure Search Service related resources like: Indexes and Indexer if timer matched(runs two times a day/every 12 Hours).
    /// </summary>
    public class TeamPostDataRefreshService : IHostedService, IDisposable
    {
        /// <summary>
        /// Instance of cron expression to holds the time expression value.
        /// </summary>
        private readonly CronExpression expression;

        /// <summary>
        /// Instance of time zone which holds the time zone information.
        /// </summary>
        private readonly TimeZoneInfo timeZoneInfo;

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
        /// Instance of Timer for executing the service at particular interval.
        /// </summary>
        private System.Timers.Timer timer;

        /// <summary>
        /// Counter for number of times the service is executing.
        /// </summary>
        private int executionCount = 0;

        /// <summary>
        /// Flag to check whether dispose is already called or not.
        /// </summary>
        private bool disposed = false;

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
            this.expression = CronExpression.Parse("* 10/22 * * *"); // Runs two times a day/every 12 Hours.
            this.timeZoneInfo = TimeZoneInfo.Utc;
        }

        /// <summary>
        /// Method to start the background task when application starts.
        /// </summary>
        /// <param name="cancellationToken">Signals cancellation to the executing method.</param>
        /// <returns>A task that Enqueue re-create Azure Search service resources task.</returns>
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                this.logger.LogInformation("Search service indexes, indexer re-creation Hosted Service is running.");
                await this.ScheduleAzureSearchResourcesCreationAsync();
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, $"Error while running the background service to refresh the data for team posts): {ex.Message}", SeverityLevel.Error);
                throw;
            }
        }

        /// <summary>
        /// Triggered when the host is performing a graceful shutdown.
        /// </summary>
        /// <param name="cancellationToken">Signals cancellation to the executing method.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        public async Task StopAsync(CancellationToken cancellationToken)
        {
            this.logger.LogInformation("Search service indexes, indexer re-creation Hosted Service is stopping.");
            await Task.CompletedTask;
        }

        /// <summary>
        /// This code added to correctly implement the disposable pattern.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Protected implementation of Dispose pattern.
        /// </summary>
        /// <param name="disposing">True if already disposed else false.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (this.disposed)
            {
                return;
            }

            if (disposing)
            {
                this.timer.Dispose();
            }

            this.disposed = true;
        }

        /// <summary>
        /// Set the timer and enqueue to re-create Azure Search service related resources like: Indexes and Indexer if timer matched.
        /// </summary>
        /// <returns>A task that Enqueue re-create Azure Search service resources task.</returns>
        private async Task ScheduleAzureSearchResourcesCreationAsync()
        {
            var count = Interlocked.Increment(ref this.executionCount);
            this.logger.LogInformation("Search service indexes, indexer re-creation Hosted Service is working. Count: {Count}", count);

            var next = this.expression.GetNextOccurrence(DateTimeOffset.Now, this.timeZoneInfo);
            if (next.HasValue)
            {
                var delay = next.Value - DateTimeOffset.Now;
                this.timer = new System.Timers.Timer(delay.TotalMilliseconds);
                this.timer.Elapsed += async (sender, args) =>
                {
                    this.logger.LogInformation($"Timer matched to re-create Search service indexes, indexer at timer value : {this.timer}");
                    this.timer.Stop();  // reset timer

                    try
                    {
                        // Re-create Search service indexes, indexer task.
                        await this.RecreateAzureSearchResourcesAsync();
                    }
                    catch (Exception ex)
                    {
                        this.logger.LogError(ex, $"Error while refreshing the team posts at {next}.");
                    }

                    await this.ScheduleAzureSearchResourcesCreationAsync();
                };

                this.timer.Start();
            }
        }

        /// <summary>
        /// Method invokes task to re-create the Search service indexes, indexer.
        /// </summary>
        /// <returns>A task that create Search service indexes, indexer.</returns>
        private async Task RecreateAzureSearchResourcesAsync()
        {
            this.logger.LogInformation("Search service indexes, indexer re-creation task queued.");

            IEnumerable<string> teamPostsIds = this.teamPostStorageProvider.GetTeamPostsIdsAsync(isRemoved: true).GetAwaiter().GetResult();

            if (teamPostsIds.Any())
            {
                await this.teamPostStorageProvider.DeleteTeamPostEntitiesAsync(teamPostsIds); // Delete the team post entities.
                await this.teamPostSearchService.InitializeSearchServiceIndexAsync();  // re-create the Search service indexes, indexer.
            }
        }
    }
}
