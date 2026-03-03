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

using SaasSuite.Metering.Interfaces;
using SaasSuite.Metering.Services;

namespace SaasSuite.Metering.Options
{
	/// <summary>
	/// Configuration options for controlling metering service behavior including data retention,
	/// metric validation, and automatic aggregation settings.
	/// </summary>
	/// <remarks>
	/// These options are configured during service registration and affect global metering behavior.
	/// Options can be configured via:
	/// <list type="bullet">
	/// <item><description>Lambda expression in AddSaasMetering(options => ...)</description></item>
	/// <item><description>Configuration file binding (appsettings.json)</description></item>
	/// <item><description>Environment variables</description></item>
	/// <item><description>Azure App Configuration or other configuration providers</description></item>
	/// </list>
	/// </remarks>
	public class MeteringOptions
	{
		#region ' Properties '

		/// <summary>
		/// Gets or sets a value indicating whether to automatically aggregate usage data in the background at regular intervals.
		/// When enabled, a background service will periodically compute and cache aggregations.
		/// </summary>
		/// <value>
		/// <see langword="true"/> to enable automatic background aggregation;
		/// <see langword="false"/> to compute aggregations only on-demand when queried.
		/// Defaults to <see langword="false"/>.
		/// </value>
		/// <remarks>
		/// Automatic aggregation is beneficial for:
		/// <list type="bullet">
		/// <item><description>Improving query performance by pre-computing summaries</description></item>
		/// <item><description>Reducing load on the database during peak usage</description></item>
		/// <item><description>Providing near-real-time dashboards without impacting user experience</description></item>
		/// <item><description>Supporting high-concurrency reporting scenarios</description></item>
		/// </list>
		/// <para>
		/// When enabled:
		/// <list type="bullet">
		/// <item><description>A background worker or hosted service should be implemented to run aggregation jobs</description></item>
		/// <item><description>Aggregations run at the interval specified by <see cref="AggregationInterval"/></description></item>
		/// <item><description>Pre-computed results should be stored in a cache or database table</description></item>
		/// <item><description>Queries should preferentially use pre-aggregated data when available</description></item>
		/// </list>
		/// </para>
		/// <para>
		/// Considerations:
		/// <list type="bullet">
		/// <item><description>Requires additional infrastructure (background workers, job schedulers)</description></item>
		/// <item><description>May have slight staleness (data is fresh as of last aggregation run)</description></item>
		/// <item><description>Needs coordination in multi-instance deployments to avoid duplicate work</description></item>
		/// <item><description>Storage overhead for pre-aggregated tables or cache entries</description></item>
		/// </list>
		/// </para>
		/// <para>
		/// Implementation patterns:
		/// <list type="bullet">
		/// <item><description>IHostedService with timer-based execution</description></item>
		/// <item><description>Hangfire or Quartz.NET for scheduled jobs</description></item>
		/// <item><description>Azure Functions or AWS Lambda for serverless aggregation</description></item>
		/// <item><description>Message queue consumers processing aggregation requests</description></item>
		/// </list>
		/// </para>
		/// </remarks>
		public bool EnableAutoAggregation { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether to validate metric names against a predefined whitelist before recording events.
		/// When <see langword="true"/>, only metrics in <see cref="ValidMetrics"/> are accepted.
		/// </summary>
		/// <value>
		/// <see langword="true"/> to enable metric validation and reject unknown metrics;
		/// <see langword="false"/> to accept any metric name.
		/// Defaults to <see langword="false"/> for maximum flexibility.
		/// </value>
		/// <remarks>
		/// Enabling metric validation provides:
		/// <list type="bullet">
		/// <item><description>Prevention of typos and misspellings in metric names</description></item>
		/// <item><description>Enforcement of a controlled metric catalog</description></item>
		/// <item><description>Protection against accidental or malicious metric pollution</description></item>
		/// <item><description>Consistency in metric naming across the application</description></item>
		/// <item><description>Easier billing configuration with known metric set</description></item>
		/// </list>
		/// <para>
		/// When enabled, <see cref="MeteringService.RecordUsageAsync(Core.TenantId, string, decimal, Dictionary{string, string}?, CancellationToken)"/>
		/// will throw <see cref="ArgumentException"/> if the metric is not in <see cref="ValidMetrics"/>.
		/// Populate <see cref="ValidMetrics"/> with all allowed metric names before enabling.
		/// </para>
		/// <para>
		/// Trade-offs:
		/// <list type="bullet">
		/// <item><description>Pros: Type safety, prevents errors, controlled catalog</description></item>
		/// <item><description>Cons: Requires configuration updates to add new metrics, less flexible</description></item>
		/// </list>
		/// </para>
		/// <para>
		/// Consider enabling validation in production to prevent billing errors, but leaving it disabled
		/// during development for easier experimentation.
		/// </para>
		/// </remarks>
		public bool ValidateMetrics { get; set; }

		/// <summary>
		/// Gets or sets the collection of valid metric names when <see cref="ValidateMetrics"/> is enabled.
		/// Only metrics in this set will be accepted for recording.
		/// </summary>
		/// <value>
		/// A <see cref="HashSet{T}"/> of string metric names that are allowed. Defaults to an empty set.
		/// Metric names are case-sensitive by default, though implementations may use case-insensitive comparison.
		/// </value>
		/// <remarks>
		/// Populate this set with all metric names used in your application:
		/// <list type="bullet">
		/// <item><description>api-calls</description></item>
		/// <item><description>storage-gb</description></item>
		/// <item><description>compute-hours</description></item>
		/// <item><description>data-transfer-gb</description></item>
		/// <item><description>active-users</description></item>
		/// <item><description>transactions</description></item>
		/// </list>
		/// <para>
		/// Best practices:
		/// <list type="bullet">
		/// <item><description>Define metrics as constants in a central location</description></item>
		/// <item><description>Use descriptive names that include units where applicable</description></item>
		/// <item><description>Maintain a metric registry or documentation</description></item>
		/// <item><description>Version metrics if definitions change (e.g., api-calls-v2)</description></item>
		/// <item><description>Consider namespacing for different subsystems (billing.api-calls, analytics.page-views)</description></item>
		/// </list>
		/// </para>
		/// <para>
		/// Configuration example:
		/// <code>
		/// services.AddSaasMetering(options =>
		/// {
		///     options.ValidateMetrics = true;
		///     options.ValidMetrics = new HashSet&lt;string&gt;
		///     {
		///         "api-calls",
		///         "storage-gb",
		///         "compute-hours"
		///     };
		/// });
		/// </code>
		/// </para>
		/// </remarks>
		public HashSet<string> ValidMetrics { get; set; } = new HashSet<string>();

		/// <summary>
		/// Gets or sets the time interval between automatic aggregation runs when <see cref="EnableAutoAggregation"/> is enabled.
		/// Determines how frequently background aggregation jobs execute.
		/// </summary>
		/// <value>
		/// A <see cref="TimeSpan"/> representing the interval between aggregation runs. Defaults to 1 hour.
		/// Should be balanced between data freshness requirements and processing overhead.
		/// </value>
		/// <remarks>
		/// The aggregation interval affects:
		/// <list type="bullet">
		/// <item><description>Data freshness - Shorter intervals provide more current data</description></item>
		/// <item><description>System load - More frequent aggregations increase CPU and I/O usage</description></item>
		/// <item><description>Storage churn - More updates to pre-aggregated tables</description></item>
		/// <item><description>Billing accuracy - More frequent aggregation reduces lag in billing calculations</description></item>
		/// </list>
		/// <para>
		/// Recommended values:
		/// <list type="bullet">
		/// <item><description>15 minutes: Near real-time dashboards, high data freshness requirements</description></item>
		/// <item><description>1 hour: Balance of freshness and performance (default)</description></item>
		/// <item><description>6 hours: Low-frequency updates, batch-oriented processing</description></item>
		/// <item><description>24 hours: Daily batch aggregation, minimal resource usage</description></item>
		/// </list>
		/// </para>
		/// <para>
		/// Considerations for choosing interval:
		/// <list type="bullet">
		/// <item><description>Data volume - Larger datasets may need longer intervals to complete processing</description></item>
		/// <item><description>User expectations - Real-time dashboards require shorter intervals</description></item>
		/// <item><description>Billing cycles - Hourly billing may need hourly aggregation</description></item>
		/// <item><description>Resource availability - Limited compute may require longer intervals</description></item>
		/// <item><description>Time zone considerations - Align with business hours or billing period boundaries</description></item>
		/// </list>
		/// </para>
		/// <para>
		/// For implementations:
		/// <list type="bullet">
		/// <item><description>Use incremental aggregation to process only new events since last run</description></item>
		/// <item><description>Implement overlap protection in distributed scenarios</description></item>
		/// <item><description>Log aggregation run times and row counts for monitoring</description></item>
		/// <item><description>Implement health checks to detect failed or stalled aggregations</description></item>
		/// </list>
		/// </para>
		/// </remarks>
		public TimeSpan AggregationInterval { get; set; } = TimeSpan.FromHours(1);

		/// <summary>
		/// Gets or sets the default retention period for usage events after which they may be archived or purged.
		/// This defines how long raw event data is kept in the primary storage before cleanup.
		/// </summary>
		/// <value>
		/// A <see cref="TimeSpan"/> representing the retention duration. Defaults to 90 days.
		/// Must be a positive value. Common values range from 30 days (minimal) to 7 years (compliance requirements).
		/// </value>
		/// <remarks>
		/// The retention period serves multiple purposes:
		/// <list type="bullet">
		/// <item><description>Billing compliance - Keep events for audit and dispute resolution</description></item>
		/// <item><description>Storage management - Limit data growth by removing old events</description></item>
		/// <item><description>Performance optimization - Reduce dataset size for faster queries</description></item>
		/// <item><description>Legal compliance - Meet data retention regulations (GDPR, SOX, HIPAA)</description></item>
		/// </list>
		/// <para>
		/// Implementation notes:
		/// <list type="bullet">
		/// <item><description>This is a soft limit - <see cref="IMeteringStore"/> implementations should implement purging</description></item>
		/// <item><description>Consider archiving to cold storage before deletion for long-term compliance</description></item>
		/// <item><description>Aggregated data may be retained longer than raw events</description></item>
		/// <item><description>Different tenants may require different retention periods based on subscription or regulation</description></item>
		/// </list>
		/// </para>
		/// <para>
		/// Recommended values:
		/// <list type="bullet">
		/// <item><description>30 days: Minimal retention for basic billing cycles</description></item>
		/// <item><description>90 days: Standard retention for most SaaS applications (default)</description></item>
		/// <item><description>365 days: Annual retention for year-over-year analysis</description></item>
		/// <item><description>7 years: Compliance requirements for financial services</description></item>
		/// </list>
		/// </para>
		/// </remarks>
		public TimeSpan RetentionPeriod { get; set; } = TimeSpan.FromDays(90);

		#endregion
	}
}