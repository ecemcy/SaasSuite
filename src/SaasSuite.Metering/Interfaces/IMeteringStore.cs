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
using SaasSuite.Metering.Enumerations;
using SaasSuite.Metering.Options;
using SaasSuite.Metering.Stores;

namespace SaasSuite.Metering.Interfaces
{
	/// <summary>
	/// Defines the contract for persisting and retrieving usage metering data in multi-tenant environments.
	/// Implementations provide the storage backend for recording granular usage events and computing
	/// aggregated statistics for billing, reporting, and analytics purposes.
	/// </summary>
	/// <remarks>
	/// This interface abstracts the underlying storage mechanism, enabling different implementations such as:
	/// <list type="bullet">
	/// <item><description>In-memory storage using concurrent collections (<see cref="InMemoryMeteringStore"/>) for testing and development</description></item>
	/// <item><description>Relational databases (SQL Server, PostgreSQL, MySQL) for ACID compliance and complex queries</description></item>
	/// <item><description>Time-series databases (InfluxDB, TimescaleDB, Prometheus) optimized for temporal data</description></item>
	/// <item><description>NoSQL databases (MongoDB, DynamoDB, CosmosDB) for scalability and flexible schemas</description></item>
	/// <item><description>Cloud-native services (AWS Timestream, Azure Data Explorer) for managed solutions</description></item>
	/// <item><description>Data lakes or warehouses (Snowflake, BigQuery) for large-scale analytics</description></item>
	/// </list>
	/// <para>
	/// Key design considerations for implementations:
	/// <list type="bullet">
	/// <item><description>Write performance - Metering generates high-volume writes that should not block application operations</description></item>
	/// <item><description>Query efficiency - Aggregations over large time ranges must perform acceptably for reporting</description></item>
	/// <item><description>Data retention - Implement archiving or purging strategies per <see cref="MeteringOptions.RetentionPeriod"/></description></item>
	/// <item><description>Data integrity - Ensure event immutability and prevent duplicate recording</description></item>
	/// <item><description>Multi-tenancy isolation - Prevent cross-tenant data access or leakage</description></item>
	/// <item><description>Time zone handling - Store timestamps with offset information and handle DST transitions</description></item>
	/// <item><description>Scalability - Support horizontal scaling for growing data volumes and tenant counts</description></item>
	/// </list>
	/// </para>
	/// </remarks>
	public interface IMeteringStore
	{
		#region ' Methods '

		/// <summary>
		/// Records a usage event asynchronously, persisting it to the underlying storage system.
		/// This method should be idempotent to handle retry scenarios in distributed systems.
		/// </summary>
		/// <param name="usageEvent">
		/// The usage event to record, containing tenant ID, metric name, value, timestamp, and optional metadata.
		/// Must not be <see langword="null"/> and should have a unique <see cref="UsageEvent.Id"/> for idempotency.
		/// </param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests during the asynchronous operation.</param>
		/// <returns>A task that represents the asynchronous recording operation. The task completes when the event is persisted.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="usageEvent"/> is <see langword="null"/>.</exception>
		/// <remarks>
		/// This method is the primary entry point for all usage data ingestion. Implementations should:
		/// <list type="bullet">
		/// <item><description>Validate required fields (TenantId, Metric, Timestamp) before persisting</description></item>
		/// <item><description>Ensure atomicity - Events are either fully written or not written at all</description></item>
		/// <item><description>Handle concurrent writes safely when the same event ID is submitted multiple times</description></item>
		/// <item><description>Use efficient bulk insertion for high-throughput scenarios</description></item>
		/// <item><description>Consider async/fire-and-forget patterns if blocking is unacceptable</description></item>
		/// <item><description>Log failures for monitoring and debugging purposes</description></item>
		/// </list>
		/// <para>
		/// Performance optimization strategies:
		/// <list type="bullet">
		/// <item><description>Batch multiple events into single database transactions</description></item>
		/// <item><description>Use message queues or event buses for asynchronous processing</description></item>
		/// <item><description>Implement write-behind caching to reduce latency</description></item>
		/// <item><description>Partition data by tenant ID or time period for write scalability</description></item>
		/// </list>
		/// </para>
		/// <para>
		/// Idempotency considerations:
		/// <list type="bullet">
		/// <item><description>Use <see cref="UsageEvent.Id"/> as unique constraint to prevent duplicates</description></item>
		/// <item><description>Check for existing events with the same ID before inserting</description></item>
		/// <item><description>Return successfully if duplicate is detected (idempotent behavior)</description></item>
		/// </list>
		/// </para>
		/// </remarks>
		Task RecordUsageAsync(UsageEvent usageEvent, CancellationToken cancellationToken = default);

		/// <summary>
		/// Retrieves pre-computed or dynamically calculated aggregated usage statistics for a specific tenant,
		/// grouped by time periods and optionally filtered by metric. This provides efficient access to summary
		/// data without processing individual events.
		/// </summary>
		/// <param name="tenantId">The unique identifier of the tenant whose usage should be aggregated. Cannot be <see langword="null"/>.</param>
		/// <param name="metric">
		/// The metric name to aggregate, or <see langword="null"/> to aggregate all metrics separately.
		/// When <see langword="null"/>, returns separate aggregations for each metric found within the time range.
		/// When specified, only aggregates the specified metric.
		/// </param>
		/// <param name="startTime">
		/// The inclusive start of the aggregation period. Defines the earliest timestamp to include in aggregations.
		/// Should align with period boundaries for optimal performance in pre-aggregated implementations.
		/// </param>
		/// <param name="endTime">
		/// The inclusive end of the aggregation period. Defines the latest timestamp to include in aggregations.
		/// Should be greater than or equal to <paramref name="startTime"/>.
		/// </param>
		/// <param name="period">
		/// The granularity for grouping events into time buckets. Determines how events are aggregated:
		/// <see cref="AggregationPeriod.Hourly"/> groups by hour, <see cref="AggregationPeriod.Daily"/> by day,
		/// <see cref="AggregationPeriod.Monthly"/> by calendar month.
		/// </param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests during the asynchronous operation.</param>
		/// <returns>
		/// A task that represents the asynchronous operation. The task result contains a collection of
		/// <see cref="UsageAggregation"/> instances, each representing statistics for a specific metric and time period.
		/// Returns an empty collection if no events exist for the specified criteria.
		/// </returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="tenantId"/> is <see langword="null"/>.</exception>
		/// <remarks>
		/// This method is the primary interface for usage reporting, billing calculations, and analytics dashboards.
		/// It provides statistical summaries including totals, averages, minimums, maximums, and event counts.
		/// <para>
		/// Aggregation strategies:
		/// <list type="bullet">
		/// <item><description>Real-time aggregation - Compute statistics on-the-fly from raw events (slower but always current)</description></item>
		/// <item><description>Pre-aggregation - Store pre-computed summaries in separate tables (faster queries but requires background jobs)</description></item>
		/// <item><description>Hybrid - Pre-aggregate historical data, real-time aggregate recent data</description></item>
		/// <item><description>Materialized views - Database-managed pre-computation with automatic refresh</description></item>
		/// </list>
		/// </para>
		/// <para>
		/// Time period calculations:
		/// <list type="bullet">
		/// <item><description>Hourly: Group events by hour, from minute 0 to minute 59</description></item>
		/// <item><description>Daily: Group events by day from midnight to 23:59:59</description></item>
		/// <item><description>Monthly: Group events by calendar month, handling variable month lengths (28-31 days)</description></item>
		/// </list>
		/// Each aggregation includes <see cref="UsageAggregation.PeriodStart"/> and <see cref="UsageAggregation.PeriodEnd"/>
		/// to precisely define the time bucket boundaries.
		/// </para>
		/// <para>
		/// Result ordering:
		/// <list type="bullet">
		/// <item><description>Results should be ordered by <see cref="UsageAggregation.PeriodStart"/> ascending for chronological analysis</description></item>
		/// <item><description>When multiple metrics are returned (metric parameter is null), group by period then metric</description></item>
		/// </list>
		/// </para>
		/// <para>
		/// Performance optimization:
		/// <list type="bullet">
		/// <item><description>Create indexes on (TenantId, Metric, Timestamp) for efficient filtering and grouping</description></item>
		/// <item><description>Use database-native aggregation functions (SUM, AVG, MIN, MAX, COUNT) for efficiency</description></item>
		/// <item><description>Consider time-series database features for optimized temporal aggregations</description></item>
		/// <item><description>Cache aggregation results for immutable historical periods</description></item>
		/// <item><description>Implement incremental aggregation for partial period updates</description></item>
		/// </list>
		/// </para>
		/// <para>
		/// Handling empty periods:
		/// <list type="bullet">
		/// <item><description>Periods with no events may be omitted from results (sparse representation)</description></item>
		/// <item><description>Alternatively, return zero-valued aggregations for all periods (dense representation)</description></item>
		/// <item><description>Document behavior to avoid confusion in client code</description></item>
		/// </list>
		/// </para>
		/// </remarks>
		Task<IEnumerable<UsageAggregation>> GetAggregatedUsageAsync(TenantId tenantId, string? metric, DateTimeOffset startTime, DateTimeOffset endTime, AggregationPeriod period, CancellationToken cancellationToken = default);

		/// <summary>
		/// Retrieves granular usage events for a specific tenant and optionally filtered by metric within a time range.
		/// Returns the raw event data without aggregation, suitable for detailed analysis and auditing.
		/// </summary>
		/// <param name="tenantId">The unique identifier of the tenant whose usage events should be retrieved. Cannot be <see langword="null"/>.</param>
		/// <param name="metric">
		/// The metric name to filter by, or <see langword="null"/> to retrieve events for all metrics.
		/// When specified, only events matching this metric name are returned. Comparison should be case-insensitive.
		/// </param>
		/// <param name="startTime">
		/// The inclusive start of the time range. Events with timestamps greater than or equal to this value are included.
		/// </param>
		/// <param name="endTime">
		/// The inclusive end of the time range. Events with timestamps less than or equal to this value are included.
		/// Should be greater than or equal to <paramref name="startTime"/>.
		/// </param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests during the asynchronous operation.</param>
		/// <returns>
		/// A task that represents the asynchronous operation. The task result contains a collection of <see cref="UsageEvent"/>
		/// instances matching the filter criteria, ordered chronologically by timestamp. Returns an empty collection if no
		/// events match the criteria.
		/// </returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="tenantId"/> is <see langword="null"/>.</exception>
		/// <remarks>
		/// This method provides access to raw, unaggregated usage data for scenarios such as:
		/// <list type="bullet">
		/// <item><description>Detailed usage audits and compliance reporting</description></item>
		/// <item><description>Billing dispute resolution with event-level transparency</description></item>
		/// <item><description>Debugging usage patterns and anomalies</description></item>
		/// <item><description>Custom analytics requiring event-level granularity</description></item>
		/// <item><description>Export to external analytics or data warehouse systems</description></item>
		/// </list>
		/// <para>
		/// Implementation guidelines:
		/// <list type="bullet">
		/// <item><description>Index on (TenantId, Timestamp) for efficient time range queries</description></item>
		/// <item><description>Add index on (TenantId, Metric, Timestamp) if metric filtering is common</description></item>
		/// <item><description>Return events ordered by timestamp ascending for chronological analysis</description></item>
		/// <item><description>Consider pagination for large result sets to avoid memory issues</description></item>
		/// <item><description>Implement query timeouts to prevent long-running queries</description></item>
		/// </list>
		/// </para>
		/// <para>
		/// Performance considerations:
		/// <list type="bullet">
		/// <item><description>Large time ranges may return millions of events - consider limiting or paginating</description></item>
		/// <item><description>Use database query optimization (covering indexes, query hints) for better performance</description></item>
		/// <item><description>Consider materialized views or summary tables for frequently accessed ranges</description></item>
		/// <item><description>Implement caching for immutable historical data</description></item>
		/// </list>
		/// </para>
		/// </remarks>
		Task<IEnumerable<UsageEvent>> GetUsageAsync(TenantId tenantId, string? metric, DateTimeOffset startTime, DateTimeOffset endTime, CancellationToken cancellationToken = default);

		#endregion
	}
}