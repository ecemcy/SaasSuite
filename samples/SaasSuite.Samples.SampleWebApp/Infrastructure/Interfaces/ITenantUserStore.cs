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

using SaasSuite.Core;
using SaasSuite.Samples.SampleWebApp.Infrastructure.Models;

namespace SaasSuite.Samples.SampleWebApp.Infrastructure.Interfaces
{
	/// <summary>
	/// Provides data access operations for managing tenant users.
	/// </summary>
	/// <remarks>
	/// This store maintains tenant-scoped user records separate from the application's primary authentication system,
	/// enabling per-tenant user management and role assignments.
	/// </remarks>
	public interface ITenantUserStore
	{
		#region ' Methods '

		/// <summary>
		/// Creates a new user record in the store.
		/// </summary>
		/// <param name="user">The <see cref="TenantUser"/> instance to create.</param>
		/// <returns>A task that represents the asynchronous create operation.</returns>
		/// <remarks>
		/// This operation should validate that the user ID is unique within the tenant before creation.
		/// In production implementations, consider emitting events for seat consumption tracking.
		/// </remarks>
		Task CreateAsync(TenantUser user);

		/// <summary>
		/// Retrieves all users belonging to a specific tenant.
		/// </summary>
		/// <param name="tenantId">The identifier of the tenant whose users to retrieve.</param>
		/// <returns>
		/// A task that represents the asynchronous operation and contains a collection of <see cref="TenantUser"/> instances.
		/// Returns an empty collection if the tenant has no users.
		/// </returns>
		Task<IEnumerable<TenantUser>> GetAllAsync(TenantId tenantId);

		/// <summary>
		/// Retrieves a specific user within a tenant by their unique identifier.
		/// </summary>
		/// <param name="tenantId">The identifier of the tenant the user belongs to.</param>
		/// <param name="userId">The unique identifier of the user to retrieve.</param>
		/// <returns>
		/// A task that represents the asynchronous operation and contains the <see cref="TenantUser"/> if found,
		/// or <see langword="null"/> if no matching user exists.
		/// </returns>
		Task<TenantUser?> GetByIdAsync(TenantId tenantId, string userId);

		#endregion
	}
}