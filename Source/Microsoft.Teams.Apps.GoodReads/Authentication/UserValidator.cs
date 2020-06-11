// <copyright file="UserValidator.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.GoodReads.Authentication
{
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Teams.Apps.GoodReads.Common.Interfaces;

    /// <summary>
    /// A class that is responsible for validating, if the user is a member of a certain team.
    /// </summary>
    public class UserValidator
    {
        private readonly IUserTeamMembershipProvider userTeamMembershipProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserValidator"/> class.
        /// </summary>
        /// <param name="userTeamMembershipProvider">The user team membership provider.</param>
        public UserValidator(IUserTeamMembershipProvider userTeamMembershipProvider)
        {
            this.userTeamMembershipProvider = userTeamMembershipProvider;
        }

        /// <summary>
        /// Check if a user is a member of a certain team.
        /// </summary>
        /// <param name="teamId">The team id that the validator checks against the user. To see if the user is a member of the team. </param>
        /// <param name="userAadObjectId">The user's AadObjectId.</param>
        /// <returns>The flag indicates that the user is a part of certain team or not.</returns>
        public async Task<bool> ValidateAsync(string teamId, string userAadObjectId)
        {
            var userTeamMembershipEntities =
                await this.userTeamMembershipProvider.GetUserTeamMembershipByUserAadObjectIdAsync(teamId, userAadObjectId);
            return userTeamMembershipEntities != null && userTeamMembershipEntities.Any();
        }
    }
}
