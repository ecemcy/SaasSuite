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
using SaasSuite.Samples.SampleWebApp.Infrastructure.Interfaces;
using SaasSuite.Samples.SampleWebApp.Infrastructure.Models;

namespace SaasSuite.Samples.SampleWebApp.Infrastructure.Stores
{
	/// <summary>
	/// In-memory implementation of <see cref="ITenantUserStore"/> for demonstration purposes.
	/// </summary>
	/// <remarks>
	/// This implementation stores tenant users in memory. In production, user data should be persisted
	/// to a database with appropriate indexes on tenant ID and user ID for efficient lookups.
	/// Consider implementing additional methods for updating and deleting users as needed.
	/// </remarks>
	public class InMemoryTenantUserStore
		: ITenantUserStore
	{
		#region ' Fields '

		/// <summary>
		/// In-memory collection storing all tenant users.
		/// </summary>
		private readonly List<TenantUser> _users = new List<TenantUser>();

		#endregion

		#region ' Methods '

		/// <inheritdoc/>
		public Task CreateAsync(TenantUser user)
		{
			// Add user to the collection (in production, validate uniqueness and emit seat consumption events)
			this._users.Add(user);
			return Task.CompletedTask;
		}

		/// <inheritdoc/>
		public Task<IEnumerable<TenantUser>> GetAllAsync(TenantId tenantId)
		{
			// Filter users by tenant ID to return only users belonging to the specified tenant
			List<TenantUser> users = this._users.Where(u => u.TenantId.Value == tenantId.Value).ToList();
			return Task.FromResult<IEnumerable<TenantUser>>(users);
		}

		/// <inheritdoc/>
		public Task<TenantUser?> GetByIdAsync(TenantId tenantId, string userId)
		{
			// Find user matching both tenant ID and user ID for tenant-scoped lookup
			TenantUser? user = this._users.FirstOrDefault(u => u.TenantId.Value == tenantId.Value && u.Id == userId);
			return Task.FromResult(user);
		}

		#endregion
	}
}