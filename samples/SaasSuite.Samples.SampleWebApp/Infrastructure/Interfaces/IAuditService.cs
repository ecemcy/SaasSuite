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
using SaasSuite.Samples.SampleWebApp.Infrastructure.Models;

namespace SaasSuite.Samples.SampleWebApp.Infrastructure.Interfaces
{
	/// <summary>
	/// Provides services for logging and retrieving tenant-scoped audit events for compliance and monitoring.
	/// </summary>
	/// <remarks>
	/// Audit events track important actions and changes within a tenant, providing an immutable record
	/// for compliance, troubleshooting, and security analysis.
	/// </remarks>
	public interface IAuditService
	{
		#region ' Methods '

		/// <summary>
		/// Logs an audit event for a specific tenant with optional metadata and correlation tracking.
		/// </summary>
		/// <param name="tenantId">The identifier of the tenant this event belongs to.</param>
		/// <param name="action">The action identifier describing what occurred (e.g., "user.created", "subscription.upgraded").</param>
		/// <param name="category">The logical category or domain of the event (e.g., "User Management", "Subscription").</param>
		/// <param name="details">Human-readable description providing context about the event.</param>
		/// <param name="metadata">Optional key-value pairs providing additional structured context. Default is <see langword="null"/>.</param>
		/// <param name="correlationId">Optional identifier for grouping related events across operations. If <see langword="null"/>, a new GUID will be generated. Default is <see langword="null"/>.</param>
		/// <returns>A task representing the asynchronous logging operation.</returns>
		/// <remarks>
		/// Use consistent action naming conventions (e.g., "resource.verb" format) to enable effective filtering and analysis.
		/// </remarks>
		Task LogAsync(TenantId tenantId, string action, string category, string details, Dictionary<string, string>? metadata = null, string? correlationId = null);

		/// <summary>
		/// Retrieves audit events for a specific tenant, ordered by most recent first.
		/// </summary>
		/// <param name="tenantId">The identifier of the tenant whose events to retrieve.</param>
		/// <param name="limit">Maximum number of events to return. Default is 100.</param>
		/// <returns>A task that represents the asynchronous operation and contains a collection of <see cref="AuditEvent"/> instances ordered by timestamp descending.</returns>
		Task<IEnumerable<AuditEvent>> GetEventsAsync(TenantId tenantId, int limit = 100);

		#endregion
	}
}