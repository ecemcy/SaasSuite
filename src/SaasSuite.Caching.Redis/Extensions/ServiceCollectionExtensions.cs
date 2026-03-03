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

using SaasSuite.Caching.Interfaces;
using SaasSuite.Caching.Options;
using SaasSuite.Caching.Redis.Options;
using SaasSuite.Caching.Redis.Services;

using StackExchange.Redis;

namespace Microsoft.Extensions.DependencyInjection
{
	/// <summary>
	/// Provides extension methods for registering Redis-backed distributed caching services in the dependency injection container.
	/// </summary>
	/// <remarks>
	/// These extensions simplify the registration of Redis-based caching services by providing fluent API methods
	/// that support both connection string-based and pre-configured multiplexer-based registration patterns.
	/// The methods register <see cref="RedisCacheService"/> as the implementation of <see cref="ICacheService"/>
	/// along with the necessary <see cref="IConnectionMultiplexer"/> for Redis connectivity.
	/// </remarks>
	public static class ServiceCollectionExtensions
	{
		#region ' Static Methods '

		/// <summary>
		/// Registers Redis-backed caching services using a connection string or custom factory.
		/// </summary>
		/// <param name="services">
		/// The <see cref="IServiceCollection"/> to add services to. Cannot be <see langword="null"/>.
		/// </param>
		/// <param name="configureRedis">
		/// An action to configure <see cref="RedisCacheOptions"/> including connection string, database index,
		/// or connection multiplexer factory. Cannot be <see langword="null"/>.
		/// </param>
		/// <param name="configureCaching">
		/// Optional action to configure general <see cref="CacheOptions"/> such as default expiration
		/// time and key prefix. When <see langword="null"/>, default cache options are used.
		/// </param>
		/// <returns>
		/// The <see cref="IServiceCollection"/> for method chaining.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="services"/> or <paramref name="configureRedis"/> is <see langword="null"/>.
		/// </exception>
		/// <exception cref="InvalidOperationException">
		/// Thrown at runtime if neither <see cref="RedisCacheOptions.ConnectionString"/> nor
		/// <see cref="RedisCacheOptions.ConnectionMultiplexerFactory"/> is provided in <paramref name="configureRedis"/>.
		/// </exception>
		/// <remarks>
		/// This method registers the following services:
		/// <list type="bullet">
		/// <item><description><see cref="IConnectionMultiplexer"/> as a singleton (created from connection string or factory)</description></item>
		/// <item><description><see cref="RedisCacheService"/> as the singleton implementation of <see cref="ICacheService"/></description></item>
		/// <item><description><see cref="RedisCacheOptions"/> configuration</description></item>
		/// <item><description><see cref="CacheOptions"/> configuration (if <paramref name="configureCaching"/> is provided)</description></item>
		/// </list>
		/// The <see cref="IConnectionMultiplexer"/> is created lazily during service resolution using either
		/// the <see cref="RedisCacheOptions.ConnectionMultiplexerFactory"/> (if provided) or by connecting
		/// to <see cref="RedisCacheOptions.ConnectionString"/>. If <see cref="RedisCacheOptions.ConnectionMultiplexerFactory"/>
		/// is set, it takes precedence over <see cref="RedisCacheOptions.ConnectionString"/>.
		/// This registration pattern is suitable for most scenarios where the application controls the Redis connection lifecycle.
		/// </remarks>
		public static IServiceCollection AddSaasCachingRedis(this IServiceCollection services, Action<RedisCacheOptions> configureRedis, Action<CacheOptions>? configureCaching = null)
		{
			// Validate that services collection is not null
			ArgumentNullException.ThrowIfNull(services);

			// Validate that Redis configuration action is not null
			ArgumentNullException.ThrowIfNull(configureRedis);

			// Register Redis-specific configuration options
			_ = services.Configure(configureRedis);

			// Register general cache configuration options if provided
			if (configureCaching != null)
			{
				_ = services.Configure(configureCaching);
			}

			// Register IConnectionMultiplexer as a singleton with lazy initialization
			_ = services.AddSingleton<IConnectionMultiplexer>(sp =>
			{
				// Resolve the configured Redis options
				RedisCacheOptions options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<RedisCacheOptions>>().Value;

				// Use custom factory if provided
				if (options.ConnectionMultiplexerFactory != null)
				{
					return options.ConnectionMultiplexerFactory();
				}

				// Validate that a connection string is provided
				if (string.IsNullOrWhiteSpace(options.ConnectionString))
				{
					throw new InvalidOperationException(
						"A Redis connection string must be provided via RedisCacheOptions.ConnectionString, " +
						"or supply a custom ConnectionMultiplexerFactory.");
				}

				// Create and return a connection multiplexer from the connection string
				return ConnectionMultiplexer.Connect(options.ConnectionString);
			});

			// Register RedisCacheService as the singleton implementation of ICacheService
			_ = services.AddSingleton<ICacheService, RedisCacheService>();

			// Return the service collection for fluent chaining
			return services;
		}

		/// <summary>
		/// Registers Redis-backed caching services using a pre-configured connection multiplexer.
		/// </summary>
		/// <param name="services">
		/// The <see cref="IServiceCollection"/> to add services to. Cannot be <see langword="null"/>.
		/// </param>
		/// <param name="multiplexer">
		/// An already-connected <see cref="IConnectionMultiplexer"/> instance to use for Redis operations.
		/// Cannot be <see langword="null"/>. The application is responsible for managing the multiplexer's lifecycle.
		/// </param>
		/// <param name="configureRedis">
		/// Optional action to configure <see cref="RedisCacheOptions"/> such as database index.
		/// When <see langword="null"/>, default Redis options are used (database -1).
		/// Note that <see cref="RedisCacheOptions.ConnectionString"/> and
		/// <see cref="RedisCacheOptions.ConnectionMultiplexerFactory"/> are ignored when using this overload.
		/// </param>
		/// <param name="configureCaching">
		/// Optional action to configure general <see cref="CacheOptions"/> such as default expiration
		/// time and key prefix. When <see langword="null"/>, default cache options are used.
		/// </param>
		/// <returns>
		/// The <see cref="IServiceCollection"/> for method chaining.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="services"/> or <paramref name="multiplexer"/> is <see langword="null"/>.
		/// </exception>
		/// <remarks>
		/// This method registers the following services:
		/// <list type="bullet">
		/// <item><description>The provided <paramref name="multiplexer"/> as a singleton <see cref="IConnectionMultiplexer"/></description></item>
		/// <item><description><see cref="RedisCacheService"/> as the singleton implementation of <see cref="ICacheService"/></description></item>
		/// <item><description><see cref="RedisCacheOptions"/> configuration (if <paramref name="configureRedis"/> is provided)</description></item>
		/// <item><description><see cref="CacheOptions"/> configuration (if <paramref name="configureCaching"/> is provided)</description></item>
		/// </list>
		/// This overload is suitable for scenarios where the application manages the Redis connection externally,
		/// such as when sharing a connection multiplexer across multiple components or when using advanced
		/// connection configuration not supported by simple connection strings. The provided <paramref name="multiplexer"/>
		/// is registered directly without any wrapping or factory pattern.
		/// The application remains responsible for disposing the multiplexer when appropriate.
		/// </remarks>
		public static IServiceCollection AddSaasCachingRedis(this IServiceCollection services, IConnectionMultiplexer multiplexer, Action<RedisCacheOptions>? configureRedis = null, Action<CacheOptions>? configureCaching = null)
		{
			// Validate that services collection is not null
			ArgumentNullException.ThrowIfNull(services);

			// Validate that the multiplexer is provided
			ArgumentNullException.ThrowIfNull(multiplexer);

			// Register Redis-specific configuration options if provided
			if (configureRedis != null)
			{
				_ = services.Configure(configureRedis);
			}

			// Register general cache configuration options if provided
			if (configureCaching != null)
			{
				_ = services.Configure(configureCaching);
			}

			// Register the provided multiplexer as a singleton
			_ = services.AddSingleton(multiplexer);

			// Register RedisCacheService as the singleton implementation of ICacheService
			_ = services.AddSingleton<ICacheService, RedisCacheService>();

			// Return the service collection for fluent chaining
			return services;
		}

		#endregion
	}
}