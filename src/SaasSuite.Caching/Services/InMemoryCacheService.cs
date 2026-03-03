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
using Microsoft.Extensions.Options;

using SaasSuite.Caching.Interfaces;
using SaasSuite.Caching.Options;

namespace SaasSuite.Caching.Services
{
	/// <summary>
	/// Provides an in-memory, thread-safe implementation of <see cref="ICacheService"/> using ASP.NET Core's <see cref="IMemoryCache"/>.
	/// </summary>
	/// <remarks>
	/// This implementation stores cached items in the application's memory, providing fast access
	/// but limited to the memory of a single application instance. It is suitable for:
	/// <list type="bullet">
	/// <item><description>Development and testing environments</description></item>
	/// <item><description>Single-instance deployments</description></item>
	/// <item><description>Caching data that doesn't need to be shared across multiple servers</description></item>
	/// </list>
	/// For distributed scenarios with multiple application instances, consider implementing a distributed
	/// cache like Redis instead. All cached data is lost when the application restarts.
	/// The implementation is thread-safe through the use of <see cref="IMemoryCache"/>.
	/// </remarks>
	public class InMemoryCacheService
		: ICacheService
	{
		#region ' Fields '

		/// <summary>
		/// Configuration options controlling cache behavior.
		/// </summary>
		/// <remarks>
		/// Contains settings like default expiration time, tenant isolation preferences, and key prefixes.
		/// Injected via the options pattern from dependency injection.
		/// </remarks>
		private readonly CacheOptions _options;

		/// <summary>
		/// The underlying memory cache for storing cached items.
		/// </summary>
		/// <remarks>
		/// ASP.NET Core's IMemoryCache provides thread-safe operations and automatic memory management
		/// with support for cache compaction when memory pressure is high.
		/// </remarks>
		private readonly IMemoryCache _cache;

		#endregion

		#region ' Constructors '

		/// <summary>
		/// Initializes a new instance of the <see cref="InMemoryCacheService"/> class.
		/// </summary>
		/// <param name="cache">The <see cref="IMemoryCache"/> instance for storing cached items. Cannot be <see langword="null"/>.</param>
		/// <param name="options">
		/// The <see cref="IOptions{CacheOptions}"/> containing cache configuration.
		/// If <see langword="null"/>, default options are used.
		/// </param>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="cache"/> is <see langword="null"/>.
		/// </exception>
		/// <remarks>
		/// This constructor is invoked by the dependency injection container when <see cref="ICacheService"/> is resolved.
		/// The <paramref name="cache"/> is typically registered via <c>services.AddMemoryCache()</c> in service configuration.
		/// </remarks>
		public InMemoryCacheService(IMemoryCache cache, IOptions<CacheOptions> options)
		{
			// Validate that cache dependency is provided
			this._cache = cache ?? throw new ArgumentNullException(nameof(cache));

			// Extract options value, using defaults if not configured
			this._options = options?.Value ?? new CacheOptions();
		}

		#endregion

		#region ' Methods '

		/// <summary>
		/// Asynchronously removes a cached value by key from in-memory storage.
		/// </summary>
		/// <param name="key">The cache key identifying the cached item to remove. Cannot be <see langword="null"/> or whitespace.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests during the asynchronous operation. Not used in this synchronous implementation.</param>
		/// <returns>A task that represents the asynchronous operation.</returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="key"/> is <see langword="null"/>, empty, or whitespace.
		/// </exception>
		/// <remarks>
		/// This operation is actually synchronous but returns a Task for interface compatibility.
		/// If the key does not exist, the operation completes successfully without error.
		/// </remarks>
		public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
		{
			// Validate that key is provided and not whitespace
			if (string.IsNullOrWhiteSpace(key))
			{
				throw new ArgumentNullException(nameof(key));
			}

			// Remove the key from cache (no-op if key doesn't exist)
			this._cache.Remove(key);

			// Return completed task for async interface compatibility
			return Task.CompletedTask;
		}

		/// <summary>
		/// Asynchronously stores a value in the in-memory cache with fine-grained expiration control.
		/// </summary>
		/// <typeparam name="T">The type of the value to cache.</typeparam>
		/// <param name="key">The cache key identifying the cached item. Cannot be <see langword="null"/> or whitespace.</param>
		/// <param name="value">The value to store in the cache.</param>
		/// <param name="options">
		/// Per-entry options controlling absolute and/or sliding expiration behavior. Cannot be <see langword="null"/>.
		/// </param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests during the asynchronous operation. Not used in this synchronous implementation.</param>
		/// <returns>A task that represents the asynchronous operation.</returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="key"/> is <see langword="null"/>, empty, or whitespace,
		/// or when <paramref name="options"/> is <see langword="null"/>.
		/// </exception>
		/// <remarks>
		/// This operation is actually synchronous but returns a Task for interface compatibility.
		/// The method configures expiration based on the provided options. If absolute expiration is set,
		/// it takes priority. If only sliding expiration is set, the entry refreshes on each access.
		/// If neither is specified, the default expiration from <see cref="CacheOptions"/> is applied.
		/// If the key already exists, it is overwritten with the new value and expiration settings.
		/// </remarks>
		public Task SetAsync<T>(string key, T value, CacheEntryOptions options, CancellationToken cancellationToken = default)
		{
			// Validate that key is provided and not whitespace
			if (string.IsNullOrWhiteSpace(key))
			{
				throw new ArgumentNullException(nameof(key));
			}

			// Validate that options are provided
			ArgumentNullException.ThrowIfNull(options);

			// Create memory cache entry options
			MemoryCacheEntryOptions cacheEntryOptions = new MemoryCacheEntryOptions();

			// Configure absolute expiration if specified
			if (options.AbsoluteExpiration.HasValue)
			{
				cacheEntryOptions.AbsoluteExpirationRelativeToNow = options.AbsoluteExpiration;
			}
			// If no sliding expiration is specified either, use default expiration
			else if (!options.SlidingExpiration.HasValue)
			{
				cacheEntryOptions.AbsoluteExpirationRelativeToNow = this._options.DefaultExpiration;
			}

			// Configure sliding expiration if specified
			if (options.SlidingExpiration.HasValue)
			{
				cacheEntryOptions.SlidingExpiration = options.SlidingExpiration;
			}

			// Store the value in cache with configured options
			this._cache.Set(key, value, cacheEntryOptions);

			// Return completed task for async interface compatibility
			return Task.CompletedTask;
		}

		/// <summary>
		/// Asynchronously stores a value in the in-memory cache with configurable absolute expiration.
		/// </summary>
		/// <typeparam name="T">The type of the value to cache.</typeparam>
		/// <param name="key">The cache key identifying the cached item. Cannot be <see langword="null"/> or whitespace.</param>
		/// <param name="value">The value to store in the cache.</param>
		/// <param name="expiration">
		/// Optional absolute expiration time relative to now. If <see langword="null"/>, uses <see cref="CacheOptions.DefaultExpiration"/>.
		/// </param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests during the asynchronous operation. Not used in this synchronous implementation.</param>
		/// <returns>A task that represents the asynchronous operation.</returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="key"/> is <see langword="null"/>, empty, or whitespace.
		/// </exception>
		/// <remarks>
		/// This operation is actually synchronous but returns a Task for interface compatibility.
		/// The value is stored with absolute expiration relative to the current time.
		/// If the key already exists, it is overwritten with the new value and expiration.
		/// </remarks>
		public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
		{
			// Validate that key is provided and not whitespace
			if (string.IsNullOrWhiteSpace(key))
			{
				throw new ArgumentNullException(nameof(key));
			}

			// Use provided expiration or fall back to configured default
			TimeSpan cacheExpiration = expiration ?? this._options.DefaultExpiration;

			// Configure cache entry with absolute expiration
			MemoryCacheEntryOptions cacheEntryOptions = new MemoryCacheEntryOptions
			{
				AbsoluteExpirationRelativeToNow = cacheExpiration
			};

			// Store the value in cache with configured options
			_ = this._cache.Set(key, value, cacheEntryOptions);

			// Return completed task for async interface compatibility
			return Task.CompletedTask;
		}

		/// <summary>
		/// Asynchronously retrieves a cached value by key from in-memory storage.
		/// </summary>
		/// <typeparam name="T">The type of the cached value.</typeparam>
		/// <param name="key">The cache key identifying the cached item. Cannot be <see langword="null"/> or whitespace.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests during the asynchronous operation. Not used in this synchronous implementation.</param>
		/// <returns>
		/// A task that represents the asynchronous operation. The task result contains the cached value of type <typeparamref name="T"/>
		/// if found, or the default value of <typeparamref name="T"/> if the key does not exist or has expired.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="key"/> is <see langword="null"/>, empty, or whitespace.
		/// </exception>
		/// <remarks>
		/// This operation is actually synchronous but returns a Task for interface compatibility.
		/// The cancellation token is not used as IMemoryCache operations are inherently synchronous.
		/// </remarks>
		public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
		{
			// Validate that key is provided and not whitespace
			if (string.IsNullOrWhiteSpace(key))
			{
				throw new ArgumentNullException(nameof(key));
			}

			// Attempt to retrieve the cached value
			T? value = this._cache.Get<T>(key);

			// Return as completed task for async interface compatibility
			return Task.FromResult(value);
		}

		#endregion
	}
}