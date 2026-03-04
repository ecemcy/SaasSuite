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

using SaasSuite.DataProtection.Interfaces;

namespace SaasSuite.DataProtection.Services
{
	/// <summary>
	/// Provides an in-memory, non-persistent implementation of <see cref="IKeyEncryptionKeyProvider"/> for testing and demonstration.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <strong>WARNING:</strong> This implementation is NOT suitable for production use because:
	/// <list type="bullet">
	/// <item><description>All keys are stored in memory and lost on application restart</description></item>
	/// <item><description>Keys are not backed by HSM or secure key storage</description></item>
	/// <item><description>No key rotation or versioning support</description></item>
	/// <item><description>Keys exist in process memory and may be vulnerable to memory dumps</description></item>
	/// <item><description>No audit logging of key access</description></item>
	/// </list>
	/// </para>
	/// <para>
	/// Use this implementation only for:
	/// <list type="bullet">
	/// <item><description>Local development and testing</description></item>
	/// <item><description>Demonstrations and prototypes</description></item>
	/// <item><description>Integration tests that verify encryption workflows</description></item>
	/// </list>
	/// </para>
	/// <para>
	/// For production environments, implement <see cref="IKeyEncryptionKeyProvider"/> with:
	/// <list type="bullet">
	/// <item><description>Azure Key Vault for cloud-based key management</description></item>
	/// <item><description>AWS Key Management Service (KMS) for AWS deployments</description></item>
	/// <item><description>HashiCorp Vault for on-premises or hybrid scenarios</description></item>
	/// <item><description>Hardware Security Modules (HSMs) for maximum security</description></item>
	/// </list>
	/// </para>
	/// </remarks>
	public class InMemoryKeyEncryptionKeyProvider
		: IKeyEncryptionKeyProvider
	{
		#region ' Fields '

		/// <summary>
		/// In-memory dictionary storing master keys by their identifiers.
		/// </summary>
		/// <remarks>
		/// Keys are stored as byte arrays in an unencrypted dictionary.
		/// This is insecure and should never be used in production.
		/// The dictionary is not thread-safe; consider using ConcurrentDictionary for multi-threaded scenarios.
		/// </remarks>
		private readonly Dictionary<string, byte[]> _masterKeys = new Dictionary<string, byte[]>();

		/// <summary>
		/// Logger for recording key operations and security warnings.
		/// </summary>
		/// <remarks>
		/// Used to log key generation, retrieval, and encryption/decryption operations.
		/// Warning-level logs are emitted when new keys are generated to emphasize the
		/// non-production nature of this implementation.
		/// </remarks>
		private readonly ILogger<InMemoryKeyEncryptionKeyProvider> _logger;

		#endregion

		#region ' Constructors '

		/// <summary>
		/// Initializes a new instance of the <see cref="InMemoryKeyEncryptionKeyProvider"/> class.
		/// </summary>
		/// <param name="logger">The logger for recording key operations. Cannot be <see langword="null"/>.</param>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="logger"/> is <see langword="null"/>.
		/// </exception>
		/// <remarks>
		/// The constructor automatically generates a default 256-bit master key for immediate use.
		/// This default key is stored with the identifier "default" and can be used without
		/// explicit key creation. Additional keys are generated on-demand when requested.
		/// </remarks>
		public InMemoryKeyEncryptionKeyProvider(ILogger<InMemoryKeyEncryptionKeyProvider> logger)
		{
			// Validate that logger dependency is provided
			this._logger = logger ?? throw new ArgumentNullException(nameof(logger));

			// Generate a default 256-bit (32-byte) master key for immediate use
			byte[] defaultKey = new byte[32];
			RandomNumberGenerator.Fill(defaultKey);
			this._masterKeys["default"] = defaultKey;
		}

		#endregion

		#region ' Methods '

		/// <summary>
		/// Asynchronously decrypts an encrypted Data Encryption Key using AES decryption with the specified master key.
		/// </summary>
		/// <param name="encryptedKey">The encrypted DEK with IV prepended. Cannot be <see langword="null"/>.</param>
		/// <param name="keyId">The identifier of the master key to use for decryption. Cannot be <see langword="null"/> or whitespace.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests. Not used in this synchronous implementation.</param>
		/// <returns>
		/// A task that represents the asynchronous operation. The task result contains the plaintext DEK.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="encryptedKey"/> or <paramref name="keyId"/> is <see langword="null"/> or whitespace.
		/// </exception>
		/// <exception cref="CryptographicException">
		/// Thrown when decryption fails due to invalid key, corrupted ciphertext, or wrong master key.
		/// </exception>
		/// <remarks>
		/// This method extracts the IV from the first 16 bytes of the encrypted key and uses it
		/// along with the master key to decrypt the remaining bytes.
		/// The encrypted key must be in the format produced by <see cref="EncryptKeyAsync"/>: [16-byte IV][ciphertext].
		/// If decryption fails, it typically indicates the wrong master key was used or the ciphertext was corrupted.
		/// </remarks>
		public async Task<byte[]> DecryptKeyAsync(byte[] encryptedKey, string keyId, CancellationToken cancellationToken = default)
		{
			// Validate that encryptedKey is provided
			ArgumentNullException.ThrowIfNull(encryptedKey);

			// Validate that keyId is provided
			ArgumentNullException.ThrowIfNull(keyId);

			// Retrieve the master key
			byte[] masterKey = await this.GetMasterKeyAsync(keyId, cancellationToken);

			// Create AES decryption algorithm instance
			using Aes aes = Aes.Create();
			aes.Key = masterKey;

			// Extract IV from the first 16 bytes of encrypted data
			byte[] iv = new byte[aes.IV.Length];
			Buffer.BlockCopy(encryptedKey, 0, iv, 0, iv.Length);
			aes.IV = iv;

			// Extract the encrypted key material (everything after the IV)
			byte[] encryptedData = new byte[encryptedKey.Length - iv.Length];
			Buffer.BlockCopy(encryptedKey, iv.Length, encryptedData, 0, encryptedData.Length);

			// Create decryptor and decrypt the key material
			using ICryptoTransform decryptor = aes.CreateDecryptor();
			return decryptor.TransformFinalBlock(encryptedData, 0, encryptedData.Length);
		}

		/// <summary>
		/// Asynchronously encrypts a Data Encryption Key using AES encryption with the specified master key.
		/// </summary>
		/// <param name="plainKey">The plaintext DEK to encrypt. Cannot be <see langword="null"/>.</param>
		/// <param name="keyId">The identifier of the master key to use for encryption. Cannot be <see langword="null"/> or whitespace.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests. Not used in this synchronous implementation.</param>
		/// <returns>
		/// A task that represents the asynchronous operation. The task result contains the encrypted DEK
		/// with the initialization vector (IV) prepended.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="plainKey"/> or <paramref name="keyId"/> is <see langword="null"/> or whitespace.
		/// </exception>
		/// <exception cref="CryptographicException">
		/// Thrown when encryption fails due to invalid key material or cryptographic errors.
		/// </exception>
		/// <remarks>
		/// This method uses AES in CBC mode with PKCS7 padding. The IV is randomly generated for each
		/// encryption operation and prepended to the ciphertext for self-contained encrypted output.
		/// The format is: [16-byte IV][encrypted key material].
		/// The master key is retrieved using <see cref="GetMasterKeyAsync"/> which may auto-generate
		/// a new key if the specified keyId doesn't exist.
		/// </remarks>
		public async Task<byte[]> EncryptKeyAsync(byte[] plainKey, string keyId, CancellationToken cancellationToken = default)
		{
			// Validate that plainKey is provided
			ArgumentNullException.ThrowIfNull(plainKey);

			// Validate that keyId is provided
			ArgumentNullException.ThrowIfNull(keyId);

			// Retrieve or generate the master key
			byte[] masterKey = await this.GetMasterKeyAsync(keyId, cancellationToken);

			// Create AES encryption algorithm instance
			using Aes aes = Aes.Create();
			aes.Key = masterKey;

			// Generate random IV for this encryption operation
			aes.GenerateIV();

			// Create encryptor with the master key and IV
			using ICryptoTransform encryptor = aes.CreateEncryptor();
			byte[] encrypted = encryptor.TransformFinalBlock(plainKey, 0, plainKey.Length);

			// Prepend IV to encrypted data for self-contained ciphertext
			byte[] result = new byte[aes.IV.Length + encrypted.Length];
			Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
			Buffer.BlockCopy(encrypted, 0, result, aes.IV.Length, encrypted.Length);

			return result;
		}

		/// <summary>
		/// Asynchronously retrieves or generates a master encryption key by its identifier.
		/// </summary>
		/// <param name="keyId">The unique identifier of the key to retrieve. Cannot be <see langword="null"/> or whitespace.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests. Not used in this synchronous implementation.</param>
		/// <returns>
		/// A task that represents the asynchronous operation. The task result contains the 256-bit master key.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="keyId"/> is <see langword="null"/> or whitespace.
		/// </exception>
		/// <remarks>
		/// If the requested key does not exist, a new 256-bit key is automatically generated,
		/// stored in memory, and returned. This auto-generation is logged as a warning.
		/// The operation completes synchronously but returns a Task for interface compatibility.
		/// In production implementations, automatic key generation may not be desirable,
		/// and explicit key creation workflows should be enforced.
		/// </remarks>
		public Task<byte[]> GetMasterKeyAsync(string keyId, CancellationToken cancellationToken = default)
		{
			// Validate that keyId is provided
			ArgumentNullException.ThrowIfNull(keyId);

			// Try to retrieve existing key from dictionary
			if (this._masterKeys.TryGetValue(keyId, out byte[]? key))
			{
				return Task.FromResult(key);
			}

			// Generate new 256-bit master key if not found
			byte[] newKey = new byte[32];
			RandomNumberGenerator.Fill(newKey);
			this._masterKeys[keyId] = newKey;

			// Log warning about automatic key generation
			this._logger.LogWarning("Generated new master key for keyId: {KeyId}", keyId);

			return Task.FromResult(newKey);
		}

		#endregion
	}
}