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

using Microsoft.Extensions.Caching.Memory;

using SaasSuite.Caching;
using SaasSuite.Caching.Interfaces;
using SaasSuite.Caching.Invalidation;
using SaasSuite.Caching.Options;
using SaasSuite.Caching.Services;

namespace Microsoft.Extensions.DependencyInjection
{
	/// <summary>
	/// Provides extension methods for configuring SaasSuite caching services in the dependency injection container.
	/// </summary>
	/// <remarks>
	/// These extensions simplify the registration of caching services by providing fluent API methods
	/// that support both basic and customized cache configurations. The methods register appropriate
	/// cache implementations, namespace version stores for logical cache invalidation, invalidation publishers,
	/// and configure caching options using the ASP.NET Core options pattern.
	/// </remarks>
	public static class ServiceCollectionExtensions
	{
		#region ' Static Methods '

		/// <summary>
		/// Registers SaasSuite caching services using the default in-memory cache implementation.
		/// </summary>
		/// <param name="services">The <see cref="IServiceCollection"/> to add services to. Cannot be <see langword="null"/>.</param>
		/// <returns>The <see cref="IServiceCollection"/> for method chaining.</returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="services"/> is <see langword="null"/>.
		/// </exception>
		/// <remarks>
		/// This method registers the following services:
		/// <list type="bullet">
		/// <item><description><see cref="InMemoryCacheService"/> as the singleton implementation of <see cref="ICacheService"/></description></item>
		/// <item><description><see cref="InMemoryNamespaceVersionStore"/> as the singleton implementation of <see cref="INamespaceVersionStore"/></description></item>
		/// <item><description><see cref="CacheInvalidationPublisher"/> as the singleton implementation of <see cref="ICacheInvalidationPublisher"/></description></item>
		/// <item><description><see cref="IMemoryCache"/> from ASP.NET Core for in-memory caching</description></item>
		/// </list>
		/// Default cache options are applied with a 30-minute expiration time and tenant isolation enabled.
		/// This configuration is suitable for single-instance deployments, development, and testing scenarios.
		/// For production distributed systems with multiple instances, consider implementing a distributed cache like Redis
		/// with corresponding distributed implementations of <see cref="INamespaceVersionStore"/>.
		/// </remarks>
		public static IServiceCollection AddSaasCaching(this IServiceCollection services)
		{
			// Validate that services collection is not null
			ArgumentNullException.ThrowIfNull(services);

			// Register ASP.NET Core's IMemoryCache for in-memory caching
			_ = services.AddMemoryCache();

			// Register the in-memory cache service implementation as a singleton
			_ = services.AddSingleton<ICacheService, InMemoryCacheService>();

			// Register the in-memory namespace version store for logical cache invalidation
			// This tracks version numbers for each tenant/area namespace
			_ = services.AddSingleton<INamespaceVersionStore, InMemoryNamespaceVersionStore>();

			// Register the cache invalidation publisher that increments namespace versions
			// This enables coordinated cache invalidation across the application
			_ = services.AddSingleton<ICacheInvalidationPublisher, CacheInvalidationPublisher>();

			// Configure cache options with default values
			_ = services.Configure<CacheOptions>(options => { });

			// Return the service collection for fluent chaining
			return services;
		}

		/// <summary>
		/// Registers SaasSuite caching services using the in-memory cache implementation with custom options.
		/// </summary>
		/// <param name="services">The <see cref="IServiceCollection"/> to add services to. Cannot be <see langword="null"/>.</param>
		/// <param name="configureOptions">
		/// An action to configure <see cref="CacheOptions"/>. Cannot be <see langword="null"/>.
		/// Use this to customize default expiration time, tenant isolation, and key prefixes.
		/// </param>
		/// <returns>The <see cref="IServiceCollection"/> for method chaining.</returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="services"/> or <paramref name="configureOptions"/> is <see langword="null"/>.
		/// </exception>
		/// <remarks>
		/// This method registers the following services:
		/// <list type="bullet">
		/// <item><description><see cref="InMemoryCacheService"/> as the singleton implementation of <see cref="ICacheService"/></description></item>
		/// <item><description><see cref="InMemoryNamespaceVersionStore"/> as the singleton implementation of <see cref="INamespaceVersionStore"/></description></item>
		/// <item><description><see cref="CacheInvalidationPublisher"/> as the singleton implementation of <see cref="ICacheInvalidationPublisher"/></description></item>
		/// <item><description><see cref="IMemoryCache"/> from ASP.NET Core for in-memory caching</description></item>
		/// </list>
		/// This overload allows customization of caching behavior through the options pattern.
		/// Common customizations include:
		/// <list type="bullet">
		/// <item><description>Adjusting default expiration times for specific application needs</description></item>
		/// <item><description>Disabling tenant isolation for single-tenant scenarios</description></item>
		/// <item><description>Customizing cache key prefixes for multi-application deployments</description></item>
		/// </list>
		/// The registered services are in-memory implementations using <see cref="IMemoryCache"/>.
		/// For distributed scenarios, implement custom versions backed by a distributed store.
		/// </remarks>
		public static IServiceCollection AddSaasCaching(this IServiceCollection services, Action<CacheOptions> configureOptions)
		{
			// Validate that services collection is not null
			ArgumentNullException.ThrowIfNull(services);

			// Validate that configuration action is not null
			ArgumentNullException.ThrowIfNull(configureOptions);

			// Register ASP.NET Core's IMemoryCache for in-memory caching
			_ = services.AddMemoryCache();

			// Register the in-memory cache service implementation as a singleton
			_ = services.AddSingleton<ICacheService, InMemoryCacheService>();

			// Register the in-memory namespace version store for logical cache invalidation
			// This tracks version numbers for each tenant/area namespace
			_ = services.AddSingleton<INamespaceVersionStore, InMemoryNamespaceVersionStore>();

			// Register the cache invalidation publisher that increments namespace versions
			// This enables coordinated cache invalidation across the application
			_ = services.AddSingleton<ICacheInvalidationPublisher, CacheInvalidationPublisher>();

			// Configure cache options using the provided action
			_ = services.Configure(configureOptions);

			// Return the service collection for fluent chaining
			return services;
		}

		#endregion
	}
}