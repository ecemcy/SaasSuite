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

using Microsoft.Extensions.DependencyInjection.Extensions;

using SaasSuite.Subscriptions.Interfaces;
using SaasSuite.Subscriptions.Options;
using SaasSuite.Subscriptions.Services;
using SaasSuite.Subscriptions.Stores;

namespace Microsoft.Extensions.DependencyInjection
{
	/// <summary>
	/// Provides extension methods for configuring SaasSuite subscription services in the dependency injection container.
	/// </summary>
	/// <remarks>
	/// These extensions simplify service registration by providing fluent API methods that can be chained in
	/// application startup configuration. They support both the default in-memory implementation (for development)
	/// and custom implementations (for production databases). All methods use <c>TryAdd</c> semantics to avoid
	/// duplicate registrations if services have already been added.
	/// </remarks>
	public static class ServiceCollectionExtensions
	{
		#region ' Static Methods '

		/// <summary>
		/// Registers SaasSuite subscription services with the default in-memory store and default configuration.
		/// </summary>
		/// <param name="services">The service collection to add services to. Cannot be <see langword="null"/>.</param>
		/// <returns>The service collection for method chaining.</returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="services"/> is <see langword="null"/>.
		/// </exception>
		/// <remarks>
		/// This overload uses default <see cref="SubscriptionOptions"/> values and the <see cref="InMemorySubscriptionStore"/>
		/// singleton implementation. Suitable for development, testing, and single-instance deployments.
		/// Registered services:
		/// <list type="bullet">
		/// <item><description><see cref="ISubscriptionStore"/> as <see cref="InMemorySubscriptionStore"/> (singleton)</description></item>
		/// <item><description><see cref="SubscriptionService"/> (scoped)</description></item>
		/// <item><description><see cref="SubscriptionOptions"/> with default values</description></item>
		/// </list>
		/// For production scenarios requiring persistent storage, use the generic overload with a custom store implementation.
		/// </remarks>
		public static IServiceCollection AddSaasSubscriptions(this IServiceCollection services)
		{
			// Delegate to the configuration overload with empty configuration action
			return services.AddSaasSubscriptions(_ => { });
		}

		/// <summary>
		/// Registers SaasSuite subscription services with the default in-memory store and custom configuration.
		/// </summary>
		/// <param name="services">The service collection to add services to. Cannot be <see langword="null"/>.</param>
		/// <param name="configureOptions">An action to configure <see cref="SubscriptionOptions"/>. Cannot be <see langword="null"/>.</param>
		/// <returns>The service collection for method chaining.</returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="services"/> or <paramref name="configureOptions"/> is <see langword="null"/>.
		/// </exception>
		/// <remarks>
		/// This overload allows customization of subscription behavior through the options pattern while using
		/// the default in-memory store. Use the <paramref name="configureOptions"/> action to set properties like:
		/// <list type="bullet">
		/// <item><description><see cref="SubscriptionOptions.AutoCancelExpired"/> - Automatic cancellation of expired subscriptions</description></item>
		/// <item><description><see cref="SubscriptionOptions.GracePeriodDays"/> - Days before suspending past due subscriptions</description></item>
		/// <item><description><see cref="SubscriptionOptions.AllowMultipleSubscriptions"/> - Whether tenants can have multiple active subscriptions</description></item>
		/// <item><description><see cref="SubscriptionOptions.DefaultTrialPeriodDays"/> - Default trial period when plans don't specify one</description></item>
		/// </list>
		/// Registered services:
		/// <list type="bullet">
		/// <item><description><see cref="ISubscriptionStore"/> as <see cref="InMemorySubscriptionStore"/> (singleton)</description></item>
		/// <item><description><see cref="SubscriptionService"/> (scoped)</description></item>
		/// <item><description><see cref="SubscriptionOptions"/> with custom configuration</description></item>
		/// </list>
		/// The in-memory store is suitable for development but data is lost on restart. For production, use the generic overload.
		/// </remarks>
		public static IServiceCollection AddSaasSubscriptions(this IServiceCollection services, Action<SubscriptionOptions> configureOptions)
		{
			// Validate the services collection is not null
			ArgumentNullException.ThrowIfNull(services);

			// Validate the configuration action is not null
			ArgumentNullException.ThrowIfNull(configureOptions);

			// Configure subscription options using the provided action
			_ = services.Configure(configureOptions);

			// Register the default in-memory subscription store as a singleton
			// TryAddSingleton prevents duplicate registration if already added
			services.TryAddSingleton<ISubscriptionStore, InMemorySubscriptionStore>();

			// Register the subscription service as scoped (per HTTP request in web apps)
			services.TryAddScoped<SubscriptionService>();

			// Return the service collection for fluent chaining
			return services;
		}

		/// <summary>
		/// Registers SaasSuite subscription services with a custom subscription store implementation and default configuration.
		/// </summary>
		/// <typeparam name="TStore">
		/// The concrete type implementing <see cref="ISubscriptionStore"/>. Must be a class with a public constructor
		/// compatible with dependency injection.
		/// </typeparam>
		/// <param name="services">The service collection to add services to. Cannot be <see langword="null"/>.</param>
		/// <returns>The service collection for method chaining.</returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="services"/> is <see langword="null"/>.
		/// </exception>
		/// <remarks>
		/// This overload allows substitution of the default in-memory store with a custom implementation while
		/// using default <see cref="SubscriptionOptions"/>. Common custom implementations include:
		/// <list type="bullet">
		/// <item><description>SQL database store using Entity Framework Core or Dapper</description></item>
		/// <item><description>NoSQL database store using MongoDB, CosmosDB, or DynamoDB</description></item>
		/// <item><description>Distributed cache store using Redis</description></item>
		/// <item><description>File-based store for simple persistence</description></item>
		/// </list>
		/// The custom implementation is registered as a singleton, which is typically appropriate for stores
		/// that manage their own connection pooling or caching. If your implementation requires scoped or
		/// transient lifetime, register it manually instead of using this extension.
		/// Registered services:
		/// <list type="bullet">
		/// <item><description><see cref="ISubscriptionStore"/> as <typeparamref name="TStore"/> (singleton)</description></item>
		/// <item><description><see cref="SubscriptionService"/> (scoped)</description></item>
		/// <item><description><see cref="SubscriptionOptions"/> with default values</description></item>
		/// </list>
		/// The implementation type must have a constructor that can be satisfied by the DI container.
		/// </remarks>
		public static IServiceCollection AddSaasSubscriptions<TStore>(this IServiceCollection services)
			where TStore : class, ISubscriptionStore
		{
			// Delegate to the configuration overload with empty configuration action
			return services.AddSaasSubscriptions<TStore>(_ => { });
		}

		/// <summary>
		/// Registers SaasSuite subscription services with a custom subscription store implementation and custom configuration.
		/// </summary>
		/// <typeparam name="TStore">
		/// The concrete type implementing <see cref="ISubscriptionStore"/>. Must be a class with a public constructor
		/// compatible with dependency injection.
		/// </typeparam>
		/// <param name="services">The service collection to add services to. Cannot be <see langword="null"/>.</param>
		/// <param name="configureOptions">An action to configure <see cref="SubscriptionOptions"/>. Cannot be <see langword="null"/>.</param>
		/// <returns>The service collection for method chaining.</returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="services"/> or <paramref name="configureOptions"/> is <see langword="null"/>.
		/// </exception>
		/// <remarks>
		/// This is the most flexible overload, allowing both a custom store implementation and custom configuration.
		/// Use this for production scenarios requiring:
		/// <list type="bullet">
		/// <item><description>Persistent storage with custom database implementations</description></item>
		/// <item><description>Specific subscription behavior policies via options configuration</description></item>
		/// <item><description>Custom lifecycle management for the store (singleton is default)</description></item>
		/// </list>
		/// The <paramref name="configureOptions"/> action configures subscription behavior:
		/// <list type="bullet">
		/// <item><description><see cref="SubscriptionOptions.AutoCancelExpired"/> - Automatic expiration handling</description></item>
		/// <item><description><see cref="SubscriptionOptions.GracePeriodDays"/> - Payment failure grace period</description></item>
		/// <item><description><see cref="SubscriptionOptions.AllowMultipleSubscriptions"/> - Multi-subscription support per tenant</description></item>
		/// <item><description><see cref="SubscriptionOptions.DefaultTrialPeriodDays"/> - Fallback trial period duration</description></item>
		/// </list>
		/// Registered services:
		/// <list type="bullet">
		/// <item><description><see cref="ISubscriptionStore"/> as <typeparamref name="TStore"/> (singleton)</description></item>
		/// <item><description><see cref="SubscriptionService"/> (scoped)</description></item>
		/// <item><description><see cref="SubscriptionOptions"/> with custom configuration</description></item>
		/// </list>
		/// The custom store type must have a constructor compatible with dependency injection. Common dependencies
		/// include DbContext, IConfiguration, ILogger, or other registered services. The store is registered as
		/// singleton to share state across requests, but ensure thread-safety if storing mutable data.
		/// </remarks>
		public static IServiceCollection AddSaasSubscriptions<TStore>(this IServiceCollection services, Action<SubscriptionOptions> configureOptions)
			where TStore : class, ISubscriptionStore
		{
			// Validate the services collection is not null
			ArgumentNullException.ThrowIfNull(services);

			// Validate the configuration action is not null
			ArgumentNullException.ThrowIfNull(configureOptions);

			// Configure subscription options using the provided action
			_ = services.Configure(configureOptions);

			// Register the custom subscription store implementation as a singleton
			// TryAddSingleton prevents duplicate registration if already added
			services.TryAddSingleton<ISubscriptionStore, TStore>();

			// Register the subscription service as scoped (per HTTP request in web apps)
			services.TryAddScoped<SubscriptionService>();

			// Return the service collection for fluent chaining
			return services;
		}

		#endregion
	}
}