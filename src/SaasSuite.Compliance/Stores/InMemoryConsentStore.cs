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

using Microsoft.Extensions.Logging;

using SaasSuite.Compliance.Interfaces;
using SaasSuite.Core;

namespace SaasSuite.Compliance.Stores
{
	/// <summary>
	/// Provides an in-memory, thread-safe implementation of <see cref="IConsentStore"/> for testing and development.
	/// </summary>
	/// <remarks>
	/// <para>This implementation stores consent records in memory using a dictionary, providing fast access
	/// but limited to a single application instance. It is suitable for:</para>
	/// <list type="bullet">
	/// <item><description>Development and testing environments</description></item>
	/// <item><description>Demonstrations and prototypes</description></item>
	/// <item><description>Integration tests that verify consent management workflow</description></item>
	/// </list>
	/// <para>For production use, implement a custom <see cref="IConsentStore"/> backed by persistent storage
	/// like a database to ensure consent records survive application restarts and support distributed deployments.
	/// All stored consent records are lost when the application stops or restarts.
	/// Thread-safety is limited to dictionary operations; consider using ConcurrentDictionary for better concurrency.</para>
	/// </remarks>
	public class InMemoryConsentStore
		: IConsentStore
	{
		#region ' Fields '

		/// <summary>
		/// In-memory dictionary storing consent records keyed by a composite of tenant ID and user ID.
		/// </summary>
		/// <remarks>
		/// The key format is "{tenantId}:{userId}" to ensure tenant isolation and per-user consent tracking.
		/// Each key maps to a list of consent records maintaining the complete consent history for that user.
		/// This structure is not thread-safe for concurrent modifications; consider ConcurrentDictionary for production.
		/// </remarks>
		private readonly Dictionary<string, List<ConsentRecord>> _consents = new Dictionary<string, List<ConsentRecord>>();

		/// <summary>
		/// Logger for recording consent management operations and diagnostics.
		/// </summary>
		/// <remarks>
		/// Used to log consent recording, queries, and any errors encountered during consent management operations.
		/// </remarks>
		private readonly ILogger<InMemoryConsentStore> _logger;

		#endregion

		#region ' Constructors '

		/// <summary>
		/// Initializes a new instance of the <see cref="InMemoryConsentStore"/> class.
		/// </summary>
		/// <param name="logger">
		/// The <see cref="ILogger{InMemoryConsentStore}"/> for logging consent operations.
		/// Cannot be <see langword="null"/>.
		/// </param>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="logger"/> is <see langword="null"/>.
		/// </exception>
		/// <remarks>
		/// This constructor is invoked by the dependency injection container when <see cref="IConsentStore"/> is resolved.
		/// </remarks>
		public InMemoryConsentStore(ILogger<InMemoryConsentStore> logger)
		{
			// Validate that logger dependency is provided
			this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}

		#endregion

		#region ' Methods '

		/// <summary>
		/// Asynchronously records a new consent decision for a user, appending it to their consent history.
		/// </summary>
		/// <param name="tenantId">The identifier of the tenant within whose context the consent is recorded. Cannot be <see langword="null"/>.</param>
		/// <param name="userId">The identifier of the user providing consent. Cannot be <see langword="null"/> or whitespace.</param>
		/// <param name="consent">
		/// The <see cref="ConsentRecord"/> containing consent details. Cannot be <see langword="null"/>.
		/// </param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests. Not used in this synchronous implementation.</param>
		/// <returns>A task that represents the asynchronous operation.</returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="tenantId"/>, its value, <paramref name="userId"/>, or <paramref name="consent"/> is <see langword="null"/> or whitespace.
		/// </exception>
		/// <remarks>
		/// This method ensures tenant and user IDs are set correctly on the consent record before storing.
		/// If no consent history exists for the user, a new list is created.
		/// The consent record is appended to the history to maintain chronological order.
		/// The operation completes synchronously but returns a Task for interface compatibility.
		/// Thread-safety note: This implementation is not fully thread-safe for concurrent writes to the same user's consent list.
		/// </remarks>
		public Task RecordConsentAsync(TenantId tenantId, string userId, ConsentRecord consent, CancellationToken cancellationToken = default)
		{
			// Validate that tenantId has a non-null value
			ArgumentNullException.ThrowIfNull(tenantId.Value, nameof(tenantId));

			// Validate that userId is provided and not whitespace
			ArgumentNullException.ThrowIfNull(userId);

			// Validate that consent record is provided
			ArgumentNullException.ThrowIfNull(consent);

			// Generate composite key for tenant-user isolation
			string key = GetKey(tenantId, userId);

			// Initialize consent list if this is the first consent for this user
			if (!this._consents.ContainsKey(key))
			{
				this._consents[key] = new List<ConsentRecord>();
			}

			// Ensure tenant and user IDs are correctly set on the consent record
			consent.TenantId = tenantId.Value;
			consent.UserId = userId;

			// Add the consent record to the user's history
			this._consents[key].Add(consent);

			// Log the consent recording with details
			this._logger.LogInformation("Recorded consent for user {UserId} in tenant {TenantId}: {ConsentType} = {IsGranted}", userId, tenantId.Value, consent.ConsentType, consent.IsGranted);

			// Return completed task for async interface compatibility
			return Task.CompletedTask;
		}

		/// <summary>
		/// Asynchronously checks whether a user currently has granted consent for a specific type of data processing.
		/// </summary>
		/// <param name="tenantId">The identifier of the tenant within whose context to check consent. Cannot be <see langword="null"/>.</param>
		/// <param name="userId">The identifier of the user whose consent to check. Cannot be <see langword="null"/> or whitespace.</param>
		/// <param name="consentType">
		/// The type of consent to check (e.g., "marketing", "analytics"). Cannot be <see langword="null"/> or whitespace.
		/// </param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests. Not used in this synchronous implementation.</param>
		/// <returns>
		/// A task that represents the asynchronous operation. The task result is <see langword="true"/> if consent
		/// is currently granted and not expired; otherwise, <see langword="false"/>.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="tenantId"/>, its value, <paramref name="userId"/>, or <paramref name="consentType"/>
		/// is <see langword="null"/> or whitespace.
		/// </exception>
		/// <remarks>
		/// <para>This method determines current consent status by:</para>
		/// <list type="number">
		/// <item><description>Finding the most recent consent record for the specified type (ordered by RecordedAt descending)</description></item>
		/// <item><description>Checking if the consent is granted (IsGranted = true)</description></item>
		/// <item><description>Verifying the consent has not expired (ExpiresAt is null or in the future)</description></item>
		/// </list>
		/// <para>If no consent record exists or the most recent record is not granted or has expired, returns <see langword="false"/>.
		/// The operation completes synchronously but returns a Task for interface compatibility.</para>
		/// </remarks>
		public Task<bool> HasConsentAsync(TenantId tenantId, string userId, string consentType, CancellationToken cancellationToken = default)
		{
			// Validate that tenantId has a non-null value
			ArgumentNullException.ThrowIfNull(tenantId.Value, nameof(tenantId));

			// Validate that userId is provided and not whitespace
			ArgumentNullException.ThrowIfNull(userId);

			// Validate that consentType is provided and not whitespace
			ArgumentNullException.ThrowIfNull(consentType);

			// Generate composite key for tenant-user isolation
			string key = GetKey(tenantId, userId);

			// Attempt to retrieve consent records for this user
			if (this._consents.TryGetValue(key, out List<ConsentRecord>? records))
			{
				// Find the most recent consent record for the specified type
				ConsentRecord? latestConsent = records
					.Where(c => c.ConsentType == consentType)
					.OrderByDescending(c => c.RecordedAt)
					.FirstOrDefault();

				// Check if a consent record was found
				if (latestConsent != null)
				{
					// Check if consent has expired
					if (latestConsent.ExpiresAt.HasValue && latestConsent.ExpiresAt.Value < DateTimeOffset.UtcNow)
					{
						// Consent has expired, return false
						return Task.FromResult(false);
					}

					// Return the grant status (true if granted and not expired)
					return Task.FromResult(latestConsent.IsGranted);
				}
			}

			// No consent record found, return false (no consent given)
			return Task.FromResult(false);
		}

		/// <summary>
		/// Asynchronously retrieves the complete consent history for a user within a tenant.
		/// </summary>
		/// <param name="tenantId">The identifier of the tenant whose user's consent history to retrieve. Cannot be <see langword="null"/>.</param>
		/// <param name="userId">The identifier of the user whose consent history to retrieve. Cannot be <see langword="null"/> or whitespace.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests. Not used in this synchronous implementation.</param>
		/// <returns>
		/// A task that represents the asynchronous operation. The task result contains a collection
		/// of <see cref="ConsentRecord"/> instances representing all consent decisions.
		/// Returns an empty collection if no consent records exist for the user.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="tenantId"/>, its value, or <paramref name="userId"/> is <see langword="null"/> or whitespace.
		/// </exception>
		/// <remarks>
		/// This method enforces tenant isolation by using a composite key of tenant ID and user ID.
		/// The operation completes synchronously but returns a Task for interface compatibility.
		/// </remarks>
		public Task<IEnumerable<ConsentRecord>> GetConsentHistoryAsync(TenantId tenantId, string userId, CancellationToken cancellationToken = default)
		{
			// Validate that tenantId has a non-null value
			ArgumentNullException.ThrowIfNull(tenantId.Value, nameof(tenantId));

			// Validate that userId is provided and not whitespace
			ArgumentNullException.ThrowIfNull(userId);

			// Generate composite key for tenant-user isolation
			string key = GetKey(tenantId, userId);

			// Attempt to retrieve consent records for this user
			if (this._consents.TryGetValue(key, out List<ConsentRecord>? records))
			{
				// Return the complete consent history
				return Task.FromResult(records.AsEnumerable());
			}

			// Return empty collection if no consents exist for this user
			return Task.FromResult(Enumerable.Empty<ConsentRecord>());
		}

		#endregion

		#region ' Static Methods '

		/// <summary>
		/// Generates a composite key for storing and retrieving consent records with tenant isolation.
		/// </summary>
		/// <param name="tenantId">The tenant identifier.</param>
		/// <param name="userId">The user identifier.</param>
		/// <returns>A composite key in the format "{tenantId}:{userId}".</returns>
		/// <remarks>
		/// This private helper method ensures consistent key formatting across all consent operations.
		/// The colon separator is safe as it's unlikely to appear in tenant or user IDs.
		/// </remarks>
		private static string GetKey(TenantId tenantId, string userId)
		{
			// Combine tenant and user IDs with colon separator for tenant-user isolation
			return $"{tenantId.Value}:{userId}";
		}

		#endregion
	}
}