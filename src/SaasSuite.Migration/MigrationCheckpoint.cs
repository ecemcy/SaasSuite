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

namespace SaasSuite.Migration
{
	/// <summary>
	/// Represents a checkpoint that enables resumption of interrupted or failed migration operations.
	/// </summary>
	/// <remarks>
	/// Checkpoints capture the state of a migration at specific intervals, allowing the migration
	/// to be resumed from the point of failure rather than starting over. This is critical for
	/// large-scale migrations affecting many tenants where failures or interruptions may occur.
	/// Checkpoints can be serialized and stored for later retrieval.
	/// </remarks>
	public class MigrationCheckpoint
	{
		#region ' Properties '

		/// <summary>
		/// Gets or sets the current batch number being processed.
		/// </summary>
		/// <value>
		/// An integer representing which batch (1-based) the migration was processing when
		/// the checkpoint was created. Defaults to 0 (not started).
		/// </value>
		/// <remarks>
		/// The batch number indicates progress through the migration when tenants are processed
		/// in batches. When resuming from a checkpoint, batches up to (but not including) this
		/// number have been completed. Batches starting from this number should be processed.
		/// For example, if CurrentBatch is 3, batches 1 and 2 are complete, and processing
		/// should resume with batch 3.
		/// </remarks>
		public int CurrentBatch { get; set; }

		/// <summary>
		/// Gets or sets the unique identifier of the migration operation.
		/// </summary>
		/// <value>
		/// A string uniquely identifying this migration instance. Defaults to an empty string.
		/// Should be set to a unique value (e.g., GUID) when creating checkpoints.
		/// </value>
		/// <remarks>
		/// The migration ID links checkpoints to specific migration runs, allowing multiple
		/// migrations to be tracked independently. This is useful when:
		/// <list type="bullet">
		/// <item><description>Multiple migration operations run concurrently</description></item>
		/// <item><description>Different migration workflows need separate tracking</description></item>
		/// <item><description>Historical migration data needs to be queried or audited</description></item>
		/// </list>
		/// Generate a new GUID for each distinct migration operation to ensure uniqueness.
		/// </remarks>
		public string MigrationId { get; set; } = string.Empty;

		/// <summary>
		/// Gets or sets the UTC timestamp when the checkpoint was created.
		/// </summary>
		/// <value>
		/// A <see cref="DateTimeOffset"/> in UTC indicating when this checkpoint was captured.
		/// Defaults to the current UTC time.
		/// </value>
		/// <remarks>
		/// The timestamp provides temporal context for the checkpoint, useful for:
		/// <list type="bullet">
		/// <item><description>Determining how stale a checkpoint is before resuming</description></item>
		/// <item><description>Auditing migration duration and performance</description></item>
		/// <item><description>Implementing checkpoint expiration policies</description></item>
		/// <item><description>Correlating checkpoints with system logs and metrics</description></item>
		/// </list>
		/// Old checkpoints may be invalid if the system state has changed significantly
		/// since creation (e.g., schema changes, tenant onboarding/offboarding).
		/// </remarks>
		public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

		/// <summary>
		/// Gets or sets additional metadata associated with the checkpoint.
		/// </summary>
		/// <value>
		/// A dictionary of key-value pairs containing custom metadata. Defaults to an empty dictionary.
		/// Cannot be <see langword="null"/>.
		/// </value>
		/// <remarks>
		/// Metadata enables extensibility without modifying the checkpoint schema. Common uses include:
		/// <list type="bullet">
		/// <item><description>Migration step identifiers or versions</description></item>
		/// <item><description>Configuration snapshots (options used during migration)</description></item>
		/// <item><description>Performance metrics (average tenant processing time)</description></item>
		/// <item><description>Environment information (server, region, deployment version)</description></item>
		/// <item><description>User or system that initiated the migration</description></item>
		/// <item><description>Reason for checkpoint creation (scheduled, error, manual)</description></item>
		/// </list>
		/// Metadata values are stored as objects to support different data types. Ensure proper
		/// serialization support for complex types if persisting checkpoints to storage.
		/// </remarks>
		public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();

		/// <summary>
		/// Gets or sets the list of tenant identifiers that have been successfully migrated.
		/// </summary>
		/// <value>
		/// A list of string tenant IDs representing tenants that completed migration successfully.
		/// Defaults to an empty list. Cannot be <see langword="null"/>.
		/// </value>
		/// <remarks>
		/// This list is used when resuming a migration to skip tenants that have already been
		/// processed successfully. The migration engine filters out these tenant IDs before
		/// starting processing, avoiding duplicate work and ensuring idempotency.
		/// <para>
		/// The list grows incrementally as tenants complete successfully. After resuming from
		/// a checkpoint, this list continues to accumulate newly completed tenants.
		/// </para>
		/// </remarks>
		public List<string> CompletedTenantIds { get; set; } = new List<string>();

		/// <summary>
		/// Gets or sets the list of tenant identifiers that failed migration.
		/// </summary>
		/// <value>
		/// A list of string tenant IDs representing tenants that encountered errors during migration.
		/// Defaults to an empty list. Cannot be <see langword="null"/>.
		/// </value>
		/// <remarks>
		/// Failed tenant IDs are tracked separately to enable targeted retry strategies.
		/// When resuming a migration, you can choose to:
		/// <list type="bullet">
		/// <item><description>Skip failed tenants (exclude from the retry)</description></item>
		/// <item><description>Retry only failed tenants (use FailedTenantIds as tenantIds parameter)</description></item>
		/// <item><description>Include failed tenants in the full retry (filter them out)</description></item>
		/// </list>
		/// Failed tenants are not automatically retried during resumption; explicit logic is
		/// required to determine retry behavior based on the failure reason and context.
		/// </remarks>
		public List<string> FailedTenantIds { get; set; } = new List<string>();

		#endregion
	}
}