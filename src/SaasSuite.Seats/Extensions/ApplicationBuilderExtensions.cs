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
using SaasSuite.Seats.Interfaces;
using SaasSuite.Seats.Middleware;
using SaasSuite.Seats.Options;

namespace Microsoft.AspNetCore.Builder
{
	/// <summary>
	/// Provides extension methods for configuring SaasSuite seat enforcement middleware in the ASP.NET Core request pipeline.
	/// These extensions enable automatic seat limit enforcement in multi-tenant applications using <see cref="IApplicationBuilder"/>.
	/// </summary>
	/// <remarks>
	/// This static class extends <see cref="IApplicationBuilder"/> to enable fluent registration of seat enforcement middleware
	/// using method chaining. The extensions are defined in the <see cref="Microsoft.AspNetCore.Builder"/> namespace
	/// to make them automatically available when the application builder namespace is imported, following the .NET
	/// convention for extension method discoverability in ASP.NET Core applications.
	/// </remarks>
	public static class ApplicationBuilderExtensions
	{
		#region ' Static Methods '

		/// <summary>
		/// Adds <see cref="SeatEnforcerMiddleware"/> to the application request pipeline.
		/// This middleware automatically enforces seat allocation limits by checking seat availability before
		/// allowing requests to proceed, returning HTTP 429 Too Many Requests responses when seat limits are exceeded.
		/// </summary>
		/// <param name="app">The application pipeline builder to add the middleware to. Cannot be <see langword="null"/>.</param>
		/// <returns>The same <paramref name="app"/> instance for method chaining, enabling fluent pipeline configuration.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="app"/> is <see langword="null"/>.</exception>
		/// <remarks>
		/// This middleware should be carefully positioned in the request pipeline to ensure proper operation.
		/// Recommended placement is after authentication and tenant resolution middleware, but before authorization:
		/// <para>
		/// Typical pipeline configuration:
		/// <code>
		/// app.UseHttpsRedirection();
		/// app.UseAuthentication();        // Authenticate users to populate User claims
		/// app.UseSaasResolution();         // Resolve tenant context from request
		/// app.UseSeatEnforcer();           // Enforce seat limits (this middleware)
		/// app.UseAuthorization();          // Check user permissions and roles
		/// app.MapControllers();
		/// </code>
		/// </para>
		/// <para>
		/// The middleware requires the following dependencies to be registered in the DI container:
		/// <list type="bullet">
		/// <item>
		/// <description>
		/// <see cref="ISeatService"/> - Provides seat management operations.
		/// Register via <c>services.AddSaasSeats()</c> or custom implementation.
		/// </description>
		/// </item>
		/// <item>
		/// <description>
		/// <see cref="ITenantAccessor"/> - Provides access to current tenant context.
		/// Register via <c>services.AddSaasCore()</c> from SaasSuite.Core package.
		/// </description>
		/// </item>
		/// <item>
		/// <description>
		/// <see cref="SeatEnforcerOptions"/> (optional) - Configure via <c>services.Configure&lt;SeatEnforcerOptions&gt;()</c>
		/// or the configureOptions parameter in <c>AddSaasSeats()</c>.
		/// </description>
		/// </item>
		/// </list>
		/// </para>
		/// <para>
		/// When a request is blocked due to seat limits:
		/// <list type="bullet">
		/// <item><description>HTTP 429 Too Many Requests status code is returned</description></item>
		/// <item><description>Content-Type header is set to application/json</description></item>
		/// <item><description>Response body contains JSON with error code, message, and user ID</description></item>
		/// <item><description>The request pipeline is terminated without calling downstream middleware</description></item>
		/// </list>
		/// </para>
		/// <para>
		/// Bypass scenarios (middleware allows request to proceed without seat enforcement):
		/// <list type="bullet">
		/// <item><description>Seat enforcement is globally disabled via <see cref="SeatEnforcerOptions.EnableEnforcement"/> = <see langword="false"/></description></item>
		/// <item><description>No tenant context is available (tenant resolution failed or not configured)</description></item>
		/// <item><description>No user ID can be extracted from claims or headers</description></item>
		/// <item><description>User ID is empty or contains only whitespace</description></item>
		/// </list>
		/// </para>
		/// <para>
		/// Important considerations:
		/// <list type="bullet">
		/// <item><description>Ensure authentication middleware runs first to populate user claims</description></item>
		/// <item><description>Ensure tenant resolution middleware runs first to populate tenant context</description></item>
		/// <item><description>Configure appropriate seat release strategies (logout handlers, session timeouts)</description></item>
		/// <item><description>Monitor seat utilization to prevent capacity issues</description></item>
		/// <item><description>Consider implementing graceful degradation for seat limit scenarios</description></item>
		/// </list>
		/// </para>
		/// </remarks>
		public static IApplicationBuilder UseSeatEnforcer(this IApplicationBuilder app)
		{
			// Validate that the application builder is not null
			ArgumentNullException.ThrowIfNull(app);

			// Register the seat enforcer middleware in the pipeline
			// The middleware will be invoked for every HTTP request in the order it was added
			return app.UseMiddleware<SeatEnforcerMiddleware>();
		}

		#endregion
	}
}