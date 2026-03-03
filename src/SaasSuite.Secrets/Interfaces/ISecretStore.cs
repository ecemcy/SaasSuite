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

using System.Security;

using SaasSuite.Core.Interfaces;
using SaasSuite.Secrets.Options;

namespace SaasSuite.Secrets.Interfaces
{
	/// <summary>
	/// Defines the contract for storing and retrieving secrets with tenant isolation.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Implementations should automatically scope all secret operations to the current tenant context
	/// obtained from <see cref="ITenantAccessor"/>. This ensures tenant isolation without requiring
	/// consumers to explicitly manage tenant scoping.
	/// </para>
	/// <para>
	/// <strong>Security Considerations:</strong>
	/// <list type="bullet">
	/// <item><description>All secret values should be stored encrypted at rest</description></item>
	/// <item><description>Secret values should never be logged or cached in plain text</description></item>
	/// <item><description>Access to secrets should be audited for security compliance</description></item>
	/// <item><description>Tenant isolation must be strictly enforced to prevent cross-tenant access</description></item>
	/// </list>
	/// </para>
	/// <para>
	/// <strong>Thread Safety:</strong> Implementations must be thread-safe and support concurrent
	/// operations across multiple tenants.
	/// </para>
	/// </remarks>
	public interface ISecretStore
	{
		#region ' Methods '

		/// <summary>
		/// Deletes a secret for the current tenant asynchronously.
		/// </summary>
		/// <param name="secretName">
		/// The name of the secret to delete. This should be the logical name without tenant prefixes.
		/// </param>
		/// <param name="cancellationToken">
		/// A token to monitor for cancellation requests. The operation may be cancelled if it's taking too long.
		/// </param>
		/// <returns>
		/// A <see cref="Task"/> representing the asynchronous operation. The task completes when
		/// the secret has been successfully deleted or confirmed as non-existent.
		/// </returns>
		/// <remarks>
		/// <para>
		/// If the specified secret does not exist, implementations may either complete silently
		/// or throw an exception, depending on the underlying store's behavior.
		/// </para>
		/// <para>
		/// <strong>Important:</strong> Deleted secrets may be recoverable for a period of time in some
		/// secret stores (e.g., soft delete in Azure Key Vault). Consult your implementation's documentation.
		/// </para>
		/// </remarks>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="secretName"/> is <see langword="null"/>.
		/// </exception>
		/// <exception cref="ArgumentException">
		/// Thrown when <paramref name="secretName"/> is empty or contains invalid characters.
		/// </exception>
		/// <exception cref="UnauthorizedAccessException">
		/// Thrown when the current user or service lacks permission to delete the secret.
		/// </exception>
		/// <exception cref="OperationCanceledException">
		/// Thrown when the operation is cancelled via the <paramref name="cancellationToken"/>.
		/// </exception>
		/// <example>
		/// <code>
		/// // Delete a database connection string for the current tenant
		/// await secretStore.DeleteSecretAsync("DatabaseConnectionString");
		/// </code>
		/// </example>
		Task DeleteSecretAsync(string secretName, CancellationToken cancellationToken = default);

		/// <summary>
		/// Stores or updates a secret value for the current tenant asynchronously.
		/// </summary>
		/// <param name="secretName">
		/// The name of the secret to store. This should be the logical name without tenant prefixes.
		/// </param>
		/// <param name="secretValue">
		/// The secret value to persist. This value will be encrypted by the underlying secret store.
		/// </param>
		/// <param name="cancellationToken">
		/// A token to monitor for cancellation requests.
		/// </param>
		/// <returns>
		/// A <see cref="Task"/> representing the asynchronous operation. The task completes when
		/// the secret has been successfully stored and is available for retrieval.
		/// </returns>
		/// <remarks>
		/// <para>
		/// If a secret with the same name already exists for the current tenant, it will be updated
		/// with the new value. Some implementations may maintain version history of previous values.
		/// </para>
		/// <para>
		/// After setting a secret, registered <see cref="ISecretRotationHandler"/> instances may be
		/// invoked to handle the rotation event.
		/// </para>
		/// <para>
		/// <strong>Security:</strong> The <paramref name="secretValue"/> parameter should be disposed
		/// securely after use when possible. Consider using <see cref="SecureString"/> for highly
		/// sensitive scenarios.
		/// </para>
		/// </remarks>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="secretName"/> or <paramref name="secretValue"/> is <see langword="null"/>.
		/// </exception>
		/// <exception cref="ArgumentException">
		/// Thrown when <paramref name="secretName"/> is empty or contains invalid characters,
		/// or when <paramref name="secretValue"/> exceeds the maximum allowed size.
		/// </exception>
		/// <exception cref="UnauthorizedAccessException">
		/// Thrown when the current user or service lacks permission to create or update secrets.
		/// </exception>
		/// <exception cref="OperationCanceledException">
		/// Thrown when the operation is cancelled via the <paramref name="cancellationToken"/>.
		/// </exception>
		/// <example>
		/// <code>
		/// // Store a new API key for the current tenant
		/// await secretStore.SetSecretAsync("ThirdPartyApiKey", "sk_live_abc123xyz");
		///
		/// // Update an existing connection string
		/// var newConnectionString = "Server=new-server;Database=mydb;...";
		/// await secretStore.SetSecretAsync("DatabaseConnectionString", newConnectionString);
		/// </code>
		/// </example>
		Task SetSecretAsync(string secretName, string secretValue, CancellationToken cancellationToken = default);

		/// <summary>
		/// Retrieves a secret value for the current tenant asynchronously.
		/// </summary>
		/// <param name="secretName">
		/// The name of the secret to retrieve. This should be the logical name without tenant prefixes.
		/// </param>
		/// <param name="cancellationToken">
		/// A token to monitor for cancellation requests.
		/// </param>
		/// <returns>
		/// A <see cref="Task{TResult}"/> representing the asynchronous operation.
		/// The task result contains the secret value if found; otherwise, <see langword="null"/>.
		/// </returns>
		/// <remarks>
		/// <para>
		/// If caching is enabled in the secret store configuration, this method may return a cached
		/// value. The cache duration is controlled by the <see cref="SecretStoreOptions.CacheDurationSeconds"/> setting.
		/// </para>
		/// <para>
		/// <strong>Security:</strong> The returned secret value should be used immediately and not
		/// stored in logs, memory dumps, or other persistent locations. Dispose of the value securely
		/// when no longer needed.
		/// </para>
		/// </remarks>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="secretName"/> is <see langword="null"/>.
		/// </exception>
		/// <exception cref="ArgumentException">
		/// Thrown when <paramref name="secretName"/> is empty or contains invalid characters.
		/// </exception>
		/// <exception cref="UnauthorizedAccessException">
		/// Thrown when the current user or service lacks permission to read the secret.
		/// </exception>
		/// <exception cref="OperationCanceledException">
		/// Thrown when the operation is cancelled via the <paramref name="cancellationToken"/>.
		/// </exception>
		/// <example>
		/// <code>
		/// // Retrieve a database connection string
		/// var connectionString = await secretStore.GetSecretAsync("DatabaseConnectionString");
		/// if (connectionString != null)
		/// {
		///     // Use the connection string
		///     using var connection = new SqlConnection(connectionString);
		///     await connection.OpenAsync();
		/// }
		/// else
		/// {
		///     throw new InvalidOperationException("Database connection string not configured");
		/// }
		/// </code>
		/// </example>
		Task<string?> GetSecretAsync(string secretName, CancellationToken cancellationToken = default);

		/// <summary>
		/// Retrieves all secret names available to the current tenant asynchronously.
		/// </summary>
		/// <param name="cancellationToken">
		/// A token to monitor for cancellation requests.
		/// </param>
		/// <returns>
		/// A <see cref="Task{TResult}"/> representing the asynchronous operation.
		/// The task result contains a collection of secret names (without tenant prefixes).
		/// Returns an empty collection if no secrets exist for the current tenant.
		/// </returns>
		/// <remarks>
		/// <para>
		/// This method returns only the names of secrets, not their values. Use <see cref="GetSecretAsync"/>
		/// to retrieve individual secret values.
		/// </para>
		/// <para>
		/// The returned names are the logical secret names without the tenant-scoping prefix,
		/// making them directly usable with other methods in this interface.
		/// </para>
		/// <para>
		/// <strong>Performance:</strong> Listing secrets may be an expensive operation in some stores,
		/// especially with large numbers of secrets. Consider caching the results when appropriate.
		/// </para>
		/// </remarks>
		/// <exception cref="UnauthorizedAccessException">
		/// Thrown when the current user or service lacks permission to list secrets.
		/// </exception>
		/// <exception cref="OperationCanceledException">
		/// Thrown when the operation is cancelled via the <paramref name="cancellationToken"/>.
		/// </exception>
		/// <example>
		/// <code>
		/// // List all secrets for the current tenant
		/// var secretNames = await secretStore.ListSecretsAsync();
		/// foreach (var name in secretNames)
		/// {
		///     Console.WriteLine($"Found secret: {name}");
		/// }
		///
		/// // Check if a specific secret exists
		/// bool hasApiKey = (await secretStore.ListSecretsAsync())
		///     .Any(name => name == "ThirdPartyApiKey");
		/// </code>
		/// </example>
		Task<IEnumerable<string>> ListSecretsAsync(CancellationToken cancellationToken = default);

		#endregion
	}
}