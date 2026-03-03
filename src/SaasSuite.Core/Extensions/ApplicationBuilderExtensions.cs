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

using SaasSuite.Core.Interfaces;
using SaasSuite.Core.Middleware;
using SaasSuite.Core.Options;

namespace Microsoft.AspNetCore.Builder
{
	/// <summary>
	/// Provides extension methods to add SaasSuite middleware components to the ASP.NET Core request pipeline.
	/// These extensions enable tenant resolution and maintenance window enforcement in web applications,
	/// ensuring that tenant context is properly established and maintained throughout the request lifecycle.
	/// </summary>
	public static class ApplicationBuilderExtensions
	{
		#region ' Static Methods '

		/// <summary>
		/// Adds <see cref="SaasResolutionMiddleware"/> to the pipeline to resolve the current tenant for each HTTP request.
		/// This middleware extracts tenant information from the request using registered <see cref="ITenantResolver"/> implementations
		/// and makes it available to downstream components through <see cref="ITenantAccessor"/>.
		/// </summary>
		/// <param name="app">The application pipeline builder to add the middleware to. Cannot be <see langword="null"/>.</param>
		/// <returns>The same <paramref name="app"/> instance for method chaining, enabling fluent configuration.</returns>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="app"/> is <see langword="null"/>.</exception>
		/// <remarks>
		/// This middleware should be added early in the pipeline so that downstream middleware, services, and endpoints
		/// can rely on the tenant context being available. Typical placement is:
		/// <list type="bullet">
		/// <item><description>After authentication middleware if tenant information comes from authenticated user claims</description></item>
		/// <item><description>Before authorization middleware to enable tenant-aware authorization policies</description></item>
		/// <item><description>Before any custom middleware or endpoints that need access to tenant context</description></item>
		/// </list>
		/// <para>
		/// The middleware populates the tenant context which remains available for the entire request lifetime
		/// through the <see cref="ITenantAccessor"/> service. If tenant resolution fails, the request
		/// continues without tenant context, which may result in a <see langword="null"/> tenant context in downstream components.
		/// Configure <see cref="SaasCoreOptions.RequireTenant"/> to enforce tenant presence if needed.
		/// </para>
		/// </remarks>
		public static IApplicationBuilder UseSaasResolution(this IApplicationBuilder app)
		{
			ArgumentNullException.ThrowIfNull(app);
			return app.UseMiddleware<SaasResolutionMiddleware>();
		}

		/// <summary>
		/// Adds <see cref="TenantMaintenanceMiddleware"/> to the pipeline to short-circuit requests
		/// for tenants that are currently under active maintenance windows.
		/// Returns HTTP 503 Service Unavailable for affected tenants with appropriate retry-after headers
		/// and a JSON error response containing maintenance information.
		/// </summary>
		/// <param name="app">The application pipeline builder to add the middleware to. Cannot be <see langword="null"/>.</param>
		/// <returns>The same <paramref name="app"/> instance for method chaining, enabling fluent configuration.</returns>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="app"/> is <see langword="null"/>.</exception>
		/// <remarks>
		/// This middleware must run after <see cref="UseSaasResolution"/> in the pipeline so that tenant context is available
		/// for maintenance status checks. If a tenant is found to be under maintenance, the request pipeline is terminated
		/// and an appropriate error response is returned to the client without invoking downstream middleware or endpoints.
		/// <para>
		/// The middleware returns:
		/// <list type="bullet">
		/// <item><description>HTTP 503 Service Unavailable status code</description></item>
		/// <item><description>Content-Type: application/json header</description></item>
		/// <item><description>Retry-After: 3600 header (1 hour) suggesting when to retry</description></item>
		/// <item><description>JSON body with error code, message, and tenant identifier</description></item>
		/// </list>
		/// </para>
		/// <para>
		/// Typical pipeline configuration:
		/// <code>
		/// app.UseSaasResolution();      // Resolve tenant first
		/// app.UseTenantMaintenance();   // Then check maintenance status
		/// app.UseAuthorization();       // Continue with authorization if not under maintenance
		/// </code>
		/// </para>
		/// </remarks>
		public static IApplicationBuilder UseTenantMaintenance(this IApplicationBuilder app)
		{
			ArgumentNullException.ThrowIfNull(app);
			return app.UseMiddleware<TenantMaintenanceMiddleware>();
		}

		#endregion
	}
}