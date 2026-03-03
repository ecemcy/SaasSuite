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

namespace SaasSuite.Samples.SampleWebApp.Infrastructure.Models
{
	/// <summary>
	/// Represents a single immutable audit event that tracks tenant activities, changes, and security events.
	/// </summary>
	/// <remarks>
	/// Audit events provide a chronological record of important actions for compliance, security monitoring,
	/// and operational troubleshooting within a multi-tenant environment.
	/// </remarks>
	public class AuditEvent
	{
		#region ' Properties '

		/// <summary>
		/// Gets or initializes the action identifier describing what operation was performed.
		/// </summary>
		/// <value>
		/// A string representing the action, typically in "resource.verb" format (e.g., "user.created", "subscription.upgraded").
		/// </value>
		public required string Action { get; init; }

		/// <summary>
		/// Gets or initializes the category or domain this event belongs to.
		/// </summary>
		/// <value>
		/// A string representing the logical category such as "User Management", "Subscription", or "Security".
		/// </value>
		public required string Category { get; init; }

		/// <summary>
		/// Gets or initializes the human-readable description of what occurred.
		/// </summary>
		/// <value>
		/// A descriptive string providing context about the event for human readers.
		/// </value>
		public required string Details { get; init; }

		/// <summary>
		/// Gets or initializes the tenant associated with this audit event.
		/// </summary>
		/// <value>
		/// The <see cref="TenantId"/> identifying which tenant this event belongs to.
		/// </value>
		public required TenantId TenantId { get; init; }

		/// <summary>
		/// Gets or initializes the correlation identifier for grouping related events.
		/// </summary>
		/// <value>
		/// An optional string used to link related events across operations, or <see langword="null"/> if not correlated.
		/// </value>
		public string? CorrelationId { get; init; }

		/// <summary>
		/// Gets or initializes the UTC timestamp when this event occurred.
		/// </summary>
		/// <value>
		/// A <see cref="DateTimeOffset"/> representing when the event was recorded. Defaults to current UTC time.
		/// </value>
		public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

		/// <summary>
		/// Gets or initializes additional structured metadata providing context for the event.
		/// </summary>
		/// <value>
		/// A dictionary of key-value pairs containing supplementary information. Defaults to an empty dictionary.
		/// </value>
		public Dictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>();

		/// <summary>
		/// Gets or sets the unique identifier for this audit event.
		/// </summary>
		/// <value>
		/// A <see cref="Guid"/> that uniquely identifies this event. Defaults to a new GUID upon instantiation.
		/// </value>
		public Guid Id { get; set; } = Guid.NewGuid();

		#endregion
	}
}