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

namespace SaasSuite.Migration.Base
{
	/// <summary>
	/// Provides a base implementation of <see cref="IMigrationStep"/> with default behavior for common scenarios.
	/// </summary>
	/// <remarks>
	/// This abstract base class simplifies migration step implementation by providing sensible defaults
	/// for validation and rollback operations. Derived classes only need to implement the <see cref="Name"/>
	/// property and <see cref="ExecuteAsync"/> method for basic migration steps. Override additional
	/// methods and properties for advanced scenarios like validation and rollback support.
	/// </remarks>
	public abstract class MigrationStepBase
		: IMigrationStep
	{
		#region ' Abstract Properties '

		/// <summary>
		/// Gets the unique name of the migration step.
		/// </summary>
		/// <value>
		/// A string that uniquely identifies this migration step for logging and tracking purposes.
		/// Cannot be <see langword="null"/> or empty.
		/// </value>
		/// <remarks>
		/// The name should be descriptive and unique across all migration steps in the system.
		/// It is used in logs, progress reports, and error messages. Good naming conventions include:
		/// <list type="bullet">
		/// <item><description>Version-based: "Migration_v1_0_AddUserTable"</description></item>
		/// <item><description>Date-based: "Migration_20240115_UpdateSchema"</description></item>
		/// <item><description>Descriptive: "AddEmailVerificationColumn"</description></item>
		/// </list>
		/// </remarks>
		public abstract string Name { get; }

		#endregion

		#region ' Virtual Properties '

		/// <summary>
		/// Gets a value indicating whether this migration step supports rollback operations.
		/// </summary>
		/// <value>
		/// <see langword="true"/> if the step implements meaningful rollback logic; otherwise, <see langword="false"/>.
		/// Defaults to <see langword="false"/>.
		/// </value>
		/// <remarks>
		/// <para>
		/// Override this property and return <see langword="true"/> if the migration step implements
		/// <see cref="RollbackAsync"/> with actual rollback logic. When <see langword="false"/>,
		/// the migration engine may skip this step during rollback operations or log a warning.
		/// </para>
		/// <para>
		/// Not all migration steps can be safely rolled back. Examples of non-rollbackable operations include:
		/// <list type="bullet">
		/// <item><description>Data aggregation or irreversible calculations</description></item>
		/// <item><description>External system integrations that don't support undo</description></item>
		/// <item><description>Operations that destroy data without backup</description></item>
		/// </list>
		/// </para>
		/// </remarks>
		public virtual bool SupportsRollback => false;

		/// <summary>
		/// Gets a human-readable description of what this migration step does.
		/// </summary>
		/// <value>
		/// A string describing the migration step's purpose and actions. Defaults to <see cref="Name"/>.
		/// </value>
		/// <remarks>
		/// Override this property to provide more detailed information about the migration step
		/// for documentation, UI displays, or detailed logging. The default implementation returns
		/// the <see cref="Name"/> property value, which is sufficient for simple scenarios.
		/// </remarks>
		public virtual string Description => this.Name;

		#endregion

		#region ' Abstract Methods '

		/// <summary>
		/// Asynchronously executes the migration step for a specific tenant.
		/// </summary>
		/// <param name="tenantId">The unique identifier of the tenant to migrate. Cannot be <see langword="null"/> or whitespace.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
		/// <returns>A task that represents the asynchronous operation.</returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="tenantId"/> is <see langword="null"/> or whitespace.
		/// </exception>
		/// <remarks>
		/// This is the core method that performs the actual migration work for a single tenant.
		/// Implementations should:
		/// <list type="bullet">
		/// <item><description>Be idempotent (safe to run multiple times)</description></item>
		/// <item><description>Handle the cancellation token appropriately</description></item>
		/// <item><description>Log progress and errors for diagnostics</description></item>
		/// <item><description>Use transactions where appropriate to ensure atomicity</description></item>
		/// <item><description>Clean up resources properly (use using statements)</description></item>
		/// </list>
		/// The method will be called once per tenant during the migration process.
		/// </remarks>
		public abstract Task ExecuteAsync(string tenantId, CancellationToken cancellationToken = default);

		#endregion

		#region ' Virtual Methods '

		/// <summary>
		/// Asynchronously rolls back the migration step for a specific tenant.
		/// </summary>
		/// <param name="tenantId">The unique identifier of the tenant to roll back. Cannot be <see langword="null"/> or whitespace.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
		/// <returns>A task that represents the asynchronous operation.</returns>
		/// <remarks>
		/// <para>
		/// The default implementation does nothing (no-op). Override this method and set
		/// <see cref="SupportsRollback"/> to <see langword="true"/> if the migration step can be reversed.
		/// </para>
		/// <para>
		/// Rollback implementations should:
		/// <list type="bullet">
		/// <item><description>Restore the system to the state before <see cref="ExecuteAsync"/> was called</description></item>
		/// <item><description>Be idempotent (safe to run multiple times)</description></item>
		/// <item><description>Handle cases where the migration was partially completed</description></item>
		/// <item><description>Use transactions to ensure atomicity</description></item>
		/// <item><description>Log rollback progress and any issues</description></item>
		/// </list>
		/// </para>
		/// <para>
		/// The migration engine checks <see cref="SupportsRollback"/> before calling this method.
		/// If rollback is not supported, the engine may skip the step or log a warning.
		/// </para>
		/// </remarks>
		public virtual Task RollbackAsync(string tenantId, CancellationToken cancellationToken = default)
		{
			// Default implementation is a no-op
			// Override this method and set SupportsRollback to true if rollback logic is needed
			return Task.CompletedTask;
		}

		/// <summary>
		/// Asynchronously validates whether the migration step can be safely executed for a specific tenant.
		/// </summary>
		/// <param name="tenantId">The unique identifier of the tenant to validate. Cannot be <see langword="null"/> or whitespace.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
		/// <returns>
		/// A task that represents the asynchronous operation. The task result is <see langword="true"/> if
		/// the migration can proceed safely; otherwise, <see langword="false"/>.
		/// </returns>
		/// <remarks>
		/// <para>
		/// The default implementation always returns <see langword="true"/>, assuming no validation is needed.
		/// Override this method to implement pre-migration validation checks such as:
		/// <list type="bullet">
		/// <item><description>Verifying prerequisite schema changes are in place</description></item>
		/// <item><description>Checking that required data exists</description></item>
		/// <item><description>Ensuring sufficient disk space or resources</description></item>
		/// <item><description>Validating tenant configuration or state</description></item>
		/// <item><description>Confirming external dependencies are available</description></item>
		/// </list>
		/// </para>
		/// <para>
		/// Validation runs during dry-run operations and can prevent actual migration execution
		/// if it returns <see langword="false"/>. This helps catch issues early without modifying data.
		/// </para>
		/// </remarks>
		public virtual Task<bool> ValidateAsync(string tenantId, CancellationToken cancellationToken = default)
		{
			// Default validation always succeeds
			return Task.FromResult(true);
		}

		#endregion
	}
}