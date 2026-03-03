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
using SaasSuite.EfCore.Interfaces;

namespace SaasSuite.EfCore.Options
{
	/// <summary>
	/// Provides configuration options for Entity Framework Core multi-tenancy features.
	/// </summary>
	public class EfCoreOptions
	{
		#region ' Properties '

		/// <summary>
		/// Gets or sets a value indicating whether to automatically assign tenant IDs to new entities.
		/// </summary>
		/// <value>
		/// <see langword="true"/> to enable automatic tenant ID assignment; otherwise, <see langword="false"/>.
		/// Default value is <see langword="true"/>.
		/// </value>
		/// <remarks>
		/// <para>
		/// When enabled, entities implementing <see cref="ITenantEntity"/> automatically receive
		/// the current tenant ID when added to the DbContext (during SaveChanges).
		/// </para>
		/// <para>
		/// This feature also validates that tenant IDs are not modified after entity creation,
		/// preventing cross-tenant data manipulation.
		/// </para>
		/// <para>
		/// Disabling this requires manual tenant ID assignment in your code, which increases
		/// the risk of data integrity issues.
		/// </para>
		/// </remarks>
		public bool AutoSetTenantId { get; set; } = true;

		/// <summary>
		/// Gets or sets a value indicating whether to automatically apply global query filters for tenant isolation.
		/// </summary>
		/// <value>
		/// <see langword="true"/> to enable automatic query filtering; otherwise, <see langword="false"/>.
		/// Default value is <see langword="true"/>.
		/// </value>
		/// <remarks>
		/// <para>
		/// When enabled, all queries against entities implementing <see cref="ITenantEntity"/> automatically
		/// filter results to match only the current tenant's data. This provides defense-in-depth security.
		/// </para>
		/// <para>
		/// Disabling this requires manual filtering in every query, which increases the risk of data leakage.
		/// Only disable if you have specific requirements and understand the security implications.
		/// </para>
		/// <para>
		/// You can bypass filters for specific queries using <c>IgnoreQueryFilters()</c> when needed.
		/// </para>
		/// </remarks>
		public bool UseGlobalQueryFilters { get; set; } = true;

		/// <summary>
		/// Gets or sets a value indicating whether to cache EF Core models separately per tenant.
		/// </summary>
		/// <value>
		/// <see langword="true"/> to enable per-tenant model caching; otherwise, <see langword="false"/>.
		/// Default value is <see langword="true"/>.
		/// </value>
		/// <remarks>
		/// <para>
		/// Enable this when tenants have different database schemas, table structures, or configurations.
		/// Each tenant will have its own compiled model cached separately.
		/// </para>
		/// <para>
		/// Disable this for better performance and reduced memory usage when all tenants share
		/// an identical schema (common in database-per-tenant or schema-per-tenant scenarios
		/// where the structure is the same).
		/// </para>
		/// <para>
		/// Note: Even with this disabled, tenant isolation is still enforced through query filters and interceptors.
		/// </para>
		/// </remarks>
		public bool UsePerTenantModelCache { get; set; } = true;

		/// <summary>
		/// Gets or sets a value indicating whether to validate tenant access during query execution.
		/// </summary>
		/// <value>
		/// <see langword="true"/> to enable tenant access validation; otherwise, <see langword="false"/>.
		/// Default value is <see langword="true"/>.
		/// </value>
		/// <remarks>
		/// <para>
		/// When enabled, the system validates that queries only access data belonging to the current tenant.
		/// This provides an additional security layer beyond query filters.
		/// </para>
		/// <para>
		/// This validation helps detect and prevent:
		/// <list type="bullet">
		/// <item><description>Accidental cross-tenant queries using <c>IgnoreQueryFilters()</c></description></item>
		/// <item><description>Queries executed without a valid tenant context</description></item>
		/// <item><description>Potential data leakage in complex query scenarios</description></item>
		/// </list>
		/// </para>
		/// <para>
		/// Disable only for system-level operations that legitimately need cross-tenant access.
		/// </para>
		/// </remarks>
		public bool ValidateTenantAccess { get; set; } = true;

		/// <summary>
		/// Gets or sets the default connection string used when a tenant has no specific connection string configured.
		/// </summary>
		/// <value>
		/// A connection string, or <see langword="null"/> if no default is configured.
		/// </value>
		/// <remarks>
		/// <para>
		/// This fallback connection string is typically used in the following multi-tenancy scenarios:
		/// <list type="bullet">
		/// <item><description><b>Shared Database:</b> All tenants use the same database with tenant ID discrimination</description></item>
		/// <item><description><b>Default Database:</b> New tenants without provisioned databases use a shared instance</description></item>
		/// <item><description><b>Development/Testing:</b> Simplified configuration for non-production environments</description></item>
		/// </list>
		/// </para>
		/// <para>
		/// If a tenant has a specific connection string in <see cref="TenantInfo.ConnectionString"/>,
		/// that value takes precedence over this default.
		/// </para>
		/// </remarks>
		public string? DefaultConnectionString { get; set; }

		#endregion
	}
}