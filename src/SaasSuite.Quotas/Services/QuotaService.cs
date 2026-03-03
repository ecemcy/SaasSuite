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

using Microsoft.Extensions.Options;

using SaasSuite.Core;
using SaasSuite.Quotas.Enumerations;
using SaasSuite.Quotas.Interfaces;
using SaasSuite.Quotas.Options;

namespace SaasSuite.Quotas.Services
{
	/// <summary>
	/// Provides high-level quota management operations including consumption, status checking, and definition management.
	/// </summary>
	/// <remarks>
	/// This service acts as the primary facade for quota operations, coordinating between the quota store
	/// and configuration options to provide a simplified API for quota enforcement throughout the application.
	/// </remarks>
	public class QuotaService
	{
		#region ' Fields '

		/// <summary>
		/// The underlying quota store for persistence operations.
		/// </summary>
		private readonly IQuotaStore _quotaStore;

		/// <summary>
		/// Configuration options controlling quota enforcement behavior.
		/// </summary>
		private readonly QuotaOptions _options;

		#endregion

		#region ' Constructors '

		/// <summary>
		/// Initializes a new instance of the <see cref="QuotaService"/> class.
		/// </summary>
		/// <param name="quotaStore">The quota store implementation for data persistence.</param>
		/// <param name="options">The configuration options for quota behavior.</param>
		public QuotaService(IQuotaStore quotaStore, IOptions<QuotaOptions> options)
		{
			this._quotaStore = quotaStore;
			this._options = options.Value;
		}

		#endregion

		#region ' Methods '

		/// <summary>
		/// Unconditionally consumes the specified amount from the quota, incrementing usage regardless of the limit.
		/// </summary>
		/// <param name="tenantId">The tenant identifier for which to consume the quota.</param>
		/// <param name="quotaName">The unique identifier of the quota to consume.</param>
		/// <param name="scope">The scope level at which to consume the quota.</param>
		/// <param name="amount">The number of units to consume. Defaults to 1.</param>
		/// <param name="scopeKey">An optional scope key for resource or user identification. Can be <see langword="null"/> for tenant scope.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests during the asynchronous operation.</param>
		/// <returns>
		/// A task that represents the asynchronous operation.
		/// </returns>
		/// <remarks>
		/// This method bypasses limit checks and always increments usage. Use with caution.
		/// Typically employed for tracking purposes or when enforcement is handled separately.
		/// The quota may become exceeded after this operation.
		/// </remarks>
		public async Task ConsumeAsync(TenantId tenantId, string quotaName, QuotaScope scope = QuotaScope.Tenant, int amount = 1, string? scopeKey = null, CancellationToken cancellationToken = default)
		{
			_ = await this._quotaStore.IncrementUsageAsync(tenantId, quotaName, scope, amount, scopeKey, cancellationToken);
		}

		/// <summary>
		/// Creates or updates a quota definition for a specific tenant.
		/// </summary>
		/// <param name="tenantId">The tenant identifier for which to define the quota.</param>
		/// <param name="quotaDefinition">The quota definition containing limit, period, scope, and metadata.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests during the asynchronous operation.</param>
		/// <returns>
		/// A task that represents the asynchronous operation.
		/// </returns>
		/// <remarks>
		/// This method is typically used by administrative functions to configure or modify quota limits.
		/// If a quota with the same name already exists for the tenant, it will be updated with the new definition.
		/// Existing usage counters are not affected by definition changes.
		/// </remarks>
		public async Task DefineQuotaAsync(TenantId tenantId, QuotaDefinition quotaDefinition, CancellationToken cancellationToken = default)
		{
			await this._quotaStore.SetQuotaDefinitionAsync(tenantId, quotaDefinition, cancellationToken);
		}

		/// <summary>
		/// Manually resets the usage counter for a specific quota to zero.
		/// </summary>
		/// <param name="tenantId">The tenant identifier for which to reset the quota.</param>
		/// <param name="quotaName">The unique identifier of the quota to reset.</param>
		/// <param name="scope">The scope level at which to reset the quota.</param>
		/// <param name="scopeKey">An optional scope key for resource or user identification. Can be <see langword="null"/> for tenant scope.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests during the asynchronous operation.</param>
		/// <returns>
		/// A task that represents the asynchronous operation.
		/// </returns>
		/// <remarks>
		/// This method provides administrative override capability to manually reset quotas.
		/// After reset, the next reset time is recalculated based on the quota's period.
		/// Useful for customer support scenarios or when quotas need to be cleared outside the normal schedule.
		/// </remarks>
		public async Task ResetQuotaAsync(TenantId tenantId, string quotaName, QuotaScope scope = QuotaScope.Tenant, string? scopeKey = null, CancellationToken cancellationToken = default)
		{
			await this._quotaStore.ResetUsageAsync(tenantId, quotaName, scope, scopeKey, cancellationToken);
		}

		/// <summary>
		/// Checks whether the specified amount can be consumed without exceeding the quota limit.
		/// </summary>
		/// <param name="tenantId">The tenant identifier for which to check the quota.</param>
		/// <param name="quotaName">The unique identifier of the quota to check.</param>
		/// <param name="scope">The scope level at which to check the quota.</param>
		/// <param name="amount">The number of units to check for availability. Defaults to 1.</param>
		/// <param name="scopeKey">An optional scope key for resource or user identification. Can be <see langword="null"/> for tenant scope.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests during the asynchronous operation.</param>
		/// <returns>
		/// A task that represents the asynchronous operation. The task result contains <see langword="true"/>
		/// if the amount can be consumed without exceeding the limit; otherwise, <see langword="false"/>.
		/// </returns>
		/// <remarks>
		/// This method performs a non-destructive check without incrementing usage counters.
		/// If enforcement is disabled via <see cref="QuotaOptions.EnableEnforcement"/>, always returns <see langword="true"/>.
		/// If the quota is not defined and <see cref="QuotaOptions.AllowIfQuotaNotDefined"/> is <see langword="true"/>, returns <see langword="true"/>.
		/// </remarks>
		public async Task<bool> CanConsumeAsync(TenantId tenantId, string quotaName, QuotaScope scope = QuotaScope.Tenant, int amount = 1, string? scopeKey = null, CancellationToken cancellationToken = default)
		{
			// If enforcement is disabled globally, always allow
			if (!this._options.EnableEnforcement)
			{
				return true;
			}

			// Retrieve the current quota status
			QuotaStatus? status = await this._quotaStore.GetQuotaStatusAsync(tenantId, quotaName, scope, scopeKey, cancellationToken);

			// Handle undefined quota based on configuration
			if (status == null)
			{
				return this._options.AllowIfQuotaNotDefined;
			}

			// Check if consuming the specified amount would exceed the limit
			return status.CurrentUsage + amount <= status.Limit;
		}

		/// <summary>
		/// Attempts to consume the specified amount from the quota, incrementing usage only if the limit allows.
		/// </summary>
		/// <param name="tenantId">The tenant identifier for which to consume the quota.</param>
		/// <param name="quotaName">The unique identifier of the quota to consume.</param>
		/// <param name="scope">The scope level at which to consume the quota.</param>
		/// <param name="amount">The number of units to consume. Defaults to 1.</param>
		/// <param name="scopeKey">An optional scope key for resource or user identification. Can be <see langword="null"/> for tenant scope.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests during the asynchronous operation.</param>
		/// <returns>
		/// A task that represents the asynchronous operation. The task result contains <see langword="true"/>
		/// if the consumption succeeded; otherwise, <see langword="false"/> if the quota would be exceeded.
		/// </returns>
		/// <remarks>
		/// This method atomically checks and increments the quota in a single operation.
		/// If the quota would be exceeded, no increment occurs and <see langword="false"/> is returned.
		/// If enforcement is disabled or the quota is undefined with permissive settings, consumption always succeeds.
		/// </remarks>
		public async Task<bool> TryConsumeAsync(TenantId tenantId, string quotaName, QuotaScope scope = QuotaScope.Tenant, int amount = 1, string? scopeKey = null, CancellationToken cancellationToken = default)
		{
			// Check if consumption is allowed
			if (!await this.CanConsumeAsync(tenantId, quotaName, scope, amount, scopeKey, cancellationToken))
			{
				return false;
			}

			// Increment the usage counter
			_ = await this._quotaStore.IncrementUsageAsync(tenantId, quotaName, scope, amount, scopeKey, cancellationToken);
			return true;
		}

		/// <summary>
		/// Retrieves the status of all defined quotas for a specific tenant.
		/// </summary>
		/// <param name="tenantId">The tenant identifier for which to retrieve all quota statuses.</param>
		/// <param name="scope">The scope level at which to query quota statuses.</param>
		/// <param name="scopeKey">An optional scope key for resource or user identification. Can be <see langword="null"/> for tenant scope.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests during the asynchronous operation.</param>
		/// <returns>
		/// A task that represents the asynchronous operation. The task result contains a collection of
		/// <see cref="QuotaStatus"/> instances for all quotas defined for the tenant, or an empty collection if none exist.
		/// </returns>
		/// <remarks>
		/// This method retrieves all quota definitions for the tenant and then queries the status of each.
		/// Useful for dashboard displays showing all quota metrics at once.
		/// </remarks>
		public async Task<IEnumerable<QuotaStatus>> GetAllQuotaStatusesAsync(TenantId tenantId, QuotaScope scope = QuotaScope.Tenant, string? scopeKey = null, CancellationToken cancellationToken = default)
		{
			// Get all quota definitions for the tenant
			IEnumerable<QuotaDefinition> definitions = await this._quotaStore.GetQuotaDefinitionsAsync(tenantId, cancellationToken);

			// Retrieve status for each defined quota
			List<QuotaStatus> statuses = new List<QuotaStatus>();
			foreach (QuotaDefinition definition in definitions)
			{
				QuotaStatus? status = await this._quotaStore.GetQuotaStatusAsync(tenantId, definition.Name, scope, scopeKey, cancellationToken);
				if (status != null)
				{
					statuses.Add(status);
				}
			}

			return statuses;
		}

		/// <summary>
		/// Retrieves the current status of a specific quota.
		/// </summary>
		/// <param name="tenantId">The tenant identifier for which to retrieve quota status.</param>
		/// <param name="quotaName">The unique identifier of the quota to query.</param>
		/// <param name="scope">The scope level at which to query the quota status.</param>
		/// <param name="scopeKey">An optional scope key for resource or user identification. Can be <see langword="null"/> for tenant scope.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests during the asynchronous operation.</param>
		/// <returns>
		/// A task that represents the asynchronous operation. The task result contains a <see cref="QuotaStatus"/>
		/// instance with current usage and calculated metrics, or <see langword="null"/> if the quota is not defined.
		/// </returns>
		/// <remarks>
		/// This method provides comprehensive quota information including usage, limits, remaining capacity,
		/// and reset timing. Useful for displaying quota status to users or in administrative interfaces.
		/// </remarks>
		public async Task<QuotaStatus?> GetQuotaStatusAsync(TenantId tenantId, string quotaName, QuotaScope scope = QuotaScope.Tenant, string? scopeKey = null, CancellationToken cancellationToken = default)
		{
			return await this._quotaStore.GetQuotaStatusAsync(tenantId, quotaName, scope, scopeKey, cancellationToken);
		}

		#endregion
	}
}