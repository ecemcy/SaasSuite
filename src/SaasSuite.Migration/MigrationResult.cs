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

using SaasSuite.Migration.Enumerations;
using SaasSuite.Migration.Interfaces;
using SaasSuite.Migration.Options;

namespace SaasSuite.Migration
{
	/// <summary>
	/// Represents the complete result of a migration operation including status, statistics, and errors.
	/// </summary>
	/// <remarks>
	/// The migration result provides comprehensive information about what happened during a migration,
	/// including which tenants were affected, what errors occurred, and whether the operation succeeded.
	/// Results can be analyzed, logged, or used to determine next steps like retry or rollback.
	/// </remarks>
	public class MigrationResult
	{
		#region ' Properties '

		/// <summary>
		/// Gets a value indicating whether the migration was successful.
		/// </summary>
		/// <value>
		/// <see langword="true"/> if the status is <see cref="MigrationStatus.Completed"/> or
		/// <see cref="MigrationStatus.DryRunCompleted"/>; otherwise, <see langword="false"/>.
		/// </value>
		/// <remarks>
		/// This computed property provides a simple boolean success indicator for scenarios
		/// where checking the specific status enum is unnecessary. It considers both
		/// actual migrations and dry runs as successful outcomes.
		/// For more granular status checking, use the <see cref="Status"/> property directly.
		/// </remarks>
		public bool IsSuccess => this.Status == MigrationStatus.Completed || this.Status == MigrationStatus.DryRunCompleted;

		/// <summary>
		/// Gets or sets the number of tenants that failed migration.
		/// </summary>
		/// <value>
		/// An integer count of tenants that encountered errors during migration.
		/// </value>
		/// <remarks>
		/// Failed tenants are those that threw exceptions or returned error states during
		/// migration step execution. This count should match the number of unique tenant IDs
		/// in <see cref="Errors"/> (though one tenant may have multiple error records).
		/// A non-zero value indicates the migration was not fully successful and may require
		/// investigation and retry.
		/// </remarks>
		public int FailedTenants { get; set; }

		/// <summary>
		/// Gets or sets the number of tenants that completed migration successfully.
		/// </summary>
		/// <value>
		/// An integer count of tenants that finished all migration steps without errors.
		/// </value>
		/// <remarks>
		/// This count matches the length of <see cref="AffectedTenants"/> and represents
		/// the successful portion of the migration scope. Compare with <see cref="FailedTenants"/>
		/// to understand success rate.
		/// </remarks>
		public int SuccessfulTenants { get; set; }

		/// <summary>
		/// Gets or sets the total number of tenants processed during the migration.
		/// </summary>
		/// <value>
		/// An integer count of all tenants that were attempted, including both successful and failed.
		/// </value>
		/// <remarks>
		/// This should equal <see cref="SuccessfulTenants"/> + <see cref="FailedTenants"/>.
		/// The count includes all tenants attempted during this operation, not counting
		/// tenants skipped via checkpoints or filtering.
		/// </remarks>
		public int TotalTenantsProcessed { get; set; }

		/// <summary>
		/// Gets or sets additional metadata associated with the migration result.
		/// </summary>
		/// <value>
		/// A dictionary of key-value pairs containing custom metadata. Defaults to an empty dictionary.
		/// Cannot be <see langword="null"/>.
		/// </value>
		/// <remarks>
		/// Metadata enables extensibility for tracking custom metrics or context, such as:
		/// <list type="bullet">
		/// <item><description>Migration step identifiers or versions executed</description></item>
		/// <item><description>Performance metrics (average per-tenant duration, throughput)</description></item>
		/// <item><description>Environment details (server, region, deployment version)</description></item>
		/// <item><description>Configuration snapshots (options used)</description></item>
		/// <item><description>Business context (initiated by, reason, approval ticket)</description></item>
		/// </list>
		/// Values are stored as objects to support different data types. Ensure proper
		/// serialization for complex types if persisting results.
		/// </remarks>
		public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();

		/// <summary>
		/// Gets or sets the list of tenant identifiers that were successfully migrated.
		/// </summary>
		/// <value>
		/// A list of string tenant IDs representing tenants that completed migration without errors.
		/// Defaults to an empty list. Cannot be <see langword="null"/>.
		/// </value>
		/// <remarks>
		/// This list contains tenants that completed all migration steps successfully.
		/// It does not include tenants that failed midway through or were skipped.
		/// Use this list for:
		/// <list type="bullet">
		/// <item><description>Confirming which tenants are now on the new version</description></item>
		/// <item><description>Auditing migration scope and coverage</description></item>
		/// <item><description>Excluding successfully migrated tenants from retries</description></item>
		/// <item><description>Generating completion reports</description></item>
		/// </list>
		/// </remarks>
		public List<string> AffectedTenants { get; set; } = new List<string>();

		/// <summary>
		/// Gets or sets the list of errors that occurred during migration.
		/// </summary>
		/// <value>
		/// A list of <see cref="MigrationError"/> objects detailing each failure.
		/// Defaults to an empty list. Cannot be <see langword="null"/>.
		/// </value>
		/// <remarks>
		/// Errors include information about which tenant failed, what went wrong, and when.
		/// Multiple errors per tenant may exist if retries occurred or multiple steps failed.
		/// Use this list for:
		/// <list type="bullet">
		/// <item><description>Identifying tenants requiring manual intervention</description></item>
		/// <item><description>Diagnosing systemic vs. tenant-specific issues</description></item>
		/// <item><description>Generating failure reports for operations teams</description></item>
		/// <item><description>Determining retry strategies</description></item>
		/// </list>
		/// An empty list indicates no errors occurred (successful migration or dry run).
		/// </remarks>
		public List<MigrationError> Errors { get; set; } = new List<MigrationError>();

		/// <summary>
		/// Gets or sets the checkpoint for resuming the migration if it was interrupted.
		/// </summary>
		/// <value>
		/// A <see cref="MigrationCheckpoint"/> object containing resumption state,
		/// or <see langword="null"/> if checkpointing is disabled or the migration completed.
		/// </value>
		/// <remarks>
		/// The checkpoint is created at configured intervals during execution (see
		/// <see cref="MigrationOptions.CheckpointInterval"/>). It contains:
		/// <list type="bullet">
		/// <item><description>Successfully completed tenant IDs</description></item>
		/// <item><description>Failed tenant IDs</description></item>
		/// <item><description>Current batch number</description></item>
		/// <item><description>Timestamp and metadata</description></item>
		/// </list>
		/// Pass the checkpoint to <see cref="IMigrationEngine.ExecuteAsync"/> to resume.
		/// Checkpoints should be persisted to durable storage for reliability.
		/// </remarks>
		public MigrationCheckpoint? Checkpoint { get; set; }

		/// <summary>
		/// Gets or sets the final status of the migration operation.
		/// </summary>
		/// <value>
		/// A <see cref="MigrationStatus"/> enumeration value indicating the outcome.
		/// </value>
		/// <remarks>
		/// The status provides a high-level summary of the migration result:
		/// <list type="bullet">
		/// <item><description><see cref="MigrationStatus.Completed"/>: All tenants migrated successfully</description></item>
		/// <item><description><see cref="MigrationStatus.Failed"/>: One or more tenants failed</description></item>
		/// <item><description><see cref="MigrationStatus.RolledBack"/>: Migration was reversed</description></item>
		/// <item><description><see cref="MigrationStatus.DryRunCompleted"/>: Validation succeeded</description></item>
		/// </list>
		/// Use <see cref="IsSuccess"/> for a simple success/failure check.
		/// </remarks>
		public MigrationStatus Status { get; set; }

		/// <summary>
		/// Gets or sets the total duration of the migration operation.
		/// </summary>
		/// <value>
		/// A <see cref="TimeSpan"/> representing how long the migration took from start to finish.
		/// </value>
		/// <remarks>
		/// The duration includes all processing time: tenant migrations, batching delays,
		/// checkpoint creation, and error handling. Use for:
		/// <list type="bullet">
		/// <item><description>Performance analysis and optimization</description></item>
		/// <item><description>Capacity planning for future migrations</description></item>
		/// <item><description>Identifying bottlenecks or slow tenants</description></item>
		/// <item><description>SLA compliance verification</description></item>
		/// </list>
		/// Calculate average per-tenant time by dividing by <see cref="TotalTenantsProcessed"/>.
		/// </remarks>
		public TimeSpan Duration { get; set; }

		#endregion
	}
}