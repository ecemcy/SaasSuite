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
using SaasSuite.Metering.Options;

namespace SaasSuite.Metering
{
	/// <summary>
	/// Represents a discrete usage event that records consumption of a metered resource in a multi-tenant SaaS application.
	/// This is the fundamental unit of usage tracking, capturing what was consumed, when, by whom, and how much.
	/// </summary>
	/// <remarks>
	/// Usage events form the basis of usage-based pricing, billing, and analytics in SaaS applications.
	/// Each event represents a single measurable occurrence such as:
	/// <list type="bullet">
	/// <item><description>API calls made by a tenant</description></item>
	/// <item><description>Storage consumed in gigabytes or terabytes</description></item>
	/// <item><description>Data transfer or bandwidth usage</description></item>
	/// <item><description>Compute time or processing units consumed</description></item>
	/// <item><description>Transactions processed or records created</description></item>
	/// <item><description>Active users or seats utilized</description></item>
	/// </list>
	/// <para>
	/// Events are immutable once recorded and should be persisted for the configured retention period
	/// to support billing disputes, auditing, and historical analysis. The granular event data can be
	/// aggregated into summary statistics using <see cref="UsageAggregation"/> for efficient reporting.
	/// </para>
	/// </remarks>
	public class UsageEvent
	{
		#region ' Properties '

		/// <summary>
		/// Gets or sets the numeric value representing the quantity of usage for this event.
		/// The interpretation of this value depends on the metric type and units.
		/// </summary>
		/// <value>
		/// A decimal number representing the usage amount. Can be positive for consumption (typical case)
		/// or negative for refunds, corrections, or credit adjustments. The decimal type provides sufficient
		/// precision for fractional units and large values while avoiding floating-point precision issues.
		/// </value>
		/// <remarks>
		/// The value interpretation varies by metric type:
		/// <list type="bullet">
		/// <item><description>Incremental metrics: Each event adds to total (e.g., API calls: 1, 5, 10)</description></item>
		/// <item><description>Gauge metrics: Each event represents a point-in-time measurement (e.g., active users: 42)</description></item>
		/// <item><description>Cumulative metrics: Running total at the time of measurement (e.g., total storage: 150.5 GB)</description></item>
		/// </list>
		/// <para>
		/// For billing purposes, this value is typically:
		/// <list type="bullet">
		/// <item><description>Multiplied by unit pricing to calculate cost</description></item>
		/// <item><description>Aggregated over time periods for tiered or volume-based pricing</description></item>
		/// <item><description>Compared against quotas or limits to trigger alerts or restrictions</description></item>
		/// </list>
		/// </para>
		/// <para>
		/// Special considerations:
		/// <list type="bullet">
		/// <item><description>Negative values should be carefully validated to prevent billing manipulation</description></item>
		/// <item><description>Very large values may indicate errors or need special handling</description></item>
		/// <item><description>Zero values are valid and may be used for audit trails or state changes</description></item>
		/// </list>
		/// </para>
		/// </remarks>
		public decimal Value { get; set; }

		/// <summary>
		/// Gets or sets the unique identifier for this usage event.
		/// This ID can be used for idempotency checks, event deduplication, and audit trail references.
		/// </summary>
		/// <value>
		/// A unique string identifier, automatically initialized to a new GUID when an instance is created.
		/// This value should remain stable once the event is persisted and should not be modified.
		/// </value>
		/// <remarks>
		/// The unique ID serves multiple purposes:
		/// <list type="bullet">
		/// <item><description>Prevents duplicate event recording in distributed systems</description></item>
		/// <item><description>Enables event correlation across different systems and logs</description></item>
		/// <item><description>Supports event replay and reconciliation scenarios</description></item>
		/// <item><description>Provides a reference for billing dispute resolution</description></item>
		/// </list>
		/// In distributed systems, consider using a distributed ID generation strategy (UUIDs, Snowflake IDs, etc.)
		/// to avoid collisions across multiple application instances.
		/// </remarks>
		public string Id { get; set; } = Guid.NewGuid().ToString();

		/// <summary>
		/// Gets or sets the metric name identifying the type of resource being consumed.
		/// This categorizes the usage into different billable or trackable dimensions.
		/// </summary>
		/// <value>
		/// A string representing the metric name such as "api-calls", "storage-gb", "compute-hours", or "transactions".
		/// Should follow a consistent naming convention across the application. Case-sensitivity depends on the
		/// <see cref="MeteringOptions.ValidateMetrics"/> configuration.
		/// </value>
		/// <remarks>
		/// Metric names should be:
		/// <list type="bullet">
		/// <item><description>Descriptive and self-documenting (e.g., "api-calls" not "ac")</description></item>
		/// <item><description>Consistent in naming convention (kebab-case, snake_case, etc.)</description></item>
		/// <item><description>Stable over time to maintain historical continuity</description></item>
		/// <item><description>Defined in a centralized registry to prevent naming variations</description></item>
		/// <item><description>Inclusive of units where relevant (e.g., "storage-gb" vs just "storage")</description></item>
		/// </list>
		/// <para>
		/// Common metric categories include:
		/// <list type="bullet">
		/// <item><description>Transaction-based: API calls, requests, database queries</description></item>
		/// <item><description>Resource-based: Storage, bandwidth, memory, CPU</description></item>
		/// <item><description>Time-based: Compute hours, connection time, session duration</description></item>
		/// <item><description>Count-based: Active users, records processed, emails sent</description></item>
		/// </list>
		/// </para>
		/// When <see cref="MeteringOptions.ValidateMetrics"/> is enabled, only metrics in the
		/// <see cref="MeteringOptions.ValidMetrics"/> set will be accepted.
		/// </remarks>
		public string Metric { get; set; } = string.Empty;

		/// <summary>
		/// Gets or sets the timestamp indicating when the usage occurred.
		/// This determines the billing period, aggregation bucket, and chronological ordering of events.
		/// </summary>
		/// <value>
		/// A <see cref="DateTimeOffset"/> representing when the usage happened, defaulting to the current UTC time
		/// when the event is created. Uses <see cref="DateTimeOffset"/> rather than <see cref="DateTime"/> to
		/// preserve time zone information for accurate billing across different regions.
		/// </value>
		/// <remarks>
		/// The timestamp is crucial for:
		/// <list type="bullet">
		/// <item><description>Determining which billing period the usage belongs to</description></item>
		/// <item><description>Aggregating events into hourly, daily, or monthly buckets</description></item>
		/// <item><description>Ordering events chronologically in reports and analytics</description></item>
		/// <item><description>Filtering usage by time ranges for specific queries</description></item>
		/// <item><description>Calculating time-based metrics like usage per hour or daily averages</description></item>
		/// </list>
		/// <para>
		/// Best practices:
		/// <list type="bullet">
		/// <item><description>Use UTC timestamps for consistency across time zones</description></item>
		/// <item><description>Record timestamps as close to the actual usage occurrence as possible</description></item>
		/// <item><description>Preserve time zone offset information for multi-region deployments</description></item>
		/// <item><description>Consider clock skew in distributed systems when ordering events</description></item>
		/// <item><description>Handle daylight saving time transitions appropriately in aggregations</description></item>
		/// </list>
		/// </para>
		/// <para>
		/// For backdated or adjusted events, the timestamp should reflect the original usage time,
		/// not the time when the correction was made. Use metadata to track adjustment information.
		/// </para>
		/// </remarks>
		public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

		/// <summary>
		/// Gets or sets optional metadata providing additional context about this usage event.
		/// Allows for extensible event enrichment without modifying the core event schema.
		/// </summary>
		/// <value>
		/// A dictionary of string key-value pairs containing supplementary information, or <see langword="null"/>
		/// if no additional metadata is needed. Keys should follow a consistent naming convention and values
		/// should be strings to ensure serializability and storage compatibility.
		/// </value>
		/// <remarks>
		/// Metadata is useful for capturing:
		/// <list type="bullet">
		/// <item><description>User context: User ID, email, or role that triggered the usage</description></item>
		/// <item><description>Request details: API endpoint, HTTP method, request ID for correlation</description></item>
		/// <item><description>Geographic information: Region, availability zone, data center location</description></item>
		/// <item><description>Resource identifiers: Specific resources or services consumed</description></item>
		/// <item><description>Quality of service: Performance tier, priority level, service class</description></item>
		/// <item><description>Business context: Project ID, department, cost center, or campaign</description></item>
		/// <item><description>Technical details: SDK version, client type, protocol version</description></item>
		/// </list>
		/// <para>
		/// Metadata enables:
		/// <list type="bullet">
		/// <item><description>Detailed usage breakdowns and drill-down analysis</description></item>
		/// <item><description>Chargeback or showback to specific business units or projects</description></item>
		/// <item><description>Debugging and troubleshooting usage patterns</description></item>
		/// <item><description>Compliance and audit trail requirements</description></item>
		/// <item><description>Customer segmentation and usage pattern analysis</description></item>
		/// </list>
		/// </para>
		/// <para>
		/// Considerations:
		/// <list type="bullet">
		/// <item><description>Avoid storing large amounts of data that could impact storage and query performance</description></item>
		/// <item><description>Do not include sensitive information like passwords or full credit card numbers</description></item>
		/// <item><description>Ensure PII (Personally Identifiable Information) complies with privacy regulations</description></item>
		/// <item><description>Use consistent key names across the application to enable meaningful aggregations</description></item>
		/// <item><description>Consider indexing frequently queried metadata fields for better performance</description></item>
		/// </list>
		/// </para>
		/// </remarks>
		public Dictionary<string, string>? Metadata { get; set; }

		/// <summary>
		/// Gets or sets the tenant identifier for this usage event.
		/// Associates the usage with a specific tenant for multi-tenant isolation and billing purposes.
		/// </summary>
		/// <value>
		/// A <see cref="Core.TenantId"/> strongly-typed identifier specifying which tenant generated this usage.
		/// This property is required and should never be <see langword="null"/> or empty.
		/// </value>
		/// <remarks>
		/// The tenant ID is critical for:
		/// <list type="bullet">
		/// <item><description>Proper billing attribution to ensure each tenant pays for their own usage</description></item>
		/// <item><description>Data isolation to prevent cross-tenant data leakage in queries and reports</description></item>
		/// <item><description>Usage quotas and limits enforcement per tenant</description></item>
		/// <item><description>Tenant-specific analytics and capacity planning</description></item>
		/// <item><description>Audit compliance and data residency requirements</description></item>
		/// </list>
		/// Always validate that the tenant ID matches the authenticated tenant context when recording usage
		/// to prevent usage attribution fraud or errors.
		/// </remarks>
		public TenantId TenantId { get; set; }

		#endregion
	}
}