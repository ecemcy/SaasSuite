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

namespace SaasSuite.Caching.Interfaces
{
	/// <summary>
	/// Defines the contract for publishing cache invalidation events that increment namespace versions.
	/// </summary>
	/// <remarks>
	/// This publisher implements a logical cache invalidation strategy based on namespace versioning.
	/// When an invalidation event is published, the namespace version for the affected tenant and area
	/// is incremented in <see cref="INamespaceVersionStore"/>. All existing cache entries that were
	/// stored with the previous version become unreachable, effectively invalidating them without
	/// physically deleting the cached data. Unreachable entries expire naturally according to their
	/// original time-to-live (TTL) settings, preventing memory leaks while avoiding expensive delete operations.
	/// This approach is particularly efficient in distributed caching scenarios.
	/// </remarks>
	public interface ICacheInvalidationPublisher
	{
		#region ' Methods '

		/// <summary>
		/// Asynchronously publishes a cache invalidation event to increment the namespace version.
		/// </summary>
		/// <param name="cacheEvent">
		/// The invalidation event containing tenant and area information. Cannot be <see langword="null"/>.
		/// </param>
		/// <param name="cancellationToken">
		/// A token to monitor for cancellation requests during the asynchronous operation.
		/// </param>
		/// <returns>
		/// A task that represents the asynchronous publish operation.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="cacheEvent"/> is <see langword="null"/>.
		/// </exception>
		/// <remarks>
		/// Publishing an invalidation event increments the namespace version for the tenant and optional area
		/// specified in <paramref name="cacheEvent"/>. All cache entries stored with the previous version
		/// become logically invalidated and will result in cache misses on subsequent lookups.
		/// The operation is typically fast, as it only updates a version counter rather than
		/// scanning and deleting individual cache entries.
		/// </remarks>
		Task PublishAsync(ICacheInvalidationEvent cacheEvent, CancellationToken cancellationToken = default);

		#endregion
	}
}