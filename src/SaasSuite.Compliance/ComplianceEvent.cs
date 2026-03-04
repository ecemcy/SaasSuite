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

using SaasSuite.Compliance.Enumerations;
using SaasSuite.Core;

namespace SaasSuite.Compliance
{
	/// <summary>
	/// Represents an immutable record of a compliance-related action performed in the system.
	/// </summary>
	/// <remarks>
	/// Compliance events provide an audit trail of data protection activities required by regulations
	/// like GDPR and CCPA. These events track actions such as data exports, deletions, anonymization,
	/// and consent changes. Maintaining these records is essential for demonstrating compliance
	/// during audits and investigations.
	/// </remarks>
	public class ComplianceEvent
	{
		#region ' Properties '

		/// <summary>
		/// Gets or sets the unique identifier for this compliance event.
		/// </summary>
		/// <value>
		/// A globally unique string identifier generated at the time of event creation.
		/// Defaults to a new GUID string representation.
		/// </value>
		/// <remarks>
		/// This identifier is used for event correlation, retrieval, and deduplication in compliance reporting.
		/// The default value is automatically generated using <see cref="Guid.NewGuid"/> to ensure uniqueness.
		/// </remarks>
		public string Id { get; set; } = Guid.NewGuid().ToString();

		/// <summary>
		/// Gets or sets the identifier of the user or system component that initiated the compliance action.
		/// </summary>
		/// <value>
		/// A string representing the user ID, username, or system identifier.
		/// Can be <see langword="null"/> for system-initiated actions without a specific user context.
		/// </value>
		/// <remarks>
		/// This field establishes accountability by recording who or what triggered the compliance action.
		/// For user-initiated actions, this should contain the user's identifier.
		/// For automated or scheduled actions, this might contain values like "system", "scheduler", or a service account name.
		/// </remarks>
		public string? InitiatedBy { get; set; }

		/// <summary>
		/// Gets or sets the type of compliance event that occurred.
		/// </summary>
		/// <value>
		/// A <see cref="ComplianceEventType"/> enumeration value indicating the specific compliance action.
		/// </value>
		/// <remarks>
		/// The event type categorizes the compliance action for filtering, reporting, and audit purposes.
		/// Common types include data exports, deletions, anonymization, and consent changes.
		/// </remarks>
		public ComplianceEventType EventType { get; set; }

		/// <summary>
		/// Gets or sets the UTC timestamp when the compliance event occurred.
		/// </summary>
		/// <value>
		/// A <see cref="DateTimeOffset"/> value in UTC representing the exact moment the action took place.
		/// Defaults to the current UTC time.
		/// </value>
		/// <remarks>
		/// Timestamps are critical for compliance auditing and must be recorded in UTC to ensure
		/// consistency across time zones. The default value is automatically set to the current time
		/// using <see cref="DateTimeOffset.UtcNow"/> when the event is created.
		/// </remarks>
		public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;

		/// <summary>
		/// Gets or sets additional contextual information about the compliance event as key-value pairs.
		/// </summary>
		/// <value>
		/// A dictionary containing custom details about the event. Defaults to an empty dictionary.
		/// Cannot be <see langword="null"/>.
		/// </value>
		/// <remarks>
		/// <para>Use this dictionary to store event-specific information such as:</para>
		/// <list type="bullet">
		/// <item><description>Export format and file sizes for data exports</description></item>
		/// <item><description>Number of records affected by deletion or anonymization</description></item>
		/// <item><description>Consent types and previous values for consent changes</description></item>
		/// <item><description>IP addresses and user agents for user-initiated actions</description></item>
		/// <item><description>Error messages or warnings encountered during the action</description></item>
		/// </list>
		/// <para>The dictionary is initialized as empty to prevent null reference exceptions.</para>
		/// </remarks>
		public Dictionary<string, string> Details { get; set; } = new Dictionary<string, string>();

		/// <summary>
		/// Gets or sets the tenant identifier associated with this compliance event.
		/// </summary>
		/// <value>
		/// A <see cref="Core.TenantId"/> representing the tenant within whose context the compliance action occurred.
		/// Cannot be <see langword="null"/>.
		/// </value>
		/// <remarks>
		/// The tenant ID ensures proper data isolation in multi-tenant scenarios and enables
		/// tenant-specific compliance reporting and audit trails.
		/// </remarks>
		public TenantId TenantId { get; set; }

		#endregion
	}
}