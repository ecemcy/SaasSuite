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

using SaasSuite.Core.Enumerations;
using SaasSuite.Core.Middleware;

namespace SaasSuite.Core
{
	/// <summary>
	/// Represents a scheduled maintenance window for a tenant.
	/// Defines the time period, status, and notification settings for planned tenant downtime or service restrictions.
	/// </summary>
	/// <remarks>
	/// Maintenance windows are used to schedule and manage planned downtime for tenant-specific operations
	/// such as database migrations, infrastructure upgrades, data backups, or system updates. During an active
	/// maintenance window, requests to the affected tenant may be blocked or rate-limited by
	/// <see cref="TenantMaintenanceMiddleware"/>.
	/// </remarks>
	public class MaintenanceWindow
	{
		#region ' Properties '

		/// <summary>
		/// Gets or sets a value indicating whether users should be notified about this maintenance window.
		/// Determines if notification systems should send alerts about the maintenance.
		/// </summary>
		/// <value>
		/// <see langword="true"/> to send user notifications about this maintenance window;
		/// <see langword="false"/> to perform silent maintenance without user notification.
		/// Defaults to <see langword="true"/>.
		/// </value>
		public bool NotifyUsers { get; set; } = true;

		/// <summary>
		/// Gets or sets the unique identifier for this maintenance window.
		/// Defaults to a new GUID string when a new instance is created.
		/// </summary>
		/// <value>
		/// A unique string identifier used to reference and manage this specific maintenance window.
		/// Used when cancelling or updating the maintenance window.
		/// </value>
		public string Id { get; set; } = Guid.NewGuid().ToString();

		/// <summary>
		/// Gets or sets the title or name of the maintenance window.
		/// Provides a brief description of the maintenance activity.
		/// </summary>
		/// <value>
		/// A short, human-readable title such as "Database Migration" or "Security Patch".
		/// Defaults to an empty string.
		/// </value>
		public string Title { get; set; } = string.Empty;

		/// <summary>
		/// Gets or sets a detailed description of the maintenance activities being performed.
		/// Optional field providing additional context about the maintenance work.
		/// </summary>
		/// <value>
		/// A detailed explanation of what maintenance will be performed, why it's necessary,
		/// and what impact users should expect. May be <see langword="null"/> if no description is provided.
		/// </value>
		public string? Description { get; set; }

		/// <summary>
		/// Gets or sets a custom notification message to send to users about the maintenance.
		/// Provides user-facing communication about the maintenance impact.
		/// </summary>
		/// <value>
		/// A custom message to display to users through notification channels (email, in-app banners, etc.).
		/// May be <see langword="null"/> to use default notification templates. Only relevant when
		/// <see cref="NotifyUsers"/> is <see langword="true"/>.
		/// </value>
		public string? NotificationMessage { get; set; }

		/// <summary>
		/// Gets or sets the scheduled end time for the maintenance window.
		/// Defines when the maintenance period is expected to complete.
		/// </summary>
		/// <value>
		/// The date and time when maintenance is scheduled to end. Uses <see cref="DateTimeOffset"/>
		/// to preserve timezone information. Should be later than <see cref="ScheduledStart"/>.
		/// </value>
		public DateTimeOffset ScheduledEnd { get; set; }

		/// <summary>
		/// Gets or sets the scheduled start time for the maintenance window.
		/// Defines when the maintenance period is expected to begin.
		/// </summary>
		/// <value>
		/// The date and time when maintenance is scheduled to start. Uses <see cref="DateTimeOffset"/>
		/// to preserve timezone information for accurate scheduling across regions.
		/// </value>
		public DateTimeOffset ScheduledStart { get; set; }

		/// <summary>
		/// Gets or sets the actual end time when maintenance completed.
		/// Populated when maintenance transitions to completed status.
		/// </summary>
		/// <value>
		/// The actual date and time when maintenance finished, or <see langword="null"/> if maintenance
		/// is still in progress, not yet started, or was cancelled. May differ from <see cref="ScheduledEnd"/>
		/// if maintenance finishes early or runs over schedule.
		/// </value>
		public DateTimeOffset? ActualEnd { get; set; }

		/// <summary>
		/// Gets or sets the actual start time when maintenance began.
		/// Populated when maintenance transitions from scheduled to active status.
		/// </summary>
		/// <value>
		/// The actual date and time when maintenance started, or <see langword="null"/> if maintenance
		/// has not yet begun or was cancelled. May differ from <see cref="ScheduledStart"/> if
		/// maintenance starts early or late.
		/// </value>
		public DateTimeOffset? ActualStart { get; set; }

		/// <summary>
		/// Gets or sets additional metadata associated with this maintenance window.
		/// Provides an extensibility mechanism for storing custom key-value data.
		/// </summary>
		/// <value>
		/// A dictionary of custom metadata key-value pairs for storing additional information
		/// such as ticket numbers, approval IDs, or system-specific tracking data.
		/// May be <see langword="null"/> if no additional metadata is needed.
		/// </value>
		public IDictionary<string, string>? Metadata { get; set; }

		/// <summary>
		/// Gets or sets the current status of the maintenance window lifecycle.
		/// Determines whether the maintenance window is enforced by middleware.
		/// </summary>
		/// <value>
		/// The current <see cref="MaintenanceStatus"/> of the window. Defaults to
		/// <see cref="MaintenanceStatus.Scheduled"/> for newly created windows.
		/// Only windows with <see cref="MaintenanceStatus.Active"/> status are enforced.
		/// </value>
		public MaintenanceStatus Status { get; set; } = MaintenanceStatus.Scheduled;

		/// <summary>
		/// Gets or sets the identifier of the tenant this maintenance window applies to.
		/// </summary>
		/// <value>
		/// The <see cref="Core.TenantId"/> of the tenant that will be affected by this maintenance period.
		/// </value>
		public TenantId TenantId { get; set; }

		#endregion
	}
}