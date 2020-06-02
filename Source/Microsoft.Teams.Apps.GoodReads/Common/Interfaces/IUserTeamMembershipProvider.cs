// <copyright file="IUserTeamMembershipProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.GoodReads.Common.Interfaces
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.Teams.Apps.GoodReads.Models;

    /// <summary>
    /// Interface for provider which stores user team membership data in Microsoft Azure Table storage.
    /// </summary>
    public interface IUserTeamMembershipProvider
    {
        /// <summary>
        /// Adds a user team membership entity in Azure storage table.
        /// </summary>
        /// <param name="teamId">The team id.</param>
        /// <param name="userAadObjectId">Azure Active Directory id of the user.</param>
        /// <param name="serviceUri">Team service Uri.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task AddUserTeamMembershipAsync(string teamId, string userAadObjectId, Uri serviceUri);

        /// <summary>
        /// Deletes all memberships belonging to a team.
        /// </summary>
        /// <param name="teamId">The team id.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task DeleteUserTeamMembershipByTeamIdAsync(string teamId);

        /// <summary>
        /// Gets all memberships by a user's Active Directory id of user and team id.
        /// </summary>
        /// <param name="teamId">The team id.</param>
        /// <param name="userAadObjectId">Azure Active Directory id of the user.</param>
        /// <returns>The memberships meet the search criteria.</returns>
        Task<IEnumerable<UserTeamMembershipEntity>> GetUserTeamMembershipByUserAadObjectIdAsync(string teamId, string userAadObjectId);

        /// <summary>
        /// Get user team membership data from Microsoft Azure Table storage.
        /// </summary>
        /// <param name="teamId">Team Id for which need to fetch data.</param>
        /// <returns>A task that represents an object to hold team preference data.</returns>
        Task<UserTeamMembershipEntity> GetUserTeamMembershipDataAsync(string teamId);
    }
}
