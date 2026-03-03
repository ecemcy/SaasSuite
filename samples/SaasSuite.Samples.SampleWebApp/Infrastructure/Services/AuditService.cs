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
using SaasSuite.Samples.SampleWebApp.Infrastructure.Interfaces;
using SaasSuite.Samples.SampleWebApp.Infrastructure.Models;

namespace SaasSuite.Samples.SampleWebApp.Infrastructure.Services
{
	/// <summary>
	/// In-memory implementation of <see cref="IAuditService"/> for demonstration purposes.
	/// </summary>
	/// <remarks>
	/// <para>
	/// This implementation stores audit events in memory, which is suitable for demos and testing but not for production use.
	/// </para>
	/// <para>
	/// In production environments, audit events should be persisted to:
	/// </para>
	/// <list type="bullet">
	/// <item><description>A database with appropriate retention policies</description></item>
	/// <item><description>A dedicated logging service (e.g., Azure Monitor, Elasticsearch)</description></item>
	/// <item><description>An immutable audit log store for compliance requirements</description></item>
	/// </list>
	/// </remarks>
	public class AuditService
		: IAuditService
	{
		#region ' Fields '

		/// <summary>
		/// Time provider for generating consistent timestamps.
		/// </summary>
		private readonly ITimeProvider _timeProvider;

		/// <summary>
		/// In-memory collection storing all audit events.
		/// </summary>
		private readonly List<AuditEvent> _events = new List<AuditEvent>();

		#endregion

		#region ' Constructors '

		/// <summary>
		/// Initializes a new instance of the <see cref="AuditService"/> class.
		/// </summary>
		/// <param name="timeProvider">The time provider for timestamp generation.</param>
		public AuditService(ITimeProvider timeProvider)
		{
			this._timeProvider = timeProvider;
		}

		#endregion

		#region ' Methods '

		/// <inheritdoc/>
		public Task LogAsync(TenantId tenantId, string action, string category, string details, Dictionary<string, string>? metadata = null, string? correlationId = null)
		{
			AuditEvent evt = new AuditEvent
			{
				TenantId = tenantId,
				Action = action,
				Category = category,
				Details = details,
				Timestamp = this._timeProvider.UtcNow,
				CorrelationId = correlationId ?? Guid.NewGuid().ToString(),
				Metadata = metadata ?? new Dictionary<string, string>()
			};

			this._events.Add(evt);
			return Task.CompletedTask;
		}

		/// <inheritdoc/>
		public Task<IEnumerable<AuditEvent>> GetEventsAsync(TenantId tenantId, int limit = 100)
		{
			// Filter events by tenant and return most recent first
			List<AuditEvent> events = this._events
				.Where(e => e.TenantId.Value == tenantId.Value)
				.OrderByDescending(e => e.Timestamp)
				.Take(limit)
				.ToList();

			return Task.FromResult<IEnumerable<AuditEvent>>(events);
		}

		#endregion
	}
}