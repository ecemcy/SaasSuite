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
	/// Represents real-time progress information for an active migration operation.
	/// </summary>
	/// <remarks>
	/// Progress updates are reported through <see cref="IProgress{T}"/> during migration execution,
	/// providing visibility into current status for monitoring, UI updates, and logging.
	/// Progress reports are typically generated after processing each tenant or batch.
	/// </remarks>
	public class MigrationProgress
	{
		#region ' Properties '

		/// <summary>
		/// Gets the percentage of migration completion.
		/// </summary>
		/// <value>
		/// A double representing completion percentage from 0 to 100.
		/// Returns 0 if <see cref="TotalTenants"/> is 0.
		/// </value>
		/// <remarks>
		/// This computed property provides a convenient completion metric for:
		/// <list type="bullet">
		/// <item><description>Progress bars and visual indicators</description></item>
		/// <item><description>Estimated time remaining calculations</description></item>
		/// <item><description>High-level status summaries</description></item>
		/// </list>
		/// The calculation uses double precision to avoid integer truncation, providing
		/// accurate percentages even for large tenant counts.
		/// </remarks>
		public double PercentComplete => this.TotalTenants > 0 ? (double)this.ProcessedTenants / this.TotalTenants * 100 : 0;

		/// <summary>
		/// Gets or sets the current batch number being processed.
		/// </summary>
		/// <value>
		/// An integer (1-based) indicating which batch is currently being processed.
		/// </value>
		/// <remarks>
		/// Batch numbers start at 1 and increment sequentially. This is useful for:
		/// <list type="bullet">
		/// <item><description>Understanding migration pacing and batching strategy</description></item>
		/// <item><description>Correlating with checkpoint creation intervals</description></item>
		/// <item><description>Estimating remaining time based on batch processing rate</description></item>
		/// </list>
		/// </remarks>
		public int CurrentBatch { get; set; }

		/// <summary>
		/// Gets or sets the number of tenants that have been processed so far.
		/// </summary>
		/// <value>
		/// An integer count of tenants completed, including both successful and failed migrations.
		/// </value>
		/// <remarks>
		/// This value increments as each tenant completes (successfully or with errors).
		/// Compare with <see cref="TotalTenants"/> to determine remaining work.
		/// The completion percentage is calculated as (ProcessedTenants / TotalTenants) * 100.
		/// </remarks>
		public int ProcessedTenants { get; set; }

		/// <summary>
		/// Gets or sets the total number of batches in the migration.
		/// </summary>
		/// <value>
		/// An integer representing how many batches the migration is divided into.
		/// </value>
		/// <remarks>
		/// The total batch count is calculated as ceiling(TotalTenants / BatchSize) at the
		/// start of migration. Compare with <see cref="CurrentBatch"/> to show batch progress
		/// (e.g., "Batch 3 of 10").
		/// </remarks>
		public int TotalBatches { get; set; }

		/// <summary>
		/// Gets or sets the total number of tenants included in the migration.
		/// </summary>
		/// <value>
		/// An integer representing the complete count of tenants to be migrated.
		/// </value>
		/// <remarks>
		/// This value remains constant throughout the migration and is used to calculate
		/// completion percentage. It represents the total scope of work, not the number
		/// of tenants remaining.
		/// </remarks>
		public int TotalTenants { get; set; }

		/// <summary>
		/// Gets or sets the tenant identifier currently being processed.
		/// </summary>
		/// <value>
		/// A string containing the current tenant ID, or <see langword="null"/> if no specific
		/// tenant is being processed (e.g., between batches).
		/// </value>
		/// <remarks>
		/// This provides granular visibility into migration progress, allowing monitoring systems
		/// to track exactly which tenant is being worked on. Useful for:
		/// <list type="bullet">
		/// <item><description>Real-time status displays and progress bars</description></item>
		/// <item><description>Debugging stuck or slow migrations</description></item>
		/// <item><description>Identifying problematic tenants</description></item>
		/// <item><description>Detailed operational logging</description></item>
		/// </list>
		/// </remarks>
		public string? CurrentTenantId { get; set; }

		/// <summary>
		/// Gets or sets an additional descriptive message about the current progress.
		/// </summary>
		/// <value>
		/// A string containing context-specific progress information, or <see langword="null"/>
		/// if no additional message is needed.
		/// </value>
		/// <remarks>
		/// The message provides human-readable context about what's happening, such as:
		/// <list type="bullet">
		/// <item><description>"Processing tenant {id} in batch {n}/{total}"</description></item>
		/// <item><description>"Waiting for batch completion..."</description></item>
		/// <item><description>"Creating checkpoint..."</description></item>
		/// <item><description>"Rolling back tenant {id}..."</description></item>
		/// </list>
		/// Messages are typically displayed in logs, console output, or user interfaces
		/// to provide operational visibility beyond numeric progress indicators.
		/// </remarks>
		public string? Message { get; set; }

		#endregion
	}
}