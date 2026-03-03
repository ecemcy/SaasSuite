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

namespace SaasSuite.Metering.Enumerations
{
	/// <summary>
	/// Defines the time-based granularity for aggregating usage data in multi-tenant metering operations.
	/// This enumeration determines how usage events are grouped together for reporting, billing, and analytics purposes.
	/// </summary>
	/// <remarks>
	/// Different aggregation periods serve different analytical and billing purposes:
	/// <list type="bullet">
	/// <item><description>Hourly aggregation provides fine-grained insights for real-time monitoring and anomaly detection</description></item>
	/// <item><description>Daily aggregation balances detail with performance for regular reporting and trend analysis</description></item>
	/// <item><description>Monthly aggregation aligns with typical billing cycles and long-term capacity planning</description></item>
	/// </list>
	/// <para>
	/// The choice of aggregation period affects:
	/// <list type="bullet">
	/// <item><description>Query performance - Shorter periods may require processing more data points</description></item>
	/// <item><description>Data granularity - Longer periods may hide short-term usage spikes or patterns</description></item>
	/// <item><description>Storage requirements - Pre-aggregated data at different periods requires additional storage</description></item>
	/// <item><description>Billing accuracy - Some usage models require specific aggregation periods for accurate pricing</description></item>
	/// </list>
	/// </para>
	/// </remarks>
	public enum AggregationPeriod
	{
		/// <summary>
		/// Aggregates usage data by hour, grouping all events within each hour-long time window.
		/// The aggregation starts at the beginning of each hour (minute 0, second 0) and includes all events
		/// until the end of that hour (minute 59, second 59).
		/// </summary>
		/// <remarks>
		/// Hourly aggregation is useful for:
		/// <list type="bullet">
		/// <item><description>Real-time monitoring dashboards showing current and recent usage patterns</description></item>
		/// <item><description>Detecting sudden spikes or drops in usage that may indicate issues</description></item>
		/// <item><description>Short-term capacity planning and auto-scaling decisions</description></item>
		/// <item><description>Debugging and troubleshooting usage-related issues with fine temporal resolution</description></item>
		/// <item><description>Applications with dynamic or highly variable usage patterns</description></item>
		/// </list>
		/// Performance considerations: Hourly aggregation generates 24 data points per day per metric,
		/// which may impact query performance when analyzing long time ranges.
		/// </remarks>
		Hourly = 0,

		/// <summary>
		/// Aggregates usage data by day, grouping all events within each 24-hour period.
		/// The aggregation starts at midnight (00:00:00) and includes all events until the end of the day (23:59:59),
		/// using the configured time zone offset.
		/// </summary>
		/// <remarks>
		/// Daily aggregation is useful for:
		/// <list type="bullet">
		/// <item><description>Regular reporting and trend analysis over weeks or months</description></item>
		/// <item><description>Identifying day-of-week usage patterns and seasonality</description></item>
		/// <item><description>Comparing usage across different days or weeks</description></item>
		/// <item><description>Daily budget tracking and cost allocation</description></item>
		/// <item><description>Balancing granularity with query performance for most reporting needs</description></item>
		/// </list>
		/// This is often the default choice for general-purpose reporting as it provides a good balance
		/// between detail and performance. Daily aggregation generates approximately 30 data points per month.
		/// </remarks>
		Daily = 1,

		/// <summary>
		/// Aggregates usage data by calendar month, grouping all events within each month-long period.
		/// The aggregation starts on the first day of each month (day 1 at 00:00:00) and includes all events
		/// until the last day of the month (day 28-31 at 23:59:59), automatically adjusting for month length.
		/// </summary>
		/// <remarks>
		/// Monthly aggregation is useful for:
		/// <list type="bullet">
		/// <item><description>Monthly billing cycles and invoice generation</description></item>
		/// <item><description>Long-term capacity planning and growth projections</description></item>
		/// <item><description>Year-over-year comparisons and historical trend analysis</description></item>
		/// <item><description>High-level executive dashboards and financial reporting</description></item>
		/// <item><description>Subscription-based pricing models with monthly billing periods</description></item>
		/// </list>
		/// Monthly aggregation provides the highest level of data compression and best query performance
		/// but loses intra-month patterns and variations. It generates only 12 data points per year per metric.
		/// Note that months have varying lengths (28-31 days), which should be considered when comparing
		/// absolute values across different months.
		/// </remarks>
		Monthly = 2
	}
}