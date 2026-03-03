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

using SaasSuite.Core.Middleware;

namespace SaasSuite.Core.Enumerations
{
	/// <summary>
	/// Represents the lifecycle state of a tenant's maintenance window.
	/// </summary>
	/// <remarks>
	/// Indicates the current status of scheduled maintenance activities and determines
	/// whether the maintenance window should be enforced by the <see cref="TenantMaintenanceMiddleware"/>.
	/// The status transitions follow a predictable lifecycle from scheduling through completion or cancellation.
	/// </remarks>
	public enum MaintenanceStatus
	{
		/// <summary>
		/// The maintenance window has been scheduled and is expected to start in the future.
		/// </summary>
		/// <remarks>
		/// The tenant is not yet affected by this maintenance window and all operations remain available.
		/// The system will automatically transition this window to <see cref="Active"/> when the scheduled start time is reached.
		/// This is the initial state for newly created maintenance windows.
		/// </remarks>
		Scheduled = 0,

		/// <summary>
		/// The maintenance window is currently active and in effect.
		/// </summary>
		/// <remarks>
		/// Tenant operations may be restricted, rate-limited, or completely unavailable during this period
		/// depending on the maintenance configuration. Requests to the tenant will typically be blocked by
		/// <see cref="Middleware.TenantMaintenanceMiddleware"/> with an HTTP 503 Service Unavailable response.
		/// The window transitions to this state when the scheduled start time is reached or when manually activated.
		/// </remarks>
		Active = 1,

		/// <summary>
		/// The maintenance window has ended successfully and all maintenance operations are complete.
		/// </summary>
		/// <remarks>
		/// The tenant is fully operational again and the maintenance window is no longer enforced.
		/// This is a terminal state indicating successful completion of all maintenance activities.
		/// The window should transition to this state when the scheduled end time is reached or when manually completed.
		/// Completed windows are retained for audit and historical reporting purposes.
		/// </remarks>
		Completed = 2,

		/// <summary>
		/// The maintenance window was cancelled and should not be enforced.
		/// </summary>
		/// <remarks>
		/// The tenant remains fully operational and the scheduled maintenance will not occur.
		/// This is a terminal state indicating that the maintenance was intentionally aborted before or during execution.
		/// Cancelled windows are retained for audit purposes and to track cancelled maintenance activities.
		/// A window can be cancelled from either <see cref="Scheduled"/> or <see cref="Active"/> status.
		/// </remarks>
		Cancelled = 3
	}
}