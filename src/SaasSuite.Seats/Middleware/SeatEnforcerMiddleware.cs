/*
|***********************************************************************|
|                                                                       |
|   Copyright © 2026 Stephen Murumba and Contributors                   |
|                                                                       |
|   Licensed under the Apache License, Version 2.0 (the "License");     |
|   you may not use this file except in compliance with the License.    |
|   You may obtain a copy of the License at                             |
|                                                                       |
|       http://www.apache.org/licenses/LICENSE-2.0                      |
|                                                                       |
|   Unless required by applicable law or agreed to in writing,          |
|   software distributed under the License is distributed on an         |
|   "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND,        |
|   either express or implied. See the License for the specific         |
|   language governing permissions and limitations under the License.   |
|                                                                       |
|***********************************************************************|
*/

using System.Security.Claims;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

using SaasSuite.Core;
using SaasSuite.Core.Interfaces;
using SaasSuite.Seats.Interfaces;
using SaasSuite.Seats.Options;

namespace SaasSuite.Seats.Middleware
{
	/// <summary>
	/// ASP.NET Core middleware that enforces seat allocation limits for multi-tenant applications.
	/// Intercepts HTTP requests to verify that users have available seats within their tenant's quota,
	/// blocking requests when seat limits are exceeded with HTTP 429 Too Many Requests responses.
	/// </summary>
	/// <remarks>
	/// This middleware integrates with the request pipeline to provide automatic seat enforcement without
	/// requiring manual checks in controllers or services. It should be placed after authentication middleware
	/// to ensure tenant and user context are available.
	/// <para>
	/// The middleware performs the following operations for each request:
	/// <list type="number">
	/// <item><description>Checks if enforcement is enabled via configuration</description></item>
	/// <item><description>Retrieves the current tenant context from <see cref="ITenantAccessor"/></description></item>
	/// <item><description>Extracts user identifier from claims or HTTP headers</description></item>
	/// <item><description>Attempts to consume a seat for the user via <see cref="ISeatService"/></description></item>
	/// <item><description>Allows the request to proceed if seat allocation succeeds</description></item>
	/// <item><description>Blocks the request with HTTP 429 if seat limit is exceeded</description></item>
	/// </list>
	/// </para>
	/// <para>
	/// The middleware can be configured via <see cref="SeatEnforcerOptions"/> to customize:
	/// <list type="bullet">
	/// <item><description>Whether enforcement is globally enabled or disabled</description></item>
	/// <item><description>Error message returned when seat limits are exceeded</description></item>
	/// <item><description>Claim type used to identify users from authentication tokens</description></item>
	/// <item><description>HTTP header name used as fallback for user identification</description></item>
	/// </list>
	/// </para>
	/// <para>
	/// Typical pipeline placement:
	/// <code>
	/// app.UseAuthentication();      // Authenticate users first
	/// app.UseSaasResolution();       // Resolve tenant context
	/// app.UseSeatEnforcer();         // Enforce seat limits
	/// app.UseAuthorization();        // Then proceed with authorization
	/// </code>
	/// </para>
	/// </remarks>
	public class SeatEnforcerMiddleware
	{
		#region ' Fields '

		/// <summary>
		/// The next middleware delegate in the ASP.NET Core pipeline.
		/// Invoked when seat enforcement succeeds or is bypassed.
		/// </summary>
		private readonly RequestDelegate _next;

		/// <summary>
		/// Configuration options controlling seat enforcement behavior.
		/// Contains settings for enforcement enablement, messages, and user identification.
		/// </summary>
		private readonly SeatEnforcerOptions _options;

		#endregion

		#region ' Constructors '

		/// <summary>
		/// Initializes a new instance of the <see cref="SeatEnforcerMiddleware"/> class
		/// with the specified pipeline delegate and configuration options.
		/// </summary>
		/// <param name="next">
		/// The next middleware in the request pipeline. Invoked when seat enforcement succeeds or is skipped.
		/// Cannot be <see langword="null"/>.
		/// </param>
		/// <param name="options">
		/// The options for configuring seat enforcement behavior. Contains the configured <see cref="SeatEnforcerOptions"/>.
		/// Cannot be <see langword="null"/>.
		/// </param>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="next"/> is <see langword="null"/> or when <paramref name="options"/> is <see langword="null"/>.
		/// </exception>
		public SeatEnforcerMiddleware(RequestDelegate next, IOptions<SeatEnforcerOptions> options)
		{
			this._next = next ?? throw new ArgumentNullException(nameof(next));
			this._options = options?.Value ?? throw new ArgumentNullException(nameof(options));
		}

		#endregion

		#region ' Methods '

		/// <summary>
		/// Extracts the user identifier from the HTTP context using configured claim types or headers.
		/// Attempts to retrieve the user ID from authentication claims first, then falls back to HTTP headers.
		/// </summary>
		/// <param name="context">The HTTP context containing user claims and request headers. Cannot be <see langword="null"/>.</param>
		/// <returns>
		/// The user identifier string if found in claims or headers; otherwise <see langword="null"/>.
		/// Returns <see langword="null"/> if the user is not authenticated or no user identifier is present.
		/// </returns>
		/// <remarks>
		/// This method checks for user identifiers in the following order:
		/// <list type="number">
		/// <item><description>Authentication claims using the configured claim type from <see cref="SeatEnforcerOptions.UserIdClaimType"/></description></item>
		/// <item><description>HTTP request header using the configured header name from <see cref="SeatEnforcerOptions.UserIdHeaderName"/></description></item>
		/// </list>
		/// The claim-based approach is preferred for authenticated users with JWT tokens or cookie authentication,
		/// while the header-based approach supports scenarios like API keys or service-to-service communication.
		/// </remarks>
		private string? GetUserId(HttpContext context)
		{
			// Try to get user ID from authentication claims (preferred method for authenticated users)
			Claim? userIdClaim = context.User?.FindFirst(this._options.UserIdClaimType);
			if (userIdClaim != null)
			{
				return userIdClaim.Value;
			}

			// Fall back to getting user ID from HTTP header (for non-authenticated scenarios or API keys)
			if (context.Request.Headers.TryGetValue(this._options.UserIdHeaderName, out StringValues headerValue))
			{
				return headerValue.ToString();
			}

			// No user identifier found in claims or headers
			return null;
		}

		/// <summary>
		/// Invokes the middleware to check and enforce seat limits for the current HTTP request.
		/// Attempts to allocate a seat for the user and blocks the request with HTTP 429 if limits are exceeded.
		/// </summary>
		/// <param name="context">The HTTP context for the current request. Provides access to request and response objects.</param>
		/// <param name="seatService">
		/// The seat service injected from DI. Used to check seat availability and consume seats.
		/// Must be registered in the service collection before this middleware is invoked.
		/// </param>
		/// <param name="tenantAccessor">
		/// The tenant accessor injected from DI. Used to retrieve the current tenant context.
		/// Must be registered in the service collection and populated by tenant resolution middleware.
		/// </param>
		/// <returns>A task that represents the asynchronous middleware execution.</returns>
		/// <remarks>
		/// The middleware uses the following logic to determine whether to enforce seat limits:
		/// <list type="number">
		/// <item><description>If <see cref="SeatEnforcerOptions.EnableEnforcement"/> is <see langword="false"/>, bypass enforcement entirely</description></item>
		/// <item><description>If no tenant context is available (not resolved or missing), bypass enforcement</description></item>
		/// <item><description>If no user ID can be extracted from claims or headers, bypass enforcement</description></item>
		/// <item><description>If the user ID is empty or whitespace, bypass enforcement</description></item>
		/// <item><description>Otherwise, attempt to consume a seat via <see cref="ISeatService.TryConsumeSeatAsync"/></description></item>
		/// </list>
		/// <para>
		/// When seat consumption succeeds (seat available or already held by user):
		/// <list type="bullet">
		/// <item><description>The request proceeds to the next middleware in the pipeline</description></item>
		/// <item><description>The user maintains their seat allocation for the duration of the request</description></item>
		/// </list>
		/// </para>
		/// <para>
		/// When seat consumption fails (seat limit exceeded):
		/// <list type="bullet">
		/// <item><description>Returns HTTP 429 Too Many Requests status code</description></item>
		/// <item><description>Sets Content-Type to application/json</description></item>
		/// <item><description>Returns a JSON error response with error code, message, and user ID</description></item>
		/// <item><description>Terminates the request pipeline without calling downstream middleware</description></item>
		/// </list>
		/// </para>
		/// <para>
		/// The JSON error response structure:
		/// <code>
		/// {
		///     "error": "seat_limit_exceeded",
		///     "message": "Configured error message from options",
		///     "userId": "The user identifier that was rejected"
		/// }
		/// </code>
		/// </para>
		/// <para>
		/// Note: This middleware checks seat availability but does not automatically release seats when
		/// requests complete. Consider implementing seat release logic in session expiration handlers,
		/// logout endpoints, or background cleanup jobs to prevent seat exhaustion from abandoned sessions.
		/// </para>
		/// </remarks>
		public async Task InvokeAsync(HttpContext context, ISeatService seatService, ITenantAccessor tenantAccessor)
		{
			// Check if enforcement is globally disabled via configuration
			if (!this._options.EnableEnforcement)
			{
				// Enforcement disabled, skip all checks and proceed to next middleware
				await this._next(context);
				return;
			}

			// Retrieve the current tenant context from the accessor
			TenantContext? tenantContext = tenantAccessor.TenantContext;
			if (tenantContext == null)
			{
				// No tenant context available (tenant resolution failed or not configured), skip enforcement
				await this._next(context);
				return;
			}

			// Attempt to extract user ID from claims or headers
			string? userId = this.GetUserId(context);
			if (string.IsNullOrWhiteSpace(userId))
			{
				// No user ID available (not authenticated or missing header), skip enforcement
				await this._next(context);
				return;
			}

			TenantId tenantId = tenantContext.TenantId;

			// Attempt to consume a seat for this user within the tenant's allocation
			bool seatAllocated = await seatService.TryConsumeSeatAsync(tenantId, userId, context.RequestAborted);

			if (!seatAllocated)
			{
				// Seat limit reached - block the request with HTTP 429 Too Many Requests
				context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
				context.Response.ContentType = "application/json";

				// Construct JSON error response with details about the rejection
				string response = System.Text.Json.JsonSerializer.Serialize(new
				{
					error = "seat_limit_exceeded",
					message = this._options.SeatLimitMessage,
					userId
				});

				// Write error response and terminate the pipeline
				await context.Response.WriteAsync(response);
				return;
			}

			// Seat successfully allocated or already held by user, proceed to next middleware
			await this._next(context);
		}

		#endregion
	}
}