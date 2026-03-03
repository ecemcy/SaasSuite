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

namespace SaasSuite.Audit
{
	/// <summary>
	/// Represents an immutable audit event record capturing a user action within a multi-tenant system.
	/// </summary>
	/// <remarks>
	/// Audit events provide a complete trail of actions performed in the system, including who performed
	/// the action, what was affected, when it occurred, and contextual details. These events are critical
	/// for compliance, security monitoring, debugging, and business analytics.
	/// Each event is assigned a unique identifier and timestamped in UTC for consistent tracking across time zones.
	/// </remarks>
	public class AuditEvent
	{
		#region ' Properties '

		/// <summary>
		/// Gets or sets the action being performed on the resource.
		/// </summary>
		/// <value>
		/// A string describing the operation, typically using standard CRUD terminology such as
		/// "Create", "Read", "Update", "Delete", "Login", "Export", or custom business actions.
		/// Cannot be <see langword="null"/>.
		/// </value>
		/// <remarks>
		/// Actions should follow a consistent naming convention across the application for effective
		/// filtering and reporting. Common patterns include:
		/// <list type="bullet">
		/// <item><description>CRUD operations: "Create", "Update", "Delete", "View"</description></item>
		/// <item><description>Authentication: "Login", "Logout", "PasswordChange"</description></item>
		/// <item><description>Business operations: "Approve", "Reject", "Submit", "Cancel"</description></item>
		/// </list>
		/// This field is required and validated during event creation.
		/// </remarks>
		public string Action { get; set; } = null!;

		/// <summary>
		/// Gets or sets the unique identifier for this audit event.
		/// </summary>
		/// <value>
		/// A globally unique string identifier generated at the time of event creation.
		/// Defaults to a new GUID string representation.
		/// </value>
		/// <remarks>
		/// This identifier is used for event correlation, retrieval, and deduplication.
		/// The default value is automatically generated using <see cref="Guid.NewGuid"/> to ensure uniqueness
		/// across distributed systems and concurrent operations.
		/// </remarks>
		public string Id { get; set; } = Guid.NewGuid().ToString();

		/// <summary>
		/// Gets or sets the resource or entity being affected by the action.
		/// </summary>
		/// <value>
		/// A string identifying the resource type or entity name, such as "Invoice", "Subscription",
		/// "User", "Document", or "ApiKey". Cannot be <see langword="null"/>.
		/// </value>
		/// <remarks>
		/// Resources typically correspond to domain entities or API resources in the system.
		/// Consistent naming across events enables filtering by resource type for compliance reports
		/// and security audits. For hierarchical resources, use dot notation (e.g., "Order.LineItem").
		/// This field is required and validated during event creation.
		/// </remarks>
		public string Resource { get; set; } = null!;

		/// <summary>
		/// Gets or sets the identifier of the user who performed the audited action.
		/// </summary>
		/// <value>
		/// A string representing the user ID, username, email, or other unique user identifier.
		/// Cannot be <see langword="null"/>.
		/// </value>
		/// <remarks>
		/// This identifier establishes accountability by recording who initiated the action.
		/// The format depends on the authentication system in use (e.g., GUID, email address, username).
		/// For system-initiated actions, this might contain values like "system" or "automated-process".
		/// This field is required and validated during event creation.
		/// </remarks>
		public string UserId { get; set; } = null!;

		/// <summary>
		/// Gets or sets additional details describing the audited action.
		/// </summary>
		/// <value>
		/// A string containing free-form text, JSON, or structured data providing context about the action.
		/// Can be <see langword="null"/> if no additional details are needed.
		/// </value>
		/// <remarks>
		/// Details can include information such as:
		/// <list type="bullet">
		/// <item><description>Changes made (before/after values)</description></item>
		/// <item><description>Reason for the action</description></item>
		/// <item><description>Error messages if the action failed</description></item>
		/// <item><description>Serialized entity state snapshots</description></item>
		/// </list>
		/// For structured data, consider using JSON format for easier parsing and querying.
		/// This field is optional and can be omitted for simple audit trails.
		/// </remarks>
		public string? Details { get; set; }

		/// <summary>
		/// Gets or sets the IP address from which the audited action was performed.
		/// </summary>
		/// <value>
		/// A string containing an IPv4 or IPv6 address, or <see langword="null"/> if not available.
		/// </value>
		/// <remarks>
		/// The IP address provides additional security context and helps identify:
		/// <list type="bullet">
		/// <item><description>Geographic location of the user</description></item>
		/// <item><description>Potential unauthorized access attempts</description></item>
		/// <item><description>Pattern analysis for fraud detection</description></item>
		/// <item><description>Compliance with data residency requirements</description></item>
		/// </list>
		/// For actions initiated by background processes or internal services, this may be <see langword="null"/>
		/// or contain internal network addresses. Extract from HTTP context when available.
		/// </remarks>
		public string? IpAddress { get; set; }

		/// <summary>
		/// Gets or sets the UTC timestamp when the audited event occurred.
		/// </summary>
		/// <value>
		/// A <see cref="DateTime"/> value in UTC representing the exact moment the action took place.
		/// </value>
		/// <remarks>
		/// All timestamps are stored in UTC to ensure consistency across different time zones and
		/// avoid ambiguity during daylight saving time transitions. The timestamp is automatically
		/// set by the audit service when the event is logged using <see cref="DateTime.UtcNow"/>.
		/// This enables chronological sorting and time-based filtering of audit events.
		/// </remarks>
		public DateTime Timestamp { get; set; }

		/// <summary>
		/// Gets or sets additional metadata associated with the audit event as key-value pairs.
		/// </summary>
		/// <value>
		/// A dictionary containing custom metadata entries. Defaults to an empty dictionary.
		/// Cannot be <see langword="null"/>.
		/// </value>
		/// <remarks>
		/// Metadata enables extensible audit trails without schema changes. Common uses include:
		/// <list type="bullet">
		/// <item><description>Session identifiers or correlation IDs</description></item>
		/// <item><description>User agent strings for browser-based actions</description></item>
		/// <item><description>API version or client application information</description></item>
		/// <item><description>Custom business context (e.g., department, cost center)</description></item>
		/// <item><description>Geographic or organizational unit identifiers</description></item>
		/// </list>
		/// Keys should use consistent naming conventions (e.g., camelCase or kebab-case) for easier querying.
		/// The dictionary is initialized as empty to prevent null reference exceptions.
		/// </remarks>
		public IDictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();

		/// <summary>
		/// Gets or sets the tenant identifier associated with this audit event.
		/// </summary>
		/// <value>
		/// A <see cref="Core.TenantId"/> representing the tenant within whose context the audited action occurred.
		/// Cannot be <see langword="null"/>.
		/// </value>
		/// <remarks>
		/// The tenant ID ensures proper data isolation in multi-tenant scenarios, allowing each tenant
		/// to maintain a separate audit trail. This field is essential for tenant-scoped audit queries
		/// and compliance reporting. All audit events must be associated with a valid tenant.
		/// </remarks>
		public TenantId TenantId { get; set; } = default!;

		#endregion
	}
}