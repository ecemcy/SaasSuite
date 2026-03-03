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

using SaasSuite.Features.Interfaces;
using SaasSuite.Features.Services;

namespace Microsoft.Extensions.DependencyInjection
{
	/// <summary>
	/// Provides extension methods for registering SaasSuite feature flag services in the dependency injection container.
	/// These extensions simplify the configuration of feature management capabilities in multi-tenant ASP.NET Core applications.
	/// </summary>
	/// <remarks>
	/// This static class extends <see cref="IServiceCollection"/> to enable fluent registration of feature flag services
	/// using method chaining. The extensions are defined in the <see cref="Microsoft.Extensions.DependencyInjection"/>
	/// namespace to make them automatically available when the DI namespace is imported, following the .NET convention
	/// for extension method discoverability.
	/// </remarks>
	public static class ServiceCollectionExtensions
	{
		#region ' Static Methods '

		/// <summary>
		/// Adds SaasSuite feature flag services to the dependency injection service collection.
		/// Registers the default in-memory implementation of <see cref="IFeatureService"/> as a singleton.
		/// </summary>
		/// <param name="services">The service collection to add the feature services to. Cannot be <see langword="null"/>.</param>
		/// <returns>The same <paramref name="services"/> instance for method chaining, enabling fluent configuration.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is <see langword="null"/>.</exception>
		/// <remarks>
		/// This method registers the following service:
		/// <list type="bullet">
		/// <item>
		/// <description>
		/// <see cref="IFeatureService"/> as <see cref="FeatureService"/> with <see cref="ServiceLifetime.Singleton"/> lifetime.
		/// The singleton lifetime is appropriate because the service uses thread-safe in-memory storage and maintains
		/// application-wide feature flag state. A single instance is shared across all requests for efficiency.
		/// </description>
		/// </item>
		/// </list>
		/// <para>
		/// The default <see cref="FeatureService"/> implementation stores feature flags in memory using concurrent
		/// collections. This is suitable for:
		/// <list type="bullet">
		/// <item><description>Development and testing environments</description></item>
		/// <item><description>Single-instance production deployments where persistence is not required</description></item>
		/// <item><description>Scenarios where feature flags are set at startup and don't need to be changed dynamically</description></item>
		/// </list>
		/// </para>
		/// <para>
		/// For production multi-instance deployments or when feature flags need to persist across restarts,
		/// consider registering a custom implementation that uses:
		/// <list type="bullet">
		/// <item><description>Database storage (SQL Server, PostgreSQL) for durability</description></item>
		/// <item><description>Distributed cache (Redis, Memcached) for performance and consistency</description></item>
		/// <item><description>External feature flag services (LaunchDarkly, Azure App Configuration)</description></item>
		/// </list>
		/// To replace the default implementation, register your custom implementation after calling this method,
		/// or register it before calling this method to prevent the default registration.
		/// </para>
		/// <para>
		/// After registration, the feature service can be injected into controllers, services, and middleware
		/// using standard dependency injection:
		/// <code>
		/// public class MyController : Controller
		/// {
		///     private readonly IFeatureService _featureService;
		///
		///     public MyController(IFeatureService featureService)
		///     {
		///         _featureService = featureService;
		///     }
		/// }
		/// </code>
		/// </para>
		/// </remarks>
		public static IServiceCollection AddSaasFeatures(this IServiceCollection services)
		{
			// Validate that services collection is not null
			ArgumentNullException.ThrowIfNull(services);

			// Register the feature service as a singleton
			// Using singleton lifetime because:
			// 1. The service uses thread-safe concurrent collections
			// 2. Feature flags are application-wide state
			// 3. A single instance is more efficient than creating one per request
			_ = services.AddSingleton<IFeatureService, FeatureService>();

			// Return services for method chaining
			return services;
		}

		#endregion
	}
}