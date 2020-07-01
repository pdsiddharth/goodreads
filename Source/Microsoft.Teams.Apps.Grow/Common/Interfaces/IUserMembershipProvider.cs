// <copyright file="IUserMembershipProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.Grow.Common.Interfaces
{
    using System.Threading.Tasks;
    using Microsoft.Teams.Apps.Grow.Models;

    /// <summary>
    /// Interface for provider which stores user membership data.
    /// </summary>
    public interface IUserMembershipProvider
    {
        /// <summary>
        /// Adds a user membership details.
        /// </summary>
        /// <param name="userConversationId">User conversation id.</param>
        /// <param name="userAadObjectId">Azure Active Directory id of the user.</param>
        /// <param name="servicePath">Service URL for a tenant.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task AddUserMembershipAsync(string userConversationId, string userAadObjectId, string servicePath);

        /// <summary>
        /// Get user membership details.
        /// </summary>
        /// <param name="userAadObjectId">Azure Active Directory id of the user.</param>
        /// <returns>A task that represents an object to hold user membership data.</returns>
        Task<UserMembershipEntity> GetUserMembershipDataAsync(string userAadObjectId);
    }
}
