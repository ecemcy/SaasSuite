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

namespace SaasSuite.DataProtection.Interfaces
{
	/// <summary>
	/// Defines the contract for managing Key Encryption Keys (KEKs) in an envelope encryption scheme.
	/// </summary>
	/// <remarks>
	/// The Key Encryption Key provider manages master keys used to protect Data Encryption Keys (DEKs).
	/// This follows the envelope encryption pattern where:
	/// <list type="number">
	/// <item><description>Data is encrypted with a DEK</description></item>
	/// <item><description>The DEK is encrypted with a KEK (master key)</description></item>
	/// <item><description>Only the encrypted DEK is stored with the data</description></item>
	/// </list>
	/// This pattern provides several security benefits including simplified key rotation,
	/// reduced KEK usage, and centralized key management. Implementations should integrate
	/// with secure key management services like Azure Key Vault, AWS KMS, or HSMs.
	/// </remarks>
	public interface IKeyEncryptionKeyProvider
	{
		#region ' Methods '

		/// <summary>
		/// Asynchronously decrypts an encrypted Data Encryption Key using the specified master key.
		/// </summary>
		/// <param name="encryptedKey">The encrypted DEK to decrypt. Cannot be <see langword="null"/>.</param>
		/// <param name="keyId">The identifier of the master key to use for decryption. Cannot be <see langword="null"/> or whitespace.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
		/// <returns>
		/// A task that represents the asynchronous operation. The task result contains the plaintext DEK
		/// as a byte array, which can then be used to decrypt the actual data.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="encryptedKey"/> or <paramref name="keyId"/> is <see langword="null"/> or whitespace.
		/// </exception>
		/// <exception cref="KeyNotFoundException">
		/// Thrown when the specified key identifier does not exist.
		/// </exception>
		/// <exception cref="CryptographicException">
		/// Thrown when decryption fails due to invalid key, corrupted ciphertext, or wrong key identifier.
		/// </exception>
		/// <remarks>
		/// This method retrieves the master key and uses it to decrypt the encrypted DEK.
		/// The decrypted DEK should be used immediately to decrypt data and then disposed of securely
		/// to minimize the time sensitive key material exists in memory.
		/// If decryption fails, it typically indicates the wrong key was used, the ciphertext was
		/// corrupted, or the data was tampered with. These scenarios should be logged and monitored
		/// for security purposes.
		/// </remarks>
		Task<byte[]> DecryptKeyAsync(byte[] encryptedKey, string keyId, CancellationToken cancellationToken = default);

		/// <summary>
		/// Asynchronously encrypts a Data Encryption Key using the specified master key.
		/// </summary>
		/// <param name="plainKey">The plaintext DEK to encrypt. Cannot be <see langword="null"/>.</param>
		/// <param name="keyId">The identifier of the master key to use for encryption. Cannot be <see langword="null"/> or whitespace.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
		/// <returns>
		/// A task that represents the asynchronous operation. The task result contains the encrypted DEK
		/// as a byte array, which can be safely stored alongside encrypted data.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="plainKey"/> or <paramref name="keyId"/> is <see langword="null"/> or whitespace.
		/// </exception>
		/// <exception cref="KeyNotFoundException">
		/// Thrown when the specified key identifier does not exist.
		/// </exception>
		/// <exception cref="CryptographicException">
		/// Thrown when encryption fails due to invalid key material or cryptographic errors.
		/// </exception>
		/// <remarks>
		/// This method is part of the envelope encryption pattern. The encrypted DEK should be stored
		/// alongside the encrypted data so that it can be decrypted later using the same master key.
		/// The plaintext DEK should be disposed of securely after encryption to minimize exposure.
		/// The encrypted output includes necessary metadata (like IV) prepended or encoded within
		/// the ciphertext for successful decryption.
		/// </remarks>
		Task<byte[]> EncryptKeyAsync(byte[] plainKey, string keyId, CancellationToken cancellationToken = default);

		/// <summary>
		/// Asynchronously retrieves a master encryption key by its identifier.
		/// </summary>
		/// <param name="keyId">The unique identifier of the key to retrieve. Cannot be <see langword="null"/> or whitespace.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
		/// <returns>
		/// A task that represents the asynchronous operation. The task result contains the master key
		/// as a byte array, typically 256 bits (32 bytes) for AES-256 encryption.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="keyId"/> is <see langword="null"/> or whitespace.
		/// </exception>
		/// <exception cref="KeyNotFoundException">
		/// Thrown when the specified key identifier does not exist.
		/// </exception>
		/// <remarks>
		/// Master keys should be stored securely in a Hardware Security Module (HSM) or
		/// key management service and should never be exposed directly in application code or logs.
		/// The key should be cached briefly if needed for performance, but cache duration should
		/// be kept minimal to support key rotation.
		/// This method is used internally by <see cref="EncryptKeyAsync"/> and <see cref="DecryptKeyAsync"/>
		/// to perform KEK operations.
		/// </remarks>
		Task<byte[]> GetMasterKeyAsync(string keyId, CancellationToken cancellationToken = default);

		#endregion
	}
}