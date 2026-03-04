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

using SaasSuite.DataProtection.Interfaces;

namespace SaasSuite.DataProtection.Helpers
{
	/// <summary>
	/// Provides utility methods for implementing envelope encryption patterns.
	/// </summary>
	/// <remarks>
	/// Envelope encryption is a security pattern that uses two levels of encryption:
	/// <list type="number">
	/// <item><description>Data is encrypted with a Data Encryption Key (DEK)</description></item>
	/// <item><description>The DEK is then encrypted with a Key Encryption Key (KEK/Master Key)</description></item>
	/// </list>
	/// This pattern provides benefits including simplified key rotation, reduced KEK usage,
	/// and support for multi-region deployments. The encrypted data includes the initialization
	/// vector (IV) prepended for secure decryption.
	/// All methods use AES encryption in CBC mode with PKCS7 padding.
	/// </remarks>
	public static class EnvelopeEncryptionHelper
	{
		#region ' Static Methods '

		/// <summary>
		/// Decrypts data that was encrypted using the envelope encryption pattern.
		/// </summary>
		/// <param name="ciphertext">
		/// The encrypted data with the initialization vector prepended. Cannot be <see langword="null"/>.
		/// Must be in the format produced by <see cref="Encrypt"/>.
		/// </param>
		/// <param name="dataEncryptionKey">The Data Encryption Key (DEK) used to decrypt the data. Cannot be <see langword="null"/>.</param>
		/// <returns>A byte array containing the decrypted plaintext data.</returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="ciphertext"/> or <paramref name="dataEncryptionKey"/> is <see langword="null"/>.
		/// </exception>
		/// <exception cref="CryptographicException">
		/// Thrown when decryption fails due to invalid key, corrupted data, or tampered ciphertext.
		/// </exception>
		/// <remarks>
		/// This method extracts the IV from the first 16 bytes of the ciphertext and uses it
		/// along with the DEK to decrypt the remaining bytes. The ciphertext must be in the
		/// format produced by <see cref="Encrypt"/> with the IV prepended.
		/// If decryption fails, it typically indicates the wrong key was used, the data was corrupted,
		/// or the ciphertext was tampered with.
		/// </remarks>
		public static byte[] Decrypt(byte[] ciphertext, byte[] dataEncryptionKey)
		{
			// Validate that ciphertext is provided
			ArgumentNullException.ThrowIfNull(ciphertext);

			// Validate that data encryption key is provided
			ArgumentNullException.ThrowIfNull(dataEncryptionKey);

			// Create AES decryption algorithm instance
			using Aes aes = Aes.Create();
			aes.Key = dataEncryptionKey;

			// Extract IV from the beginning of the ciphertext
			byte[] iv = new byte[aes.IV.Length];
			Buffer.BlockCopy(ciphertext, 0, iv, 0, iv.Length);
			aes.IV = iv;

			// Extract the encrypted data (everything after the IV)
			byte[] encryptedData = new byte[ciphertext.Length - iv.Length];
			Buffer.BlockCopy(ciphertext, iv.Length, encryptedData, 0, encryptedData.Length);

			// Create decryptor with the key and extracted IV
			using ICryptoTransform decryptor = aes.CreateDecryptor();

			// Decrypt and return the plaintext
			return decryptor.TransformFinalBlock(encryptedData, 0, encryptedData.Length);
		}

		/// <summary>
		/// Encrypts data using the envelope encryption pattern with AES algorithm.
		/// </summary>
		/// <param name="plaintext">The unencrypted data to protect. Cannot be <see langword="null"/>.</param>
		/// <param name="dataEncryptionKey">The Data Encryption Key (DEK) used to encrypt the data. Cannot be <see langword="null"/>.</param>
		/// <returns>
		/// A byte array containing the initialization vector (IV) prepended to the encrypted data.
		/// The first 16 bytes contain the IV, followed by the encrypted payload.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="plaintext"/> or <paramref name="dataEncryptionKey"/> is <see langword="null"/>.
		/// </exception>
		/// <remarks>
		/// This method encrypts the plaintext using AES in CBC mode with a randomly generated IV.
		/// The IV is prepended to the ciphertext to enable decryption without external IV storage.
		/// The data encryption key should be a 256-bit (32-byte) key generated by
		/// <see cref="GenerateDataEncryptionKey"/> or retrieved from a secure key provider.
		/// The resulting ciphertext should be stored alongside the encrypted DEK for complete envelope encryption.
		/// </remarks>
		public static byte[] Encrypt(byte[] plaintext, byte[] dataEncryptionKey)
		{
			// Validate that plaintext is provided
			ArgumentNullException.ThrowIfNull(plaintext);

			// Validate that data encryption key is provided
			ArgumentNullException.ThrowIfNull(dataEncryptionKey);

			// Create AES encryption algorithm instance
			using Aes aes = Aes.Create();
			aes.Key = dataEncryptionKey;

			// Generate a random initialization vector for this encryption operation
			aes.GenerateIV();

			// Create encryptor with the key and IV
			using ICryptoTransform encryptor = aes.CreateEncryptor();

			// Encrypt the plaintext data
			byte[] encrypted = encryptor.TransformFinalBlock(plaintext, 0, plaintext.Length);

			// Prepend IV to encrypted data for self-contained ciphertext
			byte[] result = new byte[aes.IV.Length + encrypted.Length];
			Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
			Buffer.BlockCopy(encrypted, 0, result, aes.IV.Length, encrypted.Length);

			return result;
		}

		/// <summary>
		/// Generates a new cryptographically secure Data Encryption Key (DEK) using random number generation.
		/// </summary>
		/// <param name="keySizeBits">
		/// The size of the key in bits. Must be a multiple of 8. Defaults to 256 bits (32 bytes).
		/// Common values are 128, 192, or 256 bits.
		/// </param>
		/// <returns>A byte array containing the newly generated encryption key.</returns>
		/// <exception cref="ArgumentException">
		/// Thrown when <paramref name="keySizeBits"/> is not a multiple of 8.
		/// </exception>
		/// <remarks>
		/// This method generates a cryptographically secure random key suitable for AES encryption.
		/// The default 256-bit key provides strong encryption aligned with current security best practices.
		/// The generated DEK should be:
		/// <list type="bullet">
		/// <item><description>Encrypted with a Key Encryption Key (KEK) before storage</description></item>
		/// <item><description>Unique for each encryption operation or data object</description></item>
		/// <item><description>Stored securely alongside the encrypted data</description></item>
		/// <item><description>Never reused across different tenants or data objects</description></item>
		/// </list>
		/// Use <see cref="IKeyEncryptionKeyProvider"/> to encrypt the generated DEK before persistence.
		/// </remarks>
		public static byte[] GenerateDataEncryptionKey(int keySizeBits = 256)
		{
			// Validate that key size is a multiple of 8 (whole bytes)
			if (keySizeBits % 8 != 0)
			{
				throw new ArgumentException("Key size must be a multiple of 8", nameof(keySizeBits));
			}

			// Calculate key size in bytes
			int keySizeBytes = keySizeBits / 8;

			// Generate cryptographically secure random bytes for the key
			byte[] key = new byte[keySizeBytes];
			RandomNumberGenerator.Fill(key);

			return key;
		}

		#endregion
	}
}