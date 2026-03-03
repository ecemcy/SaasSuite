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

using System.Collections.Concurrent;

using SaasSuite.Audit.Interfaces;
using SaasSuite.Core;

namespace SaasSuite.Audit.Services
{
	/// <summary>
	/// Provides an in-memory, thread-safe implementation of the audit service for event logging and retrieval.
	/// </summary>
	/// <remarks>
	/// This implementation uses a <see cref="ConcurrentBag{T}"/> to store audit events in memory, providing
	/// fast concurrent write operations without blocking. It is suitable for development, testing, and
	/// single-instance deployments where audit data persistence is not required across restarts.
	/// For production scenarios requiring durable storage, implement a custom <see cref="IAuditService"/>
	/// backed by a database, distributed logging system, or external audit service.
	/// All events are lost when the application stops or restarts.
	/// </remarks>
	public class AuditService
		: IAuditService
	{
		#region ' Fields '

		/// <summary>
		/// Thread-safe collection storing all audit events in memory.
		/// </summary>
		/// <remarks>
		/// ConcurrentBag is used for optimal concurrent add performance, which is the primary operation
		/// for audit logging. Read operations (queries) iterate the entire collection, which is acceptable
		/// for in-memory scenarios but should be replaced with indexed storage for large-scale production use.
		/// </remarks>
		private readonly ConcurrentBag<AuditEvent> _events = new ConcurrentBag<AuditEvent>();

		#endregion

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
		/// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete. Not used in this in-memory implementation.</param>
		/// <returns>
		/// A task that represents the asynchronous operation. The task result contains the created <see cref="AuditEvent"/>
		/// with populated fields including auto-generated <see cref="AuditEvent.Id"/> and <see cref="AuditEvent.Timestamp"/>.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="tenantId"/> value is <see langword="null"/>.
		/// </exception>
		/// <exception cref="ArgumentException">
		/// Thrown when <paramref name="userId"/>, <paramref name="action"/>, or <paramref name="resource"/> is <see langword="null"/>, empty, or whitespace.
		/// </exception>
		/// <remarks>
		/// This method validates all required parameters before creating the audit event.
		/// The timestamp is automatically set to the current UTC time using <see cref="DateTime.UtcNow"/>.
		/// If metadata is <see langword="null"/>, an empty dictionary is assigned to prevent null reference exceptions.
		/// The event is immediately added to the in-memory collection and returned to the caller.
		/// The operation completes synchronously but returns a Task for interface compatibility.
		/// </remarks>
		public Task<AuditEvent> LogAsync(TenantId tenantId, string userId, string action, string resource, string? details = null, IDictionary<string, string>? metadata = null, CancellationToken cancellationToken = default)
		{
			// Validate that tenantId has a non-null value
			ArgumentNullException.ThrowIfNull(tenantId.Value, nameof(tenantId));

			// Validate that userId is not null, empty, or whitespace
			if (string.IsNullOrWhiteSpace(userId))
			{
				throw new ArgumentException("User ID cannot be null or whitespace", nameof(userId));
			}

			// Validate that action is not null, empty, or whitespace
			if (string.IsNullOrWhiteSpace(action))
			{
				throw new ArgumentException("Action cannot be null or whitespace", nameof(action));
			}

			// Validate that resource is not null, empty, or whitespace
			if (string.IsNullOrWhiteSpace(resource))
			{
				throw new ArgumentException("Resource cannot be null or whitespace", nameof(resource));
			}

			// Create the audit event with all provided and generated fields
			AuditEvent auditEvent = new AuditEvent
			{
				TenantId = tenantId,
				UserId = userId,
				Action = action,
				Resource = resource,
				Details = details,
				Timestamp = DateTime.UtcNow, // Automatically capture current UTC timestamp
				Metadata = metadata ?? new Dictionary<string, string>() // Initialize empty dictionary if null
			};

			// Add the event to the thread-safe collection
			this._events.Add(auditEvent);

			// Return completed task with the created event
			return Task.FromResult(auditEvent);
		}

		/// <summary>
		/// Asynchronously retrieves audit events across all tenants with optional filtering (administrative view).
		/// </summary>
		/// <param name="startDate">Optional start date (inclusive) for filtering events by timestamp. If <see langword="null"/>, no lower bound is applied.</param>
		/// <param name="endDate">Optional end date (inclusive) for filtering events by timestamp. If <see langword="null"/>, no upper bound is applied.</param>
		/// <param name="action">Optional action filter to retrieve only events matching this action name. Matching is case-insensitive. If <see langword="null"/> or whitespace, no action filter is applied.</param>
		/// <param name="resource">Optional resource filter to retrieve only events affecting this resource type. Matching is case-insensitive. If <see langword="null"/> or whitespace, no resource filter is applied.</param>
		/// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete. Not used in this in-memory implementation.</param>
		/// <returns>
		/// A task that represents the asynchronous operation. The task result contains a collection of <see cref="AuditEvent"/>
		/// instances from all tenants matching the specified criteria, ordered by timestamp in descending order (most recent first).
		/// Returns an empty collection if no events match the filters.
		/// </returns>
		/// <remarks>
		/// This method does NOT enforce tenant isolation, returning events from all tenants.
		/// It should only be exposed to administrative or super-admin users with proper authorization.
		/// Each optional filter is applied sequentially using LINQ Where clauses.
		/// Date comparisons use inclusive boundaries, and string comparisons use case-insensitive ordinal comparison.
		/// The final result is sorted by timestamp in descending order.
		/// Unlike the tenant-scoped query, this starts with all events in the collection.
		/// </remarks>
		public Task<IEnumerable<AuditEvent>> GetAllEventsAsync(DateTime? startDate = null, DateTime? endDate = null, string? action = null, string? resource = null, CancellationToken cancellationToken = default)
		{
			// Start with all events (no tenant filtering for admin view)
			IEnumerable<AuditEvent> query = this._events.AsEnumerable();

			// Apply start date filter if provided (inclusive)
			if (startDate.HasValue)
			{
				query = query.Where(e => e.Timestamp >= startDate.Value);
			}

			// Apply end date filter if provided (inclusive)
			if (endDate.HasValue)
			{
				query = query.Where(e => e.Timestamp <= endDate.Value);
			}

			// Apply action filter if provided (case-insensitive)
			if (!string.IsNullOrWhiteSpace(action))
			{
				query = query.Where(e => e.Action.Equals(action, StringComparison.OrdinalIgnoreCase));
			}

			// Apply resource filter if provided (case-insensitive)
			if (!string.IsNullOrWhiteSpace(resource))
			{
				query = query.Where(e => e.Resource.Equals(resource, StringComparison.OrdinalIgnoreCase));
			}

			// Sort by timestamp descending (most recent first) and return
			return Task.FromResult<IEnumerable<AuditEvent>>(query.OrderByDescending(e => e.Timestamp));
		}

		/// <summary>
		/// Asynchronously retrieves audit events for a specific tenant with optional filtering.
		/// </summary>
		/// <param name="tenantId">The identifier of the tenant whose events to retrieve. Cannot be <see langword="null"/>.</param>
		/// <param name="startDate">Optional start date (inclusive) for filtering events by timestamp. If <see langword="null"/>, no lower bound is applied.</param>
		/// <param name="endDate">Optional end date (inclusive) for filtering events by timestamp. If <see langword="null"/>, no upper bound is applied.</param>
		/// <param name="action">Optional action filter to retrieve only events matching this action name. Matching is case-insensitive. If <see langword="null"/> or whitespace, no action filter is applied.</param>
		/// <param name="resource">Optional resource filter to retrieve only events affecting this resource type. Matching is case-insensitive. If <see langword="null"/> or whitespace, no resource filter is applied.</param>
		/// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete. Not used in this in-memory implementation.</param>
		/// <returns>
		/// A task that represents the asynchronous operation. The task result contains a collection of <see cref="AuditEvent"/>
		/// instances matching the specified criteria, ordered by timestamp in descending order (most recent first).
		/// Returns an empty collection if no events match the filters.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="tenantId"/> value is <see langword="null"/>.
		/// </exception>
		/// <remarks>
		/// This method enforces tenant isolation by first filtering to the specified tenant.
		/// Each optional filter is applied sequentially using LINQ Where clauses.
		/// Date comparisons use inclusive boundaries (greater-than-or-equal and less-than-or-equal).
		/// String comparisons for action and resource use case-insensitive ordinal comparison via <see cref="StringComparison.OrdinalIgnoreCase"/>.
		/// The final result is sorted by timestamp in descending order to show the most recent events first.
		/// The query is executed in-memory by iterating the concurrent bag.
		/// </remarks>
		public Task<IEnumerable<AuditEvent>> GetEventsAsync(TenantId tenantId, DateTime? startDate = null, DateTime? endDate = null, string? action = null, string? resource = null, CancellationToken cancellationToken = default)
		{
			// Validate that tenantId has a non-null value
			ArgumentNullException.ThrowIfNull(tenantId.Value, nameof(tenantId));

			// Start with tenant-scoped filtering to enforce isolation
			IEnumerable<AuditEvent> query = this._events.Where(e => e.TenantId.Value == tenantId.Value);

			// Apply start date filter if provided (inclusive)
			if (startDate.HasValue)
			{
				query = query.Where(e => e.Timestamp >= startDate.Value);
			}

			// Apply end date filter if provided (inclusive)
			if (endDate.HasValue)
			{
				query = query.Where(e => e.Timestamp <= endDate.Value);
			}

			// Apply action filter if provided (case-insensitive)
			if (!string.IsNullOrWhiteSpace(action))
			{
				query = query.Where(e => e.Action.Equals(action, StringComparison.OrdinalIgnoreCase));
			}

			// Apply resource filter if provided (case-insensitive)
			if (!string.IsNullOrWhiteSpace(resource))
			{
				query = query.Where(e => e.Resource.Equals(resource, StringComparison.OrdinalIgnoreCase));
			}

			// Sort by timestamp descending (most recent first) and return as enumerable
			return Task.FromResult(query.OrderByDescending(e => e.Timestamp).AsEnumerable());
		}

		#endregion
	}
}