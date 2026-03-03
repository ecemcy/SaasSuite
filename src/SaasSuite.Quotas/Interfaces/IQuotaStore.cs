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
using SaasSuite.Quotas.Enumerations;

namespace SaasSuite.Quotas.Interfaces
{
	/// <summary>
	/// Defines the contract for persisting and retrieving quota definitions and usage data across different storage mechanisms.
	/// </summary>
	/// <remarks>
	/// Implementations of this interface provide the storage backend for quota enforcement,
	/// supporting operations for definition management, usage tracking, and status reporting.
	/// All methods are asynchronous to support scalable, non-blocking data access patterns
	/// required in high-throughput web applications. Implementations must handle:
	/// <list type="bullet">
	/// <item><description>Thread-safe concurrent access to quota data from multiple requests</description></item>
	/// <item><description>Atomic increment operations to prevent race conditions in usage tracking</description></item>
	/// <item><description>Efficient storage and retrieval with minimal latency impact on request processing</description></item>
	/// <item><description>Proper tenant isolation to ensure quota data cannot leak between tenants</description></item>
	/// </list>
	/// The interface supports multiple scoping levels (tenant, resource, user) through the
	/// combination of scope and scopeKey parameters, enabling flexible quota tracking strategies.
	/// </remarks>
	public interface IQuotaStore
	{
		#region ' Methods '

		/// <summary>
		/// Resets the usage counter for a specific quota to zero, optionally recalculating the next reset time.
		/// </summary>
		/// <param name="tenantId">The tenant identifier for which to reset usage. Must be a valid tenant ID.</param>
		/// <param name="quotaName">The unique identifier of the quota to reset. Must not be <see langword="null"/> or empty.</param>
		/// <param name="scope">The scope level at which to reset usage (Tenant, Resource, or User).</param>
		/// <param name="scopeKey">An optional additional key for resource or user scoping. Required for <see cref="QuotaScope.Resource"/> and <see cref="QuotaScope.User"/> scopes, can be <see langword="null"/> for <see cref="QuotaScope.Tenant"/>.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests during the asynchronous operation.</param>
		/// <returns>
		/// A <see cref="Task"/> that represents the asynchronous reset operation.
		/// </returns>
		/// <remarks>
		/// This method manually resets usage, typically invoked for administrative overrides, support scenarios,
		/// or when the automatic period-based reset needs to be triggered explicitly outside the normal schedule.
		/// After reset, implementations should recalculate the next reset time based on the quota's period
		/// to ensure the quota continues functioning correctly. The operation should be atomic to prevent
		/// partial resets in concurrent scenarios. If the quota definition does not exist, implementations
		/// may choose to silently succeed (idempotent behavior) or throw an exception, depending on design preferences.
		/// </remarks>
		Task ResetUsageAsync(TenantId tenantId, string quotaName, QuotaScope scope = QuotaScope.Tenant, string? scopeKey = null, CancellationToken cancellationToken = default);

		/// <summary>
		/// Creates a new quota definition or updates an existing one for a specific tenant.
		/// </summary>
		/// <param name="tenantId">The tenant identifier for which to set the quota definition. Must be a valid tenant ID.</param>
		/// <param name="quotaDefinition">The quota definition to persist, containing limit, period, scope, and optional metadata. Must not be <see langword="null"/>.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests during the asynchronous operation.</param>
		/// <returns>
		/// A <see cref="Task"/> that represents the asynchronous set operation.
		/// </returns>
		/// <remarks>
		/// If a quota definition with the same name already exists for the tenant, it will be replaced
		/// with the new values provided in <paramref name="quotaDefinition"/>. This operation is used for
		/// administrative configuration and provisioning of quota limits and does not affect current usage counters.
		/// Implementations should ensure the operation is atomic and that the new definition becomes immediately
		/// available for quota checks. Consider validating that the quota definition contains valid values
		/// (e.g., non-negative limits) before persisting. Changes to quota definitions do not automatically
		/// reset usage counters; if a reset is desired, call <see cref="ResetUsageAsync"/> separately.
		/// </remarks>
		Task SetQuotaDefinitionAsync(TenantId tenantId, QuotaDefinition quotaDefinition, CancellationToken cancellationToken = default);

		/// <summary>
		/// Retrieves the current usage count for a specific quota at the specified scope.
		/// </summary>
		/// <param name="tenantId">The tenant identifier for tenant-scoped quotas. Must be a valid tenant ID.</param>
		/// <param name="quotaName">The unique identifier of the quota to query. Must not be <see langword="null"/> or empty.</param>
		/// <param name="scope">The scope level at which usage is tracked (Tenant, Resource, or User).</param>
		/// <param name="scopeKey">An optional additional key for resource or user scoping. Required for <see cref="QuotaScope.Resource"/> and <see cref="QuotaScope.User"/> scopes, can be <see langword="null"/> for <see cref="QuotaScope.Tenant"/>.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests during the asynchronous operation.</param>
		/// <returns>
		/// A <see cref="Task{TResult}"/> that represents the asynchronous operation. The task result contains
		/// the current usage count as a non-negative integer, or <c>0</c> if no usage has been recorded or if the usage period has expired.
		/// </returns>
		/// <remarks>
		/// The scope and scopeKey parameters work together to identify the correct usage counter within the storage system.
		/// For tenant scope, scopeKey is typically ignored or <see langword="null"/>. For resource or user scope, scopeKey identifies
		/// the specific entity (e.g., "project-123" for resource scope or "user-456" for user scope).
		/// Implementations should check if the usage period has expired and automatically reset or clean up stale entries,
		/// returning <c>0</c> for expired quotas. This method should be highly optimized as it's called frequently
		/// during quota checks before allowing operations to proceed.
		/// </remarks>
		Task<int> GetCurrentUsageAsync(TenantId tenantId, string quotaName, QuotaScope scope = QuotaScope.Tenant, string? scopeKey = null, CancellationToken cancellationToken = default);

		/// <summary>
		/// Atomically increments the usage counter for a specific quota by the specified amount.
		/// </summary>
		/// <param name="tenantId">The tenant identifier for which to increment usage. Must be a valid tenant ID.</param>
		/// <param name="quotaName">The unique identifier of the quota to increment. Must not be <see langword="null"/> or empty.</param>
		/// <param name="scope">The scope level at which to track the increment (Tenant, Resource, or User).</param>
		/// <param name="amount">The number of units to add to the current usage. Must be a positive integer. Defaults to <c>1</c>.</param>
		/// <param name="scopeKey">An optional additional key for resource or user scoping. Required for <see cref="QuotaScope.Resource"/> and <see cref="QuotaScope.User"/> scopes, can be <see langword="null"/> for <see cref="QuotaScope.Tenant"/>.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests during the asynchronous operation.</param>
		/// <returns>
		/// A <see cref="Task{TResult}"/> that represents the asynchronous operation. The task result contains
		/// the new total usage count after the increment has been applied.
		/// </returns>
		/// <remarks>
		/// This method atomically increments the usage counter in a thread-safe manner, preventing race conditions
		/// that could occur with non-atomic read-modify-write operations. Implementations must ensure that concurrent
		/// calls to this method correctly accumulate usage without losing increments due to timing issues.
		/// If the quota definition does not exist, implementations may choose to:
		/// <list type="bullet">
		/// <item><description>Create a usage entry anyway (permissive approach)</description></item>
		/// <item><description>Throw an exception (strict approach requiring pre-defined quotas)</description></item>
		/// <item><description>Return 0 or the amount (no-op approach)</description></item>
		/// </list>
		/// The amount parameter allows for batch consumption scenarios, such as consuming 5 API calls at once
		/// for batch operations or consuming storage in variable chunks. If the usage period has expired since
		/// the last increment, implementations should reset the counter to the specified amount rather than
		/// incrementing the stale value, and recalculate the next reset time based on the quota's period.
		/// </remarks>
		Task<int> IncrementUsageAsync(TenantId tenantId, string quotaName, QuotaScope scope = QuotaScope.Tenant, int amount = 1, string? scopeKey = null, CancellationToken cancellationToken = default);

		/// <summary>
		/// Retrieves all quota definitions configured for a specific tenant.
		/// </summary>
		/// <param name="tenantId">The tenant identifier for which to retrieve quota definitions. Must be a valid tenant ID.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests during the asynchronous operation.</param>
		/// <returns>
		/// A <see cref="Task{TResult}"/> that represents the asynchronous operation. The task result contains
		/// an enumerable collection of all <see cref="QuotaDefinition"/> instances configured for the tenant,
		/// or an empty collection if no quotas have been defined.
		/// </returns>
		/// <remarks>
		/// This method is useful for administrative interfaces, tenant provisioning dashboards,
		/// or initialization scenarios where all configured quotas need to be displayed or validated.
		/// The returned collection includes all quota types regardless of their current usage or enforcement status.
		/// Implementations should ensure efficient retrieval, potentially using tenant-prefixed keys or indexes
		/// to avoid scanning all quota definitions in the system. The order of returned definitions is
		/// implementation-dependent and should not be relied upon unless explicitly documented.
		/// </remarks>
		Task<IEnumerable<QuotaDefinition>> GetQuotaDefinitionsAsync(TenantId tenantId, CancellationToken cancellationToken = default);

		/// <summary>
		/// Retrieves the quota definition for a specific quota name and tenant combination.
		/// </summary>
		/// <param name="tenantId">The tenant identifier for which to retrieve the quota definition. Must be a valid tenant ID.</param>
		/// <param name="quotaName">The unique identifier of the quota definition to retrieve. Must not be <see langword="null"/> or empty.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests during the asynchronous operation.</param>
		/// <returns>
		/// A <see cref="Task{TResult}"/> that represents the asynchronous operation. The task result contains
		/// the <see cref="QuotaDefinition"/> if found, or <see langword="null"/> if no definition exists
		/// for the specified quota and tenant combination.
		/// </returns>
		/// <remarks>
		/// Quota definitions are tenant-specific, allowing different tenants to have different limits,
		/// periods, and metadata for the same logical quota type. For example, tenant A might have a
		/// 10,000 API calls/month limit while tenant B has 50,000 API calls/month for the same "api-calls" quota.
		/// This method is called frequently during quota enforcement to retrieve limit and period information,
		/// so implementations should optimize for read performance, potentially using caching strategies.
		/// Returning <see langword="null"/> indicates the quota has not been configured for the tenant,
		/// allowing calling code to apply default behavior based on <c>QuotaOptions.AllowIfQuotaNotDefined</c>.
		/// </remarks>
		Task<QuotaDefinition?> GetQuotaDefinitionAsync(TenantId tenantId, string quotaName, CancellationToken cancellationToken = default);

		/// <summary>
		/// Retrieves the complete status of a quota, combining definition data with current usage to provide calculated metrics.
		/// </summary>
		/// <param name="tenantId">The tenant identifier for which to retrieve quota status. Must be a valid tenant ID.</param>
		/// <param name="quotaName">The unique identifier of the quota to query. Must not be <see langword="null"/> or empty.</param>
		/// <param name="scope">The scope level at which to query quota status (Tenant, Resource, or User).</param>
		/// <param name="scopeKey">An optional additional key for resource or user scoping. Required for <see cref="QuotaScope.Resource"/> and <see cref="QuotaScope.User"/> scopes, can be <see langword="null"/> for <see cref="QuotaScope.Tenant"/>.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests during the asynchronous operation.</param>
		/// <returns>
		/// A <see cref="Task{TResult}"/> that represents the asynchronous operation. The task result contains
		/// a <see cref="QuotaStatus"/> instance with current usage, limits, and calculated metrics (IsExceeded,
		/// Remaining, PercentageUsed), or <see langword="null"/> if the quota definition does not exist.
		/// </returns>
		/// <remarks>
		/// This method combines data from multiple sources to provide a comprehensive snapshot of quota state:
		/// <list type="bullet">
		/// <item><description>Quota definition data (limit, period, scope) from <see cref="GetQuotaDefinitionAsync"/></description></item>
		/// <item><description>Current usage counter from <see cref="GetCurrentUsageAsync"/></description></item>
		/// <item><description>Calculated next reset time based on the period</description></item>
		/// <item><description>Derived metrics (IsExceeded, Remaining, PercentageUsed) computed from usage and limit</description></item>
		/// </list>
		/// The returned <see cref="QuotaStatus"/> object is ideal for API responses, user interfaces,
		/// monitoring dashboards, and anywhere detailed quota information needs to be displayed or analyzed.
		/// Implementations should ensure consistent data by retrieving definition and usage information
		/// atomically or accepting eventual consistency if using distributed storage. If the quota definition
		/// does not exist, return <see langword="null"/> even if usage data exists, as status cannot be
		/// calculated without knowing the limit and period.
		/// </remarks>
		Task<QuotaStatus?> GetQuotaStatusAsync(TenantId tenantId, string quotaName, QuotaScope scope = QuotaScope.Tenant, string? scopeKey = null, CancellationToken cancellationToken = default);

		#endregion
	}
}