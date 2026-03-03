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
	/// Represents a domain event that triggers logical cache invalidation for a tenant namespace.
	/// </summary>
	/// <remarks>
	/// This interface defines the contract for cache invalidation events in a multi-tenant architecture.
	/// Implementations specify which tenant's cache should be invalidated, with optional granular control
	/// via the <see cref="Area"/> property. The invalidation is logical, meaning the namespace version
	/// is incremented rather than physically deleting cache entries, allowing them to expire naturally.
	/// Events implementing this interface are published through <see cref="ICacheInvalidationPublisher"/>
	/// to trigger coordinated cache invalidation across the application.
	/// </remarks>
	public interface ICacheInvalidationEvent
	{
		#region ' Properties '

		/// <summary>
		/// Gets the unique identifier of the tenant whose cache namespace should be invalidated.
		/// </summary>
		/// <value>
		/// A non-null, non-empty string representing the tenant identifier.
		/// </value>
		/// <remarks>
		/// The tenant ID is used to scope the cache invalidation to a specific tenant's data,
		/// ensuring proper isolation in multi-tenant scenarios. All versioned cache entries
		/// for this tenant will become unreachable after the namespace version is incremented.
		/// </remarks>
		string TenantId { get; }

		/// <summary>
		/// Gets the optional cache area representing a sub-namespace within the tenant's cache.
		/// </summary>
		/// <value>
		/// A string identifying a specific cache area within the tenant namespace,
		/// or <see langword="null"/> to invalidate the entire tenant namespace.
		/// </value>
		/// <remarks>
		/// Cache areas provide granular invalidation control, allowing specific subsets of a tenant's
		/// cache to be invalidated without affecting other cached data. For example, a "products" area
		/// could be invalidated independently from a "users" area. When <see langword="null"/>,
		/// the entire tenant namespace is invalidated, orphaning all versioned cache entries for that tenant.
		/// </remarks>
		string? Area { get; }

		#endregion
	}
}