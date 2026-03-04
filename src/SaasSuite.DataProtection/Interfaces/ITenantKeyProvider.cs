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

using SaasSuite.Core;

namespace SaasSuite.DataProtection.Interfaces
{
	/// <summary>
	/// Defines the contract for managing tenant-specific encryption keys.
	/// </summary>
	/// <remarks>
	/// The tenant key provider manages Data Encryption Keys (DEKs) on a per-tenant basis,
	/// enabling tenant-level encryption isolation. Each tenant should have unique encryption
	/// keys to ensure that:
	/// <list type="bullet">
	/// <item><description>Tenant data is cryptographically isolated from other tenants</description></item>
	/// <item><description>Key compromise affects only one tenant</description></item>
	/// <item><description>Individual tenant keys can be rotated independently</description></item>
	/// <item><description>Tenant offboarding can include key deletion for data disposal</description></item>
	/// </list>
	/// Implementations typically store encrypted DEKs in a database or key management service,
	/// with the DEKs themselves encrypted using a master Key Encryption Key (KEK).
	/// </remarks>
	public interface ITenantKeyProvider
	{
		#region ' Methods '

		/// <summary>
		/// Asynchronously retrieves the encryption key for a specific tenant.
		/// </summary>
		/// <param name="tenantId">The identifier of the tenant. Cannot be <see langword="null"/>.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
		/// <returns>
		/// A task that represents the asynchronous operation. The task result contains the tenant's
		/// encryption key as a byte array, typically 256 bits (32 bytes) for AES-256 encryption.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="tenantId"/> or its value is <see langword="null"/>.
		/// </exception>
		/// <exception cref="KeyNotFoundException">
		/// Thrown when no encryption key exists for the specified tenant.
		/// </exception>
		/// <remarks>
		/// This method retrieves the tenant's DEK, which may be decrypted from storage using a master KEK.
		/// The returned key should be used immediately for encryption or decryption operations
		/// and then disposed of securely to minimize exposure time in memory.
		/// <para>
		/// If a key does not exist for a new tenant, implementations may either:
		/// <list type="bullet">
		/// <item><description>Generate a new key automatically (auto-provisioning)</description></item>
		/// <item><description>Throw an exception requiring explicit key creation</description></item>
		/// </list>
		/// </para>
		/// Keys should be cached briefly (e.g., 5-15 minutes) to reduce key management system calls,
		/// but cache duration should support timely key rotation.
		/// </remarks>
		Task<byte[]> GetKeyAsync(TenantId tenantId, CancellationToken cancellationToken = default);

		/// <summary>
		/// Asynchronously retrieves the key identifier for a specific tenant.
		/// </summary>
		/// <param name="tenantId">The identifier of the tenant. Cannot be <see langword="null"/>.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
		/// <returns>
		/// A task that represents the asynchronous operation. The task result contains a unique
		/// identifier for the tenant's encryption key (e.g., "tenant-123-key-v2", GUID, or versioned identifier).
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="tenantId"/> or its value is <see langword="null"/>.
		/// </exception>
		/// <exception cref="KeyNotFoundException">
		/// Thrown when no encryption key exists for the specified tenant.
		/// </exception>
		/// <remarks>
		/// The key identifier is used to:
		/// <list type="bullet">
		/// <item><description>Track which version of the key was used to encrypt data</description></item>
		/// <item><description>Support key rotation by maintaining multiple key versions</description></item>
		/// <item><description>Enable auditing of which keys are in use</description></item>
		/// <item><description>Link encrypted data to the correct decryption key</description></item>
		/// </list>
		/// Key identifiers should be stored alongside encrypted data to enable correct decryption,
		/// especially after key rotation where multiple key versions may coexist.
		/// The identifier format should support versioning (e.g., "tenant-123-v1", "tenant-123-v2")
		/// to facilitate gradual key rotation.
		/// </remarks>
		Task<string> GetKeyIdAsync(TenantId tenantId, CancellationToken cancellationToken = default);

		#endregion
	}
}