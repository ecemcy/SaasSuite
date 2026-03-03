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

using SaasSuite.Core.Enumerations;

namespace SaasSuite.Core.Interfaces
{
	/// <summary>
	/// Defines tenant isolation strategies and access validation policies.
	/// Implementations of this interface enforce data separation and access control between tenants
	/// according to their configured isolation levels and security requirements.
	/// </summary>
	/// <remarks>
	/// This interface is typically implemented to provide custom isolation logic for different
	/// multi-tenancy strategies (shared database, separate schemas, separate databases, etc.).
	/// The policy can be used by data access layers and security middleware to enforce tenant boundaries.
	/// </remarks>
	public interface IIsolationPolicy
	{
		#region ' Methods '

		/// <summary>
		/// Validates whether a tenant has permission to access a specific resource.
		/// This method enforces isolation boundaries by checking if the tenant's access
		/// to the requested resource is permitted under the current isolation policy.
		/// </summary>
		/// <param name="tenantId">The identifier of the tenant requesting access.</param>
		/// <param name="resourceId">The identifier of the resource being accessed. Format depends on resource type (e.g., database name, file path, API endpoint).</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
		/// <returns>
		/// A task that represents the asynchronous operation. The task result is <see langword="true"/>
		/// if the tenant is allowed to access the resource; otherwise <see langword="false"/>.
		/// </returns>
		/// <remarks>
		/// Implementations should consider the tenant's isolation level, ownership of the resource,
		/// and any sharing policies when determining access. This method can be used in data access
		/// layers, authorization handlers, or middleware to prevent cross-tenant data access.
		/// </remarks>
		Task<bool> ValidateAccessAsync(TenantId tenantId, string resourceId, CancellationToken cancellationToken = default);

		/// <summary>
		/// Retrieves the isolation level configured for a specific tenant.
		/// The isolation level determines how tenant data and resources are separated from other tenants.
		/// </summary>
		/// <param name="tenantId">The identifier of the tenant whose isolation level should be retrieved.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
		/// <returns>
		/// A task that represents the asynchronous operation. The task result contains the
		/// <see cref="IsolationLevel"/> configured for the specified tenant.
		/// </returns>
		/// <remarks>
		/// This method is typically called during tenant context initialization or when
		/// configuring data access strategies. The returned isolation level guides how
		/// resources should be accessed and filtered for the tenant.
		/// </remarks>
		Task<IsolationLevel> GetIsolationLevelAsync(TenantId tenantId, CancellationToken cancellationToken = default);

		#endregion
	}
}