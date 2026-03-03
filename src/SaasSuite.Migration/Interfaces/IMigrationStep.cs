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

namespace SaasSuite.Migration.Interfaces
{
	/// <summary>
	/// Defines the contract for an individual migration step that operates on a single tenant.
	/// </summary>
	/// <remarks>
	/// Migration steps represent atomic units of migration work, such as schema changes,
	/// data transformations, or configuration updates. Each step is executed independently
	/// for each tenant during a migration operation. Steps should be idempotent and support
	/// validation and optional rollback.
	/// </remarks>
	public interface IMigrationStep
	{
		#region ' Properties '

		/// <summary>
		/// Gets a human-readable description of what the migration step does.
		/// </summary>
		/// <value>
		/// A string describing the step's purpose and actions. Cannot be <see langword="null"/> or empty.
		/// </value>
		/// <remarks>
		/// The description provides context for operators, documentation, and logs.
		/// It should explain what changes the step makes and why, such as
		/// "Adds email verification column to support two-factor authentication".
		/// </remarks>
		string Description { get; }

		/// <summary>
		/// Gets the unique name of the migration step.
		/// </summary>
		/// <value>
		/// A string that uniquely identifies this migration step. Cannot be <see langword="null"/> or empty.
		/// </value>
		/// <remarks>
		/// The name is used for logging, progress tracking, error reporting, and identifying
		/// which steps have been executed. Use descriptive names that indicate the step's purpose,
		/// such as "AddUserEmailColumn" or "MigrateToNewAuthSystem".
		/// </remarks>
		string Name { get; }

		#endregion

		#region ' Methods '

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
		/// <para>
		/// This method performs the actual migration work for a single tenant. Implementations should:
		/// <list type="bullet">
		/// <item><description>Be idempotent - safe to run multiple times without side effects</description></item>
		/// <item><description>Use transactions to ensure atomicity where possible</description></item>
		/// <item><description>Respect the cancellation token for long-running operations</description></item>
		/// <item><description>Log progress and errors for diagnostics</description></item>
		/// <item><description>Clean up resources properly (use using statements or try-finally)</description></item>
		/// <item><description>Throw meaningful exceptions with context when failures occur</description></item>
		/// </list>
		/// </para>
		/// <para>
		/// The method will be called once per tenant by the migration engine. If it throws an exception,
		/// the tenant is marked as failed and the error is recorded in the migration result.
		/// </para>
		/// </remarks>
		Task ExecuteAsync(string tenantId, CancellationToken cancellationToken = default);

		/// <summary>
		/// Asynchronously rolls back the migration step for a specific tenant, reversing changes made by <see cref="ExecuteAsync"/>.
		/// </summary>
		/// <param name="tenantId">The unique identifier of the tenant to roll back. Cannot be <see langword="null"/> or whitespace.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
		/// <returns>A task that represents the asynchronous operation.</returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="tenantId"/> is <see langword="null"/> or whitespace.
		/// </exception>
		/// <remarks>
		/// <para>
		/// Rollback should restore the system to its exact state before <see cref="ExecuteAsync"/> was called.
		/// Implementations should:
		/// <list type="bullet">
		/// <item><description>Be idempotent - safe to run multiple times</description></item>
		/// <item><description>Handle partial execution scenarios gracefully</description></item>
		/// <item><description>Use transactions to ensure atomicity</description></item>
		/// <item><description>Log rollback progress and any issues</description></item>
		/// </list>
		/// </para>
		/// <para>
		/// Not all operations can be safely rolled back. For steps that don't support rollback:
		/// <list type="bullet">
		/// <item><description>Inherit from <see cref="MigrationStepBase"/> and leave <see cref="Base.MigrationStepBase.SupportsRollback"/> as <see langword="false"/></description></item>
		/// <item><description>Implement this method as a no-op</description></item>
		/// <item><description>The migration engine will log a warning when skipping rollback</description></item>
		/// </list>
		/// </para>
		/// </remarks>
		Task RollbackAsync(string tenantId, CancellationToken cancellationToken = default);

		/// <summary>
		/// Asynchronously validates whether the migration step can be safely executed for a specific tenant.
		/// </summary>
		/// <param name="tenantId">The unique identifier of the tenant to validate. Cannot be <see langword="null"/> or whitespace.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
		/// <returns>
		/// A task that represents the asynchronous operation. The task result is <see langword="true"/>
		/// if the migration can proceed safely; otherwise, <see langword="false"/>.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="tenantId"/> is <see langword="null"/> or whitespace.
		/// </exception>
		/// <remarks>
		/// <para>
		/// Validation runs during dry-run operations and before actual migration execution.
		/// It should check prerequisites and conditions without modifying any data. Common validations include:
		/// <list type="bullet">
		/// <item><description>Verifying required schema changes are already in place</description></item>
		/// <item><description>Checking that prerequisite data exists</description></item>
		/// <item><description>Ensuring sufficient resources (disk space, memory)</description></item>
		/// <item><description>Validating tenant configuration or feature flags</description></item>
		/// <item><description>Confirming external dependencies are accessible</description></item>
		/// </list>
		/// </para>
		/// <para>
		/// Return <see langword="false"/> if any condition that would cause migration to fail is detected.
		/// This allows early detection of issues before data modification begins.
		/// </para>
		/// </remarks>
		Task<bool> ValidateAsync(string tenantId, CancellationToken cancellationToken = default);

		#endregion
	}
}