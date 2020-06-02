// <copyright file="IDigestNotificationHelper.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.GoodReads.Common.Interfaces
{
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Interface for notification helper, which helps in sending list card notification on Monthly/Weekly basis as per the configured preference in different channels.
    /// </summary>
    public interface IDigestNotificationHelper
    {
        /// <summary>
        /// Send notification in channels on Weekly or Monthly basis as per the configured preference in different channels.
        /// </summary>
        /// <param name="fromDate">Start date from which data should fetch.</param>
        /// <param name="toDate">End date till when data should fetch.</param>
        /// <param name="digestFrequency">Digest frequency text for notification like Monthly/Weekly.</param>
        /// <returns>A task that sends notification in channel.</returns>
        Task SendNotificationInChannelAsync(DateTime fromDate, DateTime toDate, string digestFrequency);
    }
}
