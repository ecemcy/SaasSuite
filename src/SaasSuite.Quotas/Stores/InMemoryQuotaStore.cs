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

using System.Collections.Concurrent;

using SaasSuite.Core;
using SaasSuite.Quotas.Enumerations;
using SaasSuite.Quotas.Interfaces;

namespace SaasSuite.Quotas.Stores
{
	/// <summary>
	/// Provides an in-memory, thread-safe implementation of <see cref="IQuotaStore"/> for quota storage and tracking.
	/// </summary>
	/// <remarks>
	/// This implementation uses concurrent dictionaries for fast, lock-free reads and writes.
	/// Data is stored only in memory and will be lost when the application restarts.
	/// Suitable for development, testing, and single-instance deployments. For production multi-instance
	/// scenarios, use a distributed store implementation backed by Redis, SQL, or similar.
	/// </remarks>
	public class InMemoryQuotaStore
		: IQuotaStore
	{
		#region ' Fields '

		/// <summary>
		/// Thread-safe dictionary storing quota definitions keyed by tenant and quota name.
		/// </summary>
		private readonly ConcurrentDictionary<string, QuotaDefinition> _quotaDefinitions = new ConcurrentDictionary<string, QuotaDefinition>();

		/// <summary>
		/// Thread-safe dictionary storing usage entries keyed by tenant, quota name, scope, and scope key.
		/// </summary>
		private readonly ConcurrentDictionary<string, UsageEntry> _usageData = new ConcurrentDictionary<string, UsageEntry>();

		/// <summary>
		/// Semaphore used to synchronize cleanup operations on the usage data dictionary.
		/// </summary>
		private readonly SemaphoreSlim _cleanupLock = new SemaphoreSlim(1, 1);

		#endregion

		#region ' Methods '

		/// <summary>
		/// Removes expired usage entries from the in-memory store to prevent unbounded memory growth.
		/// </summary>
		/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
		/// <returns>A task representing the asynchronous cleanup operation.</returns>
		/// <remarks>
		/// This method is called periodically after usage increments to maintain memory efficiency.
		/// It identifies all usage entries where the reset time has passed and removes them from the dictionary.
		/// A semaphore ensures only one cleanup operation runs at a time, preventing redundant work
		/// when multiple concurrent increments occur. If a cleanup is already in progress, subsequent
		/// calls return immediately without waiting.
		/// </remarks>
		private async Task CleanupExpiredEntriesAsync(CancellationToken cancellationToken = default)
		{
			// Only one cleanup operation should run at a time
			if (!await this._cleanupLock.WaitAsync(0, cancellationToken))
			{
				return;
			}

			try
			{
				DateTime now = DateTime.UtcNow;

				// Find all expired entries
				List<string> expiredKeys = this._usageData
					.Where(kvp => kvp.Value.ResetTime.HasValue && kvp.Value.ResetTime.Value <= now)
					.Select(kvp => kvp.Key)
					.ToList();

				// Remove each expired entry
				foreach (string? key in expiredKeys)
				{
					_ = this._usageData.TryRemove(key, out _);
				}
			}
			finally
			{
				_ = this._cleanupLock.Release();
			}
		}

		/// <summary>
		/// Resets the usage counter for a specific quota to zero.
		/// </summary>
		/// <param name="tenantId">The tenant identifier for which to reset usage.</param>
		/// <param name="quotaName">The unique identifier of the quota to reset.</param>
		/// <param name="scope">The scope level at which to reset usage.</param>
		/// <param name="scopeKey">An optional additional key for resource or user scoping. Can be <see langword="null"/> for tenant scope.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
		/// <returns>
		/// A task that represents the asynchronous operation.
		/// </returns>
		/// <remarks>
		/// This method removes the existing usage entry and optionally recreates it with zero usage and a fresh reset time.
		/// If a quota definition exists for the specified quota, a new usage entry is created with a recalculated reset time
		/// based on the quota's period. This ensures the next increment operation starts with the correct reset boundary.
		/// Use this method for administrative overrides or when implementing custom reset logic outside the automatic period-based system.
		/// </remarks>
		public async Task ResetUsageAsync(TenantId tenantId, string quotaName, QuotaScope scope = QuotaScope.Tenant, string? scopeKey = null, CancellationToken cancellationToken = default)
		{
			// Generate the composite key for usage tracking
			string key = GetUsageKey(tenantId, quotaName, scope, scopeKey);

			// Remove the usage entry, effectively resetting to zero
			_ = this._usageData.TryRemove(key, out _);

			// Optionally recreate the entry with zero usage and a new reset time
			QuotaDefinition? definition = await this.GetQuotaDefinitionAsync(tenantId, quotaName, cancellationToken);
			if (definition != null)
			{
				DateTime? resetTime = CalculateResetTime(definition.Period);
				_ = this._usageData.TryAdd(key, new UsageEntry
				{
					CurrentUsage = 0,
					ResetTime = resetTime
				});
			}
		}

		/// <summary>
		/// Increments the usage counter for a specific quota by the specified amount.
		/// </summary>
		/// <param name="tenantId">The tenant identifier for which to increment usage.</param>
		/// <param name="quotaName">The unique identifier of the quota to increment.</param>
		/// <param name="scope">The scope level at which to track the increment.</param>
		/// <param name="amount">The number of units to add to the current usage. Must be non-negative. Defaults to 1.</param>
		/// <param name="scopeKey">An optional additional key for resource or user scoping. Can be <see langword="null"/> for tenant scope.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
		/// <returns>
		/// A task that represents the asynchronous operation. The task result contains the new total usage count
		/// after the increment has been applied.
		/// </returns>
		/// <remarks>
		/// This method atomically increments the usage counter in a thread-safe manner using <see cref="ConcurrentDictionary{TKey,TValue}.AddOrUpdate(TKey, Func{TKey, TValue}, Func{TKey, TValue, TValue})"/>.
		/// If an existing usage entry has expired (past its reset time), the counter is reset to the specified amount
		/// rather than incremented. A new reset time is calculated based on the quota definition's period.
		/// After incrementing, a background cleanup task is triggered to remove expired entries and prevent memory growth.
		/// The amount parameter allows for batch consumption, such as consuming multiple API calls in a single operation.
		/// </remarks>
		public async Task<int> IncrementUsageAsync(TenantId tenantId, string quotaName, QuotaScope scope = QuotaScope.Tenant, int amount = 1, string? scopeKey = null, CancellationToken cancellationToken = default)
		{
			// Generate the composite key for usage tracking
			string key = GetUsageKey(tenantId, quotaName, scope, scopeKey);

			// Retrieve the quota definition to determine the reset period
			QuotaDefinition? definition = await this.GetQuotaDefinitionAsync(tenantId, quotaName, cancellationToken);
			DateTime? resetTime = definition != null ? CalculateResetTime(definition.Period) : null;

			// Atomically update or add the usage entry
			UsageEntry entry = this._usageData.AddOrUpdate(
				key,
				// Factory for new entry
				_ => new UsageEntry
				{
					CurrentUsage = amount,
					ResetTime = resetTime
				},
				// Update function for existing entry
				(_, existing) =>
				{
					// Check if the existing entry has expired
					if (existing.ResetTime.HasValue && existing.ResetTime.Value <= DateTime.UtcNow)
					{
						// Reset usage and update reset time
						return new UsageEntry
						{
							CurrentUsage = amount,
							ResetTime = resetTime
						};
					}

					// Increment existing usage
					return new UsageEntry
					{
						CurrentUsage = existing.CurrentUsage + amount,
						ResetTime = existing.ResetTime
					};
				});

			// Periodically clean up expired entries to prevent memory growth
			_ = Task.Run(() => this.CleanupExpiredEntriesAsync(), cancellationToken);

			return entry.CurrentUsage;
		}

		/// <summary>
		/// Retrieves the complete status of a quota, including current usage and calculated metrics.
		/// </summary>
		/// <param name="tenantId">The tenant identifier for which to retrieve quota status.</param>
		/// <param name="quotaName">The unique identifier of the quota to query.</param>
		/// <param name="scope">The scope level at which to query quota status.</param>
		/// <param name="scopeKey">An optional additional key for resource or user scoping. Can be <see langword="null"/> for tenant scope.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
		/// <returns>
		/// A task that represents the asynchronous operation. The task result contains a <see cref="QuotaStatus"/>
		/// instance if the quota definition exists, or <see langword="null"/> if the quota is not defined.
		/// </returns>
		/// <remarks>
		/// This method combines data from the quota definition and current usage to build a comprehensive status object.
		/// The returned <see cref="QuotaStatus"/> includes calculated properties such as remaining capacity,
		/// percentage used, and whether the quota is exceeded. The reset time is calculated based on the quota's period.
		/// If no quota definition exists, <see langword="null"/> is returned even if usage data exists.
		/// </remarks>
		public async Task<QuotaStatus?> GetQuotaStatusAsync(TenantId tenantId, string quotaName, QuotaScope scope = QuotaScope.Tenant, string? scopeKey = null, CancellationToken cancellationToken = default)
		{
			// Retrieve the quota definition first
			QuotaDefinition? definition = await this.GetQuotaDefinitionAsync(tenantId, quotaName, cancellationToken);
			if (definition == null)
			{
				// Cannot create status without a definition
				return null;
			}

			// Get current usage for this quota
			int currentUsage = await this.GetCurrentUsageAsync(tenantId, quotaName, scope, scopeKey, cancellationToken);

			// Calculate the next reset time based on the quota period
			DateTime? resetTime = CalculateResetTime(definition.Period);

			// Construct and return the status object
			return new QuotaStatus
			{
				QuotaName = quotaName,
				CurrentUsage = currentUsage,
				Limit = definition.Limit,
				Period = definition.Period,
				ResetTime = resetTime
			};
		}

		/// <summary>
		/// Creates or updates a quota definition for a specific tenant.
		/// </summary>
		/// <param name="tenantId">The tenant identifier for which to set the quota definition.</param>
		/// <param name="quotaDefinition">The quota definition to persist, containing all configuration parameters.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests. Not used in this implementation.</param>
		/// <returns>
		/// A task that represents the asynchronous operation.
		/// </returns>
		/// <remarks>
		/// If a quota definition with the same name already exists for the tenant, it is replaced with the new definition.
		/// This operation only affects the definition metadata (limit, period, scope, metadata) and does not modify
		/// existing usage counters. To reset usage, call <see cref="ResetUsageAsync"/> separately.
		/// The quota definition is immediately available for use in quota checks and status queries.
		/// </remarks>
		public Task SetQuotaDefinitionAsync(TenantId tenantId, QuotaDefinition quotaDefinition, CancellationToken cancellationToken = default)
		{
			// Generate the composite key for the quota definition
			string key = GetDefinitionKey(tenantId, quotaDefinition.Name);

			// Store or update the definition
			this._quotaDefinitions[key] = quotaDefinition;

			return Task.CompletedTask;
		}

		/// <summary>
		/// Retrieves the current usage count for a specific quota.
		/// </summary>
		/// <param name="tenantId">The tenant identifier for tenant-scoped quotas.</param>
		/// <param name="quotaName">The unique identifier of the quota to query.</param>
		/// <param name="scope">The scope level at which usage is tracked.</param>
		/// <param name="scopeKey">An optional additional key for resource or user scoping. Can be <see langword="null"/> for tenant scope.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests. Not used in this implementation.</param>
		/// <returns>
		/// A task that represents the asynchronous operation. The task result contains the current usage count,
		/// or <c>0</c> if no usage has been recorded or if the usage period has expired.
		/// </returns>
		/// <remarks>
		/// This method automatically cleans up expired usage entries when detected during retrieval.
		/// If the stored usage entry has passed its reset time, it is removed and <c>0</c> is returned.
		/// The scope and scopeKey parameters work together to identify the correct usage counter.
		/// </remarks>
		public Task<int> GetCurrentUsageAsync(TenantId tenantId, string quotaName, QuotaScope scope = QuotaScope.Tenant, string? scopeKey = null, CancellationToken cancellationToken = default)
		{
			// Generate the composite key for looking up usage data
			string key = GetUsageKey(tenantId, quotaName, scope, scopeKey);

			// Attempt to retrieve the usage entry and clean up if expired
			if (this._usageData.TryGetValue(key, out UsageEntry? entry))
			{
				// Check if the entry has passed its reset time
				if (entry.ResetTime.HasValue && entry.ResetTime.Value <= DateTime.UtcNow)
				{
					// Usage period has expired, remove the stale entry
					_ = this._usageData.TryRemove(key, out _);
					return Task.FromResult(0);
				}

				return Task.FromResult(entry.CurrentUsage);
			}

			// No usage recorded yet
			return Task.FromResult(0);
		}

		/// <summary>
		/// Retrieves all quota definitions configured for a specific tenant.
		/// </summary>
		/// <param name="tenantId">The tenant identifier for which to retrieve quota definitions.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests. Not used in this implementation.</param>
		/// <returns>
		/// A task that represents the asynchronous operation. The task result contains a collection of all
		/// <see cref="QuotaDefinition"/> instances for the tenant, or an empty collection if none exist.
		/// </returns>
		/// <remarks>
		/// This method filters the internal dictionary to return only definitions belonging to the specified tenant.
		/// The returned collection includes all quota types configured for the tenant regardless of their current usage status.
		/// </remarks>
		public Task<IEnumerable<QuotaDefinition>> GetQuotaDefinitionsAsync(TenantId tenantId, CancellationToken cancellationToken = default)
		{
			// Filter all definitions that belong to the specified tenant
			string tenantPrefix = $"{tenantId.Value}:";
			IEnumerable<QuotaDefinition> definitions = this._quotaDefinitions
				.Where(kvp => kvp.Key.StartsWith(tenantPrefix))
				.Select(kvp => kvp.Value);

			return Task.FromResult(definitions);
		}

		/// <summary>
		/// Retrieves the quota definition for a specific quota and tenant.
		/// </summary>
		/// <param name="tenantId">The tenant identifier for which to retrieve the quota definition.</param>
		/// <param name="quotaName">The unique identifier of the quota definition to retrieve.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests. Not used in this implementation.</param>
		/// <returns>
		/// A task that represents the asynchronous operation. The task result contains the <see cref="QuotaDefinition"/>
		/// if found, or <see langword="null"/> if no definition exists for the specified quota and tenant.
		/// </returns>
		/// <remarks>
		/// Quota definitions are stored per tenant, allowing different tenants to have different limits
		/// for the same quota type. The definition includes the limit, period, scope, and optional metadata.
		/// </remarks>
		public Task<QuotaDefinition?> GetQuotaDefinitionAsync(TenantId tenantId, string quotaName, CancellationToken cancellationToken = default)
		{
			// Generate the composite key for the quota definition
			string key = GetDefinitionKey(tenantId, quotaName);

			// Attempt to retrieve the definition
			_ = this._quotaDefinitions.TryGetValue(key, out QuotaDefinition? definition);
			return Task.FromResult(definition);
		}

		#endregion

		#region ' Static Methods '

		/// <summary>
		/// Generates a unique key for storing and retrieving quota definitions.
		/// </summary>
		/// <param name="tenantId">The tenant identifier.</param>
		/// <param name="quotaName">The name of the quota.</param>
		/// <returns>A composite string key in the format "tenantId:quotaName".</returns>
		/// <remarks>
		/// This key format ensures tenant isolation by incorporating the tenant ID as a prefix,
		/// allowing the same quota name to have different definitions per tenant.
		/// </remarks>
		private static string GetDefinitionKey(TenantId tenantId, string quotaName)
		{
			return $"{tenantId.Value}:{quotaName}";
		}

		/// <summary>
		/// Generates a unique key for storing and retrieving usage data.
		/// </summary>
		/// <param name="tenantId">The tenant identifier.</param>
		/// <param name="quotaName">The name of the quota.</param>
		/// <param name="scope">The scope level of the quota.</param>
		/// <param name="scopeKey">An optional scope key for resource or user identification.</param>
		/// <returns>A composite string key in the format "tenantId:quotaName:scope[:scopeKey]".</returns>
		/// <remarks>
		/// The key format includes all parameters that define a unique usage context.
		/// For tenant-scoped quotas, the scopeKey is omitted. For resource or user-scoped quotas,
		/// the scopeKey is appended to differentiate between different entities within the same scope.
		/// This ensures proper isolation and accurate tracking across different scoping levels.
		/// </remarks>
		private static string GetUsageKey(TenantId tenantId, string quotaName, QuotaScope scope, string? scopeKey)
		{
			// Build the key with scope, optionally appending the scope key if provided
			string baseKey = $"{tenantId.Value}:{quotaName}:{scope}";
			return string.IsNullOrEmpty(scopeKey) ? baseKey : $"{baseKey}:{scopeKey}";
		}

		/// <summary>
		/// Calculates the next reset time based on the specified quota period.
		/// </summary>
		/// <param name="period">The period type determining reset frequency.</param>
		/// <returns>
		/// A <see cref="DateTime"/> representing the next reset time in UTC,
		/// or <see langword="null"/> for <see cref="QuotaPeriod.Total"/> quotas that never reset.
		/// </returns>
		/// <remarks>
		/// Reset times are calculated as follows:
		/// <list type="bullet">
		/// <item><description><see cref="QuotaPeriod.Hourly"/>: Top of the next hour (e.g., if current time is 14:37, reset is at 15:00)</description></item>
		/// <item><description><see cref="QuotaPeriod.Daily"/>: Midnight UTC of the next day</description></item>
		/// <item><description><see cref="QuotaPeriod.Monthly"/>: Midnight UTC on the first day of the next month</description></item>
		/// <item><description><see cref="QuotaPeriod.Total"/>: <see langword="null"/> (never resets)</description></item>
		/// </list>
		/// All times are calculated in UTC to ensure consistency across time zones.
		/// </remarks>
		private static DateTime? CalculateResetTime(QuotaPeriod period)
		{
			DateTime now = DateTime.UtcNow;

			return period switch
			{
				// Reset at the top of the next hour
				QuotaPeriod.Hourly => new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0, DateTimeKind.Utc).AddHours(1),

				// Reset at midnight UTC tomorrow
				QuotaPeriod.Daily => new DateTime(now.Year, now.Month, now.Day, 0, 0, 0, DateTimeKind.Utc).AddDays(1),

				// Reset at midnight UTC on the first day of next month
				QuotaPeriod.Monthly => new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(1),

				// Total quotas never reset
				QuotaPeriod.Total => null,

				_ => null
			};
		}

		#endregion

		#region ' Classes '

		/// <summary>
		/// Represents a usage tracking entry with current consumption and reset timing.
		/// </summary>
		/// <remarks>
		/// This private class is used internally to store usage data in the concurrent dictionary.
		/// Each entry tracks both the accumulated usage count and the timestamp when the counter should reset.
		/// Instances are created and updated atomically through the <see cref="ConcurrentDictionary{TKey,TValue}"/> operations.
		/// </remarks>
		private class UsageEntry
		{
			#region ' Properties '

			/// <summary>
			/// Gets or sets the current accumulated usage count.
			/// </summary>
			/// <value>A non-negative integer representing units consumed within the current period.</value>
			public int CurrentUsage { get; set; }

			/// <summary>
			/// Gets or sets the UTC timestamp when this usage should be reset to zero.
			/// </summary>
			/// <value>
			/// A <see cref="DateTime"/> in UTC for time-based quotas,
			/// or <see langword="null"/> for total quotas that never reset.
			/// </value>
			/// <remarks>
			/// When the current time exceeds this value, the usage entry is considered expired
			/// and will be removed during cleanup or reset to zero on the next increment.
			/// </remarks>
			public DateTime? ResetTime { get; set; }

			#endregion
		}

		#endregion
	}
}