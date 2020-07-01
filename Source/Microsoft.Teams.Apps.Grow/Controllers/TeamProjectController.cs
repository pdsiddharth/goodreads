﻿// <copyright file="TeamProjectController.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.Grow.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;
    using Microsoft.Teams.Apps.Grow.Authentication.AuthenticationPolicy;
    using Microsoft.Teams.Apps.Grow.Common;
    using Microsoft.Teams.Apps.Grow.Common.Interfaces;
    using Microsoft.Teams.Apps.Grow.Models;

    /// <summary>
    /// Controller to handle project API operations.
    /// </summary>
    [ApiController]
    [Route("api/teamproject")]
    [Authorize]
    public class TeamProjectController : BaseGrowController
    {
        /// <summary>
        /// Logs errors and information.
        /// </summary>
        private readonly ILogger logger;

        /// <summary>
        /// Helper for creating models and filtering projects as per criteria.
        /// </summary>
        private readonly IProjectHelper projectHelper;

        /// <summary>
        /// Project search service for fetching project with search criteria and filters.
        /// </summary>
        private readonly IProjectSearchService projectSearchService;

        /// <summary>
        /// Provides methods for team skills operations from database.
        /// </summary>
        private readonly ITeamSkillStorageProvider teamSkillStorageProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="TeamProjectController"/> class.
        /// </summary>
        /// <param name="logger">Logs errors and information.</param>
        /// <param name="telemetryClient">The Application Insights telemetry client.</param>
        /// <param name="projectHelper">Helper for creating models and filtering projects as per criteria.</param>
        /// <param name="projectSearchService">Project search service for fetching project with search criteria and filters.</param>
        /// <param name="teamSkillStorageProvider">Provides methods for team skills operations from database.</param>
        public TeamProjectController(
            ILogger<ProjectController> logger,
            TelemetryClient telemetryClient,
            IProjectHelper projectHelper,
            IProjectSearchService projectSearchService,
            ITeamSkillStorageProvider teamSkillStorageProvider)
            : base(telemetryClient)
        {
            this.logger = logger;
            this.projectHelper = projectHelper;
            this.projectSearchService = projectSearchService;
            this.teamSkillStorageProvider = teamSkillStorageProvider;
        }

        /// <summary>
        /// Get filtered projects for particular team as per the configured skills.
        /// </summary>
        /// <param name="teamId">Team id for which data will fetch.</param>
        /// <param name="pageCount">Page number to get search data.</param>
        /// <returns>Returns filtered list of team projects as per the configured skills.</returns>
        [HttpGet("team-projects")]
        [Authorize(PolicyNames.MustBeTeamMemberUserPolicy)]
        public async Task<IActionResult> FilteredTeamPostsAsync(string teamId, int pageCount)
        {
            this.logger.LogInformation("Call to get filtered team projects.");

            if (string.IsNullOrEmpty(teamId))
            {
                this.logger.LogError("TeamId is either null or empty.");
                return this.GetErrorResponse(StatusCodes.Status400BadRequest, "TeamId is either null or empty.");
            }

            if (pageCount < 0)
            {
                this.logger.LogError("Invalid parameter value for pageCount.");
                return this.GetErrorResponse(StatusCodes.Status400BadRequest, "Invalid parameter value for pageCount.");
            }

            var skipRecords = pageCount * Constants.LazyLoadPerPageProjectCount;

            try
            {
                // Get skills based on the team id for which skills has configured.
                var teamSkillEntity = await this.teamSkillStorageProvider.GetTeamSkillsDataAsync(teamId);

                if (teamSkillEntity != null && !string.IsNullOrEmpty(teamSkillEntity.Skills))
                {
                    // Prepare query based on the skills and get the data using search service.
                    var skillsQuery = this.projectHelper.CreateSkillsQuery(teamSkillEntity.Skills);

                    var projects = await this.projectSearchService.GetProjectsAsync(
                        ProjectSearchScope.FilterAsPerTeamSkills,
                        skillsQuery,
                        userObjectId: null,
                        count: Constants.LazyLoadPerPageProjectCount,
                        skip: skipRecords);

                    if (projects != null && projects.Any())
                    {
                        // Filter the data based on the configured skills.
                        var filteredTeamProjects = this.projectHelper.GetFilteredProjectsAsPerSkills(projects, teamSkillEntity.Skills);
                        this.RecordEvent("Filtered team project - HTTP Get call succeeded");
                        return this.Ok(filteredTeamProjects);
                    }
                }
                else
                {
                    this.logger.LogInformation($"Tags are not configured for team {teamId}.");
                }

                return this.Ok(new List<ProjectEntity>());
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, $"Error while fetching projects for team {teamId}.");
                throw;
            }
        }

        /// <summary>
        /// Get projects as per the applied filters.
        /// </summary>
        /// <param name="status">Semicolon separated status of projects like Not started/Active/Blocked/Closed.</param>
        /// <param name="projectOwnerNames">Semicolon separated project owner names to filter the projects.</param>
        /// <param name="skills">Semicolon separated skills to match the projects skills for which data will fetch.</param>
        /// <param name="teamId">Team id to get configured skills for a team.</param>
        /// <param name="pageCount">Page count for which projects needs to be fetched.</param>
        /// <returns>Returns filtered list of projects as per the selected filters.</returns>
        [HttpGet("applied-filters-projects")]
        [Authorize(PolicyNames.MustBeTeamMemberUserPolicy)]
        public async Task<IActionResult> AppliedFiltersProjectsAsync(string status, string projectOwnerNames, string skills, string teamId, int pageCount)
        {
            this.RecordEvent("Get filtered projects for team - HTTP Get call succeeded");

            if (pageCount < 0)
            {
                this.logger.LogError("Invalid argument value for pageCount.");
                return this.BadRequest(new { message = "Invalid argument value for pageCount." });
            }

            if (string.IsNullOrEmpty(teamId))
            {
                this.logger.LogError("Argument teamId cannot be null or empty.");
                return this.BadRequest(new { message = "Argument teamId cannot be null or empty." });
            }

            var skipRecords = pageCount * Constants.LazyLoadPerPageProjectCount;

            try
            {
                var teamSkillEntity = await this.teamSkillStorageProvider.GetTeamSkillsDataAsync(teamId);

                if (teamSkillEntity == null || string.IsNullOrEmpty(teamSkillEntity.Skills))
                {
                    this.logger.LogInformation($"Skills are not configured for team {teamId}.");
                    return this.BadRequest(new { message = $"Skills are not configured for team {teamId}." });
                }

                // If none of tags are selected for filtering, assign all configured tags for team to get posts which are intended for team.
                if (string.IsNullOrEmpty(skills))
                {
                    skills = teamSkillEntity.Skills;
                }
                else
                {
                    var savedTags = teamSkillEntity.Skills.Split(";");
                    var tagsList = skills.Split(';').Intersect(savedTags);
                    skills = string.Join(';', tagsList);
                }

                // If no skills selected for filtering then get projects irrespective of skills.
                var skillsQuery = this.projectHelper.CreateSkillsQuery(skills);
                var filterQuery = this.projectHelper.CreateFilterSearchQuery(status, projectOwnerNames);

                var projects = await this.projectSearchService.GetProjectsAsync(
                    ProjectSearchScope.FilterTeamProjects,
                    skillsQuery,
                    userObjectId: null,
                    filterQuery: filterQuery,
                    count: Constants.LazyLoadPerPageProjectCount,
                    skip: skipRecords);

                this.RecordEvent("Get filtered projects for team - HTTP Get call succeeded");

                return this.Ok(projects);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error while fetching filtered projects for team.");
                throw;
            }
        }

        /// <summary>
        /// Get list of projects as per the configured skills in a team and Title/Description/Skills search text.
        /// </summary>
        /// <param name="searchText">Search text represents the Title/Description/Skills field of projects.</param>
        /// <param name="teamId">Team Id for which projects needs to be fetched.</param>
        /// <param name="pageCount">Page count for which projects needs to be fetched.</param>
        /// <returns>List of projects as per the search text and configured skills.</returns>
        [HttpGet("team-search-projects")]
        [Authorize(PolicyNames.MustBeTeamMemberUserPolicy)]
        public async Task<IActionResult> TeamSearchProjectsAsync(string searchText, string teamId, int pageCount)
        {
            this.logger.LogInformation("Call to get list of projects as per the configured skills and search text.");

            if (string.IsNullOrEmpty(teamId))
            {
                this.logger.LogError("Error while fetching projects as per configured skills and search text.");
                return this.GetErrorResponse(StatusCodes.Status400BadRequest, "Error while fetching projects as per configured skills and search text.");
            }

            if (pageCount < 0)
            {
                this.logger.LogError("Invalid argument value for pageCount.");
                return this.GetErrorResponse(StatusCodes.Status400BadRequest, "Invalid argument value for pageCount.");
            }

            var skipRecords = pageCount * Constants.LazyLoadPerPageProjectCount;

            try
            {
                var teamSkillEntity = await this.teamSkillStorageProvider.GetTeamSkillsDataAsync(teamId);

                if (teamSkillEntity == null || string.IsNullOrEmpty(teamSkillEntity.Skills))
                {
                    this.logger.LogInformation($"Skills are not configured for team {teamId}.");
                    return this.GetErrorResponse(StatusCodes.Status400BadRequest, $"Skills are not configured for team {teamId}.");
                }

                var skillsQuery = this.projectHelper.CreateSkillsQuery(teamSkillEntity.Skills);
                var filterQuery = $"search.ismatch('{skillsQuery}', 'RequiredSkills')";

                var projects = await this.projectSearchService.GetProjectsAsync(
                    ProjectSearchScope.SearchProjects,
                    searchText,
                    userObjectId: null,
                    count: Constants.LazyLoadPerPageProjectCount,
                    skip: skipRecords,
                    filterQuery: filterQuery);

                this.RecordEvent("Team project search - HTTP Get call succeeded");

                return this.Ok(projects);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, $"Error while getting projects as per search text for team {teamId}.");
                throw;
            }
        }

        /// <summary>
        /// Get unique owner names as per configured skills in a team.
        /// </summary>
        /// <param name="teamId">Team id to get the configured skills for a team.</param>
        /// <returns>Returns unique user names.</returns>
        [HttpGet("project-owners-for-team-skills")]
        [Authorize(PolicyNames.MustBeTeamMemberUserPolicy)]
        public async Task<IActionResult> GetProjectOwnersAsync(string teamId)
        {
            this.logger.LogInformation("Call to get unique project owner names as per configured skills in a team.");

            if (string.IsNullOrEmpty(teamId))
            {
                this.logger.LogError("TeamId is either null or empty.");
                return this.GetErrorResponse(StatusCodes.Status400BadRequest, "TeamId is either null or empty.");
            }

            try
            {
                var projectOwnerNames = new List<string>();

                // Get skills based on the team id for which skills has configured.
                var teamSkillEntity = await this.teamSkillStorageProvider.GetTeamSkillsDataAsync(teamId);

                if (teamSkillEntity == null || string.IsNullOrEmpty(teamSkillEntity.Skills))
                {
                    this.logger.LogInformation($"Skills are not configured for team {teamId}.");
                    return this.Ok(projectOwnerNames);
                }

                var skillsQuery = this.projectHelper.CreateSkillsQuery(teamSkillEntity.Skills);
                var projects = await this.projectSearchService.GetProjectsAsync(ProjectSearchScope.FilterAsPerTeamSkills, skillsQuery, null, null);

                if (projects != null)
                {
                    projectOwnerNames = projects
                        .GroupBy(project => project.CreatedByUserId)
                        .OrderByDescending(groupedProject => groupedProject.Count())
                        .Take(50)
                        .Select(project => project.First().CreatedByName)
                        .OrderBy(createdByName => createdByName).ToList();

                    this.RecordEvent("Team Project unique owner names - HTTP Get call succeeded.");
                }

                return this.Ok(projectOwnerNames);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error while making call to get unique project owner names.");
                throw;
            }
        }
    }
}