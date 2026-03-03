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

namespace SaasSuite.Caching.Helpers
{
	/// <summary>
	/// Provides utility methods for generating cache keys with tenant isolation support.
	/// </summary>
	/// <remarks>
	/// This helper ensures consistent cache key naming conventions across the application and
	/// enforces tenant isolation by embedding tenant identifiers in cache keys. Tenant isolation
	/// prevents data leakage between tenants in multi-tenant SaaS applications.
	/// All methods are static and thread-safe, producing deterministic keys for the same inputs.
	/// </remarks>
	public static class TenantCacheKeyHelper
	{
		#region ' Static Methods '

		/// <summary>
		/// Generates a global cache key shared across all tenants for system-wide data.
		/// </summary>
		/// <param name="key">The base cache key describing the cached resource. Cannot be <see langword="null"/>, empty, or whitespace.</param>
		/// <param name="prefix">
		/// Optional custom prefix for the cache key. If <see langword="null"/>, defaults to "saas:".
		/// Use custom prefixes to distinguish between different applications sharing the same cache store.
		/// </param>
		/// <returns>
		/// A global cache key in the format "{prefix}global:{key}" accessible to all tenants.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="key"/> is <see langword="null"/>, empty, or whitespace.
		/// </exception>
		/// <remarks>
		/// Use this method only for data that is truly global and shared across all tenants, such as:
		/// <list type="bullet">
		/// <item><description>System-wide configuration settings</description></item>
		/// <item><description>Application metadata</description></item>
		/// <item><description>Feature flags not scoped to tenants</description></item>
		/// <item><description>Reference data like country codes or currencies</description></item>
		/// </list>
		/// Be cautious when using global keys to avoid unintentional data sharing between tenants.
		/// For tenant-specific data, always use <see cref="GetTenantKey(string, string, string?)"/> instead.
		/// </remarks>
		public static string GetGlobalKey(string key, string? prefix = null)
		{
			// Validate that key is provided and not whitespace
			if (string.IsNullOrWhiteSpace(key))
			{
				throw new ArgumentNullException(nameof(key));
			}

			// Use default prefix if none provided
			string basePrefix = prefix ?? "saas:";

			// Construct global cache key with hierarchical structure
			return $"{basePrefix}global:{key}";
		}

		/// <summary>
		/// Generates a cache key scoped to a specific tenant to enforce data isolation.
		/// </summary>
		/// <param name="tenantId">The unique identifier for the tenant. Cannot be <see langword="null"/>, empty, or whitespace.</param>
		/// <param name="key">The base cache key describing the cached resource. Cannot be <see langword="null"/>, empty, or whitespace.</param>
		/// <param name="prefix">
		/// Optional custom prefix for the cache key. If <see langword="null"/>, defaults to "saas:".
		/// Use custom prefixes to distinguish between different applications sharing the same cache store.
		/// </param>
		/// <returns>
		/// A composite cache key in the format "{prefix}tenant:{tenantId}:{key}" ensuring tenant isolation.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="tenantId"/> or <paramref name="key"/> is <see langword="null"/>, empty, or whitespace.
		/// </exception>
		/// <remarks>
		/// Use this method for all tenant-specific cached data to ensure proper isolation.
		/// The generated key includes the tenant ID, preventing one tenant from accessing another tenant's cached data.
		/// The format follows a hierarchical structure suitable for cache stores that support key pattern matching
		/// (e.g., Redis SCAN operations with patterns like "saas:tenant:tenant123:*").
		/// </remarks>
		public static string GetTenantKey(string tenantId, string key, string? prefix = null)
		{
			// Validate that tenantId is provided and not whitespace
			if (string.IsNullOrWhiteSpace(tenantId))
			{
				throw new ArgumentNullException(nameof(tenantId));
			}

			// Validate that key is provided and not whitespace
			if (string.IsNullOrWhiteSpace(key))
			{
				throw new ArgumentNullException(nameof(key));
			}

			// Use default prefix if none provided
			string basePrefix = prefix ?? "saas:";

			// Construct tenant-scoped cache key with hierarchical structure
			return $"{basePrefix}tenant:{tenantId}:{key}";
		}

		/// <summary>
		/// Generates a namespace-versioned tenant cache key for logical cache invalidation.
		/// </summary>
		/// <param name="tenantId">
		/// The unique identifier for the tenant. Cannot be <see langword="null"/>, empty, or whitespace.
		/// </param>
		/// <param name="version">
		/// The current namespace version obtained from <see cref="INamespaceVersionStore.GetVersion(string, string?)"/>.
		/// Must be a positive integer. This version is embedded in the cache key to enable logical invalidation.
		/// </param>
		/// <param name="key">
		/// The base cache key describing the cached resource. Cannot be <see langword="null"/>, empty, or whitespace.
		/// </param>
		/// <param name="area">
		/// Optional cache area (sub-namespace) within the tenant namespace. When <see langword="null"/>,
		/// the key is scoped to the root tenant namespace. Use areas to enable granular cache invalidation.
		/// </param>
		/// <param name="prefix">
		/// Optional custom prefix for the cache key. If <see langword="null"/>, defaults to "saas:".
		/// Use custom prefixes to distinguish between different applications sharing the same cache store.
		/// </param>
		/// <returns>
		/// A versioned, tenant-isolated cache key in one of these formats:
		/// <list type="bullet">
		/// <item><description>Without area: "{prefix}tenant:{tenantId}:v{version}:{key}"</description></item>
		/// <item><description>With area: "{prefix}tenant:{tenantId}:v{version}:{area}:{key}"</description></item>
		/// </list>
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="tenantId"/> or <paramref name="key"/> is <see langword="null"/>, empty, or whitespace.
		/// </exception>
		/// <remarks>
		/// This method generates cache keys that incorporate a namespace version number, enabling logical
		/// cache invalidation without physically deleting cache entries. When you increment the version
		/// via <see cref="ICacheInvalidationPublisher.PublishAsync(ICacheInvalidationEvent, CancellationToken)"/>,
		/// all cache entries stored under the previous version become unreachable and expire naturally.
		/// The version must be retrieved from <see cref="INamespaceVersionStore"/> before calling this method.
		/// This approach is efficient in distributed caching scenarios where physical deletion is expensive.
		/// </remarks>
		public static string GetVersionedTenantKey(string tenantId, long version, string key, string? area = null, string? prefix = null)
		{
			// Validate that tenantId is provided and not whitespace
			if (string.IsNullOrWhiteSpace(tenantId))
			{
				throw new ArgumentNullException(nameof(tenantId));
			}

			// Validate that key is provided and not whitespace
			if (string.IsNullOrWhiteSpace(key))
			{
				throw new ArgumentNullException(nameof(key));
			}

			// Use default prefix if none provided
			string basePrefix = prefix ?? "saas:";

			// Construct versioned cache key with or without area
			// Area-less format: saas:tenant:tenant123:v1:key
			// Area format: saas:tenant:tenant123:v1:products:key
			return area is null
				? $"{basePrefix}tenant:{tenantId}:v{version}:{key}"
				: $"{basePrefix}tenant:{tenantId}:v{version}:{area}:{key}";
		}

		#endregion
	}
}