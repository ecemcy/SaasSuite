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
using Microsoft.EntityFrameworkCore.Infrastructure;

using SaasSuite.Core.Interfaces;

namespace SaasSuite.EfCore.Implementations
{
	/// <summary>
	/// Implements per-tenant model caching for Entity Framework Core.
	/// </summary>
	/// <remarks>
	/// This implementation allows different tenants to have distinct database schemas or configurations
	/// by creating separate cached models per tenant. Without this, all tenants would share the same model.
	/// </remarks>
	public class TenantModelCacheKeyFactory
		: IModelCacheKeyFactory
	{
		#region ' Fields '

		/// <summary>
		/// Provides access to the current tenant context.
		/// </summary>
		private readonly ITenantAccessor _tenantAccessor;

		#endregion

		#region ' Constructors '

		/// <summary>
		/// Initializes a new instance of the <see cref="TenantModelCacheKeyFactory"/> class.
		/// </summary>
		/// <param name="tenantAccessor">The service providing current tenant context.</param>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="tenantAccessor"/> is <see langword="null"/>.
		/// </exception>
		public TenantModelCacheKeyFactory(ITenantAccessor tenantAccessor)
		{
			this._tenantAccessor = tenantAccessor ?? throw new ArgumentNullException(nameof(tenantAccessor));
		}

		#endregion

		#region ' Methods '

		/// <summary>
		/// Creates a cache key for the current tenant and DbContext combination.
		/// </summary>
		/// <param name="context">The <see cref="DbContext"/> instance being cached.</param>
		/// <param name="designTime">
		/// <see langword="true"/> if the model is being built at design time (e.g., for migrations);
		/// otherwise, <see langword="false"/>.
		/// </param>
		/// <returns>
		/// An object that serves as the cache key, incorporating tenant ID, context type, and design-time flag.
		/// </returns>
		public object Create(DbContext context, bool designTime)
		{
			// Use current tenant ID or "default" if no tenant context
			string tenantId = this._tenantAccessor.TenantContext?.TenantId.Value ?? "default";
			return new TenantModelCacheKey(context.GetType(), tenantId, designTime);
		}

		#endregion

		#region ' Classes '

		/// <summary>
		/// Internal cache key structure combining context type, tenant ID, and design-time mode.
		/// </summary>
		/// <remarks>
		/// This record-like structure implements value equality to ensure that cache lookups work correctly.
		/// Two keys are considered equal only when all three components match, ensuring tenant isolation in caching.
		/// </remarks>
		private sealed class TenantModelCacheKey
			: IEquatable<TenantModelCacheKey>
		{
			#region ' Fields '

			/// <summary>
			/// Whether the model is for design-time use (e.g., migrations, scaffolding).
			/// </summary>
			/// <remarks>
			/// Design-time and runtime models are cached separately to prevent conflicts.
			/// </remarks>
			private readonly bool _designTime;

			/// <summary>
			/// The tenant identifier.
			/// </summary>
			/// <remarks>
			/// This ensures each tenant gets its own cached model, allowing for per-tenant schema variations.
			/// </remarks>
			private readonly string _tenantId;

			/// <summary>
			/// The DbContext type being cached.
			/// </summary>
			/// <remarks>
			/// Different DbContext types will have separate cache entries even for the same tenant.
			/// </remarks>
			private readonly Type _contextType;

			#endregion

			#region ' Constructors '

			/// <summary>
			/// Initializes a new instance of the <see cref="TenantModelCacheKey"/> class.
			/// </summary>
			/// <param name="contextType">The DbContext type being cached.</param>
			/// <param name="tenantId">The tenant identifier for cache isolation.</param>
			/// <param name="designTime">Whether this is a design-time model (used for migrations and tooling).</param>
			public TenantModelCacheKey(Type contextType, string tenantId, bool designTime)
			{
				this._contextType = contextType;
				this._tenantId = tenantId;
				this._designTime = designTime;
			}

			#endregion

			#region ' Methods '

			/// <summary>
			/// Determines whether the specified cache key is equal to the current cache key.
			/// </summary>
			/// <param name="other">The cache key to compare.</param>
			/// <returns>
			/// <see langword="true"/> if all components match; otherwise, <see langword="false"/>.
			/// </returns>
			public bool Equals(TenantModelCacheKey? other)
			{
				if (other is null)
				{
					return false;
				}

				return this._contextType == other._contextType
					&& this._tenantId == other._tenantId
					&& this._designTime == other._designTime;
			}

			#endregion

			#region ' Override Methods '

			/// <summary>
			/// Determines whether the specified object is equal to the current cache key.
			/// </summary>
			/// <param name="obj">The object to compare.</param>
			/// <returns>
			/// <see langword="true"/> if the object is a <see cref="TenantModelCacheKey"/> and all components match;
			/// otherwise, <see langword="false"/>.
			/// </returns>
			public override bool Equals(object? obj)
			{
				return obj is TenantModelCacheKey key && this.Equals(key);
			}

			/// <summary>
			/// Computes the hash code for this cache key based on its components.
			/// </summary>
			/// <returns>A hash code combining context type, tenant ID, and design-time flag.</returns>
			public override int GetHashCode()
			{
				return HashCode.Combine(this._contextType, this._tenantId, this._designTime);
			}

			#endregion
		}

		#endregion
	}
}