// <copyright file="DigestNotificationBackgroundService.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.GoodReads.Common.BackgroundService
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Cronos;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using Microsoft.Teams.Apps.GoodReads.Common.Interfaces;

    /// <summary>
    /// This class inherits IHostedService and implements the methods related to background tasks for sending Weekly/Monthly notifications.
    /// </summary>
    public class DigestNotificationBackgroundService : IHostedService, IDisposable
    {
        /// <summary>
        /// Represents the Weekly digest frequency.
        /// </summary>
        private const string Weekly = "Weekly";

        /// <summary>
        ///  Represents the Monthly digest frequency.
        /// </summary>
        private const string Monthly = "Monthly";

        /// <summary>
        /// Provides a parser and scheduler for Monthly cron expression.
        /// </summary>
        private readonly CronExpression monthlyExpression;

        /// <summary>
        /// Provides a parser and scheduler for Weekly cron expression.
        /// </summary>
        private readonly CronExpression weeklyExpression;

        /// <summary>
        /// Represents any time zone in the world.
        /// </summary>
        private readonly TimeZoneInfo timeZoneInfo;

        /// <summary>
        /// Instance to send logs to the Application Insights service.
        /// </summary>
        private readonly ILogger<DigestNotificationBackgroundService> logger;

        /// <summary>
        /// Instance of notification helper which helps in sending notifications.
        /// </summary>
        private readonly IDigestNotificationHelper digestNotificationHelper;

        /// <summary>
        /// Instance of Timer for executing the service at particular interval.
        /// </summary>
        private System.Timers.Timer timer;

        /// <summary>
        /// Counter for number of times the service is executing.
        /// </summary>
        private int executionCount = 0;

        /// <summary>
        /// Flag to check whether Dispose is already called or not.
        /// </summary>
        private bool disposed = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="DigestNotificationBackgroundService"/> class.
        /// BackgroundService class that inherits IHostedService and implements the methods related to sending notification tasks.
        /// </summary>
        /// <param name="logger">Instance to send logs to the Application Insights service.</param>
        /// <param name="notificationHelper">Helper to send notification in channels.</param>
        public DigestNotificationBackgroundService(
            ILogger<DigestNotificationBackgroundService> logger,
            IDigestNotificationHelper notificationHelper)
        {
            this.logger = logger;
            this.monthlyExpression = CronExpression.Parse("0 0 10 1,15 * *", CronFormat.IncludeSeconds); // At 10:00:00am, on the 1st and 15th day, every month.
            this.weeklyExpression = CronExpression.Parse("0 0 10 ? * MON", CronFormat.IncludeSeconds); // At 10:00:00am, on every Monday, every month.
            this.timeZoneInfo = TimeZoneInfo.Utc;
            this.digestNotificationHelper = notificationHelper;
        }

        /// <summary>
        /// Method to start the background task when application starts.
        /// </summary>
        /// <param name="cancellationToken">Signals cancellation to the executing method.</param>
        /// <returns>A task instance.</returns>
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                this.logger.LogInformation("Notification Hosted Service is running.");
                await this.ScheduleNotificationWeekly();
                await this.ScheduleNotificationMonthly();
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, $"Error while running the background service to send digest notification): {ex.Message}", SeverityLevel.Error);
                throw;
            }
        }

        /// <summary>
        /// Triggered when the host is performing a graceful shutdown.
        /// </summary>
        /// <param name="cancellationToken">Signals cancellation to the executing method.</param>
        /// <returns>A task instance.</returns>
        public async Task StopAsync(CancellationToken cancellationToken)
        {
            this.logger.LogInformation("Notification Hosted Service is stopping.");
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
        /// Set the timer and send notification task if timer matched as per cron expression.
        /// </summary>
        /// <returns>A task that sends notification.</returns>
        private async Task ScheduleNotificationMonthly()
        {
            var count = Interlocked.Increment(ref this.executionCount);
            this.logger.LogInformation("Notification Hosted Service is working. Count: {Count}", count);
            var next = this.monthlyExpression.GetNextOccurrence(DateTimeOffset.UtcNow, this.timeZoneInfo);

            if (next.HasValue)
            {
                var delay = next.Value - DateTimeOffset.UtcNow;
                this.timer = new System.Timers.Timer(delay.TotalMilliseconds);
                this.timer.Elapsed += async (sender, args) =>
                {
                    this.logger.LogInformation($"Timer matched to send notification at timer value : {this.timer}");
                    this.timer.Stop();  // Reset the timer.

                    // Send digest notification if it's the 1st day of the Month.
                    if (next.Value.Day == 1)
                    {
                        this.logger.LogInformation($"First day of the month {next} and exporting the data.");
                        try
                        {
                            await this.SendNotificationMonthlyAsync(next);
                        }
                        catch (Exception ex)
                        {
                            this.logger.LogError(ex, $"Error while sending the Monthly notification at {next}.");
                        }
                    }

                    await this.ScheduleNotificationMonthly();
                };
                this.timer.Start();
            }

            await Task.CompletedTask;
        }

        /// <summary>
        /// Set the timer and send notification task if timer matched as per cron expression.
        /// </summary>
        /// <returns>A task that sends notification.</returns>
        private async Task ScheduleNotificationWeekly()
        {
            var count = Interlocked.Increment(ref this.executionCount);
            this.logger.LogInformation("Notification Hosted Service is working. Count: {Count}", count);

            var next = this.weeklyExpression.GetNextOccurrence(DateTimeOffset.UtcNow, this.timeZoneInfo);

            if (next.HasValue)
            {
                var delay = next.Value - DateTimeOffset.UtcNow;
                this.timer = new System.Timers.Timer(delay.TotalMilliseconds);
                this.timer.Elapsed += async (sender, args) =>
                {
                    this.logger.LogInformation($"Timer matched to send notification at timer value : {this.timer}");
                    this.timer.Stop();  // Reset the timer.

                    try
                    {
                        await this.SendNotificationWeeklyAsync(next);
                    }
                    catch (Exception ex)
                    {
                        this.logger.LogError(ex, $"Error while sending the Weekly notification at {next}.");
                    }

                    await this.ScheduleNotificationWeekly();
                };
                this.timer.Start();
            }

            await Task.CompletedTask;
        }

        /// <summary>
        /// Method invokes send notification task which gets posts data as per configured preference and send the notification.
        /// </summary>
        /// <returns>A task that sends notification in different channels.</returns>
        private async Task SendNotificationWeeklyAsync(DateTimeOffset? dateTimeOffset)
        {
            DateTime fromDate = dateTimeOffset.Value.AddDays(-7).Date;
            DateTime toDate = dateTimeOffset.Value.Date;

            this.logger.LogInformation("Notification task queued for sending weekly notification.");
            await this.digestNotificationHelper.SendNotificationInChannelAsync(fromDate, toDate, Weekly); // Send the notifications
        }

        /// <summary>
        /// Method invokes send notification task which gets posts data as per configured preference and send the notification.
        /// </summary>
        /// <returns>A task that sends notification in different channels.</returns>
        private async Task SendNotificationMonthlyAsync(DateTimeOffset? dateTimeOffset)
        {
            DateTime fromDate = dateTimeOffset.Value.AddMonths(-1).Date;
            DateTime toDate = dateTimeOffset.Value.Date;

            this.logger.LogInformation("Notification task queued for sending monthly notification.");
            await this.digestNotificationHelper.SendNotificationInChannelAsync(fromDate, toDate, Monthly); // Send the notifications
        }
    }
}
