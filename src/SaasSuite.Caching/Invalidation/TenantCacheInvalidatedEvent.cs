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
using SaasSuite.Caching.Interfaces;

namespace SaasSuite.Caching.Invalidation
{
	/// <summary>
	/// Represents a domain event that signals tenant-specific cache invalidation.
	/// </summary>
	/// <remarks>
	/// This event is published to trigger logical cache invalidation for a specific tenant's namespace
	/// or a sub-namespace (area) within that tenant. When published via <see cref="ICacheInvalidationPublisher"/>,
	/// the namespace version is incremented, causing all cache entries stored under the previous version
	/// to become unreachable. This provides an efficient invalidation mechanism that avoids expensive
	/// cache scanning or deletion operations, particularly beneficial in distributed caching scenarios.
	/// The event follows domain-driven design principles and can be integrated into event-sourcing
	/// or message-based architectures.
	/// </remarks>
	public class TenantCacheInvalidatedEvent
		: ICacheInvalidationEvent
	{
		#region ' Constructors '

		/// <summary>
		/// Initializes a new instance of the <see cref="TenantCacheInvalidatedEvent"/> class.
		/// </summary>
		/// <param name="tenantId">
		/// The unique identifier of the tenant whose cache namespace should be invalidated.
		/// Cannot be <see langword="null"/>, empty, or whitespace.
		/// </param>
		/// <param name="area">
		/// The optional cache area (sub-namespace) to invalidate within the tenant.
		/// When <see langword="null"/>, the entire tenant namespace is invalidated.
		/// </param>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="tenantId"/> is <see langword="null"/>, empty, or whitespace.
		/// </exception>
		/// <remarks>
		/// This constructor validates the tenant ID to ensure proper event initialization.
		/// The area parameter allows for granular invalidation of specific cache subsets,
		/// such as invalidating only product-related cache while preserving user-related cache.
		/// </remarks>
		public TenantCacheInvalidatedEvent(string tenantId, string? area = null)
		{
			// Validate that tenant ID is provided and not whitespace
			if (string.IsNullOrWhiteSpace(tenantId))
			{
				throw new ArgumentNullException(nameof(tenantId));
			}

			this.TenantId = tenantId;
			this.Area = area;
		}

		#endregion

		#region ' Properties '

		/// <summary>
		/// Gets the unique identifier of the tenant whose cache namespace should be invalidated.
		/// </summary>
		/// <value>
		/// A non-null, non-empty string representing the tenant identifier.
		/// </value>
		/// <remarks>
		/// This identifier is used to scope the cache invalidation to a specific tenant's data.
		/// When the event is published, all versioned cache entries for this tenant (or the specified
		/// area within the tenant) become unreachable by incrementing the namespace version counter.
		/// The tenant ID must match the identifier used when storing cache entries via
		/// <see cref="TenantCacheKeyHelper.GetVersionedTenantKey"/>.
		/// </remarks>
		public string TenantId { get; }

		/// <summary>
		/// Gets the optional cache area (sub-namespace) to invalidate within the tenant.
		/// </summary>
		/// <value>
		/// A string identifying a specific cache area within the tenant namespace,
		/// or <see langword="null"/> to invalidate the entire tenant namespace.
		/// </value>
		/// <remarks>
		/// Cache areas enable fine-grained invalidation control, allowing specific subsets of
		/// a tenant's cache to be invalidated independently. For example, when product data changes,
		/// only the "products" area needs invalidation, leaving other areas like "users" or "settings" intact.
		/// When <see langword="null"/>, all cache entries for the tenant across all areas are invalidated.
		/// The area name must match the area used when storing cache entries to ensure proper invalidation scope.
		/// </remarks>
		public string? Area { get; }

		#endregion
	}
}