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

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

using SaasSuite.Core.Interfaces;

namespace SaasSuite.Core.Middleware
{
	/// <summary>
	/// ASP.NET Core middleware that enforces tenant maintenance windows by blocking requests
	/// to tenants that are currently undergoing maintenance operations.
	/// Returns HTTP 503 Service Unavailable with retry-after headers for affected tenants.
	/// </summary>
	/// <remarks>
	/// This middleware must be registered after <see cref="SaasResolutionMiddleware"/> in the pipeline
	/// using <see cref="ApplicationBuilderExtensions.UseTenantMaintenance"/>. It checks the
	/// current tenant's maintenance status and short-circuits the request pipeline if maintenance is active.
	/// <para>
	/// When a tenant is under maintenance, the middleware:
	/// <list type="bullet">
	/// <item><description>Returns HTTP 503 Service Unavailable status code</description></item>
	/// <item><description>Sets Content-Type to application/json</description></item>
	/// <item><description>Includes a Retry-After header (default 3600 seconds / 1 hour)</description></item>
	/// <item><description>Returns a JSON error response with tenant information</description></item>
	/// <item><description>Prevents downstream middleware and endpoints from executing</description></item>
	/// </list>
	/// </para>
	/// <para>
	/// The HTTP 503 status code signals to clients and load balancers that the service is temporarily
	/// unavailable, and the Retry-After header suggests when to retry the request.
	/// </para>
	/// </remarks>
	public class TenantMaintenanceMiddleware
	{
		#region ' Fields '

		/// <summary>
		/// The next middleware delegate in the ASP.NET Core pipeline.
		/// Invoked only if the tenant is not under maintenance.
		/// </summary>
		private readonly RequestDelegate _next;

		#endregion

		#region ' Constructors '

		/// <summary>
		/// Initializes a new instance of the <see cref="TenantMaintenanceMiddleware"/> class.
		/// </summary>
		/// <param name="next">The next middleware in the request pipeline. Invoked only if maintenance checks pass.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="next"/> is <see langword="null"/>.</exception>
		public TenantMaintenanceMiddleware(RequestDelegate next)
		{
			this._next = next ?? throw new ArgumentNullException(nameof(next));
		}

		#endregion

		#region ' Methods '

		/// <summary>
		/// Invokes the middleware to check if the current tenant is under maintenance.
		/// Short-circuits the pipeline with an HTTP 503 response if maintenance is active;
		/// otherwise continues to the next middleware.
		/// </summary>
		/// <param name="context">The HTTP context for the current request. Used to access tenant context and write the response.</param>
		/// <param name="maintenanceService">
		/// The maintenance service injected from DI. Used to check the current tenant's maintenance status.
		/// Must be registered in the service collection before this middleware is invoked.
		/// </param>
		/// <param name="tenantAccessor">
		/// The tenant accessor service injected from DI. Used to retrieve the current tenant context.
		/// Must be registered in the service collection before this middleware is invoked.
		/// </param>
		/// <returns>A task that represents the asynchronous middleware execution.</returns>
		/// <remarks>
		/// This method uses per-request dependency injection for the maintenance service and tenant accessor.
		/// If no tenant context is available (tenant resolution failed or wasn't attempted), the middleware
		/// allows the request to proceed without maintenance checks.
		/// <para>
		/// The maintenance check is performed on every request, so implementations of <see cref="IMaintenanceService.IsUnderMaintenanceAsync"/>
		/// should be optimized for performance, typically using caching for frequently accessed maintenance windows.
		/// </para>
		/// <para>
		/// The error response includes:
		/// <list type="bullet">
		/// <item><description><c>error</c>: A machine-readable error code ("tenant_under_maintenance")</description></item>
		/// <item><description><c>message</c>: A human-readable error message</description></item>
		/// <item><description><c>tenantId</c>: The identifier of the affected tenant</description></item>
		/// </list>
		/// </para>
		/// </remarks>
		public async Task InvokeAsync(HttpContext context, IMaintenanceService maintenanceService, ITenantAccessor tenantAccessor)
		{
			// Retrieve the current tenant context (may be null if resolution failed)
			TenantContext? tenantContext = tenantAccessor.TenantContext;

			if (tenantContext != null)
			{
				// Check if the tenant is currently under an active maintenance window
				bool isUnderMaintenance = await maintenanceService.IsUnderMaintenanceAsync(
					tenantContext.TenantId,
					context.RequestAborted);

				if (isUnderMaintenance)
				{
					// Short-circuit the pipeline and return HTTP 503 Service Unavailable
					context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
					context.Response.ContentType = "application/json";

					// Suggest retry after 1 hour (3600 seconds)
					_ = context.Response.Headers.TryAdd("Retry-After", "3600");

					// Construct a JSON error response with maintenance details
					string response = System.Text.Json.JsonSerializer.Serialize(new
					{
						error = "tenant_under_maintenance",
						message = "This tenant is currently undergoing maintenance. Please try again later.",
						tenantId = tenantContext.TenantId.Value
					});

					// Write the error response and terminate the pipeline
					await context.Response.WriteAsync(response);
					return;
				}
			}

			// Tenant is not under maintenance (or no tenant context), continue to next middleware
			await this._next(context);
		}

		#endregion
	}
}