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

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

using SaasSuite.Core;
using SaasSuite.Core.Interfaces;
using SaasSuite.EfCore.Interfaces;

namespace SaasSuite.EfCore.Interceptors
{
	/// <summary>
	/// EF Core save changes interceptor that automatically assigns tenant IDs and enforces tenant immutability.
	/// </summary>
	/// <remarks>
	/// This interceptor ensures that:
	/// <list type="bullet">
	/// <item><description>New entities automatically receive the current tenant ID</description></item>
	/// <item><description>Tenant IDs cannot be modified after an entity is created</description></item>
	/// </list>
	/// </remarks>
	public class TenantSaveChangesInterceptor
		: SaveChangesInterceptor
	{
		#region ' Fields '

		/// <summary>
		/// Provides access to the current tenant context.
		/// </summary>
		private readonly ITenantAccessor _tenantAccessor;

		#endregion

		#region ' Constructors '

		/// <summary>
		/// Initializes a new instance of the <see cref="TenantSaveChangesInterceptor"/> class.
		/// </summary>
		/// <param name="tenantAccessor">The service providing current tenant context.</param>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="tenantAccessor"/> is <see langword="null"/>.
		/// </exception>
		public TenantSaveChangesInterceptor(ITenantAccessor tenantAccessor)
		{
			this._tenantAccessor = tenantAccessor ?? throw new ArgumentNullException(nameof(tenantAccessor));
		}

		#endregion

		#region ' Methods '

		/// <summary>
		/// Applies tenant ID logic to all tracked entities implementing <see cref="ITenantEntity"/>.
		/// </summary>
		/// <param name="context">The DbContext whose changes are being saved. Can be <see langword="null"/>.</param>
		/// <remarks>
		/// This method performs the following actions:
		/// <list type="bullet">
		/// <item><description>For entities in <see cref="EntityState.Added"/> state: assigns the current tenant ID automatically</description></item>
		/// <item><description>For entities in <see cref="EntityState.Modified"/> state: validates that the tenant ID hasn't been changed</description></item>
		/// <item><description>Entities in other states (Unchanged, Deleted, Detached) are ignored</description></item>
		/// </list>
		/// <para>
		/// If no tenant context is available (e.g., during background jobs or system operations),
		/// this method returns early without applying any tenant logic.
		/// </para>
		/// </remarks>
		/// <exception cref="InvalidOperationException">
		/// Thrown when an attempt is made to modify an entity's tenant ID after it has been persisted.
		/// This prevents accidental or malicious cross-tenant data manipulation.
		/// </exception>
		private void SetTenantId(DbContext? context)
		{
			if (context == null)
			{
				return;
			}

			TenantContext? tenantContext = this._tenantAccessor.TenantContext;
			if (tenantContext == null)
			{
				return;
			}

			// Get all entities being added or modified
			IEnumerable<EntityEntry<ITenantEntity>> entries = context.ChangeTracker.Entries<ITenantEntity>()
				.Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

			foreach (EntityEntry<ITenantEntity>? entry in entries)
			{
				if (entry.State == EntityState.Added)
				{
					// Auto-assign tenant ID for new entities
					entry.Entity.TenantId = tenantContext.TenantId;
				}
				else if (entry.State == EntityState.Modified)
				{
					// Validate that tenant ID hasn't been modified
					string originalTenantId = entry.OriginalValues.GetValue<string>(nameof(ITenantEntity.TenantId));
					if (entry.Entity.TenantId.Value != originalTenantId)
					{
						throw new InvalidOperationException(
							$"Cannot modify TenantId on entity of type {entry.Entity.GetType().Name}. " +
							$"Original: {originalTenantId}, New: {entry.Entity.TenantId.Value}");
					}
				}
			}
		}

		#endregion

		#region ' Override Methods '

		/// <summary>
		/// Intercepts synchronous save operations to apply tenant logic before persisting changes.
		/// </summary>
		/// <param name="eventData">Contextual information about the DbContext.</param>
		/// <param name="result">The current result; can be modified to short-circuit the save.</param>
		/// <returns>The potentially modified interception result.</returns>
		public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
		{
			this.SetTenantId(eventData.Context);
			return base.SavingChanges(eventData, result);
		}

		/// <summary>
		/// Intercepts asynchronous save operations to apply tenant logic before persisting changes.
		/// </summary>
		/// <param name="eventData">Contextual information about the DbContext.</param>
		/// <param name="result">The current result; can be modified to short-circuit the save.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
		/// <returns>
		/// A <see cref="ValueTask{TResult}"/> representing the asynchronous operation.
		/// The task result contains the potentially modified interception result.
		/// </returns>
		public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
		{
			this.SetTenantId(eventData.Context);
			return base.SavingChangesAsync(eventData, result, cancellationToken);
		}

		#endregion
	}
}