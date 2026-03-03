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

using System.Text.Json;

using Microsoft.Extensions.Options;

using SaasSuite.Caching.Interfaces;
using SaasSuite.Caching.Options;
using SaasSuite.Caching.Redis.Options;

using StackExchange.Redis;

namespace SaasSuite.Caching.Redis.Services
{
	/// <summary>
	/// Provides a Redis-backed distributed implementation of <see cref="ICacheService"/> using StackExchange.Redis.
	/// </summary>
	/// <remarks>
	/// This implementation stores cached items in a Redis server, enabling cache sharing across multiple
	/// application instances in distributed deployments. The service uses System.Text.Json for serialization
	/// and StackExchange.Redis for Redis connectivity. Key features include:
	/// <list type="bullet">
	/// <item><description>Distributed caching suitable for multi-instance deployments</description></item>
	/// <item><description>Automatic JSON serialization and deserialization</description></item>
	/// <item><description>Graceful handling of deserialization failures (treats as cache miss)</description></item>
	/// <item><description>Support for absolute expiration with best-effort sliding expiration</description></item>
	/// <item><description>Configurable key prefixes for namespace isolation</description></item>
	/// </list>
	/// The class is sealed to prevent inheritance and ensure predictable caching behavior.
	/// All operations are asynchronous and thread-safe through StackExchange.Redis's connection multiplexer.
	/// </remarks>
	public sealed class RedisCacheService
		: ICacheService
	{
		#region ' Fields '

		/// <summary>
		/// Configuration options controlling general cache behavior.
		/// </summary>
		/// <remarks>
		/// Contains settings shared across all cache implementations, such as default expiration
		/// time and key prefix. These options are merged with Redis-specific configuration.
		/// </remarks>
		private readonly CacheOptions _cacheOptions;

		/// <summary>
		/// The Redis database instance used for cache operations.
		/// </summary>
		/// <remarks>
		/// Obtained from <see cref="IConnectionMultiplexer"/> using the database index
		/// specified in <see cref="RedisCacheOptions.Database"/>. This field provides
		/// access to all Redis commands for storing, retrieving, and deleting cache entries.
		/// </remarks>
		private readonly IDatabase _database;

		#endregion

		#region ' Static Fields '

		/// <summary>
		/// JSON serializer options configured for web defaults.
		/// </summary>
		/// <remarks>
		/// Uses <see cref="JsonSerializerDefaults.Web"/> for consistent serialization behavior
		/// including camelCase property names, case-insensitive deserialization, and number handling.
		/// This static field is shared across all instances to optimize memory usage.
		/// </remarks>
		private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);

		#endregion

		#region ' Constructors '

		/// <summary>
		/// Initializes a new instance of the <see cref="RedisCacheService"/> class.
		/// </summary>
		/// <param name="multiplexer">
		/// The Redis connection multiplexer providing access to Redis servers. Cannot be <see langword="null"/>.
		/// </param>
		/// <param name="redisCacheOptions">
		/// Redis-specific configuration options including database index. Cannot be <see langword="null"/>.
		/// </param>
		/// <param name="cacheOptions">
		/// General cache configuration options including default expiration and key prefix. Cannot be <see langword="null"/>.
		/// </param>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="multiplexer"/>, <paramref name="redisCacheOptions"/>,
		/// or <paramref name="cacheOptions"/> is <see langword="null"/>.
		/// </exception>
		/// <remarks>
		/// This constructor is invoked by the dependency injection container when <see cref="ICacheService"/> is resolved.
		/// The Redis database is obtained from the multiplexer using the configured database index.
		/// If the database index is -1, the connection's default database is used.
		/// </remarks>
		public RedisCacheService(IConnectionMultiplexer multiplexer, IOptions<RedisCacheOptions> redisCacheOptions, IOptions<CacheOptions> cacheOptions)
		{
			// Validate that all dependencies are provided
			ArgumentNullException.ThrowIfNull(multiplexer);
			ArgumentNullException.ThrowIfNull(redisCacheOptions);
			ArgumentNullException.ThrowIfNull(cacheOptions);

			// Get the Redis database instance using the configured database index
			this._database = multiplexer.GetDatabase(redisCacheOptions.Value.Database);

			// Extract cache options value, using defaults if not configured
			this._cacheOptions = cacheOptions.Value ?? new CacheOptions();
		}

		#endregion

		#region ' Methods '

		/// <summary>
		/// Builds the final Redis key by prepending the configured key prefix.
		/// </summary>
		/// <param name="key">The base cache key provided by the caller.</param>
		/// <returns>
		/// The final Redis key with the configured prefix applied, or the original key
		/// if no prefix is configured or the prefix is empty.
		/// </returns>
		/// <remarks>
		/// This method applies the <see cref="CacheOptions.KeyPrefix"/> to the provided key
		/// for namespace isolation. If the prefix is <see langword="null"/> or empty, the
		/// original key is returned unchanged. The prefix helps prevent key collisions when
		/// multiple applications share the same Redis instance.
		/// </remarks>
		private string BuildKey(string key)
		{
			return this._cacheOptions.KeyPrefix is { Length: > 0 } prefix
				? $"{prefix}{key}"
				: key;
		}

		/// <summary>
		/// Asynchronously removes a cached value by key from Redis storage.
		/// </summary>
		/// <param name="key">
		/// The unique cache key identifying the cached item to remove. Cannot be <see langword="null"/>, empty, or whitespace.
		/// The key is automatically prefixed with <see cref="CacheOptions.KeyPrefix"/> if configured.
		/// </param>
		/// <param name="cancellationToken">
		/// A token to monitor for cancellation requests during the asynchronous operation.
		/// </param>
		/// <returns>
		/// A task that represents the asynchronous operation.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="key"/> is <see langword="null"/>, empty, or whitespace.
		/// </exception>
		/// <remarks>
		/// This method performs a Redis DEL operation to remove the specified key.
		/// If the key does not exist, the operation completes successfully without error.
		/// The key is prefixed using <see cref="BuildKey"/> to ensure proper namespace isolation.
		/// Use this method to invalidate cache entries when underlying data changes.
		/// </remarks>
		public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
		{
			// Validate that key is provided and not whitespace
			if (string.IsNullOrWhiteSpace(key))
			{
				throw new ArgumentNullException(nameof(key));
			}

			// Delete the key from Redis
			_ = await this._database.KeyDeleteAsync(this.BuildKey(key)).ConfigureAwait(false);
		}

		/// <summary>
		/// Asynchronously stores a value in Redis cache with fine-grained expiration control.
		/// </summary>
		/// <typeparam name="T">
		/// The type of the value to cache. Must be JSON-serializable.
		/// </typeparam>
		/// <param name="key">
		/// The unique cache key identifying the cached item. Cannot be <see langword="null"/>, empty, or whitespace.
		/// The key is automatically prefixed with <see cref="CacheOptions.KeyPrefix"/> if configured.
		/// </param>
		/// <param name="value">
		/// The value to store in the cache. The value is serialized to JSON before storage.
		/// </param>
		/// <param name="options">
		/// Per-entry options controlling expiration behavior. Cannot be <see langword="null"/>.
		/// </param>
		/// <param name="cancellationToken">
		/// A token to monitor for cancellation requests during the asynchronous operation.
		/// </param>
		/// <returns>
		/// A task that represents the asynchronous operation.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="key"/> is <see langword="null"/>, empty, or whitespace,
		/// or when <paramref name="options"/> is <see langword="null"/>.
		/// </exception>
		/// <remarks>
		/// This method provides fine-grained control over cache entry expiration through <see cref="CacheEntryOptions"/>.
		/// Redis does not natively support sliding expiration (automatic extension on access). The method handles
		/// expiration options with the following priority:
		/// <list type="number">
		/// <item><description>If <see cref="CacheEntryOptions.AbsoluteExpiration"/> is set, it is used as the Redis TTL</description></item>
		/// <item><description>If only <see cref="CacheEntryOptions.SlidingExpiration"/> is set, it is used as a fixed TTL (best-effort approximation)</description></item>
		/// <item><description>If neither is set, <see cref="CacheOptions.DefaultExpiration"/> is used</description></item>
		/// </list>
		/// The value is serialized to JSON using System.Text.Json and stored in Redis with the determined TTL.
		/// If the key already exists, its value and expiration are overwritten.
		/// The key is prefixed using <see cref="BuildKey"/> to ensure proper namespace isolation.
		/// </remarks>
		public async Task SetAsync<T>(string key, T value, CacheEntryOptions options, CancellationToken cancellationToken = default)
		{
			// Validate that key is provided and not whitespace
			if (string.IsNullOrWhiteSpace(key))
			{
				throw new ArgumentNullException(nameof(key));
			}

			// Validate that options are provided
			ArgumentNullException.ThrowIfNull(options);

			// Determine the TTL based on the provided options
			// Redis does not natively support sliding expiration, so we use absolute expiration when
			// set; otherwise fall back to the default. When only sliding expiration is provided, we use
			// it as a fixed TTL (best-effort approximation for Redis).
			TimeSpan ttl;
			if (options.AbsoluteExpiration.HasValue)
			{
				// Prefer absolute expiration if specified
				ttl = options.AbsoluteExpiration.Value;
			}
			else if (options.SlidingExpiration.HasValue)
			{
				// Use sliding expiration as fixed TTL (Redis limitation)
				ttl = options.SlidingExpiration.Value;
			}
			else
			{
				// Fall back to default expiration
				ttl = this._cacheOptions.DefaultExpiration;
			}

			// Serialize the value to JSON
			string json = JsonSerializer.Serialize(value, _jsonOptions);

			// Store in Redis with the determined TTL
			_ = await this._database.StringSetAsync(this.BuildKey(key), json, ttl).ConfigureAwait(false);
		}

		/// <summary>
		/// Asynchronously stores a value in Redis cache with configurable absolute expiration.
		/// </summary>
		/// <typeparam name="T">
		/// The type of the value to cache. Must be JSON-serializable.
		/// </typeparam>
		/// <param name="key">
		/// The unique cache key identifying the cached item. Cannot be <see langword="null"/>, empty, or whitespace.
		/// The key is automatically prefixed with <see cref="CacheOptions.KeyPrefix"/> if configured.
		/// </param>
		/// <param name="value">
		/// The value to store in the cache. The value is serialized to JSON before storage.
		/// </param>
		/// <param name="expiration">
		/// Optional absolute expiration time relative to now. If <see langword="null"/>,
		/// uses <see cref="CacheOptions.DefaultExpiration"/> from configuration.
		/// </param>
		/// <param name="cancellationToken">
		/// A token to monitor for cancellation requests during the asynchronous operation.
		/// </param>
		/// <returns>
		/// A task that represents the asynchronous operation.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="key"/> is <see langword="null"/>, empty, or whitespace.
		/// </exception>
		/// <remarks>
		/// This method serializes the value to JSON using System.Text.Json and stores it in Redis
		/// with the specified or default expiration time. The expiration is absolute relative to the current time.
		/// If the key already exists, its value and expiration are overwritten.
		/// The key is prefixed using <see cref="BuildKey"/> to ensure proper namespace isolation.
		/// </remarks>
		public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
		{
			// Validate that key is provided and not whitespace
			if (string.IsNullOrWhiteSpace(key))
			{
				throw new ArgumentNullException(nameof(key));
			}

			// Use provided expiration or fall back to configured default
			TimeSpan ttl = expiration ?? this._cacheOptions.DefaultExpiration;

			// Serialize the value to JSON
			string json = JsonSerializer.Serialize(value, _jsonOptions);

			// Store in Redis with the specified TTL
			_ = await this._database.StringSetAsync(this.BuildKey(key), json, ttl).ConfigureAwait(false);
		}

		/// <summary>
		/// Asynchronously retrieves a cached value by key from Redis storage.
		/// </summary>
		/// <typeparam name="T">
		/// The type of the cached value. Must be JSON-serializable.
		/// </typeparam>
		/// <param name="key">
		/// The unique cache key identifying the cached item. Cannot be <see langword="null"/>, empty, or whitespace.
		/// The key is automatically prefixed with <see cref="CacheOptions.KeyPrefix"/> if configured.
		/// </param>
		/// <param name="cancellationToken">
		/// A token to monitor for cancellation requests during the asynchronous operation.
		/// </param>
		/// <returns>
		/// A task that represents the asynchronous operation. The task result contains the cached value
		/// of type <typeparamref name="T"/> if found and successfully deserialized, or the default value
		/// of <typeparamref name="T"/> if the key does not exist, has expired, or deserialization fails.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="key"/> is <see langword="null"/>, empty, or whitespace.
		/// </exception>
		/// <remarks>
		/// This method performs a Redis GET operation and deserializes the retrieved JSON string to type <typeparamref name="T"/>.
		/// If deserialization fails due to corrupted data or schema mismatch, the corrupted entry is automatically
		/// deleted from Redis, and the default value for <typeparamref name="T"/> is returned.
		/// The key is prefixed using <see cref="BuildKey"/> to ensure proper namespace isolation.
		/// </remarks>
		public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
		{
			// Validate that key is provided and not whitespace
			if (string.IsNullOrWhiteSpace(key))
			{
				throw new ArgumentNullException(nameof(key));
			}

			// Build the full Redis key with prefix
			string redisKey = this.BuildKey(key);

			// Retrieve the raw string value from Redis
			RedisValue value = await this._database.StringGetAsync(redisKey).ConfigureAwait(false);

			// Return default if key doesn't exist or has expired
			if (value.IsNullOrEmpty)
			{
				return default;
			}

			try
			{
				// Attempt to deserialize the JSON string to the requested type
				return JsonSerializer.Deserialize<T>(value.ToString(), _jsonOptions);
			}
			catch (JsonException)
			{
				// Treat deserialization failure as a cache miss and remove the corrupted entry
				// This prevents repeated deserialization attempts for corrupted data
				_ = await this._database.KeyDeleteAsync(redisKey).ConfigureAwait(false);
				return default;
			}
		}

		#endregion
	}
}