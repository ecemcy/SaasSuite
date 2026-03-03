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

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using SaasSuite.Core;
using SaasSuite.Migration.Base;
using SaasSuite.Migration.Enumerations;
using SaasSuite.Migration.Interfaces;
using SaasSuite.Migration.Options;

namespace SaasSuite.Migration.Implementations
{
	/// <summary>
	/// Provides the default implementation of <see cref="IMigrationEngine"/> for orchestrating multi-tenant migrations.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The migration engine coordinates complex migration operations across multiple tenants with a focus on
	/// reliability, resumability, and comprehensive error handling. It implements a batch-processing model
	/// where tenants are divided into manageable groups and processed either sequentially or in parallel.
	/// </para>
	/// <para>
	/// Key features include:
	/// <list type="bullet">
	/// <item><description>Batch processing with configurable parallelism and concurrency limits</description></item>
	/// <item><description>Checkpoint-based resumability enabling recovery from failures or interruptions</description></item>
	/// <item><description>Real-time progress tracking through <see cref="IProgress{MigrationProgress}"/> callbacks</description></item>
	/// <item><description>Comprehensive error collection with tenant-level granularity</description></item>
	/// <item><description>Per-tenant timeout enforcement to prevent indefinite hangs</description></item>
	/// <item><description>Dry-run validation mode for pre-migration testing without data modification</description></item>
	/// <item><description>Rollback capabilities for reversible migration steps</description></item>
	/// </list>
	/// </para>
	/// <para>
	/// <strong>Threading Model:</strong> The engine processes batches sequentially but can process tenants
	/// within a batch in parallel when <see cref="MigrationOptions.EnableParallelExecution"/> is enabled.
	/// All shared state (result object) is protected with locks to ensure thread safety. Migration steps
	/// are executed sequentially per tenant, with all steps completing before the next tenant begins.
	/// </para>
	/// <para>
	/// <strong>Error Handling:</strong> The engine supports two error handling strategies via
	/// <see cref="MigrationOptions.ContinueOnFailure"/>. When enabled, tenant failures are logged
	/// and migration continues with remaining tenants. When disabled, the first failure stops
	/// the entire migration immediately.
	/// </para>
	/// </remarks>
	public class MigrationEngine
		: IMigrationEngine
	{
		#region ' Fields '

		/// <summary>
		/// Logger for recording migration operations, progress, and errors.
		/// </summary>
		/// <remarks>
		/// Logging includes informational messages for migration lifecycle events (start, batch processing,
		/// completion), debug messages for individual tenant and step processing, and error messages
		/// for all failures. Log levels should be configured appropriately for production environments.
		/// </remarks>
		private readonly ILogger<MigrationEngine> _logger;

		/// <summary>
		/// Provides tenant discovery and enumeration for migrations.
		/// </summary>
		/// <remarks>
		/// The tenant provider supplies the complete list of tenants to migrate when no explicit
		/// tenant IDs are provided. Implementations typically query tenant databases or registries.
		/// </remarks>
		private readonly ITenantProvider _tenantProvider;

		/// <summary>
		/// Default migration options loaded from configuration.
		/// </summary>
		/// <remarks>
		/// These options provide defaults for batch size, parallelism, checkpointing, timeouts, and other
		/// configuration. Individual migration operations can override these by passing explicit options
		/// to the migration methods.
		/// </remarks>
		private readonly MigrationOptions _defaultOptions;

		#endregion

		#region ' Constructors '

		/// <summary>
		/// Initializes a new instance of the <see cref="MigrationEngine"/> class with required dependencies.
		/// </summary>
		/// <param name="tenantProvider">
		/// The tenant provider for retrieving tenant lists. Cannot be <see langword="null"/>.
		/// Used to enumerate all available tenants when no explicit tenant IDs are provided.
		/// </param>
		/// <param name="logger">
		/// The logger for recording migration operations. Cannot be <see langword="null"/>.
		/// Used for informational, debug, and error logging throughout the migration lifecycle.
		/// </param>
		/// <param name="options">
		/// The default migration options from configuration. Cannot be <see langword="null"/>.
		/// If the Value property is <see langword="null"/>, a new default instance is created.
		/// </param>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="tenantProvider"/> or <paramref name="logger"/> is <see langword="null"/>.
		/// </exception>
		/// <remarks>
		/// <para>
		/// This constructor is invoked by the dependency injection container when resolving
		/// <see cref="IMigrationEngine"/>. All dependencies are registered via
		/// <see cref="ServiceCollectionExtensions.AddSaasMigration"/>.
		/// </para>
		/// <para>
		/// The migration engine has a scoped lifetime by default, ensuring each migration operation
		/// gets a fresh instance with isolated state.
		/// </para>
		/// </remarks>
		public MigrationEngine(ITenantProvider tenantProvider, ILogger<MigrationEngine> logger, IOptions<MigrationOptions> options)
		{
			// Validate required dependencies
			this._tenantProvider = tenantProvider ?? throw new ArgumentNullException(nameof(tenantProvider));
			this._logger = logger ?? throw new ArgumentNullException(nameof(logger));

			// Use configured options or create defaults if not configured
			this._defaultOptions = options?.Value ?? new MigrationOptions();
		}

		#endregion

		#region ' Methods '

		/// <summary>
		/// Processes a batch of tenants either sequentially or in parallel based on migration options.
		/// </summary>
		/// <param name="batch">The list of tenant IDs in this batch to process.</param>
		/// <param name="steps">The ordered list of migration steps to execute for each tenant.</param>
		/// <param name="options">Migration options controlling parallel execution and other behavior.</param>
		/// <param name="result">Shared result object for accumulating statistics and errors (thread-safe).</param>
		/// <param name="currentBatch">The 1-based current batch number for progress reporting.</param>
		/// <param name="totalBatches">The total number of batches for progress calculation.</param>
		/// <param name="totalTenants">The total number of tenants across all batches for progress calculation.</param>
		/// <param name="progress">Optional progress callback for reporting status updates.</param>
		/// <param name="cancellationToken">Cancellation token for stopping batch processing.</param>
		/// <returns>A task representing the asynchronous batch processing operation.</returns>
		/// <remarks>
		/// <para>
		/// This method implements the parallelism control logic. When <see cref="MigrationOptions.EnableParallelExecution"/>
		/// is <see langword="true"/>, tenants are processed concurrently using <see cref="Parallel.ForEachAsync{TSource}(IEnumerable{TSource}, ParallelOptions, Func{TSource, CancellationToken, ValueTask})"/>
		/// with a maximum degree of parallelism set by <see cref="MigrationOptions.MaxDegreeOfParallelism"/>.
		/// When <see langword="false"/>, tenants are processed sequentially using a foreach loop.
		/// </para>
		/// <para>
		/// The result object is shared across all concurrent operations and is protected with locks
		/// in <see cref="ProcessTenantAsync"/> to ensure thread safety when updating counters and error lists.
		/// </para>
		/// </remarks>
		private async Task ProcessBatchAsync(List<string> batch, List<IMigrationStep> steps, MigrationOptions options, MigrationResult result, int currentBatch, int totalBatches, int totalTenants, IProgress<MigrationProgress>? progress, CancellationToken cancellationToken)
		{
			if (options.EnableParallelExecution)
			{
				// Process tenants in parallel with controlled concurrency
				ParallelOptions parallelOptions = new ParallelOptions
				{
					MaxDegreeOfParallelism = options.MaxDegreeOfParallelism,
					CancellationToken = cancellationToken
				};

				// Use Parallel.ForEachAsync for concurrent tenant processing
				// Each tenant gets its own async task up to MaxDegreeOfParallelism limit
				await Parallel.ForEachAsync(batch, parallelOptions, async (tenantId, ct) =>
				{
					await this.ProcessTenantAsync(tenantId, steps, options, result, currentBatch, totalBatches, totalTenants, progress, ct);
				});
			}
			else
			{
				// Process tenants sequentially in the order they appear in the batch
				foreach (string tenantId in batch)
				{
					await this.ProcessTenantAsync(tenantId, steps, options, result, currentBatch, totalBatches, totalTenants, progress, cancellationToken);
				}
			}
		}

		/// <summary>
		/// Processes a single tenant through all migration steps with timeout enforcement and error handling.
		/// </summary>
		/// <param name="tenantId">The unique identifier of the tenant to process.</param>
		/// <param name="steps">The ordered list of migration steps to execute sequentially.</param>
		/// <param name="options">Migration options including timeout and continue-on-failure settings.</param>
		/// <param name="result">Shared result object for thread-safe accumulation of statistics and errors.</param>
		/// <param name="currentBatch">The 1-based current batch number for progress reporting.</param>
		/// <param name="totalBatches">The total number of batches for progress calculation.</param>
		/// <param name="totalTenants">The total number of tenants for progress percentage calculation.</param>
		/// <param name="progress">Optional progress callback for reporting current tenant processing.</param>
		/// <param name="cancellationToken">Cancellation token from the outer migration operation.</param>
		/// <returns>A task representing the asynchronous tenant processing operation.</returns>
		/// <remarks>
		/// <para>
		/// This method implements the core tenant migration logic:
		/// <list type="number">
		/// <item><description>Creates a linked cancellation token combining the outer token with a timeout token</description></item>
		/// <item><description>Reports progress before starting to indicate which tenant is being processed</description></item>
		/// <item><description>Executes all migration steps sequentially using the linked token</description></item>
		/// <item><description>On success, updates the result object (thread-safe via lock)</description></item>
		/// <item><description>On failure, logs error, updates result, and rethrows if not continuing on failure</description></item>
		/// </list>
		/// </para>
		/// <para>
		/// <strong>Thread Safety:</strong> This method may be called concurrently from <see cref="ProcessBatchAsync"/>
		/// when parallel execution is enabled. All updates to the shared result object are protected with locks.
		/// </para>
		/// <para>
		/// <strong>Timeout Handling:</strong> A separate <see cref="CancellationTokenSource"/> is created with the
		/// configured timeout (<see cref="MigrationOptions.TenantTimeout"/>). If the timeout expires before all
		/// steps complete, the linked token is cancelled, causing step execution to throw <see cref="OperationCanceledException"/>.
		/// </para>
		/// </remarks>
		private async Task ProcessTenantAsync(string tenantId, List<IMigrationStep> steps, MigrationOptions options, MigrationResult result, int currentBatch, int totalBatches, int totalTenants, IProgress<MigrationProgress>? progress, CancellationToken cancellationToken)
		{
			try
			{
				this._logger.LogDebug("Processing tenant {TenantId}", tenantId);

				// Create linked cancellation token combining outer cancellation with per-tenant timeout
				// This ensures the tenant respects both the overall migration cancellation and its own timeout
				using CancellationTokenSource timeoutCts = new CancellationTokenSource(options.TenantTimeout);
				using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

				// Report progress before starting tenant to show current work
				this.ReportProgress(progress, tenantId, result, currentBatch, totalBatches, totalTenants);

				// Execute all migration steps sequentially for this tenant
				// Steps must complete in order to maintain data consistency
				foreach (IMigrationStep step in steps)
				{
					this._logger.LogDebug("Executing step {StepName} for tenant {TenantId}", step.Name, tenantId);
					await step.ExecuteAsync(tenantId, linkedCts.Token);
				}

				// Update result with successful completion
				// Use lock for thread safety when parallel execution is enabled
				lock (result)
				{
					result.AffectedTenants.Add(tenantId);
					result.SuccessfulTenants++;
					result.TotalTenantsProcessed++;
				}

				this._logger.LogDebug("Successfully processed tenant {TenantId}", tenantId);
			}
			catch (Exception ex)
			{
				this._logger.LogError(ex, "Failed to process tenant {TenantId}", tenantId);

				// Record error and update failure count
				// Use lock for thread safety when parallel execution is enabled
				lock (result)
				{
					result.Errors.Add(new MigrationError
					{
						TenantId = tenantId,
						Message = $"Failed to migrate tenant {tenantId}",
						ExceptionDetails = ex.ToString()
					});
					result.FailedTenants++;
					result.TotalTenantsProcessed++;
				}

				// Rethrow exception if fail-fast mode is enabled
				// This will bubble up and stop the entire migration
				if (!options.ContinueOnFailure)
				{
					throw;
				}
				// Otherwise, error is logged and migration continues to next tenant
			}
		}

		/// <summary>
		/// Rolls back a batch of tenants either sequentially or in parallel based on migration options.
		/// </summary>
		/// <param name="batch">The list of tenant IDs in this batch to roll back.</param>
		/// <param name="steps">The ordered list of migration steps to roll back (already in reverse order).</param>
		/// <param name="options">Migration options controlling parallel execution.</param>
		/// <param name="result">Shared result object for thread-safe accumulation of rollback results.</param>
		/// <param name="currentBatch">The 1-based current batch number for progress reporting.</param>
		/// <param name="totalBatches">The total number of batches for progress calculation.</param>
		/// <param name="totalTenants">The total number of tenants for progress calculation.</param>
		/// <param name="progress">Optional progress callback for reporting rollback status.</param>
		/// <param name="cancellationToken">Cancellation token for stopping batch rollback.</param>
		/// <returns>A task representing the asynchronous batch rollback operation.</returns>
		/// <remarks>
		/// This method is the rollback equivalent of <see cref="ProcessBatchAsync"/>, implementing
		/// the same parallelism control logic but calling <see cref="RollbackTenantAsync"/> instead
		/// of <see cref="ProcessTenantAsync"/>. The steps are already in reverse order when passed in.
		/// </remarks>
		private async Task RollbackBatchAsync(List<string> batch, List<IMigrationStep> steps, MigrationOptions options, MigrationResult result, int currentBatch, int totalBatches, int totalTenants, IProgress<MigrationProgress>? progress, CancellationToken cancellationToken)
		{
			if (options.EnableParallelExecution)
			{
				ParallelOptions parallelOptions = new ParallelOptions
				{
					MaxDegreeOfParallelism = options.MaxDegreeOfParallelism,
					CancellationToken = cancellationToken
				};

				await Parallel.ForEachAsync(batch, parallelOptions, async (tenantId, ct) =>
				{
					await this.RollbackTenantAsync(tenantId, steps, result, currentBatch, totalBatches, totalTenants, progress, ct);
				});
			}
			else
			{
				foreach (string tenantId in batch)
				{
					await this.RollbackTenantAsync(tenantId, steps, result, currentBatch, totalBatches, totalTenants, progress, cancellationToken);
				}
			}
		}

		/// <summary>
		/// Rolls back a single tenant by calling RollbackAsync on all steps in reverse order.
		/// </summary>
		/// <param name="tenantId">The unique identifier of the tenant to roll back.</param>
		/// <param name="steps">The list of migration steps in reverse order (last executed first).</param>
		/// <param name="result">Shared result object for thread-safe accumulation of rollback results.</param>
		/// <param name="currentBatch">The 1-based current batch number for progress reporting.</param>
		/// <param name="totalBatches">The total number of batches for progress calculation.</param>
		/// <param name="totalTenants">The total number of tenants for progress percentage calculation.</param>
		/// <param name="progress">Optional progress callback for reporting current tenant rollback.</param>
		/// <param name="cancellationToken">Cancellation token for stopping rollback.</param>
		/// <returns>A task representing the asynchronous tenant rollback operation.</returns>
		/// <remarks>
		/// <para>
		/// This method rolls back changes for a single tenant by calling <see cref="IMigrationStep.RollbackAsync"/>
		/// on each step in reverse order. Before calling rollback, it checks if the step supports rollback
		/// by testing if it's a <see cref="MigrationStepBase"/> with <see cref="MigrationStepBase.SupportsRollback"/>
		/// set to <see langword="false"/>. Steps that don't support rollback are skipped with a warning.
		/// </para>
		/// <para>
		/// Rollback steps should:
		/// <list type="bullet">
		/// <item><description>Restore the exact pre-migration state</description></item>
		/// <item><description>Be idempotent (safe to run multiple times)</description></item>
		/// <item><description>Handle partial migration scenarios gracefully</description></item>
		/// <item><description>Use transactions to ensure atomicity</description></item>
		/// </list>
		/// </para>
		/// </remarks>
		private async Task RollbackTenantAsync(string tenantId, List<IMigrationStep> steps, MigrationResult result, int currentBatch, int totalBatches, int totalTenants, IProgress<MigrationProgress>? progress, CancellationToken cancellationToken)
		{
			try
			{
				this._logger.LogDebug("Rolling back tenant {TenantId}", tenantId);

				this.ReportProgress(progress, tenantId, result, currentBatch, totalBatches, totalTenants);

				// Rollback steps in reverse order (already reversed by caller)
				foreach (IMigrationStep step in steps)
				{
					// Check if step supports rollback by testing for MigrationStepBase
					// Steps that don't support rollback are skipped to avoid errors
					if (step is MigrationStepBase stepBase && !stepBase.SupportsRollback)
					{
						this._logger.LogWarning("Skipping rollback for step {StepName} (rollback not supported) for tenant {TenantId}", step.Name, tenantId);
						continue;
					}

					this._logger.LogDebug("Rolling back step {StepName} for tenant {TenantId}", step.Name, tenantId);
					await step.RollbackAsync(tenantId, cancellationToken);
				}

				// Mark as successful rollback (thread-safe)
				lock (result)
				{
					result.AffectedTenants.Add(tenantId);
					result.SuccessfulTenants++;
					result.TotalTenantsProcessed++;
				}

				this._logger.LogDebug("Successfully rolled back tenant {TenantId}", tenantId);
			}
			catch (Exception ex)
			{
				this._logger.LogError(ex, "Failed to roll back tenant {TenantId}", tenantId);

				// Record rollback failure (thread-safe)
				lock (result)
				{
					result.Errors.Add(new MigrationError
					{
						TenantId = tenantId,
						Message = $"Rollback failed for tenant {tenantId}",
						ExceptionDetails = ex.ToString()
					});
					result.FailedTenants++;
					result.TotalTenantsProcessed++;
				}
				// Note: We don't rethrow to ensure all tenants are attempted
				// Even in rollback, we typically want to try all tenants
			}
		}

		/// <summary>
		/// Validates a batch of tenants either sequentially or in parallel based on migration options.
		/// </summary>
		/// <param name="batch">The list of tenant IDs in this batch to validate.</param>
		/// <param name="steps">The ordered list of migration steps to validate for each tenant.</param>
		/// <param name="options">Migration options controlling parallel execution.</param>
		/// <param name="result">Shared result object for thread-safe accumulation of validation results.</param>
		/// <param name="currentBatch">The 1-based current batch number for progress reporting.</param>
		/// <param name="totalBatches">The total number of batches for progress calculation.</param>
		/// <param name="totalTenants">The total number of tenants for progress calculation.</param>
		/// <param name="progress">Optional progress callback for reporting validation status.</param>
		/// <param name="cancellationToken">Cancellation token for stopping batch validation.</param>
		/// <returns>A task representing the asynchronous batch validation operation.</returns>
		/// <remarks>
		/// This method is the validation equivalent of <see cref="ProcessBatchAsync"/>, implementing
		/// the same parallelism control logic but calling <see cref="ValidateTenantAsync"/> instead
		/// of <see cref="ProcessTenantAsync"/>. It does not modify any data.
		/// </remarks>
		private async Task ValidateBatchAsync(List<string> batch, List<IMigrationStep> steps, MigrationOptions options, MigrationResult result, int currentBatch, int totalBatches, int totalTenants, IProgress<MigrationProgress>? progress, CancellationToken cancellationToken)
		{
			if (options.EnableParallelExecution)
			{
				ParallelOptions parallelOptions = new ParallelOptions
				{
					MaxDegreeOfParallelism = options.MaxDegreeOfParallelism,
					CancellationToken = cancellationToken
				};

				await Parallel.ForEachAsync(batch, parallelOptions, async (tenantId, ct) =>
				{
					await this.ValidateTenantAsync(tenantId, steps, result, currentBatch, totalBatches, totalTenants, progress, ct);
				});
			}
			else
			{
				foreach (string tenantId in batch)
				{
					await this.ValidateTenantAsync(tenantId, steps, result, currentBatch, totalBatches, totalTenants, progress, cancellationToken);
				}
			}
		}

		/// <summary>
		/// Validates a single tenant by calling ValidateAsync on all migration steps without executing them.
		/// </summary>
		/// <param name="tenantId">The unique identifier of the tenant to validate.</param>
		/// <param name="steps">The ordered list of migration steps to validate sequentially.</param>
		/// <param name="result">Shared result object for thread-safe accumulation of validation results.</param>
		/// <param name="currentBatch">The 1-based current batch number for progress reporting.</param>
		/// <param name="totalBatches">The total number of batches for progress calculation.</param>
		/// <param name="totalTenants">The total number of tenants for progress percentage calculation.</param>
		/// <param name="progress">Optional progress callback for reporting current tenant validation.</param>
		/// <param name="cancellationToken">Cancellation token for stopping validation.</param>
		/// <returns>A task representing the asynchronous tenant validation operation.</returns>
		/// <remarks>
		/// <para>
		/// This method validates tenant prerequisites by calling <see cref="IMigrationStep.ValidateAsync"/>
		/// for each step. If any step returns <see langword="false"/> or throws an exception, the tenant
		/// is marked as failed validation and the error is recorded.
		/// </para>
		/// <para>
		/// Validation checks typically include:
		/// <list type="bullet">
		/// <item><description>Verifying required schema or configuration is in place</description></item>
		/// <item><description>Checking that prerequisite data exists</description></item>
		/// <item><description>Ensuring resources (connections, disk space) are available</description></item>
		/// <item><description>Confirming tenant is in an appropriate state for migration</description></item>
		/// </list>
		/// </para>
		/// </remarks>
		private async Task ValidateTenantAsync(string tenantId, List<IMigrationStep> steps, MigrationResult result, int currentBatch, int totalBatches, int totalTenants, IProgress<MigrationProgress>? progress, CancellationToken cancellationToken)
		{
			try
			{
				this._logger.LogDebug("Validating tenant {TenantId}", tenantId);

				this.ReportProgress(progress, tenantId, result, currentBatch, totalBatches, totalTenants);

				// Validate each step without executing
				// All steps must return true for the tenant to be considered valid
				foreach (IMigrationStep step in steps)
				{
					this._logger.LogDebug("Validating step {StepName} for tenant {TenantId}", step.Name, tenantId);
					bool isValid = await step.ValidateAsync(tenantId, cancellationToken);

					if (!isValid)
					{
						throw new InvalidOperationException($"Validation failed for step {step.Name}");
					}
				}

				// Mark as successful validation (thread-safe)
				lock (result)
				{
					result.AffectedTenants.Add(tenantId);
					result.SuccessfulTenants++;
					result.TotalTenantsProcessed++;
				}

				this._logger.LogDebug("Successfully validated tenant {TenantId}", tenantId);
			}
			catch (Exception ex)
			{
				this._logger.LogError(ex, "Failed to validate tenant {TenantId}", tenantId);

				// Record validation failure (thread-safe)
				lock (result)
				{
					result.Errors.Add(new MigrationError
					{
						TenantId = tenantId,
						Message = $"Validation failed for tenant {tenantId}",
						ExceptionDetails = ex.ToString()
					});
					result.FailedTenants++;
					result.TotalTenantsProcessed++;
				}
				// Note: Unlike ProcessTenantAsync, we don't rethrow here
				// Validation always continues to check all tenants
			}
		}

		/// <summary>
		/// Resolves the list of tenants to process from explicit IDs or the tenant provider.
		/// </summary>
		/// <param name="tenantIds">
		/// Optional explicit tenant IDs. If not <see langword="null"/>, these IDs are returned directly.
		/// If <see langword="null"/>, all tenants are retrieved from <see cref="_tenantProvider"/>.
		/// </param>
		/// <param name="cancellationToken">
		/// Cancellation token for the asynchronous tenant provider call.
		/// </param>
		/// <returns>
		/// A task representing the asynchronous operation. The task result contains a list of tenant ID strings
		/// to be processed by the migration.
		/// </returns>
		/// <remarks>
		/// This method provides a single point for tenant resolution, simplifying the logic in the main
		/// migration methods. When explicit IDs are provided, they are used as-is. When not provided,
		/// all tenants are retrieved from the tenant provider, which may query a database or registry.
		/// </remarks>
		private async Task<List<string>> GetTenantsAsync(IEnumerable<string>? tenantIds, CancellationToken cancellationToken)
		{
			// Use explicit tenant IDs if provided (fast path)
			if (tenantIds != null)
			{
				return tenantIds.ToList();
			}

			// Otherwise, get all tenants from provider (may involve database queries)
			IEnumerable<TenantInfo> allTenants = await this._tenantProvider.GetAllTenantsAsync(cancellationToken);
			return allTenants.Select(t => t.Id.Value).ToList();
		}

		/// <summary>
		/// Reports progress to the provided callback if progress reporting is enabled and a callback is provided.
		/// </summary>
		/// <param name="progress">
		/// Optional progress callback. If <see langword="null"/>, no progress is reported.
		/// </param>
		/// <param name="tenantId">
		/// The ID of the tenant currently being processed, for inclusion in the progress report.
		/// </param>
		/// <param name="result">
		/// The current result object containing processed tenant count for percentage calculation.
		/// </param>
		/// <param name="currentBatch">
		/// The 1-based current batch number for progress reporting.
		/// </param>
		/// <param name="totalBatches">
		/// The total number of batches for progress context.
		/// </param>
		/// <param name="totalTenants">
		/// The total number of tenants across all batches for percentage calculation.
		/// </param>
		/// <remarks>
		/// <para>
		/// This method creates a <see cref="MigrationProgress"/> object containing current status information
		/// and reports it through the <see cref="IProgress{T}"/> callback. Progress includes:
		/// <list type="bullet">
		/// <item><description>Total tenant count</description></item>
		/// <item><description>Currently processed tenant count</description></item>
		/// <item><description>Current and total batch numbers</description></item>
		/// <item><description>Currently processing tenant ID</description></item>
		/// <item><description>Human-readable status message</description></item>
		/// <item><description>Calculated completion percentage (via <see cref="MigrationProgress.PercentComplete"/>)</description></item>
		/// </list>
		/// </para>
		/// <para>
		/// Progress reporting is typically used for:
		/// <list type="bullet">
		/// <item><description>Updating user interfaces with migration status</description></item>
		/// <item><description>Real-time monitoring dashboards</description></item>
		/// <item><description>Logging detailed progress for diagnostics</description></item>
		/// <item><description>Estimating remaining time</description></item>
		/// </list>
		/// </para>
		/// </remarks>
		private void ReportProgress(IProgress<MigrationProgress>? progress, string tenantId, MigrationResult result, int currentBatch, int totalBatches, int totalTenants)
		{
			// Skip if no callback provided
			if (progress == null)
			{
				return;
			}

			// Create progress report with current state
			MigrationProgress progressInfo = new MigrationProgress
			{
				TotalTenants = totalTenants,
				CurrentBatch = currentBatch,
				TotalBatches = totalBatches,
				ProcessedTenants = result.TotalTenantsProcessed,
				CurrentTenantId = tenantId,
				Message = $"Processing tenant {tenantId} in batch {currentBatch}/{totalBatches}"
			};

			// Report to callback (PercentComplete is calculated automatically by the property)
			progress.Report(progressInfo);
		}

		/// <summary>
		/// Divides the tenant list into batches of the specified size using sequential partitioning.
		/// </summary>
		/// <param name="tenants">
		/// The complete list of tenant IDs to be divided into batches.
		/// The list is not modified by this method.
		/// </param>
		/// <param name="batchSize">
		/// The maximum number of tenants per batch. Must be greater than 0.
		/// The last batch may contain fewer tenants if the total count is not evenly divisible.
		/// </param>
		/// <returns>
		/// A list of batches, where each batch is a list of tenant IDs. Batches are created in sequence,
		/// so tenants appear in the same order as the input list.
		/// </returns>
		/// <remarks>
		/// <para>
		/// This method implements simple sequential batching using LINQ's Skip and Take operations.
		/// Batches are created by taking slices of the input list at regular intervals.
		/// </para>
		/// <para>
		/// For example, with 25 tenants and batchSize=10:
		/// <list type="bullet">
		/// <item><description>Batch 1: tenants 0-9 (10 tenants)</description></item>
		/// <item><description>Batch 2: tenants 10-19 (10 tenants)</description></item>
		/// <item><description>Batch 3: tenants 20-24 (5 tenants)</description></item>
		/// </list>
		/// </para>
		/// <para>
		/// Batching benefits include:
		/// <list type="bullet">
		/// <item><description>Controlled memory usage by processing subsets at a time</description></item>
		/// <item><description>Natural boundaries for checkpoint creation</description></item>
		/// <item><description>Easier progress tracking and estimation</description></item>
		/// <item><description>Bounded blast radius if issues occur</description></item>
		/// </list>
		/// </para>
		/// </remarks>
		private List<List<string>> CreateBatches(List<string> tenants, int batchSize)
		{
			List<List<string>> batches = new List<List<string>>();

			// Iterate through tenant list in batch-sized chunks
			for (int i = 0; i < tenants.Count; i += batchSize)
			{
				// Take up to batchSize tenants starting at position i
				// Last batch may be smaller if tenant count is not evenly divisible
				batches.Add(tenants.Skip(i).Take(batchSize).ToList());
			}

			return batches;
		}

		/// <summary>
		/// Creates a checkpoint capturing the current migration state for resumability.
		/// </summary>
		/// <param name="result">
		/// The current migration result containing lists of completed and failed tenants.
		/// </param>
		/// <param name="currentBatch">
		/// The 1-based current batch number that was just completed.
		/// </param>
		/// <returns>
		/// A new <see cref="MigrationCheckpoint"/> object containing:
		/// <list type="bullet">
		/// <item><description>A unique migration ID (new GUID)</description></item>
		/// <item><description>The current batch number</description></item>
		/// <item><description>Copy of completed tenant IDs</description></item>
		/// <item><description>List of failed tenant IDs extracted from errors</description></item>
		/// <item><description>Current UTC timestamp</description></item>
		/// </list>
		/// </returns>
		/// <remarks>
		/// <para>
		/// Checkpoints enable resumption of interrupted or failed migrations. They capture enough state
		/// to skip already-completed tenants and resume from the next batch when <see cref="ExecuteAsync"/>
		/// is called again with the checkpoint.
		/// </para>
		/// <para>
		/// Checkpoints should be:
		/// <list type="bullet">
		/// <item><description>Persisted to durable storage (database, file system) immediately after creation</description></item>
		/// <item><description>Associated with the specific migration operation for tracking</description></item>
		/// <item><description>Expired or cleaned up after successful migration completion</description></item>
		/// <item><description>Validated for staleness before resumption (check timestamp)</description></item>
		/// </list>
		/// </para>
		/// <para>
		/// Note: A new GUID is generated for each checkpoint. In production systems, you may want to
		/// maintain a consistent migration ID across checkpoints by passing it through metadata or
		/// modifying this method to accept a migration ID parameter.
		/// </para>
		/// </remarks>
		private MigrationCheckpoint CreateCheckpoint(MigrationResult result, int currentBatch)
		{
			return new MigrationCheckpoint
			{
				// Generate unique ID for this checkpoint
				MigrationId = Guid.NewGuid().ToString(),

				// Record which batch was just completed
				CurrentBatch = currentBatch,

				// Copy completed tenant list (defensive copy to prevent external modification)
				CompletedTenantIds = new List<string>(result.AffectedTenants),

				// Extract failed tenant IDs from error records
				FailedTenantIds = result.Errors.Where(e => e.TenantId != null)
					.Select(e => e.TenantId!)
					.ToList(),

				// Record checkpoint creation time
				Timestamp = DateTimeOffset.UtcNow
			};
		}

		/// <summary>
		/// Asynchronously performs a dry run of migration steps to validate prerequisites without modifying data.
		/// </summary>
		/// <param name="migrationSteps">
		/// The ordered collection of migration steps to validate. Cannot be <see langword="null"/> or empty.
		/// The <see cref="IMigrationStep.ValidateAsync"/> method is called for each step instead of <see cref="IMigrationStep.ExecuteAsync"/>.
		/// </param>
		/// <param name="tenantIds">
		/// Optional collection of tenant identifiers to validate. If <see langword="null"/>,
		/// all tenants from <see cref="ITenantProvider"/> are validated.
		/// </param>
		/// <param name="options">
		/// Optional migration options controlling validation behavior. If <see langword="null"/>,
		/// uses the default options. Parallelism and batching settings apply to validation.
		/// </param>
		/// <param name="progress">
		/// Optional progress callback for receiving real-time validation updates.
		/// Reports completion percentage and current tenant being validated.
		/// </param>
		/// <param name="cancellationToken">
		/// Cancellation token for stopping the dry run. When cancelled, validation stops gracefully
		/// after completing the current tenant.
		/// </param>
		/// <returns>
		/// A task representing the asynchronous operation. The task result contains a <see cref="MigrationResult"/>
		/// with status <see cref="MigrationStatus.DryRunCompleted"/> for success or <see cref="MigrationStatus.Failed"/>
		/// for validation failures. Errors include details about which tenants or steps failed validation.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="migrationSteps"/> is <see langword="null"/>.
		/// </exception>
		/// <exception cref="InvalidOperationException">
		/// Thrown when <paramref name="migrationSteps"/> is an empty collection.
		/// </exception>
		/// <remarks>
		/// <para>
		/// A dry run executes the <see cref="IMigrationStep.ValidateAsync"/> method for each step without
		/// calling <see cref="IMigrationStep.ExecuteAsync"/>. This allows detection of issues before actual
		/// migration, such as missing prerequisites, invalid states, or resource constraints.
		/// </para>
		/// <para>
		/// Common validation checks include:
		/// <list type="bullet">
		/// <item><description>Verifying required schema changes are already in place</description></item>
		/// <item><description>Checking that prerequisite data exists</description></item>
		/// <item><description>Ensuring sufficient resources (disk space, memory, connections)</description></item>
		/// <item><description>Validating tenant configuration or feature flags</description></item>
		/// <item><description>Confirming external dependencies are accessible</description></item>
		/// </list>
		/// </para>
		/// <para>
		/// <strong>Best Practice:</strong> Always run a dry run before executing actual migrations in production,
		/// especially when migrating large numbers of tenants. This catches configuration errors and validation
		/// failures early without risk of data modification.
		/// </para>
		/// <para>
		/// Note that successful validation does not guarantee actual migration will succeed, as runtime
		/// conditions may change between validation and execution. However, it significantly reduces
		/// the risk of widespread failures.
		/// </para>
		/// </remarks>
		public async Task<MigrationResult> DryRunAsync(IEnumerable<IMigrationStep> migrationSteps, IEnumerable<string>? tenantIds = null, MigrationOptions? options = null, IProgress<MigrationProgress>? progress = null, CancellationToken cancellationToken = default)
		{
			MigrationOptions effectiveOptions = options ?? this._defaultOptions;
			DateTimeOffset startTime = DateTimeOffset.UtcNow;
			MigrationResult result = new MigrationResult
			{
				Status = MigrationStatus.Running
			};

			try
			{
				this._logger.LogInformation("Starting migration dry run");

				List<string> tenants = await this.GetTenantsAsync(tenantIds, cancellationToken);
				List<IMigrationStep> steps = migrationSteps.ToList();

				if (steps.Count == 0)
				{
					throw new InvalidOperationException("No migration steps provided");
				}

				// Initialize counters
				result.TotalTenantsProcessed = 0;
				result.SuccessfulTenants = 0;
				result.FailedTenants = 0;

				List<List<string>> batches = this.CreateBatches(tenants, effectiveOptions.BatchSize);
				int totalBatches = batches.Count;
				int totalTenants = tenants.Count;

				// Validate each batch
				for (int batchIndex = 0; batchIndex < batches.Count; batchIndex++)
				{
					List<string> batch = batches[batchIndex];
					this._logger.LogInformation("Validating batch {Current}/{Total}", batchIndex + 1, totalBatches);

					// Validate tenants by calling ValidateAsync instead of ExecuteAsync
					await this.ValidateBatchAsync(
						batch,
						steps,
						effectiveOptions,
						result,
						batchIndex + 1,
						totalBatches,
						totalTenants,
						progress,
						cancellationToken);
				}

				// Set appropriate status based on validation results
				result.Status = result.FailedTenants > 0 ? MigrationStatus.Failed : MigrationStatus.DryRunCompleted;
				result.Duration = DateTimeOffset.UtcNow - startTime;

				this._logger.LogInformation(
					"Dry run completed. Status: {Status}, Valid: {Success}, Invalid: {Failed}",
					result.Status,
					result.SuccessfulTenants,
					result.FailedTenants);

				return result;
			}
			catch (Exception ex)
			{
				this._logger.LogError(ex, "Dry run failed");
				result.Status = MigrationStatus.Failed;
				result.Errors.Add(new MigrationError
				{
					Message = "Dry run failed",
					ExceptionDetails = ex.ToString()
				});
				result.Duration = DateTimeOffset.UtcNow - startTime;
				return result;
			}
		}

		/// <summary>
		/// Asynchronously executes migration steps for specified tenants with comprehensive error handling and resumability.
		/// </summary>
		/// <param name="migrationSteps">
		/// The ordered collection of migration steps to execute. Cannot be <see langword="null"/> or empty.
		/// Steps are executed in the order provided, sequentially for each tenant.
		/// </param>
		/// <param name="tenantIds">
		/// Optional collection of tenant identifiers to migrate. If <see langword="null"/>, all tenants
		/// from <see cref="ITenantProvider"/> are migrated. If provided, only specified tenants are processed.
		/// </param>
		/// <param name="options">
		/// Optional migration options controlling execution behavior. If <see langword="null"/>,
		/// uses the default options configured during service registration.
		/// </param>
		/// <param name="checkpoint">
		/// Optional checkpoint to resume from a previous migration run. If provided, tenants in
		/// <see cref="MigrationCheckpoint.CompletedTenantIds"/> are skipped, and processing resumes
		/// from <see cref="MigrationCheckpoint.CurrentBatch"/>.
		/// </param>
		/// <param name="progress">
		/// Optional progress callback for receiving real-time updates. Called after processing each tenant
		/// with current status, batch information, and completion percentage.
		/// </param>
		/// <param name="cancellationToken">
		/// Cancellation token for gracefully stopping the migration. When cancelled, the current batch
		/// completes and a checkpoint is created (if enabled) before returning.
		/// </param>
		/// <returns>
		/// A task representing the asynchronous operation. The task result contains a <see cref="MigrationResult"/>
		/// with execution statistics, affected tenants, errors, duration, and an optional checkpoint for resumption.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="migrationSteps"/> is <see langword="null"/>.
		/// </exception>
		/// <exception cref="InvalidOperationException">
		/// Thrown when <paramref name="migrationSteps"/> is an empty collection.
		/// </exception>
		/// <remarks>
		/// <para>
		/// This method implements the complete migration workflow:
		/// <list type="number">
		/// <item><description>Resolves tenant list from explicit IDs or tenant provider</description></item>
		/// <item><description>Filters out completed tenants if resuming from a checkpoint</description></item>
		/// <item><description>Divides remaining tenants into batches based on configured batch size</description></item>
		/// <item><description>Processes each batch sequentially (batches are never parallelized)</description></item>
		/// <item><description>Within each batch, processes tenants sequentially or in parallel based on options</description></item>
		/// <item><description>For each tenant, executes all migration steps in order</description></item>
		/// <item><description>Creates checkpoints at configured intervals for resumability</description></item>
		/// <item><description>Collects errors and continues or stops based on failure policy</description></item>
		/// <item><description>Returns comprehensive result with statistics and optional checkpoint</description></item>
		/// </list>
		/// </para>
		/// <para>
		/// <strong>Idempotency:</strong> The migration is idempotent if all migration steps implement
		/// idempotent logic. The checkpoint mechanism ensures already-migrated tenants are skipped on resume.
		/// </para>
		/// <para>
		/// <strong>Parallelism:</strong> When <see cref="MigrationOptions.EnableParallelExecution"/> is enabled,
		/// tenants within a single batch are processed concurrently up to <see cref="MigrationOptions.MaxDegreeOfParallelism"/>.
		/// Batches themselves are always processed sequentially to maintain checkpoint consistency.
		/// </para>
		/// <para>
		/// <strong>Timeout Handling:</strong> Each tenant is subject to <see cref="MigrationOptions.TenantTimeout"/>.
		/// If exceeded, the tenant is cancelled, marked as failed, and processing continues to the next tenant
		/// (if <see cref="MigrationOptions.ContinueOnFailure"/> is enabled).
		/// </para>
		/// </remarks>
		public async Task<MigrationResult> ExecuteAsync(IEnumerable<IMigrationStep> migrationSteps, IEnumerable<string>? tenantIds = null, MigrationOptions? options = null, MigrationCheckpoint? checkpoint = null, IProgress<MigrationProgress>? progress = null, CancellationToken cancellationToken = default)
		{
			// Use provided options or fall back to configured defaults
			MigrationOptions effectiveOptions = options ?? this._defaultOptions;

			// Record start time for duration calculation
			DateTimeOffset startTime = DateTimeOffset.UtcNow;

			// Initialize result object with running status
			MigrationResult result = new MigrationResult
			{
				Status = MigrationStatus.Running
			};

			try
			{
				this._logger.LogInformation("Starting migration execution");

				// Resolve tenant list from explicit IDs or provider
				List<string> tenants = await this.GetTenantsAsync(tenantIds, cancellationToken);
				List<IMigrationStep> steps = migrationSteps.ToList();

				// Validate that at least one migration step was provided
				if (steps.Count == 0)
				{
					throw new InvalidOperationException("No migration steps provided");
				}

				// Filter out already completed tenants if resuming from checkpoint
				// This ensures idempotency by skipping tenants that have already been migrated
				if (checkpoint != null)
				{
					tenants = tenants.Where(t => !checkpoint.CompletedTenantIds.Contains(t)).ToList();
					this._logger.LogInformation("Resuming from checkpoint. Remaining tenants: {Count}", tenants.Count);
				}

				// Initialize result counters to zero
				result.TotalTenantsProcessed = 0;
				result.SuccessfulTenants = 0;
				result.FailedTenants = 0;

				// Divide tenants into batches based on configured batch size
				// Batching provides natural checkpointing boundaries and controls resource usage
				List<List<string>> batches = this.CreateBatches(tenants, effectiveOptions.BatchSize);

				// Determine starting batch (0-based internally, but 1-based for logging)
				int currentBatch = checkpoint?.CurrentBatch ?? 0;
				int totalBatches = batches.Count;
				int totalTenants = tenants.Count;

				// Process each batch sequentially
				// Note: Batches are never parallelized to maintain checkpoint consistency
				foreach (List<string>? batch in batches.Skip(currentBatch))
				{
					// Increment batch counter (becomes 1-based for human-readable logging)
					currentBatch++;
					this._logger.LogInformation("Processing batch {Current}/{Total}", currentBatch, totalBatches);

					// Process all tenants in the current batch (may be parallel based on options)
					await this.ProcessBatchAsync(
						batch,
						steps,
						effectiveOptions,
						result,
						currentBatch,
						totalBatches,
						totalTenants,
						progress,
						cancellationToken);

					// Create checkpoint at configured intervals if checkpointing is enabled
					// This allows resumption from this point if the migration is interrupted
					if (effectiveOptions.EnableCheckpointing && currentBatch % effectiveOptions.CheckpointInterval == 0)
					{
						result.Checkpoint = this.CreateCheckpoint(result, currentBatch);
						this._logger.LogDebug("Checkpoint created at batch {Batch}", currentBatch);
					}
				}

				// Determine final status based on failure count
				// Migration is considered failed if any tenants failed, even with continue-on-failure
				result.Status = result.FailedTenants > 0 ? MigrationStatus.Failed : MigrationStatus.Completed;
				result.Duration = DateTimeOffset.UtcNow - startTime;

				this._logger.LogInformation(
					"Migration completed. Status: {Status}, Successful: {Success}, Failed: {Failed}",
					result.Status,
					result.SuccessfulTenants,
					result.FailedTenants);

				return result;
			}
			catch (Exception ex)
			{
				// Catch-all for unexpected exceptions during migration orchestration
				// This handles exceptions from the framework itself, not from individual tenant migrations
				this._logger.LogError(ex, "Migration execution failed");

				result.Status = MigrationStatus.Failed;
				result.Errors.Add(new MigrationError
				{
					Message = "Migration execution failed",
					ExceptionDetails = ex.ToString()
				});
				result.Duration = DateTimeOffset.UtcNow - startTime;

				return result;
			}
		}

		/// <summary>
		/// Asynchronously rolls back migration steps for specified tenants in reverse order.
		/// </summary>
		/// <param name="migrationSteps">
		/// The ordered collection of migration steps to roll back. Cannot be <see langword="null"/> or empty.
		/// Steps are executed in reverse order (last to first) to properly undo changes.
		/// Steps that don't support rollback (where <see cref="MigrationStepBase.SupportsRollback"/> is <see langword="false"/>)
		/// are skipped with a warning.
		/// </param>
		/// <param name="tenantIds">
		/// The collection of tenant identifiers to roll back. Cannot be <see langword="null"/> or empty.
		/// Unlike execute operations, explicit tenant specification is required for safety.
		/// </param>
		/// <param name="options">
		/// Optional migration options controlling rollback behavior. If <see langword="null"/>,
		/// uses the default options. Parallelism and batching settings apply to rollback.
		/// </param>
		/// <param name="progress">
		/// Optional progress callback for receiving real-time rollback updates.
		/// Reports completion percentage and current tenant being rolled back.
		/// </param>
		/// <param name="cancellationToken">
		/// Cancellation token for stopping the rollback. When cancelled, rollback stops gracefully
		/// after completing the current tenant, potentially leaving some tenants in a rolled-back state
		/// and others in the migrated state.
		/// </param>
		/// <returns>
		/// A task representing the asynchronous operation. The task result contains a <see cref="MigrationResult"/>
		/// with status <see cref="MigrationStatus.RolledBack"/> for success or <see cref="MigrationStatus.Failed"/>
		/// if any tenants failed to roll back. The result includes the list of successfully rolled-back tenants
		/// and any errors encountered.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="migrationSteps"/> or <paramref name="tenantIds"/> is <see langword="null"/>.
		/// </exception>
		/// <exception cref="InvalidOperationException">
		/// Thrown when <paramref name="migrationSteps"/> is an empty collection.
		/// </exception>
		/// <remarks>
		/// <para>
		/// Rollback executes migration steps in reverse order (from last to first), calling
		/// <see cref="IMigrationStep.RollbackAsync"/> for each step. This reverses changes made during
		/// the forward migration. Steps are checked for rollback support via <see cref="MigrationStepBase.SupportsRollback"/>
		/// before attempting rollback.
		/// </para>
		/// <para>
		/// <strong>Important Considerations:</strong>
		/// <list type="bullet">
		/// <item><description>Not all operations can be safely rolled back (e.g., data aggregations, external integrations)</description></item>
		/// <item><description>Rollback steps must restore the exact pre-migration state</description></item>
		/// <item><description>Partial migrations may result in incomplete rollback if some steps don't support it</description></item>
		/// <item><description>Steps that don't support rollback are skipped with a warning logged</description></item>
		/// <item><description>Rollback should be thoroughly tested before use in production</description></item>
		/// </list>
		/// </para>
		/// <para>
		/// Rollback is typically used for:
		/// <list type="bullet">
		/// <item><description>Recovering from failed migrations that left the system in an inconsistent state</description></item>
		/// <item><description>Reverting changes that caused unexpected issues in production</description></item>
		/// <item><description>Testing migration and rollback procedures in non-production environments</description></item>
		/// <item><description>Implementing blue-green deployment rollback strategies</description></item>
		/// </list>
		/// </para>
		/// <para>
		/// <strong>Safety Note:</strong> Explicit tenant IDs are required (no default to all tenants) to prevent
		/// accidental large-scale rollbacks. This forces operators to consciously specify which tenants to roll back.
		/// </para>
		/// </remarks>
		public async Task<MigrationResult> RollbackAsync(IEnumerable<IMigrationStep> migrationSteps, IEnumerable<string> tenantIds, MigrationOptions? options = null, IProgress<MigrationProgress>? progress = null, CancellationToken cancellationToken = default)
		{
			MigrationOptions effectiveOptions = options ?? this._defaultOptions;
			DateTimeOffset startTime = DateTimeOffset.UtcNow;
			MigrationResult result = new MigrationResult
			{
				Status = MigrationStatus.Running
			};

			try
			{
				this._logger.LogInformation("Starting migration rollback for {Count} tenants", tenantIds.Count());

				// Reverse step order for rollback (last executed first)
				// This ensures changes are undone in the opposite order they were applied
				List<IMigrationStep> steps = migrationSteps.Reverse().ToList();
				List<string> tenants = tenantIds.ToList();

				result.TotalTenantsProcessed = 0;
				result.SuccessfulTenants = 0;
				result.FailedTenants = 0;

				List<List<string>> batches = this.CreateBatches(tenants, effectiveOptions.BatchSize);
				int totalBatches = batches.Count;
				int totalTenants = tenants.Count;

				// Roll back each batch
				for (int batchIndex = 0; batchIndex < batches.Count; batchIndex++)
				{
					List<string> batch = batches[batchIndex];
					this._logger.LogInformation("Rolling back batch {Current}/{Total}", batchIndex + 1, totalBatches);

					await this.RollbackBatchAsync(
						batch,
						steps,
						effectiveOptions,
						result,
						batchIndex + 1,
						totalBatches,
						totalTenants,
						progress,
						cancellationToken);
				}

				result.Status = result.FailedTenants > 0 ? MigrationStatus.Failed : MigrationStatus.RolledBack;
				result.Duration = DateTimeOffset.UtcNow - startTime;

				this._logger.LogInformation(
					"Rollback completed. Status: {Status}, Successful: {Success}, Failed: {Failed}",
					result.Status,
					result.SuccessfulTenants,
					result.FailedTenants);

				return result;
			}
			catch (Exception ex)
			{
				this._logger.LogError(ex, "Rollback failed");
				result.Status = MigrationStatus.Failed;
				result.Errors.Add(new MigrationError
				{
					Message = "Rollback failed",
					ExceptionDetails = ex.ToString()
				});
				result.Duration = DateTimeOffset.UtcNow - startTime;
				return result;
			}
		}

		#endregion
	}
}