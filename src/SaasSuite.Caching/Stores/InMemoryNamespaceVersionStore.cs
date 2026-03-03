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

using System.Collections.Concurrent;

using SaasSuite.Caching.Interfaces;

namespace SaasSuite.Caching
{
	/// <summary>
	/// Provides an in-memory, thread-safe implementation of <see cref="INamespaceVersionStore"/>
	/// using a concurrent dictionary for version storage.
	/// </summary>
	/// <remarks>
	/// This implementation stores namespace version counters in application memory using
	/// <see cref="ConcurrentDictionary{TKey,TValue}"/> to ensure thread-safe atomic operations.
	/// The store is suitable for single-instance deployments and development scenarios.
	/// For distributed applications with multiple instances, consider implementing a distributed
	/// version store backed by Redis or a similar shared data store to ensure version consistency
	/// across all application instances. Version data is lost when the application restarts.
	/// </remarks>
	public class InMemoryNamespaceVersionStore
		: INamespaceVersionStore
	{
		#region ' Fields '

		/// <summary>
		/// Thread-safe dictionary storing namespace version numbers keyed by tenant ID and optional area.
		/// </summary>
		/// <remarks>
		/// Uses <see cref="StringComparer.Ordinal"/> for case-sensitive key comparisons to ensure
		/// consistent tenant and area identification. Version values are positive integers starting at 1.
		/// </remarks>
		private readonly ConcurrentDictionary<string, long> _versions = new ConcurrentDictionary<string, long>(StringComparer.Ordinal);

		#endregion

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
		/// This method atomically retrieves or initializes the version to 1 if the namespace
		/// has never been accessed. The operation is thread-safe and does not require explicit locking.
		/// </remarks>
		public long GetVersion(string tenantId, string? area = null)
		{
			// Validate tenant ID is provided
			if (string.IsNullOrWhiteSpace(tenantId))
			{
				throw new ArgumentNullException(nameof(tenantId));
			}

			// Atomically get or add version, defaulting to 1 for new namespaces
			return this._versions.GetOrAdd(BuildKey(tenantId, area), 1L);
		}

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
		/// This method atomically increments the version counter or initializes it to 2 for new namespaces.
		/// The operation is thread-safe and guarantees that concurrent calls will produce unique,
		/// monotonically increasing version numbers without race conditions.
		/// </remarks>
		public long IncrementVersion(string tenantId, string? area = null)
		{
			// Validate tenant ID is provided
			if (string.IsNullOrWhiteSpace(tenantId))
			{
				throw new ArgumentNullException(nameof(tenantId));
			}

			// Atomically add or update version: initialize to 2, or increment existing value
			return this._versions.AddOrUpdate(BuildKey(tenantId, area), 2L, (_, current) => current + 1);
		}

		#endregion

		#region ' Static Methods '

		/// <summary>
		/// Constructs a dictionary key from the tenant ID and optional area.
		/// </summary>
		/// <param name="tenantId">The tenant identifier.</param>
		/// <param name="area">The optional cache area within the tenant namespace.</param>
		/// <returns>
		/// A composite key string. Returns <paramref name="tenantId"/> alone if <paramref name="area"/>
		/// is <see langword="null"/>, or "{tenantId}:{area}" if an area is specified.
		/// </returns>
		/// <remarks>
		/// This method creates a hierarchical key structure that separates tenant and area identifiers
		/// with a colon delimiter. The key format enables independent version tracking for different
		/// areas within the same tenant namespace.
		/// </remarks>
		private static string BuildKey(string tenantId, string? area)
		{
			// Return tenant ID alone for root namespace, or composite key for area-specific namespace
			return area is null ? tenantId : $"{tenantId}:{area}";
		}

		#endregion
	}
}