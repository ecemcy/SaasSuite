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

using SaasSuite.Quotas.Middleware;

namespace Microsoft.AspNetCore.Builder
{
	/// <summary>
	/// Provides extension methods for <see cref="IApplicationBuilder"/> to register quota enforcement middleware in the ASP.NET Core request pipeline.
	/// </summary>
	/// <remarks>
	/// <para>
	/// This class contains extension methods that simplify the integration of quota enforcement
	/// into ASP.NET Core applications. The middleware registered through these extensions intercepts
	/// HTTP requests and automatically enforces configured quota limits before allowing requests to proceed.
	/// </para>
	/// <para>
	/// The quota enforcement system works in conjunction with the quota services that must be registered
	/// in the dependency injection container using the <c>AddSaasQuotas</c> extension method.
	/// </para>
	/// <example>
	/// Basic usage in a Razor Pages application:
	/// <code>
	/// var builder = WebApplication.CreateBuilder(args);
	///
	/// // Register quota services
	/// builder.Services.AddSaasQuotas(options =>
	/// {
	///     options.EnableDetailedErrors = true;
	/// });
	///
	/// var app = builder.Build();
	///
	/// app.UseHttpsRedirection();
	/// app.UseStaticFiles();
	/// app.UseRouting();
	/// app.UseAuthentication();
	///
	/// // Add quota enforcement middleware after authentication
	/// app.UseQuotaEnforcement();
	///
	/// app.UseAuthorization();
	/// app.MapRazorPages();
	///
	/// app.Run();
	/// </code>
	///
	/// For Blazor Server applications:
	/// <code>
	/// var builder = WebApplication.CreateBuilder(args);
	///
	/// builder.Services.AddRazorPages();
	/// builder.Services.AddServerSideBlazor();
	/// builder.Services.AddSaasQuotas();
	///
	/// var app = builder.Build();
	///
	/// app.UseHttpsRedirection();
	/// app.UseStaticFiles();
	/// app.UseRouting();
	/// app.UseAuthentication();
	///
	/// // Enforce quotas before authorization
	/// app.UseQuotaEnforcement();
	///
	/// app.UseAuthorization();
	/// app.MapBlazorHub();
	/// app.MapFallbackToPage("/_Host");
	///
	/// app.Run();
	/// </code>
	/// </example>
	/// </remarks>
	public static class ApplicationBuilderExtensions
	{
		#region ' Static Methods '

		/// <summary>
		/// Adds quota enforcement middleware to the ASP.NET Core request pipeline.
		/// </summary>
		/// <param name="app">
		/// The <see cref="IApplicationBuilder"/> instance to configure. This parameter represents
		/// the application's request processing pipeline and cannot be null.
		/// </param>
		/// <returns>
		/// The same <see cref="IApplicationBuilder"/> instance for method chaining, allowing
		/// additional middleware to be configured in a fluent manner.
		/// </returns>
		/// <remarks>
		/// <para>
		/// This middleware should be placed strategically in the pipeline, typically after authentication
		/// and tenant resolution middleware, but before authorization and endpoint routing. The recommended
		/// pipeline order is:
		/// </para>
		/// <list type="number">
		/// <item><description>Exception handling middleware (UseExceptionHandler)</description></item>
		/// <item><description>HTTPS redirection (UseHttpsRedirection)</description></item>
		/// <item><description>Static files (UseStaticFiles) - optional</description></item>
		/// <item><description>Routing (UseRouting)</description></item>
		/// <item><description>Authentication (UseAuthentication)</description></item>
		/// <item><description>Tenant resolution (if using multi-tenancy)</description></item>
		/// <item><description><strong>Quota enforcement (UseQuotaEnforcement) - this middleware</strong></description></item>
		/// <item><description>Authorization (UseAuthorization)</description></item>
		/// <item><description>Endpoint mapping (MapRazorPages, MapBlazorHub, MapControllers, etc.)</description></item>
		/// </list>
		/// <para>
		/// The middleware will automatically check configured quotas on each request and return
		/// HTTP 429 (Too Many Requests) responses when limits are exceeded. Quota tracking and
		/// increment operations occur before the request reaches downstream middleware or endpoints.
		/// </para>
		/// <para>
		/// Ensure that quota services have been properly registered using the <c>AddSaasQuotas</c>
		/// extension method in your service configuration before calling this method, otherwise
		/// the middleware will fail to resolve required dependencies at runtime.
		/// </para>
		/// <example>
		/// Minimal usage example:
		/// <code>
		/// var app = builder.Build();
		///
		/// app.UseAuthentication();
		/// app.UseQuotaEnforcement(); // Add quota enforcement here
		/// app.UseAuthorization();
		/// app.MapRazorPages();
		/// </code>
		///
		/// Complete pipeline configuration:
		/// <code>
		/// var app = builder.Build();
		///
		/// if (!app.Environment.IsDevelopment())
		/// {
		///     app.UseExceptionHandler("/Error");
		///     app.UseHsts();
		/// }
		///
		/// app.UseHttpsRedirection();
		/// app.UseStaticFiles();
		/// app.UseRouting();
		/// app.UseAuthentication();
		///
		/// // Apply quota enforcement after user identity is established
		/// app.UseQuotaEnforcement();
		///
		/// app.UseAuthorization();
		/// app.MapRazorPages();
		/// app.MapBlazorHub();
		///
		/// app.Run();
		/// </code>
		/// </example>
		/// </remarks>
		/// <exception cref="InvalidOperationException">
		/// Thrown at runtime if required quota services (IQuotaStore, QuotaService, QuotaOptions) have not
		/// been registered in the dependency injection container. This typically occurs when
		/// <c>AddSaasQuotas</c> has not been called during service registration.
		/// </exception>
		/// <seealso cref="QuotaEnforcementMiddleware"/>
		/// <seealso cref="IApplicationBuilder"/>
		public static IApplicationBuilder UseQuotaEnforcement(this IApplicationBuilder app)
		{
			// Register the quota enforcement middleware in the request pipeline.
			// The middleware will be instantiated per application lifetime with constructor injection,
			// and it will intercept each incoming HTTP request to validate quota limits before
			// allowing the request to proceed to downstream middleware or endpoints.
			// If quotas are exceeded, the middleware returns an HTTP 429 (Too Many Requests) response.
			return app.UseMiddleware<QuotaEnforcementMiddleware>();
		}

		#endregion
	}
}