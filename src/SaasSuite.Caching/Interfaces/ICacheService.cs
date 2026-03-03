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

using SaasSuite.Caching.Helpers;
using SaasSuite.Caching.Options;

namespace SaasSuite.Caching.Interfaces
{
	/// <summary>
	/// Defines the contract for asynchronous cache operations in a multi-tenant SaaS application.
	/// </summary>
	/// <remarks>
	/// This interface provides a unified abstraction over various caching implementations
	/// (in-memory, distributed, Redis, etc.), enabling cache strategy changes without code modifications.
	/// All operations are asynchronous to support non-blocking I/O for distributed cache scenarios.
	/// Implementations should be thread-safe and handle concurrent access appropriately.
	/// </remarks>
	public interface ICacheService
	{
		#region ' Methods '

		/// <summary>
		/// Asynchronously removes a cached value by key.
		/// </summary>
		/// <param name="key">The unique cache key identifying the cached item to remove. Cannot be <see langword="null"/> or whitespace.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests during the asynchronous operation.</param>
		/// <returns>A task that represents the asynchronous operation.</returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="key"/> is <see langword="null"/>, empty, or whitespace.
		/// </exception>
		/// <remarks>
		/// This method removes the specified key from the cache if it exists.
		/// If the key does not exist, the operation completes successfully without error.
		/// Use this method to invalidate cache entries when underlying data changes,
		/// or to implement cache eviction strategies.
		/// For tenant-specific caching, use <see cref="TenantCacheKeyHelper.GetTenantKey(string, string, string?)"/> to generate the key.
		/// </remarks>
		Task RemoveAsync(string key, CancellationToken cancellationToken = default);

		/// <summary>
		/// Asynchronously stores a value in the cache with fine-grained expiration control.
		/// </summary>
		/// <typeparam name="T">The type of the value to cache. Must support serialization for distributed cache implementations.</typeparam>
		/// <param name="key">The unique cache key identifying the cached item. Cannot be <see langword="null"/> or whitespace.</param>
		/// <param name="value">The value to store in the cache. Can be <see langword="null"/> if <typeparamref name="T"/> is a reference type.</param>
		/// <param name="options">
		/// Per-entry options controlling absolute and/or sliding expiration behavior. Cannot be <see langword="null"/>.
		/// </param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests during the asynchronous operation.</param>
		/// <returns>A task that represents the asynchronous operation.</returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="key"/> is <see langword="null"/>, empty, or whitespace,
		/// or when <paramref name="options"/> is <see langword="null"/>.
		/// </exception>
		/// <remarks>
		/// This overload provides fine-grained control over cache entry expiration through <see cref="CacheEntryOptions"/>.
		/// You can configure absolute expiration, sliding expiration, or both. If both are set, the entry expires
		/// at whichever condition is met first. If neither expiration type is specified in <paramref name="options"/>,
		/// the implementation may fall back to <see cref="CacheOptions.DefaultExpiration"/>.
		/// If the key already exists, its value is overwritten with the new value and expiration settings.
		/// For tenant-specific caching, use <see cref="TenantCacheKeyHelper.GetTenantKey(string, string, string?)"/> to generate the key.
		/// </remarks>
		Task SetAsync<T>(string key, T value, CacheEntryOptions options, CancellationToken cancellationToken = default);

		/// <summary>
		/// Asynchronously stores a value in the cache with an optional absolute expiration time.
		/// </summary>
		/// <typeparam name="T">The type of the value to cache. Must support serialization for distributed cache implementations.</typeparam>
		/// <param name="key">The unique cache key identifying the cached item. Cannot be <see langword="null"/> or whitespace.</param>
		/// <param name="value">The value to store in the cache. Can be <see langword="null"/> if <typeparamref name="T"/> is a reference type.</param>
		/// <param name="expiration">
		/// Optional absolute expiration time relative to now. If <see langword="null"/>, the default expiration
		/// configured in <see cref="CacheOptions.DefaultExpiration"/> is used.
		/// </param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests during the asynchronous operation.</param>
		/// <returns>A task that represents the asynchronous operation.</returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="key"/> is <see langword="null"/>, empty, or whitespace.
		/// </exception>
		/// <remarks>
		/// This method serializes and stores the value in the cache with absolute expiration.
		/// If the key already exists, its value is overwritten with the new value and expiration.
		/// The expiration is relative to the current time. For sliding expiration or combined expiration strategies,
		/// use the <see cref="SetAsync{T}(string, T, CacheEntryOptions, CancellationToken)"/> overload instead.
		/// For tenant-specific caching, use <see cref="TenantCacheKeyHelper.GetTenantKey(string, string, string?)"/> to generate the key.
		/// </remarks>
		Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default);

		/// <summary>
		/// Asynchronously retrieves a cached value by key.
		/// </summary>
		/// <typeparam name="T">The type of the cached value. Must support serialization for distributed cache implementations.</typeparam>
		/// <param name="key">The unique cache key identifying the cached item. Cannot be <see langword="null"/> or whitespace.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests during the asynchronous operation.</param>
		/// <returns>
		/// A task that represents the asynchronous operation. The task result contains the cached value of type <typeparamref name="T"/>
		/// if found, or the default value of <typeparamref name="T"/> (typically <see langword="null"/> for reference types) if the key does not exist.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="key"/> is <see langword="null"/>, empty, or whitespace.
		/// </exception>
		/// <remarks>
		/// This method performs a cache lookup and deserializes the cached value to the requested type.
		/// Returns the default value for <typeparamref name="T"/> when the key is not found or has expired.
		/// Implementations should handle deserialization failures gracefully, typically by returning the default value.
		/// For tenant-specific caching, use <see cref="TenantCacheKeyHelper.GetTenantKey(string, string, string?)"/> to generate the key.
		/// </remarks>
		Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);

		#endregion
	}
}