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

using System.Linq.Expressions;
using System.Reflection;

using Microsoft.EntityFrameworkCore.Metadata;

using SaasSuite.Core;
using SaasSuite.Core.Interfaces;
using SaasSuite.EfCore.Interfaces;

namespace Microsoft.EntityFrameworkCore
{
	/// <summary>
	/// Provides helper methods for applying global query filters that enforce tenant isolation in Entity Framework Core.
	/// </summary>
	public static class TenantQueryFilterExtensions
	{
		#region ' Static Methods '

		/// <summary>
		/// Creates a lambda expression for filtering entities by tenant ID.
		/// </summary>
		/// <typeparam name="TEntity">The entity type implementing <see cref="ITenantEntity"/>.</typeparam>
		/// <param name="tenantAccessor">The <see cref="ITenantAccessor"/> providing current tenant context.</param>
		/// <returns>
		/// An expression that evaluates to <see langword="true"/> when the entity belongs to the current tenant
		/// or when no tenant context is available (allowing design-time operations).
		/// </returns>
		/// <remarks>
		/// The generated filter expression is compiled and cached by EF Core for performance.
		/// When <see cref="ITenantAccessor.TenantContext"/> is null, the filter is effectively bypassed,
		/// which is necessary for design-time operations like migrations.
		/// </remarks>
		private static Expression<Func<TEntity, bool>> CreateTenantFilter<TEntity>(ITenantAccessor tenantAccessor)
			where TEntity : class, ITenantEntity
		{
			return entity => tenantAccessor.TenantContext == null ||
							 entity.TenantId == tenantAccessor.TenantContext.TenantId;
		}

		/// <summary>
		/// Automatically applies global query filters to all entities implementing <see cref="ITenantEntity"/>.
		/// </summary>
		/// <param name="modelBuilder">The <see cref="ModelBuilder"/> used to configure the data model.</param>
		/// <param name="tenantAccessor">The <see cref="ITenantAccessor"/> providing current tenant context.</param>
		/// <remarks>
		/// This method uses reflection to discover all <see cref="ITenantEntity"/> implementations
		/// in the model and applies tenant filtering to each one.
		/// </remarks>
		public static void ApplyTenantFilters(this ModelBuilder modelBuilder, ITenantAccessor tenantAccessor)
		{
			foreach (IMutableEntityType entityType in modelBuilder.Model.GetEntityTypes())
			{
				// Check if entity implements ITenantEntity
				if (typeof(ITenantEntity).IsAssignableFrom(entityType.ClrType))
				{
					// Invoke ApplyTenantFilter<T> via reflection
					MethodInfo method = typeof(TenantQueryFilterExtensions)
						.GetMethod(nameof(ApplyTenantFilter))!
						.MakeGenericMethod(entityType.ClrType);

					_ = method.Invoke(null, new object[] { modelBuilder, tenantAccessor });
				}
			}
		}

		/// <summary>
		/// Applies a global query filter to a specific entity type to restrict queries to the current tenant's data.
		/// </summary>
		/// <typeparam name="TEntity">The entity type implementing <see cref="ITenantEntity"/>.</typeparam>
		/// <param name="modelBuilder">The <see cref="ModelBuilder"/> used to configure the data model.</param>
		/// <param name="tenantAccessor">The <see cref="ITenantAccessor"/> providing current tenant context.</param>
		/// <remarks>
		/// This filter ensures that all queries against <typeparamref name="TEntity"/> automatically
		/// filter results to match the current tenant ID. The filter is bypassed when no tenant context is available.
		/// </remarks>
		public static void ApplyTenantFilter<TEntity>(this ModelBuilder modelBuilder, ITenantAccessor tenantAccessor)
			where TEntity : class, ITenantEntity
		{
			_ = modelBuilder.Entity<TEntity>().HasQueryFilter(CreateTenantFilter<TEntity>(tenantAccessor));
		}

		/// <summary>
		/// Automatically configures all entities implementing <see cref="ITenantEntity"/> with standard tenant property settings.
		/// </summary>
		/// <param name="modelBuilder">The <see cref="ModelBuilder"/> used to configure the data model.</param>
		/// <remarks>
		/// <para>
		/// This method uses reflection to discover all <see cref="ITenantEntity"/> implementations
		/// in the model and applies consistent TenantId configuration to each one.
		/// </para>
		/// <para>
		/// This is called during model building (typically in OnModelCreating), which occurs at
		/// application startup, so the reflection overhead is minimal and only happens once.
		/// </para>
		/// </remarks>
		public static void ConfigureTenantEntities(this ModelBuilder modelBuilder)
		{
			foreach (IMutableEntityType entityType in modelBuilder.Model.GetEntityTypes())
			{
				// Check if entity implements ITenantEntity
				if (typeof(ITenantEntity).IsAssignableFrom(entityType.ClrType))
				{
					// Invoke ConfigureTenantEntity<T> via reflection
					MethodInfo method = typeof(TenantQueryFilterExtensions)
						.GetMethod(nameof(ConfigureTenantEntity))!
						.MakeGenericMethod(entityType.ClrType);

					_ = method.Invoke(null, new object[] { modelBuilder });
				}
			}
		}

		/// <summary>
		/// Configures the TenantId property for a specific entity type including value conversion and indexing.
		/// </summary>
		/// <typeparam name="TEntity">The entity type implementing <see cref="ITenantEntity"/>.</typeparam>
		/// <param name="modelBuilder">The <see cref="ModelBuilder"/> used to configure the data model.</param>
		/// <remarks>
		/// This method configures:
		/// <list type="bullet">
		/// <item><description>Value conversion between <see cref="TenantId"/> and string for database storage</description></item>
		/// <item><description>Required constraint on the TenantId column (prevents null values)</description></item>
		/// <item><description>Maximum length of 256 characters for efficient indexing</description></item>
		/// <item><description>Database index for improved query performance on tenant-filtered queries</description></item>
		/// </list>
		/// <para>
		/// The value converter ensures that the strongly-typed <see cref="TenantId"/> is properly
		/// serialized and deserialized when persisting to and from the database.
		/// </para>
		/// </remarks>
		public static void ConfigureTenantEntity<TEntity>(this ModelBuilder modelBuilder)
			where TEntity : class, ITenantEntity
		{
			_ = modelBuilder.Entity<TEntity>(entity =>
			{
				// Configure TenantId property
				_ = entity.Property(e => e.TenantId)
					.HasConversion(
						v => v.Value,
						v => new TenantId(v))
					.IsRequired()
					.HasMaxLength(256);

				// Add index for performance
				_ = entity.HasIndex(e => e.TenantId);
			});
		}

		#endregion
	}
}