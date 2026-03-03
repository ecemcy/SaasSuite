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

using SaasSuite.Migration.Interfaces;

namespace SaasSuite.Migration.Enumerations
{
	/// <summary>
	/// Represents the current status of a migration operation.
	/// </summary>
	/// <remarks>
	/// The status indicates where the migration is in its lifecycle, from initial state through
	/// completion or failure. This enumeration is used in <see cref="MigrationResult"/> and
	/// <see cref="MigrationProgress"/> to track and report migration state.
	/// </remarks>
	public enum MigrationStatus
	{
		/// <summary>
		/// The migration has not yet started.
		/// </summary>
		/// <remarks>
		/// This is the initial state before any migration operations have been initiated.
		/// A migration in this state has no associated progress, errors, or affected tenants.
		/// </remarks>
		NotStarted = 0,

		/// <summary>
		/// The migration is currently executing.
		/// </summary>
		/// <remarks>
		/// This status indicates the migration is actively processing tenants. During this state:
		/// <list type="bullet">
		/// <item><description>Migration steps are being executed for one or more tenants</description></item>
		/// <item><description>Progress is being tracked and reported</description></item>
		/// <item><description>The operation can be cancelled via CancellationToken</description></item>
		/// </list>
		/// The status will transition to Completed, Failed, or RolledBack when execution finishes.
		/// </remarks>
		Running = 1,

		/// <summary>
		/// The migration completed successfully for all tenants.
		/// </summary>
		/// <remarks>
		/// This status indicates that all migration steps executed successfully for all processed tenants.
		/// No errors occurred, and all tenants are in the desired post-migration state.
		/// The <see cref="MigrationResult"/> will contain the list of affected tenants and statistics.
		/// </remarks>
		Completed = 2,

		/// <summary>
		/// The migration failed for one or more tenants.
		/// </summary>
		/// <remarks>
		/// This status indicates that at least one tenant failed to migrate successfully.
		/// The <see cref="MigrationResult"/> will contain:
		/// <list type="bullet">
		/// <item><description>A list of errors with tenant IDs and details</description></item>
		/// <item><description>Counts of successful vs. failed tenants</description></item>
		/// <item><description>Optionally, a checkpoint for resuming the migration</description></item>
		/// </list>
		/// Depending on <see cref="Options.MigrationOptions.ContinueOnFailure"/>, the migration
		/// may have processed additional tenants after the first failure.
		/// </remarks>
		Failed = 3,

		/// <summary>
		/// The migration was rolled back after execution.
		/// </summary>
		/// <remarks>
		/// This status indicates that a rollback operation was executed to reverse migration changes.
		/// Rollback may occur:
		/// <list type="bullet">
		/// <item><description>Manually via <see cref="IMigrationEngine.RollbackAsync"/></description></item>
		/// <item><description>Automatically if configured (not in v1)</description></item>
		/// </list>
		/// The <see cref="MigrationResult"/> will contain information about which tenants were
		/// rolled back and whether the rollback was successful for all of them.
		/// </remarks>
		RolledBack = 4,

		/// <summary>
		/// A dry run (validation-only) completed successfully.
		/// </summary>
		/// <remarks>
		/// This status indicates that a dry run executed without errors. During a dry run:
		/// <list type="bullet">
		/// <item><description>All migration steps were validated but not executed</description></item>
		/// <item><description>No data was modified</description></item>
		/// <item><description>Validation logic identified no issues that would prevent migration</description></item>
		/// </list>
		/// A successful dry run does not guarantee actual migration will succeed, but it
		/// provides confidence that prerequisites and validation checks pass.
		/// </remarks>
		DryRunCompleted = 5
	}
}