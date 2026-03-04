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

using System.Security.Cryptography;

using Microsoft.Extensions.Logging;

using SaasSuite.Core;
using SaasSuite.DataProtection.Interfaces;

namespace SaasSuite.DataProtection.Services
{
	/// <summary>
	/// Provides an in-memory, non-persistent implementation of <see cref="ITenantKeyProvider"/> for testing and demonstration.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <strong>WARNING:</strong> This implementation is NOT suitable for production use because:
	/// <list type="bullet">
	/// <item><description>All tenant keys are stored in memory and lost on application restart</description></item>
	/// <item><description>Encrypted data becomes unrecoverable after application restart</description></item>
	/// <item><description>No persistence layer for key storage or backup</description></item>
	/// <item><description>Keys are not encrypted at rest</description></item>
	/// <item><description>No key rotation or versioning capabilities</description></item>
	/// <item><description>No audit logging of key access or usage</description></item>
	/// </list>
	/// </para>
	/// <para>
	/// Use this implementation only for:
	/// <list type="bullet">
	/// <item><description>Local development and testing</description></item>
	/// <item><description>Demonstrations and prototypes</description></item>
	/// <item><description>Integration tests that verify encryption isolation between tenants</description></item>
	/// <item><description>Ephemeral environments where data loss is acceptable</description></item>
	/// </list>
	/// </para>
	/// <para>
	/// For production environments, implement <see cref="ITenantKeyProvider"/> with:
	/// <list type="bullet">
	/// <item><description>Database storage with encrypted key material (using envelope encryption)</description></item>
	/// <item><description>Azure Key Vault for cloud-hosted key management</description></item>
	/// <item><description>AWS Secrets Manager or KMS for AWS deployments</description></item>
	/// <item><description>HashiCorp Vault for on-premises or hybrid scenarios</description></item>
	/// </list>
	/// </para>
	/// </remarks>
	public class InMemoryTenantKeyProvider
		: ITenantKeyProvider
	{
		#region ' Fields '

		/// <summary>
		/// In-memory dictionary storing tenant keys and their identifiers, keyed by tenant ID.
		/// </summary>
		/// <remarks>
		/// Each entry contains a tuple with:
		/// <list type="bullet">
		/// <item><description>Key: The 256-bit encryption key as a byte array</description></item>
		/// <item><description>KeyId: A unique identifier for tracking the key version</description></item>
		/// </list>
		/// This dictionary is not thread-safe; consider using ConcurrentDictionary for multi-threaded scenarios.
		/// All data is lost when the application stops or restarts.
		/// </remarks>
		private readonly Dictionary<string, (byte[] Key, string KeyId)> _tenantKeys = new Dictionary<string, (byte[] Key, string KeyId)>();

		/// <summary>
		/// Logger for recording key operations and security warnings.
		/// </summary>
		/// <remarks>
		/// Used to log tenant key generation and retrieval operations.
		/// Warning-level logs are emitted when new keys are generated to emphasize
		/// the non-production nature of this implementation and aid in debugging.
		/// </remarks>
		private readonly ILogger<InMemoryTenantKeyProvider> _logger;

		#endregion

		#region ' Constructors '

		/// <summary>
		/// Initializes a new instance of the <see cref="InMemoryTenantKeyProvider"/> class.
		/// </summary>
		/// <param name="logger">The logger for recording key operations. Cannot be <see langword="null"/>.</param>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="logger"/> is <see langword="null"/>.
		/// </exception>
		/// <remarks>
		/// The constructor initializes an empty key store. Keys are generated on-demand
		/// when first requested for each tenant, providing automatic key provisioning.
		/// </remarks>
		public InMemoryTenantKeyProvider(ILogger<InMemoryTenantKeyProvider> logger)
		{
			// Validate that logger dependency is provided
			this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}

		#endregion

		#region ' Methods '

		/// <summary>
		/// Asynchronously retrieves or generates the encryption key for a specific tenant.
		/// </summary>
		/// <param name="tenantId">The identifier of the tenant. Cannot be <see langword="null"/>.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests. Not used in this synchronous implementation.</param>
		/// <returns>
		/// A task that represents the asynchronous operation. The task result contains the tenant's
		/// 256-bit encryption key as a byte array.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="tenantId"/> or its value is <see langword="null"/>.
		/// </exception>
		/// <remarks>
		/// If a key does not exist for the tenant, a new 256-bit key is automatically generated using
		/// cryptographically secure random number generation, stored in memory, and returned.
		/// The auto-generation is logged as a warning to indicate non-production behavior.
		/// The operation completes synchronously but returns a Task for interface compatibility.
		/// <para>
		/// The generated key identifier follows the format: "tenant-{tenantId}-{guid}"
		/// to ensure uniqueness and provide basic versioning support.
		/// </para>
		/// </remarks>
		public Task<byte[]> GetKeyAsync(TenantId tenantId, CancellationToken cancellationToken = default)
		{
			// Validate that tenantId has a non-null value
			ArgumentNullException.ThrowIfNull(tenantId.Value, nameof(tenantId));

			// Try to retrieve existing key from dictionary
			if (this._tenantKeys.TryGetValue(tenantId.Value, out (byte[] Key, string KeyId) keyInfo))
			{
				return Task.FromResult(keyInfo.Key);
			}

			// Generate new 256-bit key for tenant (32 bytes)
			byte[] key = new byte[32];
			RandomNumberGenerator.Fill(key);

			// Create unique key identifier for tracking
			string keyId = $"tenant-{tenantId.Value}-{Guid.NewGuid():N}";

			// Store the key and identifier in dictionary
			this._tenantKeys[tenantId.Value] = (key, keyId);

			// Log warning about automatic key generation
			this._logger.LogWarning("Generated new encryption key for tenant: {TenantId}", tenantId.Value);

			return Task.FromResult(key);
		}

		/// <summary>
		/// Asynchronously retrieves or generates the key identifier for a specific tenant.
		/// </summary>
		/// <param name="tenantId">The identifier of the tenant. Cannot be <see langword="null"/>.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests. Not used in this synchronous implementation.</param>
		/// <returns>
		/// A task that represents the asynchronous operation. The task result contains a unique
		/// identifier for the tenant's encryption key in the format "tenant-{tenantId}-{guid}".
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="tenantId"/> or its value is <see langword="null"/>.
		/// </exception>
		/// <remarks>
		/// If a key does not exist for the tenant, this method triggers key generation
		/// (similar to <see cref="GetKeyAsync"/>) to ensure the key and identifier are created together.
		/// The key identifier can be stored alongside encrypted data to track which key version
		/// was used for encryption, enabling future key rotation support.
		/// The operation completes synchronously but returns a Task for interface compatibility.
		/// </remarks>
		public Task<string> GetKeyIdAsync(TenantId tenantId, CancellationToken cancellationToken = default)
		{
			// Validate that tenantId has a non-null value
			ArgumentNullException.ThrowIfNull(tenantId.Value, nameof(tenantId));

			// Try to retrieve existing key identifier from dictionary
			if (this._tenantKeys.TryGetValue(tenantId.Value, out (byte[] Key, string KeyId) keyInfo))
			{
				return Task.FromResult(keyInfo.KeyId);
			}

			// Generate new key and identifier if not found
			byte[] key = new byte[32];
			RandomNumberGenerator.Fill(key);
			string keyId = $"tenant-{tenantId.Value}-{Guid.NewGuid():N}";

			// Store the key and identifier in dictionary
			this._tenantKeys[tenantId.Value] = (key, keyId);

			return Task.FromResult(keyId);
		}

		#endregion
	}
}