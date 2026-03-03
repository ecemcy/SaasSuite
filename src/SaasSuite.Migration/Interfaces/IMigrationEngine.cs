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

using SaasSuite.Migration.Base;
using SaasSuite.Migration.Enumerations;
using SaasSuite.Migration.Options;

namespace SaasSuite.Migration.Interfaces
{
	/// <summary>
	/// Defines the contract for orchestrating multi-tenant migration operations.
	/// </summary>
	/// <remarks>
	/// The migration engine coordinates the execution of migration steps across multiple tenants,
	/// providing support for batching, parallel execution, progress tracking, checkpointing,
	/// and rollback operations. It handles error management and allows resumption of failed migrations.
	/// </remarks>
	public interface IMigrationEngine
	{
		#region ' Methods '

		/// <summary>
		/// Asynchronously performs a dry run (validation-only) of migration steps without modifying data.
		/// </summary>
		/// <param name="migrationSteps">
		/// The ordered collection of migration steps to validate. Cannot be <see langword="null"/> or empty.
		/// </param>
		/// <param name="tenantIds">
		/// Optional collection of tenant identifiers to validate. If <see langword="null"/>,
		/// all tenants from <see cref="ITenantProvider"/> are validated.
		/// </param>
		/// <param name="options">
		/// Optional migration options controlling validation behavior. If <see langword="null"/>,
		/// default options are used.
		/// </param>
		/// <param name="progress">
		/// Optional progress callback for real-time validation status updates.
		/// </param>
		/// <param name="cancellationToken">
		/// A token to monitor for cancellation requests.
		/// </param>
		/// <returns>
		/// A task that represents the asynchronous operation. The task result contains a
		/// <see cref="MigrationResult"/> indicating validation success or failure for each tenant.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="migrationSteps"/> is <see langword="null"/>.
		/// </exception>
		/// <exception cref="InvalidOperationException">
		/// Thrown when <paramref name="migrationSteps"/> is empty.
		/// </exception>
		/// <remarks>
		/// <para>
		/// A dry run executes the <see cref="IMigrationStep.ValidateAsync"/> method for each step
		/// without calling <see cref="IMigrationStep.ExecuteAsync"/>. This allows detection of issues
		/// before actual migration, such as:
		/// <list type="bullet">
		/// <item><description>Missing prerequisites or schema dependencies</description></item>
		/// <item><description>Insufficient resources or permissions</description></item>
		/// <item><description>Invalid tenant states or data</description></item>
		/// <item><description>Configuration errors</description></item>
		/// </list>
		/// </para>
		/// <para>
		/// The dry run result uses <see cref="MigrationStatus.DryRunCompleted"/> status for successful
		/// validation and <see cref="MigrationStatus.Failed"/> for validation failures. Error details
		/// are included for tenants that fail validation.
		/// </para>
		/// <para>
		/// Best practice is to run a dry run before executing actual migrations, especially in
		/// production environments or when migrating large numbers of tenants.
		/// </para>
		/// </remarks>
		Task<MigrationResult> DryRunAsync(IEnumerable<IMigrationStep> migrationSteps, IEnumerable<string>? tenantIds = null, MigrationOptions? options = null, IProgress<MigrationProgress>? progress = null, CancellationToken cancellationToken = default);

		/// <summary>
		/// Asynchronously executes migration steps for specified tenants.
		/// </summary>
		/// <param name="migrationSteps">
		/// The ordered collection of migration steps to execute. Cannot be <see langword="null"/> or empty.
		/// Steps are executed in the order provided.
		/// </param>
		/// <param name="tenantIds">
		/// Optional collection of tenant identifiers to migrate. If <see langword="null"/>,
		/// all tenants from <see cref="ITenantProvider"/> are migrated.
		/// </param>
		/// <param name="options">
		/// Optional migration options controlling execution behavior. If <see langword="null"/>,
		/// default options are used from the configured <see cref="MigrationOptions"/>.
		/// </param>
		/// <param name="checkpoint">
		/// Optional checkpoint to resume a previous migration. If provided, already completed
		/// tenants are skipped. This enables resumption after failures.
		/// </param>
		/// <param name="progress">
		/// Optional progress callback for real-time status updates. Reports include current
		/// tenant, batch progress, and completion percentage.
		/// </param>
		/// <param name="cancellationToken">
		/// A token to monitor for cancellation requests.
		/// Allows graceful cancellation of long-running migrations.
		/// </param>
		/// <returns>
		/// A task that represents the asynchronous operation. The task result contains a
		/// <see cref="MigrationResult"/> with execution statistics, affected tenants, errors,
		/// and optional checkpoint for resumption.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="migrationSteps"/> is <see langword="null"/>.
		/// </exception>
		/// <exception cref="InvalidOperationException">
		/// Thrown when <paramref name="migrationSteps"/> is empty.
		/// </exception>
		/// <remarks>
		/// <para>
		/// The execution process follows these steps:
		/// <list type="number">
		/// <item><description>Resolves tenant list from <paramref name="tenantIds"/> or <see cref="ITenantProvider"/></description></item>
		/// <item><description>Filters out tenants completed in the checkpoint (if resuming)</description></item>
		/// <item><description>Divides tenants into batches based on <see cref="MigrationOptions.BatchSize"/></description></item>
		/// <item><description>Processes each batch sequentially or in parallel</description></item>
		/// <item><description>For each tenant, executes all migration steps in order</description></item>
		/// <item><description>Creates checkpoints at configured intervals</description></item>
		/// <item><description>Collects errors and continues based on <see cref="MigrationOptions.ContinueOnFailure"/></description></item>
		/// <item><description>Returns comprehensive result with statistics and errors</description></item>
		/// </list>
		/// </para>
		/// <para>
		/// Behavior is controlled by <see cref="MigrationOptions"/>:
		/// <list type="bullet">
		/// <item><description><see cref="MigrationOptions.BatchSize"/>: Number of tenants per batch</description></item>
		/// <item><description><see cref="MigrationOptions.EnableParallelExecution"/>: Whether to process tenants in parallel within batches</description></item>
		/// <item><description><see cref="MigrationOptions.MaxDegreeOfParallelism"/>: Maximum concurrent tenant migrations</description></item>
		/// <item><description><see cref="MigrationOptions.TenantTimeout"/>: Maximum time per tenant migration</description></item>
		/// <item><description><see cref="MigrationOptions.ContinueOnFailure"/>: Whether to continue after tenant failures</description></item>
		/// <item><description><see cref="MigrationOptions.EnableCheckpointing"/>: Whether to create resumption checkpoints</description></item>
		/// </list>
		/// </para>
		/// <para>
		/// The migration is idempotent if all migration steps are idempotent. Failed migrations
		/// can be resumed using the checkpoint from the result.
		/// </para>
		/// </remarks>
		Task<MigrationResult> ExecuteAsync(IEnumerable<IMigrationStep> migrationSteps, IEnumerable<string>? tenantIds = null, MigrationOptions? options = null, MigrationCheckpoint? checkpoint = null, IProgress<MigrationProgress>? progress = null, CancellationToken cancellationToken = default);

		/// <summary>
		/// Asynchronously rolls back migration steps for specified tenants.
		/// </summary>
		/// <param name="migrationSteps">
		/// The ordered collection of migration steps to roll back. Cannot be <see langword="null"/> or empty.
		/// Steps are rolled back in reverse order.
		/// </param>
		/// <param name="tenantIds">
		/// The collection of tenant identifiers to roll back. Cannot be <see langword="null"/> or empty.
		/// Unlike execute operations, this cannot be null as explicit tenant selection is required for rollback.
		/// </param>
		/// <param name="options">
		/// Optional migration options controlling rollback behavior. If <see langword="null"/>,
		/// default options are used.
		/// </param>
		/// <param name="progress">
		/// Optional progress callback for real-time rollback status updates.
		/// </param>
		/// <param name="cancellationToken">
		/// A token to monitor for cancellation requests.
		/// </param>
		/// <returns>
		/// A task that represents the asynchronous operation. The task result contains a
		/// <see cref="MigrationResult"/> with <see cref="MigrationStatus.RolledBack"/> status
		/// if successful, or <see cref="MigrationStatus.Failed"/> if rollback errors occurred.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="migrationSteps"/> or <paramref name="tenantIds"/> is <see langword="null"/>.
		/// </exception>
		/// <exception cref="InvalidOperationException">
		/// Thrown when <paramref name="migrationSteps"/> is empty.
		/// </exception>
		/// <remarks>
		/// <para>
		/// Rollback executes migration steps in reverse order, calling <see cref="IMigrationStep.RollbackAsync"/>
		/// for each step. Steps that don't support rollback (where <see cref="MigrationStepBase.SupportsRollback"/>
		/// is <see langword="false"/>) are skipped with a warning logged.
		/// </para>
		/// <para>
		/// Important considerations:
		/// <list type="bullet">
		/// <item><description>Not all operations can be safely rolled back (e.g., data aggregations, external integrations)</description></item>
		/// <item><description>Rollback steps must restore the exact pre-migration state</description></item>
		/// <item><description>Partial migrations may result in incomplete rollback if some steps don't support it</description></item>
		/// <item><description>Rollback should be tested thoroughly before use in production</description></item>
		/// </list>
		/// </para>
		/// <para>
		/// Rollback is typically used for:
		/// <list type="bullet">
		/// <item><description>Recovering from failed migrations</description></item>
		/// <item><description>Reverting changes that caused unexpected issues</description></item>
		/// <item><description>Testing migration and rollback procedures</description></item>
		/// </list>
		/// </para>
		/// </remarks>
		Task<MigrationResult> RollbackAsync(IEnumerable<IMigrationStep> migrationSteps, IEnumerable<string> tenantIds, MigrationOptions? options = null, IProgress<MigrationProgress>? progress = null, CancellationToken cancellationToken = default);

		#endregion
	}
}