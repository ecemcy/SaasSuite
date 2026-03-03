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
using SaasSuite.EfCore.Interceptors;

namespace SaasSuite.EfCore.Interfaces
{
	/// <summary>
	/// Marks an entity as tenant-scoped, enabling automatic tenant isolation and ID management.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Entities implementing this interface benefit from:
	/// <list type="bullet">
	/// <item><description>Automatic tenant ID assignment via <see cref="TenantSaveChangesInterceptor"/></description></item>
	/// <item><description>Global query filters that restrict data to the current tenant</description></item>
	/// <item><description>Tenant ID validation to prevent cross-tenant data access</description></item>
	/// <item><description>Database indexing on TenantId for optimal query performance</description></item>
	/// </list>
	/// </para>
	/// <para>
	/// <b>Implementation Example:</b>
	/// <code>
	/// public class Order : ITenantEntity
	/// {
	///     public int Id { get; set; }
	///     public TenantId TenantId { get; set; }
	///     public string OrderNumber { get; set; }
	///     // other properties...
	/// }
	/// </code>
	/// </para>
	/// <para>
	/// <b>Security Note:</b> Once set, the TenantId cannot be changed. Attempts to modify it
	/// will result in an <see cref="InvalidOperationException"/> to prevent data corruption.
	/// </para>
	/// </remarks>
	public interface ITenantEntity
	{
		#region ' Properties '

		/// <summary>
		/// Gets or sets the identifier of the tenant that owns this entity.
		/// </summary>
		/// <value>
		/// A <see cref="Core.TenantId"/> representing the owning tenant.
		/// This property is automatically set when the entity is added to the DbContext.
		/// </value>
		/// <remarks>
		/// <para>
		/// You typically do not need to set this property manually. It is automatically
		/// assigned based on the current tenant context when SaveChanges is called.
		/// </para>
		/// <para>
		/// This property is immutable after initial assignment. Any attempt to change it
		/// after the entity has been persisted will throw an exception.
		/// </para>
		/// </remarks>
		TenantId TenantId { get; set; }

		#endregion
	}
}