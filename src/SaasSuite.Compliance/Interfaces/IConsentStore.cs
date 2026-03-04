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

namespace SaasSuite.Compliance.Interfaces
{
	/// <summary>
	/// Defines the contract for managing and persisting user consent records.
	/// </summary>
	/// <remarks>
	/// This interface supports compliance with regulations requiring documented user consent
	/// for data processing activities, such as GDPR Article 6 and 7 (Lawfulness and Consent),
	/// CCPA opt-out mechanisms, and cookie consent requirements. Implementations must maintain
	/// a complete audit trail of consent decisions with timestamps and contextual information.
	/// </remarks>
	public interface IConsentStore
	{
		#region ' Methods '

		/// <summary>
		/// Asynchronously checks whether a user has currently granted consent for a specific type of data processing.
		/// </summary>
		/// <param name="tenantId">The identifier of the tenant within whose context to check consent. Cannot be <see langword="null"/>.</param>
		/// <param name="userId">The identifier of the user whose consent to check. Cannot be <see langword="null"/> or whitespace.</param>
		/// <param name="consentType">
		/// The type of consent to check (e.g., "marketing", "analytics", "data-processing").
		/// Cannot be <see langword="null"/> or whitespace.
		/// </param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
		/// <returns>
		/// A task that represents the asynchronous operation. The task result is <see langword="true"/> if the user
		/// has currently granted consent for the specified type and the consent has not expired; otherwise, <see langword="false"/>.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="tenantId"/>, its value, <paramref name="userId"/>, or <paramref name="consentType"/>
		/// is <see langword="null"/> or whitespace.
		/// </exception>
		/// <remarks>
		/// <para>This method determines current consent status by:</para>
		/// <list type="number">
		/// <item><description>Finding the most recent consent record for the specified type</description></item>
		/// <item><description>Checking if the consent is granted (IsGranted = true)</description></item>
		/// <item><description>Verifying the consent has not expired (ExpiresAt is null or in the future)</description></item>
		/// </list>
		/// <para>If no consent record exists for the specified type, the method returns <see langword="false"/>.
		/// Use this method before processing user data to ensure compliance with consent requirements.
		/// Consent types should be consistently named across the application for reliable consent checking.</para>
		/// </remarks>
		Task<bool> HasConsentAsync(TenantId tenantId, string userId, string consentType, CancellationToken cancellationToken = default);

		/// <summary>
		/// Asynchronously records a new consent decision for a user.
		/// </summary>
		/// <param name="tenantId">The identifier of the tenant within whose context the consent is recorded. Cannot be <see langword="null"/>.</param>
		/// <param name="userId">The identifier of the user providing consent. Cannot be <see langword="null"/> or whitespace.</param>
		/// <param name="consent">
		/// The <see cref="ConsentRecord"/> containing consent details including type, grant status, expiration, and metadata.
		/// Cannot be <see langword="null"/>.
		/// </param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
		/// <returns>A task that represents the asynchronous operation.</returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="tenantId"/>, its value, <paramref name="userId"/>, or <paramref name="consent"/> is <see langword="null"/>.
		/// </exception>
		/// <remarks>
		/// <para>This method appends the consent record to the user's consent history, preserving all previous records
		/// to maintain a complete audit trail. The record should include:</para>
		/// <list type="bullet">
		/// <item><description>Consent type (e.g., "marketing", "analytics", "data-processing")</description></item>
		/// <item><description>Whether consent is granted or revoked</description></item>
		/// <item><description>Timestamp of the decision</description></item>
		/// <item><description>Optional expiration date</description></item>
		/// <item><description>IP address and user agent for legal evidence</description></item>
		/// <item><description>Any additional context in metadata</description></item>
		/// </list>
		/// <para>Implementations must ensure thread-safety for concurrent consent recording.
		/// The consent record's TenantId and UserId properties are set from the parameters to ensure consistency.</para>
		/// </remarks>
		Task RecordConsentAsync(TenantId tenantId, string userId, ConsentRecord consent, CancellationToken cancellationToken = default);

		/// <summary>
		/// Asynchronously retrieves the complete consent history for a user, ordered chronologically.
		/// </summary>
		/// <param name="tenantId">The identifier of the tenant whose user's consent history to retrieve. Cannot be <see langword="null"/>.</param>
		/// <param name="userId">The identifier of the user whose consent history to retrieve. Cannot be <see langword="null"/> or whitespace.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
		/// <returns>
		/// A task that represents the asynchronous operation. The task result contains an ordered collection
		/// of <see cref="ConsentRecord"/> instances representing all consent decisions made by the user.
		/// Returns an empty collection if no consent records exist for the user.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="tenantId"/>, its value, or <paramref name="userId"/> is <see langword="null"/> or whitespace.
		/// </exception>
		/// <remarks>
		/// <para>This method enforces tenant isolation by only returning consent records for the specified tenant.
		/// The returned collection includes both granted and revoked consent records to provide a complete history.
		/// Results should be ordered by timestamp (RecordedAt) in descending or ascending order depending on use case.</para>
		/// <para>Use this method to:</para>
		/// <list type="bullet">
		/// <item><description>Display consent history to users</description></item>
		/// <item><description>Generate compliance reports</description></item>
		/// <item><description>Audit consent management practices</description></item>
		/// <item><description>Support data subject access requests (DSARs)</description></item>
		/// </list>
		/// </remarks>
		Task<IEnumerable<ConsentRecord>> GetConsentHistoryAsync(TenantId tenantId, string userId, CancellationToken cancellationToken = default);

		#endregion
	}
}