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

namespace SaasSuite.Migration.Options
{
	/// <summary>
	/// Configuration options that control migration execution behavior.
	/// </summary>
	/// <remarks>
	/// These options determine how the migration engine processes tenants, handles errors,
	/// manages checkpoints, and reports progress. Options can be configured globally through
	/// dependency injection or provided per-migration operation for fine-grained control.
	/// </remarks>
	public class MigrationOptions
	{
		#region ' Properties '

		/// <summary>
		/// Gets or sets a value indicating whether to continue processing remaining tenants after individual tenant failures.
		/// </summary>
		/// <value>
		/// <see langword="true"/> to continue on failure; <see langword="false"/> to stop immediately.
		/// Defaults to <see langword="true"/>.
		/// </value>
		/// <remarks>
		/// <para>
		/// When <see langword="true"/> (continue on failure):
		/// <list type="bullet">
		/// <item><description>Failed tenants are logged and added to error collection</description></item>
		/// <item><description>Migration proceeds to remaining tenants in batch and subsequent batches</description></item>
		/// <item><description>Final result contains both successful and failed tenant lists</description></item>
		/// <item><description>Maximizes successful migrations even when some fail</description></item>
		/// </list>
		/// </para>
		/// <para>
		/// When <see langword="false"/> (fail fast):
		/// <list type="bullet">
		/// <item><description>First tenant failure stops entire migration immediately</description></item>
		/// <item><description>Enables quick identification of systemic issues</description></item>
		/// <item><description>Prevents cascading failures or data inconsistencies</description></item>
		/// <item><description>Useful for testing or when tenant failures indicate critical problems</description></item>
		/// </list>
		/// </para>
		/// <para>
		/// Use continue-on-failure for:
		/// <list type="bullet">
		/// <item><description>Production migrations where partial success is acceptable</description></item>
		/// <item><description>Migrations where tenant failures are expected to be isolated</description></item>
		/// <item><description>Scenarios requiring maximum throughput and progress</description></item>
		/// </list>
		/// </para>
		/// </remarks>
		public bool ContinueOnFailure { get; set; } = true;

		/// <summary>
		/// Gets or sets a value indicating whether to create checkpoints for migration resumability.
		/// </summary>
		/// <value>
		/// <see langword="true"/> to enable checkpoint creation; <see langword="false"/> to disable.
		/// Defaults to <see langword="true"/>.
		/// </value>
		/// <remarks>
		/// Checkpoints capture migration state at regular intervals, enabling resumption after
		/// interruptions or failures. Benefits include:
		/// <list type="bullet">
		/// <item><description>Avoiding reprocessing of already-migrated tenants</description></item>
		/// <item><description>Reducing overall migration time after failures</description></item>
		/// <item><description>Enabling safe cancellation and restart</description></item>
		/// <item><description>Supporting incremental migration strategies</description></item>
		/// </list>
		/// <para>
		/// Checkpoints add slight overhead for state capture but are highly recommended for:
		/// <list type="bullet">
		/// <item><description>Large-scale migrations (hundreds or thousands of tenants)</description></item>
		/// <item><description>Long-running migrations (hours or days)</description></item>
		/// <item><description>Production environments where interruptions may occur</description></item>
		/// <item><description>Migrations with non-negligible per-tenant processing time</description></item>
		/// </list>
		/// </para>
		/// Disable only for small, fast migrations where restart cost is minimal.
		/// </remarks>
		public bool EnableCheckpointing { get; set; } = true;

		/// <summary>
		/// Gets or sets a value indicating whether to process tenants in parallel within each batch.
		/// </summary>
		/// <value>
		/// <see langword="true"/> to enable parallel tenant processing; <see langword="false"/> for sequential processing.
		/// Defaults to <see langword="false"/>.
		/// </value>
		/// <remarks>
		/// <para>
		/// Parallel execution significantly improves throughput for I/O-bound migrations
		/// (database operations, API calls) but requires careful consideration:
		/// <list type="bullet">
		/// <item><description><strong>Thread safety:</strong> Migration steps must be thread-safe</description></item>
		/// <item><description><strong>Resource limits:</strong> Database connections, API rate limits</description></item>
		/// <item><description><strong>Error handling:</strong> Failures become more complex to diagnose</description></item>
		/// <item><description><strong>Ordering:</strong> Execution order is not guaranteed</description></item>
		/// </list>
		/// </para>
		/// <para>
		/// Enable parallel execution when:
		/// <list type="bullet">
		/// <item><description>Migration steps are independent and idempotent</description></item>
		/// <item><description>System resources (CPU, connections, memory) can handle concurrency</description></item>
		/// <item><description>Faster completion time is critical</description></item>
		/// </list>
		/// </para>
		/// When enabled, control concurrency with <see cref="MaxDegreeOfParallelism"/>.
		/// </remarks>
		public bool EnableParallelExecution { get; set; } = false;

		/// <summary>
		/// Gets or sets a value indicating whether to report progress updates during migration.
		/// </summary>
		/// <value>
		/// <see langword="true"/> to enable progress reporting; <see langword="false"/> to disable.
		/// Defaults to <see langword="true"/>.
		/// </value>
		/// <remarks>
		/// When enabled, progress updates are reported through <see cref="IProgress{MigrationProgress}"/>
		/// after processing each tenant. Progress reports include:
		/// <list type="bullet">
		/// <item><description>Current and total tenant counts</description></item>
		/// <item><description>Current and total batch numbers</description></item>
		/// <item><description>Completion percentage</description></item>
		/// <item><description>Currently processing tenant ID</description></item>
		/// <item><description>Descriptive status messages</description></item>
		/// </list>
		/// <para>
		/// Enable progress reporting for:
		/// <list type="bullet">
		/// <item><description>User-facing migrations with UI progress indicators</description></item>
		/// <item><description>Long-running migrations requiring monitoring</description></item>
		/// <item><description>Operations teams needing real-time visibility</description></item>
		/// <item><description>Automated systems that track migration status</description></item>
		/// </list>
		/// </para>
		/// <para>
		/// Disable for:
		/// <list type="bullet">
		/// <item><description>Background migrations where progress isn't monitored</description></item>
		/// <item><description>Reducing overhead in high-throughput scenarios</description></item>
		/// <item><description>Testing when progress updates aren't needed</description></item>
		/// </list>
		/// </para>
		/// Progress reporting has minimal overhead but can be disabled if not used.
		/// </remarks>
		public bool EnableProgressReporting { get; set; } = true;

		/// <summary>
		/// Gets or sets the number of tenants to process in each batch.
		/// </summary>
		/// <value>
		/// An integer representing the batch size. Must be greater than 0. Defaults to 10.
		/// </value>
		/// <remarks>
		/// Batching divides the total tenant list into smaller groups for processing.
		/// Benefits of batching include:
		/// <list type="bullet">
		/// <item><description>Controlled resource consumption and memory usage</description></item>
		/// <item><description>Regular checkpoint opportunities for resumability</description></item>
		/// <item><description>Easier progress tracking and monitoring</description></item>
		/// <item><description>Bounded blast radius if issues occur</description></item>
		/// </list>
		/// <para>
		/// Choose batch size based on:
		/// <list type="bullet">
		/// <item><description>Average migration time per tenant</description></item>
		/// <item><description>System resource constraints</description></item>
		/// <item><description>Acceptable checkpoint frequency</description></item>
		/// <item><description>Parallel processing capabilities</description></item>
		/// </list>
		/// </para>
		/// Smaller batches provide more frequent checkpoints but increase overhead.
		/// Larger batches improve throughput but reduce resumability granularity.
		/// </remarks>
		public int BatchSize { get; set; } = 10;

		/// <summary>
		/// Gets or sets the frequency of checkpoint creation measured in batches.
		/// </summary>
		/// <value>
		/// An integer representing how many batches complete between checkpoints. Must be greater than 0.
		/// Defaults to 1 (checkpoint after each batch).
		/// </value>
		/// <remarks>
		/// This setting controls checkpoint granularity and overhead.
		/// It only applies when <see cref="EnableCheckpointing"/> is <see langword="true"/>.
		/// <para>
		/// Checkpoint interval trade-offs:
		/// <list type="bullet">
		/// <item><description><strong>Interval = 1:</strong> Maximum resumability, more frequent I/O, higher overhead</description></item>
		/// <item><description><strong>Interval &gt; 1:</strong> Reduced overhead, less frequent resumability, larger replay window</description></item>
		/// </list>
		/// </para>
		/// <para>
		/// Choose based on:
		/// <list type="bullet">
		/// <item><description>Batch processing time (longer batches warrant more frequent checkpoints)</description></item>
		/// <item><description>Checkpoint storage cost and latency</description></item>
		/// <item><description>Acceptable replay window after resume</description></item>
		/// <item><description>System stability and expected interruption frequency</description></item>
		/// </list>
		/// </para>
		/// For migrations with batch size 10 and interval 2, checkpoints occur every 20 tenants.
		/// </remarks>
		public int CheckpointInterval { get; set; } = 1;

		/// <summary>
		/// Gets or sets the maximum number of tenants to process concurrently when parallel execution is enabled.
		/// </summary>
		/// <value>
		/// An integer representing the maximum degree of parallelism. Must be greater than 0. Defaults to 4.
		/// </value>
		/// <remarks>
		/// This setting limits concurrent tenant processing to prevent resource exhaustion.
		/// It only applies when <see cref="EnableParallelExecution"/> is <see langword="true"/>.
		/// <para>
		/// Choose the value based on:
		/// <list type="bullet">
		/// <item><description>Available CPU cores (typically 1-2x core count for I/O-bound work)</description></item>
		/// <item><description>Database connection pool size</description></item>
		/// <item><description>External API rate limits</description></item>
		/// <item><description>Memory per tenant processing</description></item>
		/// </list>
		/// </para>
		/// <para>
		/// Too high: May overwhelm system resources, exhaust connection pools, or hit rate limits.
		/// Too low: Underutilizes available resources and reduces parallelism benefits.
		/// </para>
		/// Monitor system metrics during migrations to tune this value appropriately.
		/// </remarks>
		public int MaxDegreeOfParallelism { get; set; } = 4;

		/// <summary>
		/// Gets or sets the maximum duration allowed for migrating a single tenant.
		/// </summary>
		/// <value>
		/// A <see cref="TimeSpan"/> representing the per-tenant timeout. Defaults to 5 minutes.
		/// Must be positive.
		/// </value>
		/// <remarks>
		/// The timeout prevents indefinite hangs on problematic tenants, ensuring the migration
		/// makes forward progress. When a tenant exceeds this timeout:
		/// <list type="bullet">
		/// <item><description>The operation is cancelled via <see cref="CancellationToken"/></description></item>
		/// <item><description>The tenant is marked as failed</description></item>
		/// <item><description>An error is recorded with timeout details</description></item>
		/// <item><description>Processing continues to next tenant (if <see cref="ContinueOnFailure"/> is true)</description></item>
		/// </list>
		/// <para>
		/// Set the timeout based on:
		/// <list type="bullet">
		/// <item><description>Expected maximum migration duration per tenant</description></item>
		/// <item><description>95th or 99th percentile processing times from testing</description></item>
		/// <item><description>Complexity of migration steps</description></item>
		/// <item><description>External dependencies (database, APIs) response times</description></item>
		/// </list>
		/// </para>
		/// <para>
		/// Too short: May cause false failures for legitimately slow tenants.
		/// Too long: Delays detection of hung operations and slows overall migration.
		/// </para>
		/// Monitor tenant processing times during test runs to calibrate appropriately.
		/// </remarks>
		public TimeSpan TenantTimeout { get; set; } = TimeSpan.FromMinutes(5);

		#endregion
	}
}