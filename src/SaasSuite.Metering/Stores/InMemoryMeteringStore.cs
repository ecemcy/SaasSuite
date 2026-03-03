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
using SaasSuite.Metering.Enumerations;
using SaasSuite.Metering.Interfaces;

namespace SaasSuite.Metering.Stores
{
	/// <summary>
	/// Thread-safe in-memory implementation of <see cref="IMeteringStore"/> using concurrent collections.
	/// Stores all usage events in application memory for development, testing, and single-instance scenarios
	/// where persistence is not required.
	/// </summary>
	/// <remarks>
	/// This implementation provides a lightweight, zero-configuration storage solution suitable for:
	/// <list type="bullet">
	/// <item><description>Development and local testing environments</description></item>
	/// <item><description>Integration tests that require isolated, disposable data stores</description></item>
	/// <item><description>Proof-of-concept and demonstration applications</description></item>
	/// <item><description>Single-instance deployments with low data volumes</description></item>
	/// <item><description>Scenarios where usage data retention across restarts is not needed</description></item>
	/// </list>
	/// <para>
	/// Key characteristics:
	/// <list type="bullet">
	/// <item><description>All data is lost when the application restarts - no persistence</description></item>
	/// <item><description>Thread-safe operations using <see cref="ConcurrentBag{T}"/> and <see cref="SemaphoreSlim"/></description></item>
	/// <item><description>Not suitable for multi-instance deployments - each instance has separate data</description></item>
	/// <item><description>Memory consumption grows linearly with event count - implement retention policies</description></item>
	/// <item><description>Query performance degrades as data volume increases - O(n) filtering</description></item>
	/// <item><description>Real-time aggregation computed on-the-fly from raw events</description></item>
	/// </list>
	/// </para>
	/// <para>
	/// For production multi-instance deployments, replace with persistent implementations using:
	/// <list type="bullet">
	/// <item><description>SQL databases (SQL Server, PostgreSQL) for relational querying and ACID transactions</description></item>
	/// <item><description>Time-series databases (InfluxDB, TimescaleDB) for optimized temporal operations</description></item>
	/// <item><description>NoSQL databases (MongoDB, CosmosDB) for scalability and flexible schemas</description></item>
	/// <item><description>Cloud-native services (AWS Timestream, Azure Data Explorer) for managed solutions</description></item>
	/// </list>
	/// </para>
	/// </remarks>
	public class InMemoryMeteringStore
		: IMeteringStore
	{
		#region ' Fields '

		/// <summary>
		/// Thread-safe collection storing all usage events in memory.
		/// Uses <see cref="ConcurrentBag{T}"/> for lock-free concurrent additions.
		/// </summary>
		/// <remarks>
		/// <see cref="ConcurrentBag{T}"/> is optimized for scenarios where the same thread both adds and removes items.
		/// It provides efficient thread-safe additions without locks, making it suitable for high-throughput event recording.
		/// The bag does not maintain ordering, so events are explicitly sorted during queries.
		/// </remarks>
		private readonly ConcurrentBag<UsageEvent> _events = new ConcurrentBag<UsageEvent>();

		/// <summary>
		/// Semaphore providing exclusive access during read operations to ensure consistent snapshots.
		/// Protects query operations from concurrent modifications while allowing concurrent reads.
		/// </summary>
		/// <remarks>
		/// The semaphore ensures that queries see a consistent view of the data. While <see cref="ConcurrentBag{T}"/>
		/// is thread-safe, we use a semaphore for read operations to provide snapshot isolation during aggregations
		/// where consistency across multiple events is important. The semaphore allows one concurrent reader at a time.
		/// For better read concurrency, consider using <see cref="SemaphoreSlim"/> with count > 1 or <see cref="ReaderWriterLockSlim"/>.
		/// </remarks>
		private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);

		#endregion

		#region ' Methods '

		/// <summary>
		/// Computes aggregated usage statistics by grouping events into time periods and calculating
		/// sum, average, min, max, and count. Aggregation is performed in real-time from raw events.
		/// </summary>
		/// <param name="tenantId">The unique identifier of the tenant whose usage should be aggregated.</param>
		/// <param name="metric">
		/// The metric name to aggregate, or <see langword="null"/> to aggregate all metrics separately.
		/// Each unique metric gets its own set of aggregations.
		/// </param>
		/// <param name="startTime">The inclusive start of the aggregation period.</param>
		/// <param name="endTime">The inclusive end of the aggregation period.</param>
		/// <param name="period">The granularity for grouping events (hourly, daily, or monthly).</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests during the asynchronous lock acquisition.</param>
		/// <returns>
		/// A task containing a collection of usage aggregations, one per time period and metric combination.
		/// Returns an empty collection if no events match the criteria.
		/// </returns>
		/// <remarks>
		/// This method performs real-time aggregation:
		/// <list type="number">
		/// <item><description>Acquires semaphore for consistent snapshot</description></item>
		/// <item><description>Filters events by tenant, time range, and optionally metric</description></item>
		/// <item><description>Groups events by metric and normalized period key</description></item>
		/// <item><description>For each group, computes statistical aggregates (sum, average, min, max, count)</description></item>
		/// <item><description>Calculates period boundaries for each aggregation</description></item>
		/// <item><description>Orders results chronologically by period start</description></item>
		/// <item><description>Releases semaphore in finally block</description></item>
		/// </list>
		/// <para>
		/// Statistical calculations:
		/// <list type="bullet">
		/// <item><description>TotalValue: Sum of all event values in the period</description></item>
		/// <item><description>EventCount: Number of events in the period</description></item>
		/// <item><description>AverageValue: Arithmetic mean of event values (sum / count)</description></item>
		/// <item><description>MinValue: Smallest event value in the period</description></item>
		/// <item><description>MaxValue: Largest event value in the period</description></item>
		/// </list>
		/// </para>
		/// <para>
		/// Performance characteristics:
		/// <list type="bullet">
		/// <item><description>Time complexity: O(n) for filtering + O(n log n) for grouping and aggregation</description></item>
		/// <item><description>Memory usage: Creates intermediate collections for grouping and aggregation</description></item>
		/// <item><description>Computation: All aggregation is real-time; consider pre-aggregation for large datasets</description></item>
		/// </list>
		/// </para>
		/// <para>
		/// Sparse vs. dense results:
		/// This implementation returns sparse results - only periods with actual events are included.
		/// Periods with zero events are omitted from the result set. Clients should handle gaps appropriately
		/// when visualizing time-series data.
		/// </para>
		/// </remarks>
		public async Task<IEnumerable<UsageAggregation>> GetAggregatedUsageAsync(TenantId tenantId, string? metric, DateTimeOffset startTime, DateTimeOffset endTime, AggregationPeriod period, CancellationToken cancellationToken = default)
		{
			// Acquire semaphore to ensure consistent snapshot during aggregation
			await this._lock.WaitAsync(cancellationToken);
			try
			{
				// Filter events by tenant and time range
				IEnumerable<UsageEvent> events = this._events
					.Where(e => e.TenantId == tenantId &&
							   e.Timestamp >= startTime &&
							   e.Timestamp <= endTime);

				// Apply optional metric filter if specified
				if (!string.IsNullOrEmpty(metric))
				{
					events = events.Where(e => e.Metric.Equals(metric, StringComparison.OrdinalIgnoreCase));
				}

				// Materialize to list for efficient enumeration in grouping operations
				List<UsageEvent> eventsList = events.ToList();

				// Return empty collection if no events match the filter criteria
				if (eventsList.Count == 0)
				{
					return Enumerable.Empty<UsageAggregation>();
				}

				// Group events by metric and period bucket
				// Each group represents all events for a specific metric in a specific time period
				var grouped = eventsList
					.GroupBy(e => new
					{
						e.Metric,                                    // Group by metric name
						PeriodKey = GetPeriodKey(e.Timestamp, period) // Group by normalized period start
					});

				// Transform each group into an aggregation with statistical summaries
				List<UsageAggregation> aggregations = grouped.Select(g =>
				{
					// Calculate the exact period boundaries for this group
					(DateTimeOffset periodStart, DateTimeOffset periodEnd) = GetPeriodBounds(g.Key.PeriodKey, period);

					// Extract all values for statistical calculations
					List<decimal> values = g.Select(e => e.Value).ToList();

					// Create aggregation with computed statistics
					return new UsageAggregation
					{
						TenantId = tenantId,
						Metric = g.Key.Metric,
						PeriodStart = periodStart,
						PeriodEnd = periodEnd,
						TotalValue = values.Sum(), // ?? values
						EventCount = values.Count, // n
						AverageValue = values.Average(), // ?? values / n
						MinValue = values.Min(), // min(values)
						MaxValue = values.Max() // max(values)
					};
				})
				.OrderBy(a => a.PeriodStart) // Sort chronologically for time-series analysis
				.ToList();

				return aggregations;
			}
			finally
			{
				// Always release the semaphore, even if an exception occurs
				_ = this._lock.Release();
			}
		}

		/// <summary>
		/// Retrieves usage events for a specific tenant within a time range, optionally filtered by metric.
		/// Acquires a read lock to ensure a consistent snapshot of events during query execution.
		/// </summary>
		/// <param name="tenantId">The unique identifier of the tenant whose usage events should be retrieved.</param>
		/// <param name="metric">
		/// The metric name to filter by, or <see langword="null"/> to retrieve all metrics.
		/// Metric comparison is case-insensitive.
		/// </param>
		/// <param name="startTime">The inclusive start of the time range.</param>
		/// <param name="endTime">The inclusive end of the time range.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests during the asynchronous lock acquisition.</param>
		/// <returns>
		/// A task containing an ordered collection of usage events matching the filter criteria.
		/// Events are sorted chronologically by timestamp ascending.
		/// </returns>
		/// <remarks>
		/// This method filters the in-memory event collection using LINQ:
		/// <list type="number">
		/// <item><description>Acquires semaphore for consistent read snapshot</description></item>
		/// <item><description>Filters by tenant ID (exact match)</description></item>
		/// <item><description>Filters by time range (inclusive boundaries)</description></item>
		/// <item><description>Optionally filters by metric name (case-insensitive)</description></item>
		/// <item><description>Orders results by timestamp ascending</description></item>
		/// <item><description>Materializes to list to release lock quickly</description></item>
		/// <item><description>Releases semaphore in finally block</description></item>
		/// </list>
		/// <para>
		/// Performance characteristics:
		/// <list type="bullet">
		/// <item><description>Time complexity: O(n) where n is total event count - scans entire collection</description></item>
		/// <item><description>Memory usage: Creates a new list containing filtered events</description></item>
		/// <item><description>Concurrency: Blocks concurrent readers and writers during query execution</description></item>
		/// </list>
		/// For large datasets, consider implementing pagination or limiting the result set size.
		/// </para>
		/// </remarks>
		public async Task<IEnumerable<UsageEvent>> GetUsageAsync(TenantId tenantId, string? metric, DateTimeOffset startTime, DateTimeOffset endTime, CancellationToken cancellationToken = default)
		{
			// Acquire semaphore to ensure consistent snapshot during read
			await this._lock.WaitAsync(cancellationToken);
			try
			{
				// Start with base query filtering by tenant and time range
				IEnumerable<UsageEvent> query = this._events
					.Where(e => e.TenantId == tenantId && // Exact tenant match
								e.Timestamp >= startTime && // Inclusive start
								e.Timestamp <= endTime); // Inclusive end

				// Apply optional metric filter if specified
				if (!string.IsNullOrEmpty(metric))
				{
					// Case-insensitive metric comparison for user convenience
					query = query.Where(e => e.Metric.Equals(metric, StringComparison.OrdinalIgnoreCase));
				}

				// Order by timestamp for chronological analysis and materialize to list
				// Materialization releases the enumerable allowing the lock to be released quickly
				return query.OrderBy(e => e.Timestamp).ToList();
			}
			finally
			{
				// Always release the semaphore, even if an exception occurs
				_ = this._lock.Release();
			}
		}

		/// <summary>
		/// Records a usage event by adding it to the in-memory collection.
		/// This operation is thread-safe and does not block concurrent writes.
		/// </summary>
		/// <param name="usageEvent">The usage event to record. Must not be <see langword="null"/>.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests. Not used in this implementation.</param>
		/// <returns>A completed task indicating the event has been added to the collection.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="usageEvent"/> is <see langword="null"/>.</exception>
		/// <remarks>
		/// This implementation uses <see cref="ConcurrentBag{T}.Add"/> which is lock-free and thread-safe.
		/// Events are added immediately without validation beyond the null check. The operation completes
		/// synchronously but returns a <see cref="Task"/> for interface compliance.
		/// <para>
		/// Note: This implementation does not enforce idempotency - the same event can be added multiple times
		/// if submitted with different instances. Production implementations should check <see cref="UsageEvent.Id"/>
		/// to prevent duplicates.
		/// </para>
		/// </remarks>
		public Task RecordUsageAsync(UsageEvent usageEvent, CancellationToken cancellationToken = default)
		{
			// Validate that the usage event is not null
			ArgumentNullException.ThrowIfNull(usageEvent);

			// Add the event to the thread-safe concurrent bag
			// ConcurrentBag.Add is lock-free and optimized for high-throughput concurrent additions
			this._events.Add(usageEvent);

			// Return completed task since the operation is synchronous
			return Task.CompletedTask;
		}

		#endregion

		#region ' Static Methods '

		/// <summary>
		/// Calculates the inclusive start and end boundaries for a given period key.
		/// Returns a closed interval [start, end] that fully encompasses the period.
		/// </summary>
		/// <param name="periodKey">The normalized period start timestamp from <see cref="GetPeriodKey"/>.</param>
		/// <param name="period">The aggregation period determining the period duration.</param>
		/// <returns>
		/// A tuple containing the period start (inclusive) and period end (inclusive) timestamps.
		/// </returns>
		/// <remarks>
		/// The end timestamp is calculated as one tick before the start of the next period:
		/// <list type="bullet">
		/// <item><description>Hourly: End is the last tick of the hour (59:59.9999999)</description></item>
		/// <item><description>Daily: End is the last tick of the day (23:59:59.9999999)</description></item>
		/// <item><description>Monthly: End is the last tick of the last day of the month (varies: 28-31 days)</description></item>
		/// </list>
		/// <para>
		/// Using inclusive boundaries ensures that events exactly at period boundaries are counted correctly.
		/// The AddTicks(-1) operation ensures contiguous, non-overlapping periods.
		/// </para>
		/// </remarks>
		private static (DateTimeOffset Start, DateTimeOffset End) GetPeriodBounds(DateTimeOffset periodKey, AggregationPeriod period)
		{
			// Period start is the normalized period key
			DateTimeOffset start = periodKey;

			// Calculate the end as one tick before the next period starts
			DateTimeOffset end = period switch
			{
				// Add 1 hour and subtract 1 tick: 14:00:00 ??? 14:59:59.9999999
				AggregationPeriod.Hourly => start.AddHours(1).AddTicks(-1),

				// Add 1 day and subtract 1 tick: midnight ??? 23:59:59.9999999
				AggregationPeriod.Daily => start.AddDays(1).AddTicks(-1),

				// Add 1 month and subtract 1 tick: handles variable month lengths automatically
				AggregationPeriod.Monthly => start.AddMonths(1).AddTicks(-1),

				// For unknown periods, end equals start
				_ => start
			};

			return (start, end);
		}

		/// <summary>
		/// Calculates the period bucket key for a given timestamp based on the aggregation period.
		/// This normalizes timestamps to the start of their respective period boundaries for grouping.
		/// </summary>
		/// <param name="timestamp">The timestamp to normalize to a period boundary.</param>
		/// <param name="period">The aggregation period determining the bucket size.</param>
		/// <returns>
		/// A <see cref="DateTimeOffset"/> representing the start of the period containing the timestamp.
		/// </returns>
		/// <remarks>
		/// This method truncates timestamps to period boundaries:
		/// <list type="bullet">
		/// <item><description>Hourly: Truncates to the start of the hour (minute 0, second 0, millisecond 0)</description></item>
		/// <item><description>Daily: Truncates to midnight (hour 0, minute 0, second 0, millisecond 0)</description></item>
		/// <item><description>Monthly: Truncates to midnight of the first day of the month</description></item>
		/// </list>
		/// <para>
		/// Time zone offset is preserved from the original timestamp to maintain regional accuracy.
		/// This ensures that events are grouped according to their local time zone, which is important
		/// for billing periods that may span daylight saving time transitions.
		/// </para>
		/// </remarks>
		private static DateTimeOffset GetPeriodKey(DateTimeOffset timestamp, AggregationPeriod period)
		{
			return period switch
			{
				// Truncate to the start of the hour (e.g., 2024-01-15 14:45:23 ??? 2024-01-15 14:00:00)
				AggregationPeriod.Hourly => new DateTimeOffset(timestamp.Year, timestamp.Month, timestamp.Day, timestamp.Hour, 0, 0, timestamp.Offset),

				// Truncate to midnight of the day (e.g., 2024-01-15 14:45:23 ??? 2024-01-15 00:00:00)
				AggregationPeriod.Daily => new DateTimeOffset(timestamp.Year, timestamp.Month, timestamp.Day, 0, 0, 0, timestamp.Offset),

				// Truncate to midnight of the first day of the month (e.g., 2024-01-15 14:45:23 ??? 2024-01-01 00:00:00)
				AggregationPeriod.Monthly => new DateTimeOffset(timestamp.Year, timestamp.Month, 1, 0, 0, 0, timestamp.Offset),

				// For unknown periods, return the original timestamp unchanged
				_ => timestamp
			};
		}

		#endregion
	}
}