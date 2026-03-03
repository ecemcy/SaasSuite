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

namespace SaasSuite.Migration.Interfaces
{
	/// <summary>
	/// Defines the contract for retrieving tenant information for migration operations.
	/// </summary>
	/// <remarks>
	/// The tenant provider supplies the list of tenants to be migrated when no explicit
	/// tenant list is provided to the migration engine. Implementations typically query
	/// tenant data from a database, configuration store, or tenant management service.
	/// </remarks>
	public interface ITenantProvider
	{
		#region ' Methods '

		/// <summary>
		/// Asynchronously retrieves all available tenants for migration.
		/// </summary>
		/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
		/// <returns>
		/// A task that represents the asynchronous operation. The task result contains a collection
		/// of <see cref="TenantInfo"/> objects representing all active tenants in the system.
		/// </returns>
		/// <remarks>
		/// <para>
		/// This method is called by the migration engine when no explicit tenant IDs are provided
		/// to migration operations. The returned collection should include all tenants that are
		/// eligible for migration, which typically means:
		/// <list type="bullet">
		/// <item><description>Active tenants (not deleted or suspended)</description></item>
		/// <item><description>Tenants that have completed onboarding</description></item>
		/// <item><description>Tenants in regions or tiers eligible for the migration</description></item>
		/// </list>
		/// </para>
		/// <para>
		/// Implementations may filter tenants based on:
		/// <list type="bullet">
		/// <item><description>Tenant status or state</description></item>
		/// <item><description>Subscription tier or plan</description></item>
		/// <item><description>Geographic region</description></item>
		/// <item><description>Feature flags or beta participation</description></item>
		/// <item><description>Migration history or version</description></item>
		/// </list>
		/// </para>
		/// <para>
		/// For large multi-tenant systems, consider pagination or streaming approaches
		/// to avoid loading all tenant information into memory at once. The migration
		/// engine processes tenants in batches, so full materialization is not necessary.
		/// </para>
		/// <para>
		/// The method should be efficient as it may be called multiple times during
		/// migration planning, dry runs, and actual execution.
		/// </para>
		/// </remarks>
		Task<IEnumerable<TenantInfo>> GetAllTenantsAsync(CancellationToken cancellationToken = default);

		#endregion
	}
}