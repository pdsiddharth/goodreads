// <copyright file="BaseGoodReadsController.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.GoodReads.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.ApplicationInsights;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Error = Microsoft.Teams.Apps.GoodReads.Models.ErrorResponse;

    /// <summary>
    /// Base controller to handle good read posts API operations.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class BaseGoodReadsController : ControllerBase
    {
        /// <summary>
        /// Instance of application insights telemetry client.
        /// </summary>
        private readonly TelemetryClient telemetryClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseGoodReadsController"/> class.
        /// </summary>
        /// <param name="telemetryClient">The Application Insights telemetry client.</param>
        public BaseGoodReadsController(TelemetryClient telemetryClient)
        {
            this.telemetryClient = telemetryClient;
        }

        /// <summary>
        /// Gets the user tenant id from the HttpContext.
        /// </summary>
        protected string UserTenantId
        {
            get
            {
                var tenantClaimType = "http://schemas.microsoft.com/identity/claims/tenantid";
                var claim = this.User.Claims.FirstOrDefault(p => tenantClaimType.Equals(p.Type, StringComparison.OrdinalIgnoreCase));
                if (claim == null)
                {
                    return null;
                }

                return claim.Value;
            }
        }

        /// <summary>
        /// Gets the user Azure Active Directory id from the HttpContext.
        /// </summary>
        protected string UserAadId
        {
            get
            {
                var oidClaimType = "http://schemas.microsoft.com/identity/claims/objectidentifier";
                var claim = this.User.Claims.FirstOrDefault(p => oidClaimType.Equals(p.Type, StringComparison.OrdinalIgnoreCase));
                if (claim == null)
                {
                    return null;
                }

                return claim.Value;
            }
        }

        /// <summary>
        /// Gets the user name from the HttpContext.
        /// </summary>
        protected string UserName
        {
            get
            {
                var claim = this.User.Claims.FirstOrDefault(p => "name".Equals(p.Type, StringComparison.OrdinalIgnoreCase));
                if (claim == null)
                {
                    return null;
                }

                return claim.Value;
            }
        }

        /// <summary>
        /// Records event data to Application Insights telemetry client.
        /// </summary>
        /// <param name="eventName">Name of the event.</param>
        public void RecordEvent(string eventName)
        {
            this.telemetryClient.TrackEvent(eventName, new Dictionary<string, string>
            {
                { "userId", this.UserAadId },
            });
        }

        /// <summary>
        /// Creates the error response as per the status codes.
        /// </summary>
        /// <param name="statusCode">Describes the type of error.</param>
        /// <param name="errorMessage">Describes the error message.</param>
        /// <returns>Returns error response with appropriate message and status code.</returns>
        protected IActionResult GetErrorResponse(int statusCode, string errorMessage)
        {
            return statusCode switch
            {
                StatusCodes.Status400BadRequest => this.StatusCode(
                    StatusCodes.Status400BadRequest,
                    new Error
                    {
                        StatusCode = "badRequest",
                        ErrorMessage = errorMessage,
                    }),

                _ => this.StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new Error
                    {
                        StatusCode = "internalServerError",
                        ErrorMessage = errorMessage,
                    }),
            };
        }
    }
}