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

namespace SaasSuite.Metering
{
	/// <summary>
	/// Represents statistical summary of aggregated usage data for a specific metric over a defined time period.
	/// This model consolidates multiple <see cref="UsageEvent"/> instances into comprehensive statistical metrics
	/// for efficient reporting, billing, and analytics without processing individual events.
	/// </summary>
	/// <remarks>
	/// Usage aggregations are computed from raw <see cref="UsageEvent"/> data to provide:
	/// <list type="bullet">
	/// <item><description>Efficient querying by reducing the number of records to process</description></item>
	/// <item><description>Statistical insights including totals, averages, minimums, and maximums</description></item>
	/// <item><description>Time-series data for trend analysis and visualization</description></item>
	/// <item><description>Simplified billing calculations based on period totals</description></item>
	/// <item><description>Performance optimization for dashboards and reports</description></item>
	/// </list>
	/// <para>
	/// Aggregations are typically generated:
	/// <list type="bullet">
	/// <item><description>On-demand during query execution (real-time aggregation)</description></item>
	/// <item><description>Periodically as background jobs (pre-computed aggregations)</description></item>
	/// <item><description>At the end of billing periods (monthly, daily summaries)</description></item>
	/// <item><description>For specific time ranges requested by users or reports</description></item>
	/// </list>
	/// </para>
	/// </remarks>
	public class UsageAggregation
	{
		#region ' Properties '

		/// <summary>
		/// Gets or sets the arithmetic mean of usage values within the aggregation period.
		/// Calculated as <see cref="TotalValue"/> divided by <see cref="EventCount"/>.
		/// </summary>
		/// <value>
		/// A decimal number representing the average usage per event. For periods with no events (EventCount = 0),
		/// this value may be zero or undefined depending on implementation.
		/// </value>
		/// <remarks>
		/// The average value is particularly useful for:
		/// <list type="bullet">
		/// <item><description>Gauge metrics - Represents typical state during the period (e.g., average active users)</description></item>
		/// <item><description>Resource utilization - Shows average consumption rate or intensity</description></item>
		/// <item><description>Trend analysis - Smooths out spikes to reveal underlying patterns</description></item>
		/// <item><description>Anomaly detection - Deviations from average indicate unusual activity</description></item>
		/// <item><description>Comparative analysis - Enables meaningful comparisons across different period lengths</description></item>
		/// </list>
		/// <para>
		/// Interpretation varies by metric type:
		/// <list type="bullet">
		/// <item><description>Incremental metrics (API calls): Average calls per event occurrence</description></item>
		/// <item><description>Gauge metrics (active users): Average concurrent usage during period</description></item>
		/// <item><description>Resource metrics (CPU %): Average utilization level</description></item>
		/// </list>
		/// </para>
		/// <para>
		/// Caution: The average can be misleading if:
		/// <list type="bullet">
		/// <item><description>Event distribution is highly skewed with outliers</description></item>
		/// <item><description>Events occur irregularly (gaps may not be represented)</description></item>
		/// <item><description>Mixed event types are aggregated together</description></item>
		/// </list>
		/// Consider using <see cref="MinValue"/> and <see cref="MaxValue"/> to assess data distribution.
		/// </para>
		/// </remarks>
		public decimal AverageValue { get; set; }

		/// <summary>
		/// Gets or sets the largest usage value recorded within the aggregation period.
		/// Represents the peak consumption level or highest measurement observed.
		/// </summary>
		/// <value>
		/// A decimal number representing the largest <see cref="UsageEvent.Value"/> among all events
		/// in the aggregation. This indicates the maximum demand or capacity requirement during the period.
		/// </value>
		/// <remarks>
		/// The maximum value is critical for:
		/// <list type="bullet">
		/// <item><description>Capacity planning - Determines peak load requirements and infrastructure sizing</description></item>
		/// <item><description>Spike detection - Identifies unusual bursts or anomalous activity</description></item>
		/// <item><description>Peak-based billing - Some pricing models charge based on maximum usage</description></item>
		/// <item><description>Resource allocation - Ensures sufficient resources for peak demands</description></item>
		/// <item><description>Performance optimization - Identifies bottlenecks and scaling needs</description></item>
		/// <item><description>SLA compliance - Verifies service can handle peak loads</description></item>
		/// </list>
		/// <para>
		/// Interpretation by metric type:
		/// <list type="bullet">
		/// <item><description>Concurrent users: Peak simultaneous usage during period</description></item>
		/// <item><description>API requests: Highest request burst or batch size</description></item>
		/// <item><description>Storage: Maximum storage used at any point</description></item>
		/// <item><description>Bandwidth: Peak transfer rate achieved</description></item>
		/// </list>
		/// </para>
		/// <para>
		/// Considerations:
		/// <list type="bullet">
		/// <item><description>Outliers may skew maximum values; consider percentile-based metrics (p95, p99) for production systems</description></item>
		/// <item><description>Short-duration spikes may not reflect sustained capacity needs</description></item>
		/// <item><description>Maximum values should trigger capacity alerts when approaching limits</description></item>
		/// <item><description>Compare maximum against average to assess usage volatility</description></item>
		/// </list>
		/// </para>
		/// </remarks>
		public decimal MaxValue { get; set; }

		/// <summary>
		/// Gets or sets the smallest usage value recorded within the aggregation period.
		/// Represents the minimum consumption level or lowest measurement observed.
		/// </summary>
		/// <value>
		/// A decimal number representing the smallest <see cref="UsageEvent.Value"/> among all events
		/// in the aggregation. For periods with no events, this may be zero or undefined.
		/// </value>
		/// <remarks>
		/// The minimum value is valuable for:
		/// <list type="bullet">
		/// <item><description>Baseline usage analysis - Identifies minimum operational requirements</description></item>
		/// <item><description>Anomaly detection - Unexpectedly low values may indicate service degradation</description></item>
		/// <item><description>Service Level Agreement (SLA) verification - Ensures minimum service delivery</description></item>
		/// <item><description>Range analysis - Combined with <see cref="MaxValue"/> to understand variability</description></item>
		/// <item><description>Idle resource detection - Identifies underutilized capacity</description></item>
		/// </list>
		/// <para>
		/// For different scenarios:
		/// <list type="bullet">
		/// <item><description>API calls: Minimum may be zero during quiet periods</description></item>
		/// <item><description>Storage: Minimum shows baseline storage footprint</description></item>
		/// <item><description>Active users: Minimum indicates off-peak usage</description></item>
		/// </list>
		/// </para>
		/// <para>
		/// Negative values may be valid for:
		/// <list type="bullet">
		/// <item><description>Credits or refunds</description></item>
		/// <item><description>Corrections to previously recorded usage</description></item>
		/// <item><description>Bidirectional metrics (data transfer: upload vs download)</description></item>
		/// </list>
		/// Always validate the semantic meaning of negative minimums in your specific context.
		/// </para>
		/// </remarks>
		public decimal MinValue { get; set; }

		/// <summary>
		/// Gets or sets the sum of all usage values within the aggregation period.
		/// Represents the total consumption of the metric during the time window.
		/// </summary>
		/// <value>
		/// A decimal number representing the cumulative sum of all <see cref="UsageEvent.Value"/> properties
		/// for events matching the tenant, metric, and time period criteria. This is the primary value
		/// used for billing calculations in usage-based pricing models.
		/// </value>
		/// <remarks>
		/// The total value is the most commonly used metric for:
		/// <list type="bullet">
		/// <item><description>Usage-based billing - Multiply by unit price to calculate cost</description></item>
		/// <item><description>Quota enforcement - Compare against allocation limits</description></item>
		/// <item><description>Capacity planning - Identify trends and growth patterns</description></item>
		/// <item><description>Cost allocation - Distribute costs across departments or projects</description></item>
		/// </list>
		/// <para>
		/// For different metric types:
		/// <list type="bullet">
		/// <item><description>Incremental (API calls): Sum represents total number of calls in period</description></item>
		/// <item><description>Gauge (active users): Sum may not be meaningful; use <see cref="AverageValue"/> or <see cref="MaxValue"/> instead</description></item>
		/// <item><description>Cumulative (storage): Use final value rather than sum to avoid double-counting</description></item>
		/// </list>
		/// </para>
		/// </remarks>
		public decimal TotalValue { get; set; }

		/// <summary>
		/// Gets or sets the number of individual usage events included in this aggregation.
		/// Indicates the granularity and data density of the underlying raw events.
		/// </summary>
		/// <value>
		/// An integer count of <see cref="UsageEvent"/> instances that were aggregated to produce this summary.
		/// A value of zero indicates no usage occurred during the period.
		/// </value>
		/// <remarks>
		/// The event count is useful for:
		/// <list type="bullet">
		/// <item><description>Understanding usage patterns - High count suggests frequent, small events</description></item>
		/// <item><description>Data quality assessment - Very low counts may indicate missing data</description></item>
		/// <item><description>Anomaly detection - Sudden changes in event count may signal issues</description></item>
		/// <item><description>Calculating statistical confidence - More events provide more reliable statistics</description></item>
		/// <item><description>Optimizing data collection - Identifies opportunities for batching or sampling</description></item>
		/// </list>
		/// <para>
		/// Interpreting event count:
		/// <list type="bullet">
		/// <item><description>Zero events: No usage during period (normal for some tenants/periods)</description></item>
		/// <item><description>One event: Single measurement or batch event representing aggregated usage</description></item>
		/// <item><description>High count: Frequent individual events, good granularity for analysis</description></item>
		/// </list>
		/// </para>
		/// </remarks>
		public int EventCount { get; set; }

		/// <summary>
		/// Gets or sets the metric name that was aggregated.
		/// Identifies which type of usage or resource consumption this aggregation represents.
		/// </summary>
		/// <value>
		/// A string representing the metric name such as "api-calls", "storage-gb", or "compute-hours".
		/// Must match one of the metric values from the underlying <see cref="UsageEvent.Metric"/> properties.
		/// </value>
		/// <remarks>
		/// The metric name allows:
		/// <list type="bullet">
		/// <item><description>Filtering aggregations to specific types of usage</description></item>
		/// <item><description>Applying different pricing models per metric type</description></item>
		/// <item><description>Comparing usage patterns across different metrics</description></item>
		/// <item><description>Building metric-specific visualizations and reports</description></item>
		/// </list>
		/// Each aggregation represents a single metric; to get data for multiple metrics,
		/// multiple aggregation queries or results are required.
		/// </remarks>
		public string Metric { get; set; } = string.Empty;

		/// <summary>
		/// Gets or sets the end timestamp of the aggregation period (inclusive).
		/// Defines the conclusion of the time window for which usage was aggregated.
		/// </summary>
		/// <value>
		/// A <see cref="DateTimeOffset"/> marking the last moment included in this aggregation.
		/// Typically set to one tick before the start of the next period to create contiguous,
		/// non-overlapping time windows.
		/// </value>
		/// <remarks>
		/// The period end is inclusive, meaning events with timestamps equal to or before this time
		/// are included in the aggregation. The combination of <see cref="PeriodStart"/> and
		/// <see cref="PeriodEnd"/> creates a closed interval: [PeriodStart, PeriodEnd].
		/// <para>
		/// For different aggregation periods:
		/// <list type="bullet">
		/// <item><description>Hourly: Last moment of the hour (minute 59, second 59, with maximum ticks)</description></item>
		/// <item><description>Daily: Last moment of the day (23:59:59 with maximum ticks)</description></item>
		/// <item><description>Monthly: Last moment of the last day of the month (varies by month: 28-31 days)</description></item>
		/// </list>
		/// </para>
		/// The period duration can be calculated as: PeriodEnd - PeriodStart + 1 tick.
		/// </remarks>
		public DateTimeOffset PeriodEnd { get; set; }

		/// <summary>
		/// Gets or sets the start timestamp of the aggregation period (inclusive).
		/// Defines the beginning of the time window for which usage was aggregated.
		/// </summary>
		/// <value>
		/// A <see cref="DateTimeOffset"/> marking the first moment included in this aggregation.
		/// For hourly aggregation, this is the top of the hour. For daily, midnight of the day.
		/// For monthly, midnight of the first day of the month.
		/// </value>
		/// <remarks>
		/// The period start is inclusive, meaning events with timestamps equal to or after this time
		/// are included in the aggregation. Combined with <see cref="PeriodEnd"/>, this defines
		/// the complete time window: [PeriodStart, PeriodEnd].
		/// <para>
		/// The period boundaries are determined by the <see cref="AggregationPeriod"/>:
		/// <list type="bullet">
		/// <item><description>Hourly: Start of each hour (minute 0, second 0)</description></item>
		/// <item><description>Daily: Midnight (00:00:00) of each day</description></item>
		/// <item><description>Monthly: Midnight (00:00:00) of the first day of each month</description></item>
		/// </list>
		/// </para>
		/// </remarks>
		public DateTimeOffset PeriodStart { get; set; }

		/// <summary>
		/// Gets or sets the tenant identifier for which this aggregation was computed.
		/// Associates the aggregated data with a specific tenant for multi-tenant isolation.
		/// </summary>
		/// <value>
		/// A <see cref="Core.TenantId"/> strongly-typed identifier specifying which tenant this aggregation represents.
		/// Must match the tenant ID of all underlying <see cref="UsageEvent"/> instances included in the aggregation.
		/// </value>
		/// <remarks>
		/// The tenant ID ensures:
		/// <list type="bullet">
		/// <item><description>Proper billing attribution and cost allocation per tenant</description></item>
		/// <item><description>Data isolation in multi-tenant reporting and analytics</description></item>
		/// <item><description>Tenant-specific quota tracking and limit enforcement</description></item>
		/// <item><description>Accurate per-tenant usage trending and forecasting</description></item>
		/// </list>
		/// </remarks>
		public TenantId TenantId { get; set; }

		#endregion
	}
}