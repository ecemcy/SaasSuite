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
using SaasSuite.Core.Interfaces;

namespace SaasSuite.Samples.SampleWebApp.Infrastructure.Stores
{
	/// <summary>
	/// In-memory implementation of <see cref="ITenantStore"/> for demonstration purposes.
	/// </summary>
	/// <remarks>
	/// This implementation stores tenant data in memory. In production, tenant information should be
	/// persisted to a database (e.g., SQL Server, PostgreSQL, MongoDB) with appropriate indexing
	/// for efficient lookups by ID and custom identifiers.
	/// </remarks>
	public class InMemoryTenantStore
		: ITenantStore
	{
		#region ' Fields '

		/// <summary>
		/// In-memory dictionary storing tenants keyed by their identifier.
		/// </summary>
		private readonly Dictionary<string, TenantInfo> _tenants = new Dictionary<string, TenantInfo>();

		#endregion

		#region ' Methods '

		/// <inheritdoc/>
		public Task RemoveAsync(TenantId tenantId, CancellationToken cancellationToken = default)
		{
			this._tenants.Remove(tenantId.Value);
			return Task.CompletedTask;
		}

		/// <inheritdoc/>
		public Task SaveAsync(TenantInfo tenantInfo, CancellationToken cancellationToken = default)
		{
			// Add new tenant or update existing tenant
			this._tenants[tenantInfo.Id.Value] = tenantInfo;
			return Task.CompletedTask;
		}

		/// <inheritdoc/>
		public Task<TenantInfo?> GetByIdAsync(TenantId tenantId, CancellationToken cancellationToken = default)
		{
			this._tenants.TryGetValue(tenantId.Value, out TenantInfo? tenant);
			return Task.FromResult(tenant);
		}

		/// <inheritdoc/>
		public Task<TenantInfo?> GetByIdentifierAsync(string identifierKey, CancellationToken cancellationToken = default)
		{
			// Search for tenant using name as an alternate identifier (case-insensitive)
			TenantInfo? tenant = this._tenants.Values.FirstOrDefault(t => t.Name?.Equals(identifierKey, StringComparison.OrdinalIgnoreCase) == true);
			return Task.FromResult(tenant);
		}

		#endregion
	}
}