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

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

using SaasSuite.Core;
using SaasSuite.Core.Interfaces;
using SaasSuite.Quotas.Enumerations;
using SaasSuite.Quotas.Options;
using SaasSuite.Quotas.Services;

namespace SaasSuite.Quotas.Middleware
{
	/// <summary>
	/// ASP.NET Core middleware that automatically enforces quota limits on incoming HTTP requests.
	/// </summary>
	/// <remarks>
	/// This middleware intercepts incoming requests early in the pipeline, checks configured quotas,
	/// and returns HTTP 429 (Too Many Requests) responses when quotas are exceeded. It operates at
	/// the tenant scope by default, preventing entire organizations from exceeding their allocated limits.
	/// The middleware can optionally include quota status information in response headers following
	/// industry-standard rate limiting conventions (X-RateLimit-* headers). This allows API clients
	/// to implement intelligent retry logic and display quota information to end users.
	/// Quota enforcement can be globally disabled via configuration without removing the middleware
	/// from the pipeline, which is useful for testing or emergency override scenarios.
	/// </remarks>
	public class QuotaEnforcementMiddleware
	{
		#region ' Fields '

		/// <summary>
		/// Configuration options controlling quota enforcement behavior, header inclusion, and error messages.
		/// </summary>
		/// <remarks>
		/// Captured at middleware instantiation from the options pattern. These options determine:
		/// <list type="bullet">
		/// <item><description>Whether enforcement is active (<see cref="QuotaOptions.EnableEnforcement"/>)</description></item>
		/// <item><description>Which quotas to check (<see cref="QuotaOptions.TrackedQuotas"/>)</description></item>
		/// <item><description>Whether to include quota headers in responses (<see cref="QuotaOptions.IncludeQuotaHeaders"/>)</description></item>
		/// <item><description>The error message returned on quota violations (<see cref="QuotaOptions.QuotaExceededMessage"/>)</description></item>
		/// <item><description>Behavior for undefined quotas (<see cref="QuotaOptions.AllowIfQuotaNotDefined"/>)</description></item>
		/// </list>
		/// </remarks>
		private readonly QuotaOptions _options;

		/// <summary>
		/// The next middleware component in the ASP.NET Core request pipeline.
		/// </summary>
		/// <remarks>
		/// Invoked after quota checks pass successfully to continue processing the request.
		/// If quota checks fail, this middleware short-circuits the pipeline by returning
		/// an HTTP 429 response without calling the next middleware.
		/// </remarks>
		private readonly RequestDelegate _next;

		#endregion

		#region ' Constructors '

		/// <summary>
		/// Initializes a new instance of the <see cref="QuotaEnforcementMiddleware"/> class with required dependencies.
		/// </summary>
		/// <param name="next">The next middleware component in the request pipeline. Must not be <see langword="null"/>.</param>
		/// <param name="options">The configured quota options wrapped in the options pattern. Must not be <see langword="null"/>.</param>
		/// <remarks>
		/// The constructor is invoked once per application lifetime when the middleware is registered.
		/// Dependencies injected here (<paramref name="next"/> and <paramref name="options"/>) are stored
		/// as fields and reused for all requests. Per-request dependencies (QuotaService, ITenantAccessor)
		/// are injected into the <see cref="InvokeAsync"/> method instead and are resolved fresh for each request.
		/// </remarks>
		/// <exception cref="ArgumentNullException">
		/// Thrown if <paramref name="next"/> or <paramref name="options"/> is <see langword="null"/>.
		/// </exception>
		public QuotaEnforcementMiddleware(RequestDelegate next, IOptions<QuotaOptions> options)
		{
			this._next = next;
			this._options = options.Value;
		}

		#endregion

		#region ' Methods '

		/// <summary>
		/// Processes an HTTP request, enforcing configured quota limits before allowing the request to proceed to downstream middleware.
		/// </summary>
		/// <param name="context">The HTTP context for the current request, containing request and response data. Must not be <see langword="null"/>.</param>
		/// <param name="quotaService">The quota service for checking and consuming quotas. Injected per request from the DI container.</param>
		/// <param name="tenantAccessor">The tenant accessor for retrieving the current tenant context. Injected per request from the DI container.</param>
		/// <returns>
		/// A <see cref="Task"/> that represents the asynchronous middleware execution.
		/// </returns>
		/// <remarks>
		/// This method executes on every HTTP request that reaches this point in the pipeline. It performs the following steps:
		/// <list type="number">
		/// <item><description>Checks if enforcement is enabled via <see cref="QuotaOptions.EnableEnforcement"/>; if disabled, passes through immediately</description></item>
		/// <item><description>Retrieves the current tenant context from <paramref name="tenantAccessor"/>; if no tenant is resolved, passes through</description></item>
		/// <item><description>For each quota name in <see cref="QuotaOptions.TrackedQuotas"/>, attempts to consume one unit using <see cref="QuotaService.TryConsumeAsync"/></description></item>
		/// <item><description>If any quota is exceeded, retrieves quota status and returns HTTP 429 with optional quota headers</description></item>
		/// <item><description>If all quotas allow consumption, proceeds to the next middleware and optionally adds quota headers to the response</description></item>
		/// </list>
		/// Quota consumption is atomic and occurs before the request reaches endpoints, ensuring
		/// the quota is reserved even if downstream processing fails or is cancelled. The middleware
		/// operates at tenant scope by default, though this can be extended to support other scopes.
		/// When quota headers are enabled, successful responses include current quota status for the
		/// first tracked quota, allowing clients to monitor their usage proactively.
		/// </remarks>
		public async Task InvokeAsync(HttpContext context, QuotaService quotaService, ITenantAccessor tenantAccessor)
		{
			// Early exit if enforcement is globally disabled (bypass all quota checks)
			if (!this._options.EnableEnforcement)
			{
				await this._next(context);
				return;
			}

			// Retrieve the current tenant context for this request
			TenantContext? tenant = tenantAccessor.TenantContext;
			if (tenant?.TenantId == null)
			{
				// No tenant context available, cannot enforce tenant-scoped quotas
				// This can occur for unauthenticated requests or requests without tenant resolution
				await this._next(context);
				return;
			}

			// Check each configured quota in sequence
			if (this._options.TrackedQuotas != null && this._options.TrackedQuotas.Count != 0)
			{
				foreach (string quotaName in this._options.TrackedQuotas)
				{
					// Attempt to atomically consume one unit of the quota
					// TryConsumeAsync checks the limit and increments usage in a single operation
					bool canConsume = await quotaService.TryConsumeAsync(
						tenant.TenantId,
						quotaName);

					if (!canConsume)
					{
						// Quota exceeded - retrieve current status for response headers
						QuotaStatus? status = await quotaService.GetQuotaStatusAsync(
							tenant.TenantId,
							quotaName);

						// Add standard rate limiting headers if configured
						if (this._options.IncludeQuotaHeaders && status != null)
						{
							AddQuotaHeaders(context.Response, status);
						}

						// Return HTTP 429 Too Many Requests and short-circuit the pipeline
						context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
						await context.Response.WriteAsync(this._options.QuotaExceededMessage);
						return;
					}
				}
			}

			// All quota checks passed, continue to the next middleware in the pipeline
			await this._next(context);

			// Optionally add quota headers to successful responses for client monitoring
			if (this._options.IncludeQuotaHeaders && this._options.TrackedQuotas != null && this._options.TrackedQuotas.Count != 0)
			{
				// Add headers for the first tracked quota (primary quota)
				// In multi-quota scenarios, consider adding headers for all quotas or the most restrictive one
				string primaryQuota = this._options.TrackedQuotas.First();
				QuotaStatus? status = await quotaService.GetQuotaStatusAsync(
					tenant.TenantId,
					primaryQuota);

				if (status != null)
				{
					AddQuotaHeaders(context.Response, status);
				}
			}
		}

		#endregion

		#region ' Static Methods '

		/// <summary>
		/// Adds standard rate limiting headers to the HTTP response following industry conventions.
		/// </summary>
		/// <param name="response">The HTTP response to which headers will be added. Must not be <see langword="null"/>.</param>
		/// <param name="status">The quota status containing limit, usage, and reset time information. Must not be <see langword="null"/>.</param>
		/// <remarks>
		/// Adds the following headers following widely-adopted rate limiting conventions:
		/// <list type="bullet">
		/// <item><description><c>X-RateLimit-Limit</c>: The maximum quota limit (total units allowed in the period)</description></item>
		/// <item><description><c>X-RateLimit-Remaining</c>: Units remaining before the quota is exceeded (always non-negative)</description></item>
		/// <item><description><c>X-RateLimit-Reset</c>: Unix timestamp (seconds since epoch) when the quota resets</description></item>
		/// </list>
		/// These headers allow API clients to:
		/// <list type="bullet">
		/// <item><description>Implement intelligent retry logic with exponential backoff until the reset time</description></item>
		/// <item><description>Display quota information to users in real-time (e.g., "42 API calls remaining")</description></item>
		/// <item><description>Proactively throttle requests when approaching limits to avoid 429 errors</description></item>
		/// <item><description>Plan batch operations based on available capacity</description></item>
		/// </list>
		/// The reset time header is only included if <see cref="QuotaStatus.ResetTime"/> has a value.
		/// For <see cref="QuotaPeriod.Total"/> quotas that never reset, this header is omitted.
		/// Headers follow the X-RateLimit-* convention used by GitHub, Twitter, and many other APIs.
		/// </remarks>
		private static void AddQuotaHeaders(HttpResponse response, QuotaStatus status)
		{
			// Add the maximum limit header (total units allowed in the current period)
			response.Headers["X-RateLimit-Limit"] = status.Limit.ToString();

			// Add the remaining capacity header (units available before exceeding the quota)
			response.Headers["X-RateLimit-Remaining"] = status.Remaining.ToString();

			// Add the reset time as a Unix timestamp if available (seconds since January 1, 1970 UTC)
			if (status.ResetTime.HasValue)
			{
				long resetTimestamp = new DateTimeOffset(status.ResetTime.Value).ToUnixTimeSeconds();
				response.Headers["X-RateLimit-Reset"] = resetTimestamp.ToString();
			}
		}

		#endregion
	}
}