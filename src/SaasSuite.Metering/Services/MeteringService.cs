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
using SaasSuite.Metering.Enumerations;
using SaasSuite.Metering.Interfaces;
using SaasSuite.Metering.Options;

namespace SaasSuite.Metering.Services
{
	/// <summary>
	/// Primary service facade for recording and querying usage data in multi-tenant SaaS applications.
	/// Provides high-level methods for usage tracking, validation, and retrieval with built-in metric validation
	/// and convenient time period helpers.
	/// </summary>
	/// <remarks>
	/// This service acts as the main entry point for all metering operations, providing:
	/// <list type="bullet">
	/// <item><description>Simplified API for recording usage with automatic event construction</description></item>
	/// <item><description>Metric validation when enabled via <see cref="MeteringOptions.ValidateMetrics"/></description></item>
	/// <item><description>Convenient methods for common time periods (current month)</description></item>
	/// <item><description>Abstraction over the underlying <see cref="IMeteringStore"/> implementation</description></item>
	/// <item><description>Centralized configuration management via <see cref="MeteringOptions"/></description></item>
	/// </list>
	/// <para>
	/// The service is registered as scoped in the DI container, allowing it to be injected into controllers,
	/// background services, and other application components that need to track or query usage data.
	/// </para>
	/// <para>
	/// Typical usage patterns:
	/// <list type="bullet">
	/// <item><description>Record usage after operations complete (API calls, data processing, storage changes)</description></item>
	/// <item><description>Query usage for billing calculations and invoice generation</description></item>
	/// <item><description>Generate usage reports and analytics dashboards</description></item>
	/// <item><description>Monitor tenant consumption against quotas and limits</description></item>
	/// <item><description>Provide usage transparency to end users</description></item>
	/// </list>
	/// </para>
	/// </remarks>
	public class MeteringService
	{
		#region ' Fields '

		/// <summary>
		/// The underlying storage implementation for persisting and retrieving usage data.
		/// Abstracts the specific storage technology (database, time-series store, etc.).
		/// </summary>
		private readonly IMeteringStore _store;

		/// <summary>
		/// Configuration options controlling metering behavior including validation rules and retention policies.
		/// </summary>
		private readonly MeteringOptions _options;

		#endregion

		#region ' Constructors '

		/// <summary>
		/// Initializes a new instance of the <see cref="MeteringService"/> class with the specified store and options.
		/// </summary>
		/// <param name="store">
		/// The metering store implementation for data persistence. Cannot be <see langword="null"/>.
		/// Typically injected from DI container based on registration in <c>AddSaasMetering</c>.
		/// </param>
		/// <param name="options">
		/// The configured metering options wrapped in <see cref="IOptions{T}"/>. Cannot be <see langword="null"/>.
		/// The options pattern allows configuration from various sources (appsettings.json, environment variables, etc.).
		/// </param>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="store"/> is <see langword="null"/> or when <paramref name="options"/> is <see langword="null"/>.
		/// </exception>
		public MeteringService(IMeteringStore store, IOptions<MeteringOptions> options)
		{
			// Validate required dependencies are provided
			this._store = store ?? throw new ArgumentNullException(nameof(store));
			this._options = options?.Value ?? throw new ArgumentNullException(nameof(options));
		}

		#endregion

		#region ' Methods '

		/// <summary>
		/// Records a usage event for a specific tenant and metric by constructing a <see cref="UsageEvent"/>
		/// from the provided parameters. This is the primary method for recording usage in application code.
		/// </summary>
		/// <param name="tenantId">
		/// The unique identifier of the tenant generating the usage. Cannot be <see langword="null"/>.
		/// Must correspond to a valid tenant in the system for proper billing attribution.
		/// </param>
		/// <param name="metric">
		/// The name of the metric being tracked (e.g., "api-calls", "storage-gb"). Must not be <see langword="null"/> or whitespace.
		/// If <see cref="MeteringOptions.ValidateMetrics"/> is enabled, must exist in <see cref="MeteringOptions.ValidMetrics"/>.
		/// </param>
		/// <param name="value">
		/// The numeric value representing the quantity of usage. Can be positive (consumption) or negative (credits/corrections).
		/// The interpretation depends on the metric type and billing model.
		/// </param>
		/// <param name="metadata">
		/// Optional additional context about the usage event as key-value pairs. Can be <see langword="null"/>.
		/// Use to capture details like user ID, API endpoint, region, or any other relevant dimensions.
		/// </param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests during the asynchronous operation.</param>
		/// <returns>A task that represents the asynchronous operation. The task completes when the event is recorded.</returns>
		/// <exception cref="ArgumentException">
		/// Thrown when <paramref name="metric"/> is <see langword="null"/> or whitespace, or when metric validation
		/// is enabled and the metric is not in the valid metrics list.
		/// </exception>
		/// <remarks>
		/// This convenience method:
		/// <list type="number">
		/// <item><description>Validates the metric name is not null or whitespace</description></item>
		/// <item><description>Optionally validates against the allowed metrics list if validation is enabled</description></item>
		/// <item><description>Constructs a new <see cref="UsageEvent"/> with auto-generated ID and current timestamp</description></item>
		/// <item><description>Delegates to the store for persistence</description></item>
		/// </list>
		/// <para>
		/// The timestamp is automatically set to the current UTC time. For backdated events or corrections,
		/// use the <see cref="RecordUsageAsync(UsageEvent, CancellationToken)"/> overload with a pre-constructed event.
		/// </para>
		/// <para>
		/// Best practices:
		/// <list type="bullet">
		/// <item><description>Record usage as close to the actual consumption as possible for accuracy</description></item>
		/// <item><description>Use consistent metric naming conventions across the application</description></item>
		/// <item><description>Include relevant metadata for detailed analysis and troubleshooting</description></item>
		/// <item><description>Handle exceptions appropriately - failed metering should not break core functionality</description></item>
		/// <item><description>Consider async fire-and-forget patterns for non-critical metering to avoid blocking</description></item>
		/// </list>
		/// </para>
		/// </remarks>
		public async Task RecordUsageAsync(TenantId tenantId, string metric, decimal value, Dictionary<string, string>? metadata = null, CancellationToken cancellationToken = default)
		{
			// Validate that metric name is provided and not empty
			if (string.IsNullOrWhiteSpace(metric))
			{
				throw new ArgumentException("Metric cannot be null or whitespace.", nameof(metric));
			}

			// If metric validation is enabled, check against the allowed metrics list
			if (this._options.ValidateMetrics && !this._options.ValidMetrics.Contains(metric))
			{
				throw new ArgumentException($"Metric '{metric}' is not in the list of valid metrics.", nameof(metric));
			}

			// Construct a new usage event with auto-generated ID and current timestamp
			UsageEvent usageEvent = new UsageEvent
			{
				TenantId = tenantId, // Associate with the specified tenant
				Metric = metric, // Set the metric name
				Value = value, // Set the usage quantity
				Metadata = metadata // Attach optional metadata (can be null)
									// Id and Timestamp are auto-initialized in UsageEvent constructor
			};

			// Delegate to the store for persistence
			await this._store.RecordUsageAsync(usageEvent, cancellationToken);
		}

		/// <summary>
		/// Records a pre-constructed usage event directly. This overload provides full control over all event properties
		/// including timestamp and ID, useful for backdating events, corrections, or batch imports.
		/// </summary>
		/// <param name="usageEvent">
		/// The fully constructed usage event to record. Cannot be <see langword="null"/>.
		/// All required properties (TenantId, Metric, Value, Timestamp) should be populated.
		/// </param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests during the asynchronous operation.</param>
		/// <returns>A task that represents the asynchronous operation. The task completes when the event is recorded.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="usageEvent"/> is <see langword="null"/>.</exception>
		/// <exception cref="ArgumentException">
		/// Thrown when metric validation is enabled and the event's metric is not in the valid metrics list.
		/// </exception>
		/// <remarks>
		/// This method validates the event's metric if validation is enabled, then delegates to the store.
		/// Use this overload when you need to:
		/// <list type="bullet">
		/// <item><description>Set a specific timestamp (backdating, corrections, imports)</description></item>
		/// <item><description>Control the event ID for idempotency or external correlation</description></item>
		/// <item><description>Construct events outside the service with custom logic</description></item>
		/// <item><description>Replay or migrate events from another system</description></item>
		/// </list>
		/// <para>
		/// Validation performed:
		/// <list type="bullet">
		/// <item><description>Null check on the entire event object</description></item>
		/// <item><description>Metric validation if enabled (same as the other overload)</description></item>
		/// <item><description>No validation of timestamp, value, or other properties (assumed valid)</description></item>
		/// </list>
		/// </para>
		/// <para>
		/// The store implementation may perform additional validation like checking for duplicate IDs
		/// or enforcing schema constraints.
		/// </para>
		/// </remarks>
		public async Task RecordUsageAsync(UsageEvent usageEvent, CancellationToken cancellationToken = default)
		{
			// Validate that the usage event is not null
			ArgumentNullException.ThrowIfNull(usageEvent);

			// If metric validation is enabled, check the event's metric against the allowed list
			if (this._options.ValidateMetrics && !this._options.ValidMetrics.Contains(usageEvent.Metric))
			{
				throw new ArgumentException($"Metric '{usageEvent.Metric}' is not in the list of valid metrics.", nameof(usageEvent));
			}

			// Delegate to the store for persistence
			await this._store.RecordUsageAsync(usageEvent, cancellationToken);
		}

		/// <summary>
		/// Retrieves aggregated usage statistics for a specific tenant over a time period with the specified granularity.
		/// Returns pre-computed or dynamically calculated summaries for efficient reporting and billing.
		/// </summary>
		/// <param name="tenantId">
		/// The unique identifier of the tenant whose usage should be aggregated. Cannot be <see langword="null"/>.
		/// </param>
		/// <param name="startTime">
		/// The inclusive start of the aggregation period. Defines the earliest timestamp to include.
		/// </param>
		/// <param name="endTime">
		/// The inclusive end of the aggregation period. Defines the latest timestamp to include.
		/// Should be greater than or equal to <paramref name="startTime"/>.
		/// </param>
		/// <param name="period">
		/// The granularity for grouping events into time buckets (hourly, daily, or monthly).
		/// Determines how events are aggregated and the number of result records.
		/// </param>
		/// <param name="metric">
		/// Optional metric name to aggregate. If <see langword="null"/>, aggregates all metrics separately.
		/// If specified, only aggregates the specified metric.
		/// </param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests during the asynchronous operation.</param>
		/// <returns>
		/// A task containing a collection of usage aggregations with statistical summaries (sum, average, min, max, count)
		/// grouped by time period and metric. Returns an empty collection if no events exist for the criteria.
		/// </returns>
		/// <remarks>
		/// This method is the primary interface for:
		/// <list type="bullet">
		/// <item><description>Usage-based billing calculations</description></item>
		/// <item><description>Usage reports and analytics dashboards</description></item>
		/// <item><description>Trend analysis and forecasting</description></item>
		/// <item><description>Quota monitoring and capacity planning</description></item>
		/// <item><description>Performance metrics and SLA monitoring</description></item>
		/// </list>
		/// <para>
		/// This is a thin wrapper over the store's GetAggregatedUsageAsync method, providing a consistent
		/// parameter order and delegating aggregation logic to the store implementation.
		/// </para>
		/// <para>
		/// Each aggregation includes:
		/// <list type="bullet">
		/// <item><description>Period boundaries (start and end timestamps)</description></item>
		/// <item><description>Total value (sum of all event values)</description></item>
		/// <item><description>Average value (mean of event values)</description></item>
		/// <item><description>Minimum and maximum values</description></item>
		/// <item><description>Event count (number of events in the period)</description></item>
		/// </list>
		/// </para>
		/// </remarks>
		public Task<IEnumerable<UsageAggregation>> GetAggregatedUsageAsync(TenantId tenantId, DateTimeOffset startTime, DateTimeOffset endTime, AggregationPeriod period, string? metric = null, CancellationToken cancellationToken = default)
		{
			// Delegate to the store with parameters reordered to match the store interface
			return this._store.GetAggregatedUsageAsync(tenantId, metric, startTime, endTime, period, cancellationToken);
		}

		/// <summary>
		/// Convenience method to retrieve aggregated usage for the current calendar month for a specific tenant.
		/// Automatically calculates the month boundaries based on the current UTC date and aggregates at the specified granularity.
		/// </summary>
		/// <param name="tenantId">
		/// The unique identifier of the tenant whose current month usage should be aggregated. Cannot be <see langword="null"/>.
		/// </param>
		/// <param name="period">
		/// The granularity for aggregation within the current month (hourly, daily, or monthly).
		/// Hourly provides up to 744 data points, daily provides up to 31 points, monthly provides 1 point.
		/// </param>
		/// <param name="metric">
		/// Optional metric name to aggregate. If <see langword="null"/>, aggregates all metrics separately.
		/// </param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests during the asynchronous operation.</param>
		/// <returns>
		/// A task containing a collection of usage aggregations for the current month with the specified granularity.
		/// Returns an empty collection if no events exist.
		/// </returns>
		/// <remarks>
		/// This helper method is particularly useful for:
		/// <list type="bullet">
		/// <item><description>Month-to-date usage dashboards showing trends within the current billing cycle</description></item>
		/// <item><description>Current month billing previews with daily or hourly breakdowns</description></item>
		/// <item><description>Real-time usage monitoring against monthly quotas</description></item>
		/// <item><description>Intra-month trend analysis and anomaly detection</description></item>
		/// </list>
		/// <para>
		/// Month boundaries are calculated identically to <see cref="GetCurrentMonthUsageAsync"/>:
		/// <list type="bullet">
		/// <item><description>Start: First day of current month at midnight UTC</description></item>
		/// <item><description>End: Last tick of the last day of current month</description></item>
		/// </list>
		/// </para>
		/// <para>
		/// Period interpretation examples for partial months:
		/// <list type="bullet">
		/// <item><description>If called on Jan 15, hourly aggregation returns data from Jan 1 00:00 through Jan 31 23:59</description></item>
		/// <item><description>Future periods (Jan 16-31) may have no data if events haven't occurred yet</description></item>
		/// <item><description>Daily aggregation shows complete days plus partial current day</description></item>
		/// <item><description>Monthly aggregation returns single record for entire current month</description></item>
		/// </list>
		/// </para>
		/// <para>
		/// Typical use case - current month billing preview:
		/// <code>
		/// var aggregations = await meteringService.GetCurrentMonthAggregatedUsageAsync(
		///     tenantId,
		///     AggregationPeriod.Daily,
		///     "api-calls");
		///
		/// decimal totalCalls = aggregations.Sum(a => a.TotalValue);
		/// decimal estimatedCost = totalCalls * pricePerCall;
		/// </code>
		/// </para>
		/// </remarks>
		public Task<IEnumerable<UsageAggregation>> GetCurrentMonthAggregatedUsageAsync(TenantId tenantId, AggregationPeriod period, string? metric = null, CancellationToken cancellationToken = default)
		{
			// Get current UTC time
			DateTimeOffset now = DateTimeOffset.UtcNow;

			// Calculate start of current month: first day at midnight UTC
			DateTimeOffset startOfMonth = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero);

			// Calculate end of current month: one tick before start of next month
			DateTimeOffset endOfMonth = startOfMonth.AddMonths(1).AddTicks(-1);

			// Delegate to the standard GetAggregatedUsageAsync method with calculated boundaries
			return this.GetAggregatedUsageAsync(tenantId, startOfMonth, endOfMonth, period, metric, cancellationToken);
		}

		/// <summary>
		/// Convenience method to retrieve usage events for the current calendar month for a specific tenant.
		/// Automatically calculates the month boundaries based on the current UTC date.
		/// </summary>
		/// <param name="tenantId">
		/// The unique identifier of the tenant whose current month usage should be retrieved. Cannot be <see langword="null"/>.
		/// </param>
		/// <param name="metric">
		/// Optional metric name to filter by. If <see langword="null"/>, returns events for all metrics.
		/// </param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests during the asynchronous operation.</param>
		/// <returns>
		/// A task containing a collection of usage events from the first day of the current month (midnight UTC)
		/// to the last moment of the current month. Returns an empty collection if no events exist.
		/// </returns>
		/// <remarks>
		/// This helper method is useful for:
		/// <list type="bullet">
		/// <item><description>Month-to-date usage displays in user interfaces</description></item>
		/// <item><description>Current billing cycle usage queries</description></item>
		/// <item><description>Quota tracking against monthly limits</description></item>
		/// <item><description>Real-time billing previews</description></item>
		/// </list>
		/// <para>
		/// Month boundaries are calculated as:
		/// <list type="bullet">
		/// <item><description>Start: First day of current month at midnight UTC (YYYY-MM-01 00:00:00 +00:00)</description></item>
		/// <item><description>End: Last tick of the last day of current month (YYYY-MM-DD 23:59:59.9999999 +00:00)</description></item>
		/// </list>
		/// The end boundary is calculated as start of next month minus one tick, automatically handling
		/// variable month lengths (28-31 days).
		/// </para>
		/// <para>
		/// Time zone considerations:
		/// <list type="bullet">
		/// <item><description>Uses UTC for calculation - may not align with tenant's local billing period</description></item>
		/// <item><description>For local time zone billing, consider using custom start/end times with tenant's offset</description></item>
		/// <item><description>Daylight saving transitions may affect period interpretation</description></item>
		/// </list>
		/// </para>
		/// </remarks>
		public Task<IEnumerable<UsageEvent>> GetCurrentMonthUsageAsync(TenantId tenantId, string? metric = null, CancellationToken cancellationToken = default)
		{
			// Get current UTC time
			DateTimeOffset now = DateTimeOffset.UtcNow;

			// Calculate start of current month: first day at midnight UTC
			DateTimeOffset startOfMonth = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero);

			// Calculate end of current month: one tick before start of next month
			// This automatically handles variable month lengths (28, 29, 30, 31 days)
			DateTimeOffset endOfMonth = startOfMonth.AddMonths(1).AddTicks(-1);

			// Delegate to the standard GetUsageAsync method with calculated boundaries
			return this.GetUsageAsync(tenantId, startOfMonth, endOfMonth, metric, cancellationToken);
		}

		/// <summary>
		/// Retrieves granular usage events for a specific tenant within a time range, optionally filtered by metric.
		/// Returns the raw event data suitable for detailed analysis, auditing, and compliance.
		/// </summary>
		/// <param name="tenantId">
		/// The unique identifier of the tenant whose usage events should be retrieved. Cannot be <see langword="null"/>.
		/// </param>
		/// <param name="startTime">
		/// The inclusive start of the time range. Events with timestamps greater than or equal to this value are included.
		/// </param>
		/// <param name="endTime">
		/// The inclusive end of the time range. Events with timestamps less than or equal to this value are included.
		/// Should be greater than or equal to <paramref name="startTime"/>.
		/// </param>
		/// <param name="metric">
		/// Optional metric name to filter by. If <see langword="null"/>, returns events for all metrics.
		/// If specified, only events matching this metric are returned.
		/// </param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests during the asynchronous operation.</param>
		/// <returns>
		/// A task containing a collection of usage events matching the criteria, ordered chronologically by timestamp.
		/// Returns an empty collection if no events match.
		/// </returns>
		/// <remarks>
		/// This method provides direct access to raw usage events for:
		/// <list type="bullet">
		/// <item><description>Detailed usage audits and compliance reporting</description></item>
		/// <item><description>Billing dispute resolution with event-level transparency</description></item>
		/// <item><description>Debugging and troubleshooting usage patterns</description></item>
		/// <item><description>Custom analytics requiring event-level granularity</description></item>
		/// <item><description>Export to external systems or data warehouses</description></item>
		/// </list>
		/// <para>
		/// This is a thin wrapper over the store's GetUsageAsync method, providing a consistent
		/// parameter order and delegating all logic to the store implementation.
		/// </para>
		/// <para>
		/// Performance considerations:
		/// <list type="bullet">
		/// <item><description>Large time ranges may return millions of events</description></item>
		/// <item><description>Consider pagination for large result sets</description></item>
		/// <item><description>Use aggregated queries when event-level detail is not needed</description></item>
		/// </list>
		/// </para>
		/// </remarks>
		public Task<IEnumerable<UsageEvent>> GetUsageAsync(TenantId tenantId, DateTimeOffset startTime, DateTimeOffset endTime, string? metric = null, CancellationToken cancellationToken = default)
		{
			// Delegate to the store with parameters reordered to match the store interface
			return this._store.GetUsageAsync(tenantId, metric, startTime, endTime, cancellationToken);
		}

		#endregion
	}
}