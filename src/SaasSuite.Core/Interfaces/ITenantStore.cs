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

namespace SaasSuite.Core.Interfaces
{
	/// <summary>
	/// Provides persistence and retrieval operations for tenant metadata and configuration.
	/// Implementations manage the storage and querying of tenant information used throughout the application.
	/// </summary>
	/// <remarks>
	/// This interface abstracts the underlying storage mechanism, which could be:
	/// <list type="bullet">
	/// <item><description>Relational Database - SQL Server, PostgreSQL, MySQL</description></item>
	/// <item><description>NoSQL Database - MongoDB, CosmosDB, DynamoDB</description></item>
	/// <item><description>Configuration Files - JSON, YAML, XML</description></item>
	/// <item><description>In-Memory Cache - For testing or small deployments</description></item>
	/// <item><description>External API - Tenant management service</description></item>
	/// </list>
	/// <para>
	/// Implementations should consider caching strategies to minimize database calls during
	/// tenant resolution, as this occurs on every request. Distributed caching should be used
	/// in multi-instance deployments to ensure consistency.
	/// </para>
	/// </remarks>
	public interface ITenantStore
	{
		#region ' Methods '

		/// <summary>
		/// Removes a tenant and all associated metadata from the store.
		/// This operation is typically used during tenant offboarding or deletion.
		/// </summary>
		/// <param name="tenantId">The unique identifier of the tenant to remove.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
		/// <returns>A task that represents the asynchronous operation.</returns>
		/// <remarks>
		/// This method only removes tenant metadata managed by this store. Implementations should:
		/// <list type="bullet">
		/// <item><description>Complete without error if the tenant does not exist</description></item>
		/// <item><description>Consider soft-delete patterns for audit and recovery purposes</description></item>
		/// <item><description>Invalidate any cached tenant data</description></item>
		/// <item><description>Not delete tenant application data (which should be handled separately)</description></item>
		/// </list>
		/// </remarks>
		Task RemoveAsync(TenantId tenantId, CancellationToken cancellationToken = default);

		/// <summary>
		/// Persists tenant information to the store, creating a new tenant or updating an existing one.
		/// The operation is determined by whether a tenant with the given ID already exists.
		/// </summary>
		/// <param name="tenantInfo">The tenant information to persist, including all metadata and configuration.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
		/// <returns>A task that represents the asynchronous operation.</returns>
		/// <remarks>
		/// Implementations should:
		/// <list type="bullet">
		/// <item><description>Validate required fields before persisting</description></item>
		/// <item><description>Update the <see cref="TenantInfo.UpdatedAt"/> timestamp</description></item>
		/// <item><description>Invalidate any cached tenant data</description></item>
		/// <item><description>Ensure identifier keys remain unique across tenants</description></item>
		/// <item><description>Handle concurrent updates appropriately (optimistic locking, last-write-wins, etc.)</description></item>
		/// </list>
		/// </remarks>
		Task SaveAsync(TenantInfo tenantInfo, CancellationToken cancellationToken = default);

		/// <summary>
		/// Retrieves tenant information by its unique tenant identifier.
		/// This is the primary method used during tenant resolution in the request pipeline.
		/// </summary>
		/// <param name="tenantId">The unique identifier of the tenant to retrieve.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
		/// <returns>
		/// A task that represents the asynchronous operation. The task result contains the
		/// <see cref="TenantInfo"/> if found, or <see langword="null"/> if no tenant with the specified ID exists.
		/// </returns>
		/// <remarks>
		/// This method is frequently called and should be optimized for performance.
		/// Consider implementing caching with appropriate expiration policies to reduce
		/// database load while ensuring tenant information remains reasonably current.
		/// </remarks>
		Task<TenantInfo?> GetByIdAsync(TenantId tenantId, CancellationToken cancellationToken = default);

		/// <summary>
		/// Retrieves tenant information by an identifier key such as subdomain, host, or custom identifier.
		/// This method enables lookups using human-readable or URL-friendly tenant identifiers.
		/// </summary>
		/// <param name="identifierKey">
		/// The identifier key to search for. Common examples include subdomain names,
		/// custom domain names, or tenant slug identifiers.
		/// </param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
		/// <returns>
		/// A task that represents the asynchronous operation. The task result contains the
		/// <see cref="TenantInfo"/> if found, or <see langword="null"/> if no tenant with the specified identifier exists.
		/// </returns>
		/// <remarks>
		/// This method is used when tenant resolution is based on hostname, subdomain, or other
		/// non-ID identifiers. Implementations should:
		/// <list type="bullet">
		/// <item><description>Ensure identifier keys are indexed for fast lookups</description></item>
		/// <item><description>Handle case sensitivity appropriately for the identifier type</description></item>
		/// <item><description>Consider caching results to improve performance</description></item>
		/// <item><description>Validate that identifier keys are unique across tenants</description></item>
		/// </list>
		/// </remarks>
		Task<TenantInfo?> GetByIdentifierAsync(string identifierKey, CancellationToken cancellationToken = default);

		#endregion
	}
}