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

namespace SaasSuite.Audit.Interfaces
{
	/// <summary>
	/// Defines the contract for recording and retrieving audit events in a multi-tenant SaaS system.
	/// </summary>
	/// <remarks>
	/// This service provides a centralized interface for audit logging and querying, ensuring
	/// consistent event tracking across the application. Implementations may store events in memory,
	/// databases, distributed logs, or external audit systems.
	/// The interface supports tenant-scoped and global queries, enabling both tenant-specific
	/// audit trails and administrative cross-tenant monitoring.
	/// </remarks>
	public interface IAuditService
	{
		#region ' Methods '

		/// <summary>
		/// Asynchronously logs an audit event for a specific tenant.
		/// </summary>
		/// <param name="tenantId">The identifier of the tenant within whose context the action occurred. Cannot be <see langword="null"/>.</param>
		/// <param name="userId">The identifier of the user who performed the action. Cannot be <see langword="null"/> or whitespace.</param>
		/// <param name="action">The action being performed, such as "Create", "Update", "Delete", or custom operations. Cannot be <see langword="null"/> or whitespace.</param>
		/// <param name="resource">The resource or entity being affected, such as "Invoice", "Subscription", or "User". Cannot be <see langword="null"/> or whitespace.</param>
		/// <param name="details">Optional additional details about the action, such as change descriptions or error messages. Can be <see langword="null"/>.</param>
		/// <param name="metadata">Optional additional metadata as key-value pairs for custom context. Can be <see langword="null"/>.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
		/// <returns>
		/// A task that represents the asynchronous operation. The task result contains the created <see cref="AuditEvent"/>
		/// with populated fields including auto-generated <see cref="AuditEvent.Id"/> and <see cref="AuditEvent.Timestamp"/>.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="tenantId"/> is <see langword="null"/>.
		/// </exception>
		/// <exception cref="ArgumentException">
		/// Thrown when <paramref name="userId"/>, <paramref name="action"/>, or <paramref name="resource"/> is <see langword="null"/>, empty, or whitespace.
		/// </exception>
		/// <remarks>
		/// This method creates a complete audit event with the current UTC timestamp and adds it to the audit store.
		/// The returned event includes all provided information plus system-generated fields like ID and timestamp.
		/// Implementations should be thread-safe and handle concurrent logging operations efficiently.
		/// The operation completes synchronously in the in-memory implementation but may be truly asynchronous
		/// in database or external service implementations.
		/// </remarks>
		Task<AuditEvent> LogAsync(TenantId tenantId, string userId, string action, string resource, string? details = null, IDictionary<string, string>? metadata = null, CancellationToken cancellationToken = default);

		/// <summary>
		/// Asynchronously retrieves audit events across all tenants with optional filtering (administrative view).
		/// </summary>
		/// <param name="startDate">Optional start date (inclusive) for filtering events by timestamp. If <see langword="null"/>, no lower bound is applied.</param>
		/// <param name="endDate">Optional end date (inclusive) for filtering events by timestamp. If <see langword="null"/>, no upper bound is applied.</param>
		/// <param name="action">Optional action filter to retrieve only events matching this action name. Matching is case-insensitive. If <see langword="null"/> or whitespace, no action filter is applied.</param>
		/// <param name="resource">Optional resource filter to retrieve only events affecting this resource type. Matching is case-insensitive. If <see langword="null"/> or whitespace, no resource filter is applied.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
		/// <returns>
		/// A task that represents the asynchronous operation. The task result contains a collection of <see cref="AuditEvent"/>
		/// instances from all tenants matching the specified criteria, ordered by timestamp in descending order (most recent first).
		/// Returns an empty collection if no events match the filters.
		/// </returns>
		/// <remarks>
		/// This method provides a global view of audit events across all tenants and should be restricted to
		/// administrative or super-admin roles. It does not enforce tenant isolation, allowing cross-tenant
		/// analysis for security monitoring, compliance reporting, and system-wide audits.
		/// All filter parameters are optional and can be combined to narrow the result set.
		/// Date filtering uses inclusive boundaries, and string filters use case-insensitive comparison.
		/// The results are sorted by timestamp in descending order to prioritize recent activity.
		/// Use this method with caution and proper authorization checks to prevent unauthorized access to
		/// sensitive cross-tenant audit data.
		/// </remarks>
		Task<IEnumerable<AuditEvent>> GetAllEventsAsync(DateTime? startDate = null, DateTime? endDate = null, string? action = null, string? resource = null, CancellationToken cancellationToken = default);

		/// <summary>
		/// Asynchronously retrieves audit events for a specific tenant with optional filtering.
		/// </summary>
		/// <param name="tenantId">The identifier of the tenant whose events to retrieve. Cannot be <see langword="null"/>.</param>
		/// <param name="startDate">Optional start date (inclusive) for filtering events by timestamp. If <see langword="null"/>, no lower bound is applied.</param>
		/// <param name="endDate">Optional end date (inclusive) for filtering events by timestamp. If <see langword="null"/>, no upper bound is applied.</param>
		/// <param name="action">Optional action filter to retrieve only events matching this action name. Matching is case-insensitive. If <see langword="null"/> or whitespace, no action filter is applied.</param>
		/// <param name="resource">Optional resource filter to retrieve only events affecting this resource type. Matching is case-insensitive. If <see langword="null"/> or whitespace, no resource filter is applied.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
		/// <returns>
		/// A task that represents the asynchronous operation. The task result contains a collection of <see cref="AuditEvent"/>
		/// instances matching the specified criteria, ordered by timestamp in descending order (most recent first).
		/// Returns an empty collection if no events match the filters.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="tenantId"/> is <see langword="null"/>.
		/// </exception>
		/// <remarks>
		/// This method enforces tenant isolation by only returning events for the specified tenant.
		/// All filter parameters are optional and can be combined to narrow the result set.
		/// Date filtering uses inclusive boundaries, meaning events exactly matching the start or end dates are included.
		/// Action and resource filters use case-insensitive comparison for convenience.
		/// The results are always sorted by timestamp in descending order to show the most recent events first,
		/// which is the typical use case for audit trail displays.
		/// </remarks>
		Task<IEnumerable<AuditEvent>> GetEventsAsync(TenantId tenantId, DateTime? startDate = null, DateTime? endDate = null, string? action = null, string? resource = null, CancellationToken cancellationToken = default);

		#endregion
	}
}