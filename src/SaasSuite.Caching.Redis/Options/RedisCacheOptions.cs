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

using SaasSuite.Caching.Options;
using SaasSuite.Caching.Redis.Services;

using StackExchange.Redis;

namespace SaasSuite.Caching.Redis.Options
{
	/// <summary>
	/// Configuration options for the Redis-backed distributed cache service.
	/// </summary>
	/// <remarks>
	/// This class provides configuration settings specific to Redis connectivity and database selection.
	/// Options can be configured through the ASP.NET Core options pattern or via configuration files.
	/// Redis-specific settings are combined with general <see cref="CacheOptions"/>
	/// to fully configure the <see cref="RedisCacheService"/>.
	/// </remarks>
	public class RedisCacheOptions
	{
		#region ' Properties '

		/// <summary>
		/// Gets or sets the Redis logical database index to use for cache operations.
		/// </summary>
		/// <value>
		/// An integer representing the Redis database index. Defaults to -1, which uses the default database (typically database 0).
		/// Valid values range from -1 to the maximum number of databases configured on the Redis server (typically 0-15).
		/// </value>
		/// <remarks>
		/// Redis supports multiple logical databases within a single instance, allowing data segregation.
		/// Common use cases for different database indices include:
		/// <list type="bullet">
		/// <item><description>Separating cache data by environment (dev, staging, production)</description></item>
		/// <item><description>Isolating different applications sharing the same Redis instance</description></item>
		/// <item><description>Segregating different types of cached data</description></item>
		/// </list>
		/// A value of -1 instructs StackExchange.Redis to use the connection's default database.
		/// Note that using separate database indices is less efficient than using key prefixes for multi-tenancy
		/// and is not supported in Redis Cluster mode. For distributed Redis deployments, use
		/// <see cref="CacheOptions.KeyPrefix"/> instead.
		/// </remarks>
		public int Database { get; set; } = -1;

		/// <summary>
		/// Gets or sets the Redis connection string used to establish a connection to the Redis server.
		/// </summary>
		/// <value>
		/// A connection string in StackExchange.Redis format (e.g., "localhost:6379" or "redis.example.com:6379,password=secret").
		/// Defaults to <see langword="null"/>.
		/// </value>
		/// <remarks>
		/// This connection string is used only when <see cref="ConnectionMultiplexerFactory"/> is not provided.
		/// The string should follow StackExchange.Redis configuration format and may include:
		/// <list type="bullet">
		/// <item><description>Host and port (e.g., "localhost:6379")</description></item>
		/// <item><description>Password authentication (e.g., "password=secretkey")</description></item>
		/// <item><description>SSL/TLS settings (e.g., "ssl=true")</description></item>
		/// <item><description>Connection timeout and retry settings</description></item>
		/// </list>
		/// If both <see cref="ConnectionString"/> and <see cref="ConnectionMultiplexerFactory"/> are <see langword="null"/>,
		/// an <see cref="InvalidOperationException"/> will be thrown during service initialization.
		/// For production environments, consider storing connection strings in secure configuration providers
		/// like Azure Key Vault or AWS Secrets Manager.
		/// </remarks>
		public string? ConnectionString { get; set; }

		/// <summary>
		/// Gets or sets a factory function that creates a custom <see cref="IConnectionMultiplexer"/> instance.
		/// </summary>
		/// <value>
		/// A factory function returning a configured <see cref="IConnectionMultiplexer"/>,
		/// or <see langword="null"/> to use <see cref="ConnectionString"/> instead.
		/// Defaults to <see langword="null"/>.
		/// </value>
		/// <remarks>
		/// When this property is set, <see cref="ConnectionString"/> is ignored, and the factory function
		/// is invoked to create the Redis connection. This provides maximum flexibility for advanced scenarios:
		/// <list type="bullet">
		/// <item><description>Using pre-configured <see cref="ConfigurationOptions"/></description></item>
		/// <item><description>Implementing custom connection retry logic</description></item>
		/// <item><description>Integrating with connection pooling or external connection managers</description></item>
		/// <item><description>Applying custom event handlers or connection multiplexer settings</description></item>
		/// </list>
		/// The factory function should return a connected and ready-to-use multiplexer instance.
		/// The factory is invoked once during service registration, and the resulting multiplexer
		/// is registered as a singleton in the dependency injection container.
		/// If both <see cref="ConnectionMultiplexerFactory"/> and <see cref="ConnectionString"/> are <see langword="null"/>,
		/// an <see cref="InvalidOperationException"/> will be thrown during service initialization.
		/// </remarks>
		public Func<IConnectionMultiplexer>? ConnectionMultiplexerFactory { get; set; }

		#endregion
	}
}