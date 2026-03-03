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

using SaasSuite.Quotas.Interfaces;
using SaasSuite.Quotas.Options;
using SaasSuite.Quotas.Services;
using SaasSuite.Quotas.Stores;

namespace Microsoft.Extensions.DependencyInjection
{
	/// <summary>
	/// Provides extension methods for <see cref="IServiceCollection"/> to register quota services and dependencies in the dependency injection container.
	/// </summary>
	/// <remarks>
	/// This class simplifies quota system configuration by providing fluent extension methods that register
	/// all required services with appropriate lifetimes. The methods support various configuration scenarios
	/// from simple default setups to advanced custom store implementations with full configuration control.
	/// </remarks>
	public static class ServiceCollectionExtensions
	{
		#region ' Static Methods '

		/// <summary>
		/// Registers quota services with the dependency injection container using default settings and the in-memory store.
		/// </summary>
		/// <param name="services">The <see cref="IServiceCollection"/> to register services with.</param>
		/// <returns>The same <see cref="IServiceCollection"/> instance for method chaining.</returns>
		/// <remarks>
		/// This overload provides the simplest configuration option with no customization required.
		/// It uses default quota options with enforcement enabled and registers the in-memory quota store.
		/// The in-memory store is suitable for development, testing, and single-instance deployments but does not
		/// persist data across application restarts or share state across multiple instances in load-balanced scenarios.
		/// The following services are registered with their respective lifetimes:
		/// <list type="bullet">
		/// <item><description><see cref="IQuotaStore"/> as <see cref="InMemoryQuotaStore"/> (Singleton lifetime)</description></item>
		/// <item><description><see cref="QuotaService"/> (Scoped lifetime, one instance per HTTP request)</description></item>
		/// <item><description><see cref="QuotaOptions"/> with default configuration (enforcement enabled, quota headers enabled)</description></item>
		/// </list>
		/// For production environments with multiple instances, consider using a custom store implementation
		/// backed by Redis, SQL Server, or another distributed cache solution.
		/// </remarks>
		public static IServiceCollection AddSaasQuotas(this IServiceCollection services)
		{
			// Delegate to the overload with an empty configuration action (use defaults)
			return services.AddSaasQuotas(_ => { });
		}

		/// <summary>
		/// Registers quota services with the dependency injection container using the in-memory store and custom configuration options.
		/// </summary>
		/// <param name="services">The <see cref="IServiceCollection"/> to register services with.</param>
		/// <param name="configureOptions">An action delegate to configure <see cref="QuotaOptions"/> properties.</param>
		/// <returns>The same <see cref="IServiceCollection"/> instance for method chaining.</returns>
		/// <remarks>
		/// This overload allows customization of quota behavior through the options pattern while using
		/// the default in-memory store. Use the configuration action to customize properties such as:
		/// <list type="bullet">
		/// <item><description><see cref="QuotaOptions.EnableEnforcement"/> - Enable or disable quota enforcement globally</description></item>
		/// <item><description><see cref="QuotaOptions.TrackedQuotas"/> - Specify which quota names to monitor</description></item>
		/// <item><description><see cref="QuotaOptions.QuotaExceededMessage"/> - Customize the message returned when quotas are exceeded</description></item>
		/// <item><description><see cref="QuotaOptions.AllowIfQuotaNotDefined"/> - Control behavior for undefined quotas</description></item>
		/// <item><description><see cref="QuotaOptions.IncludeQuotaHeaders"/> - Enable or disable quota headers in responses</description></item>
		/// </list>
		/// The following services are registered:
		/// <list type="bullet">
		/// <item><description><see cref="IQuotaStore"/> as <see cref="InMemoryQuotaStore"/> (Singleton lifetime)</description></item>
		/// <item><description><see cref="QuotaService"/> (Scoped lifetime)</description></item>
		/// <item><description><see cref="QuotaOptions"/> with custom configuration</description></item>
		/// </list>
		/// The in-memory store remains suitable only for single-instance deployments.
		/// </remarks>
		public static IServiceCollection AddSaasQuotas(this IServiceCollection services, Action<QuotaOptions> configureOptions)
		{
			// Configure options using the provided action (properties are set via the action)
			_ = services.Configure(configureOptions);

			// Register the in-memory quota store as a singleton (one instance for the application lifetime)
			_ = services.AddSingleton<IQuotaStore, InMemoryQuotaStore>();

			// Register the quota service as scoped (one instance per HTTP request or scope)
			_ = services.AddScoped<QuotaService>();

			return services;
		}

		/// <summary>
		/// Registers quota services with the dependency injection container using a custom quota store implementation and default options.
		/// </summary>
		/// <typeparam name="TQuotaStore">The concrete type implementing <see cref="IQuotaStore"/>. Must have a public constructor compatible with dependency injection.</typeparam>
		/// <param name="services">The <see cref="IServiceCollection"/> to register services with.</param>
		/// <returns>The same <see cref="IServiceCollection"/> instance for method chaining.</returns>
		/// <remarks>
		/// This overload allows substitution of the default in-memory store with a custom implementation
		/// while using default quota options. Common custom store implementations include:
		/// <list type="bullet">
		/// <item><description>Redis-backed stores for distributed scenarios with multiple application instances</description></item>
		/// <item><description>SQL database stores for persistent quota tracking with transaction support</description></item>
		/// <item><description>Distributed cache stores (e.g., NCache, Azure Cache) for cloud-native deployments</description></item>
		/// <item><description>Hybrid stores combining in-memory caching with persistent backends</description></item>
		/// </list>
		/// The custom store type must implement <see cref="IQuotaStore"/> and have a public constructor
		/// that can be satisfied by the dependency injection container. Constructor parameters will be
		/// automatically resolved from registered services.
		/// The following services are registered:
		/// <list type="bullet">
		/// <item><description><see cref="IQuotaStore"/> as <typeparamref name="TQuotaStore"/> (Singleton lifetime)</description></item>
		/// <item><description><see cref="QuotaService"/> (Scoped lifetime)</description></item>
		/// <item><description><see cref="QuotaOptions"/> with default configuration</description></item>
		/// </list>
		/// </remarks>
		public static IServiceCollection AddSaasQuotas<TQuotaStore>(this IServiceCollection services)
			where TQuotaStore : class, IQuotaStore
		{
			// Delegate to the overload with an empty configuration action (use defaults)
			return services.AddSaasQuotas<TQuotaStore>(_ => { });
		}

		/// <summary>
		/// Registers quota services with the dependency injection container using a custom quota store implementation and custom configuration options.
		/// </summary>
		/// <typeparam name="TQuotaStore">The concrete type implementing <see cref="IQuotaStore"/>. Must have a public constructor compatible with dependency injection.</typeparam>
		/// <param name="services">The <see cref="IServiceCollection"/> to register services with.</param>
		/// <param name="configureOptions">An action delegate to configure <see cref="QuotaOptions"/> properties.</param>
		/// <returns>The same <see cref="IServiceCollection"/> instance for method chaining.</returns>
		/// <remarks>
		/// This is the most flexible overload, allowing both custom quota store implementation and custom configuration.
		/// It provides complete control over all aspects of the quota system including storage backend and enforcement behavior.
		/// Use this overload for production-ready scenarios requiring:
		/// <list type="bullet">
		/// <item><description>Distributed quota tracking across multiple application instances</description></item>
		/// <item><description>Persistent quota data that survives application restarts</description></item>
		/// <item><description>Custom enforcement policies and quota tracking strategies</description></item>
		/// <item><description>Integration with existing data stores or caching infrastructure</description></item>
		/// </list>
		/// The custom store implementation is registered as a singleton, meaning a single instance serves
		/// all requests throughout the application lifetime. The store must handle thread safety and concurrent
		/// access appropriately, typically through thread-safe collections, locking mechanisms, or atomic operations.
		/// The following services are registered:
		/// <list type="bullet">
		/// <item><description><see cref="IQuotaStore"/> as <typeparamref name="TQuotaStore"/> (Singleton lifetime)</description></item>
		/// <item><description><see cref="QuotaService"/> (Scoped lifetime)</description></item>
		/// <item><description><see cref="QuotaOptions"/> with custom configuration</description></item>
		/// </list>
		/// </remarks>
		public static IServiceCollection AddSaasQuotas<TQuotaStore>(this IServiceCollection services, Action<QuotaOptions> configureOptions)
			where TQuotaStore : class, IQuotaStore
		{
			// Configure options using the provided action
			_ = services.Configure(configureOptions);

			// Register the custom quota store implementation as a singleton
			// The store instance will be resolved once and reused for all requests
			_ = services.AddSingleton<IQuotaStore, TQuotaStore>();

			// Register the quota service as scoped (new instance per HTTP request or scope)
			_ = services.AddScoped<QuotaService>();

			return services;
		}

		#endregion
	}
}