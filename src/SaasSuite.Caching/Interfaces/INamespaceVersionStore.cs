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

namespace SaasSuite.Caching.Interfaces
{
	/// <summary>
	/// Defines the contract for storing and managing namespace version numbers used for logical cache invalidation.
	/// </summary>
	/// <remarks>
	/// This store maintains version counters for tenant cache namespaces and optional sub-namespaces (areas).
	/// Each tenant/area combination has an associated version number that starts at 1 and increments
	/// whenever cache invalidation is needed. By embedding the version number in cache keys via
	/// <see cref="TenantCacheKeyHelper.GetVersionedTenantKey"/>, incrementing the version
	/// effectively orphans all cache entries stored under the previous version, achieving invalidation
	/// without physically deleting entries. This approach is efficient and works well with distributed caches.
	/// Implementations must be thread-safe to support concurrent access from multiple requests.
	/// </remarks>
	public interface INamespaceVersionStore
	{
		#region ' Methods '

		/// <summary>
		/// Retrieves the current namespace version for the specified tenant and optional area.
		/// </summary>
		/// <param name="tenantId">
		/// The unique identifier of the tenant. Cannot be <see langword="null"/>, empty, or whitespace.
		/// </param>
		/// <param name="area">
		/// The optional cache area (sub-namespace) within the tenant namespace.
		/// When <see langword="null"/>, returns the version for the root tenant namespace.
		/// </param>
		/// <returns>
		/// The current namespace version as a positive integer. Returns 1 for new namespaces
		/// that have not been previously accessed or invalidated.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="tenantId"/> is <see langword="null"/>, empty, or whitespace.
		/// </exception>
		/// <remarks>
		/// This method is called when constructing versioned cache keys to ensure cache lookups
		/// and stores use the current version. If the namespace has never been accessed,
		/// the version is initialized to 1. The operation must be thread-safe and efficient,
		/// as it may be called frequently during cache operations.
		/// </remarks>
		long GetVersion(string tenantId, string? area = null);

		/// <summary>
		/// Atomically increments the namespace version for the specified tenant and optional area.
		/// </summary>
		/// <param name="tenantId">
		/// The unique identifier of the tenant. Cannot be <see langword="null"/>, empty, or whitespace.
		/// </param>
		/// <param name="area">
		/// The optional cache area (sub-namespace) within the tenant namespace.
		/// When <see langword="null"/>, increments the version for the root tenant namespace.
		/// </param>
		/// <returns>
		/// The new namespace version after incrementing.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="tenantId"/> is <see langword="null"/>, empty, or whitespace.
		/// </exception>
		/// <remarks>
		/// This method is called when cache invalidation is triggered via <see cref="ICacheInvalidationPublisher"/>.
		/// Incrementing the version causes all cache entries stored with the previous version to become
		/// unreachable, effectively invalidating them. For new namespaces, the version is initialized to 2
		/// (skipping 1 as the initial version). The operation must be atomic to prevent race conditions
		/// in concurrent invalidation scenarios.
		/// </remarks>
		long IncrementVersion(string tenantId, string? area = null);

		#endregion
	}
}