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

using SaasSuite.Compliance.Interfaces;

namespace SaasSuite.Compliance
{
	/// <summary>
	/// Represents an immutable record of user consent for a specific type of data processing.
	/// </summary>
	/// <remarks>
	/// Consent records are essential for GDPR Article 6 and 7 compliance, documenting explicit user
	/// consent for various data processing activities. Each record captures the who, what, when, and how
	/// of a consent decision, providing legally defensible evidence of user preferences.
	/// Records should never be deleted or modified once created to maintain audit trail integrity.
	/// </remarks>
	public class ConsentRecord
	{
		#region ' Properties '

		/// <summary>
		/// Gets or sets a value indicating whether consent is granted or revoked.
		/// </summary>
		/// <value>
		/// <see langword="true"/> if consent is granted; <see langword="false"/> if consent is revoked or denied.
		/// </value>
		/// <remarks>
		/// This boolean captures the user's consent decision. Both grant and revocation actions create
		/// new consent records, preserving the complete history of consent decisions.
		/// The most recent record for a consent type determines the current consent status.
		/// </remarks>
		public bool IsGranted { get; set; }

		/// <summary>
		/// Gets or sets the type of consent being recorded.
		/// </summary>
		/// <value>
		/// A string categorizing the consent type, such as "marketing", "analytics", "data-processing", or "cookies".
		/// Defaults to an empty string. Should not be <see langword="null"/>.
		/// </value>
		/// <remarks>
		/// <para>Consent types should use consistent naming across the application for reliable consent checking.
		/// Common consent types include:</para>
		/// <list type="bullet">
		/// <item><description>"marketing": Email marketing and promotional communications</description></item>
		/// <item><description>"analytics": Usage tracking and behavior analysis</description></item>
		/// <item><description>"data-processing": General data processing for service operation</description></item>
		/// <item><description>"cookies": Cookie usage beyond strictly necessary cookies</description></item>
		/// <item><description>"third-party-sharing": Sharing data with third parties</description></item>
		/// </list>
		/// </remarks>
		public string ConsentType { get; set; } = string.Empty;

		/// <summary>
		/// Gets or sets the unique identifier for this consent record.
		/// </summary>
		/// <value>
		/// A globally unique string identifier generated at the time of record creation.
		/// Defaults to a new GUID string representation.
		/// </value>
		/// <remarks>
		/// This identifier is used for record retrieval, correlation, and audit trail maintenance.
		/// The default value is automatically generated using <see cref="Guid.NewGuid"/> to ensure uniqueness.
		/// </remarks>
		public string Id { get; set; } = Guid.NewGuid().ToString();

		/// <summary>
		/// Gets or sets the identifier of the tenant within whose context the consent was recorded.
		/// </summary>
		/// <value>
		/// A string representing the tenant identifier. Defaults to an empty string. Should not be <see langword="null"/>.
		/// </value>
		/// <remarks>
		/// This field ensures tenant isolation by scoping the consent record to a specific tenant.
		/// It is automatically set when the consent is recorded through <see cref="IConsentStore.RecordConsentAsync"/>.
		/// </remarks>
		public string TenantId { get; set; } = string.Empty;

		/// <summary>
		/// Gets or sets the identifier of the user who provided or revoked consent.
		/// </summary>
		/// <value>
		/// A string representing the user ID, username, email, or other unique user identifier.
		/// Defaults to an empty string. Should not be <see langword="null"/>.
		/// </value>
		/// <remarks>
		/// This field links the consent record to a specific user, establishing accountability.
		/// The format depends on the authentication system in use (e.g., GUID, email, username).
		/// </remarks>
		public string UserId { get; set; } = string.Empty;

		/// <summary>
		/// Gets or sets the IP address from which the consent was provided.
		/// </summary>
		/// <value>
		/// A string containing an IPv4 or IPv6 address, or <see langword="null"/> if not available.
		/// </value>
		/// <remarks>
		/// Recording the IP address provides additional context and legal evidence for consent decisions.
		/// This information can be valuable during audits or disputes about consent validity.
		/// Extract from HTTP context (HttpContext.Connection.RemoteIpAddress) when recording consent through web interfaces.
		/// For programmatic or system-initiated consent, this may be <see langword="null"/> or contain internal addresses.
		/// </remarks>
		public string? IpAddress { get; set; }

		/// <summary>
		/// Gets or sets the UTC timestamp when the consent was recorded.
		/// </summary>
		/// <value>
		/// A <see cref="DateTimeOffset"/> value in UTC representing when the consent decision was made.
		/// Defaults to the current UTC time.
		/// </value>
		/// <remarks>
		/// Timestamps are critical for compliance and legal defensibility, proving when consent was obtained.
		/// The default value is automatically set to <see cref="DateTimeOffset.UtcNow"/> when the record is created.
		/// This enables chronological sorting of consent history and checking consent expiration.
		/// </remarks>
		public DateTimeOffset RecordedAt { get; set; } = DateTimeOffset.UtcNow;

		/// <summary>
		/// Gets or sets the optional expiration timestamp for the consent.
		/// </summary>
		/// <value>
		/// A nullable <see cref="DateTimeOffset"/> indicating when the consent expires.
		/// If <see langword="null"/>, the consent does not expire and remains valid until explicitly revoked.
		/// </value>
		/// <remarks>
		/// Some regulations and business policies require consent to be periodically reconfirmed.
		/// When an expiration is set, the consent becomes invalid after the specified time,
		/// and the system should prompt the user for renewed consent before continuing data processing.
		/// Check expiration in <see cref="Interfaces.IConsentStore.HasConsentAsync"/> to enforce expiry.
		/// </remarks>
		public DateTimeOffset? ExpiresAt { get; set; }

		/// <summary>
		/// Gets or sets additional contextual metadata about the consent as key-value pairs.
		/// </summary>
		/// <value>
		/// A dictionary containing supplementary information about the consent decision.
		/// Defaults to an empty dictionary. Cannot be <see langword="null"/>.
		/// </value>
		/// <remarks>
		/// <para>Use this dictionary to store additional context such as:</para>
		/// <list type="bullet">
		/// <item><description>User agent string (browser/application information)</description></item>
		/// <item><description>Consent mechanism (e.g., "checkbox", "banner", "api", "email-link")</description></item>
		/// <item><description>Form or page where consent was obtained</description></item>
		/// <item><description>Language in which consent was presented</description></item>
		/// <item><description>Version of terms and conditions or privacy policy</description></item>
		/// <item><description>Geographic location (country, region)</description></item>
		/// </list>
		/// <para>This metadata enhances the legal defensibility and auditability of consent records.
		/// The dictionary is initialized as empty to prevent null reference exceptions.</para>
		/// </remarks>
		public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();

		#endregion
	}
}