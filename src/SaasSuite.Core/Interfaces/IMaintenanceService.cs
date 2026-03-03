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

namespace SaasSuite.Core.Interfaces
{
	/// <summary>
	/// Provides tenant maintenance window scheduling, management, and enforcement operations.
	/// This service enables controlled downtime periods for tenant-specific operations such as
	/// upgrades, migrations, backups, or infrastructure changes.
	/// </summary>
	/// <remarks>
	/// Implementations are responsible for persisting maintenance windows and providing
	/// real-time status checks that can be used by middleware to block or rate-limit requests
	/// during maintenance periods. The service supports multiple concurrent maintenance windows
	/// per tenant with different statuses and schedules.
	/// </remarks>
	public interface IMaintenanceService
	{
		#region ' Methods '

		/// <summary>
		/// Cancels a previously scheduled maintenance window by setting its status to <see cref="MaintenanceStatus.Cancelled"/>.
		/// Cancelled windows will not be enforced by maintenance middleware.
		/// </summary>
		/// <param name="tenantId">The identifier of the tenant whose maintenance window should be cancelled.</param>
		/// <param name="windowId">The unique identifier of the maintenance window to cancel.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
		/// <returns>A task that represents the asynchronous operation.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="tenantId"/> is <see langword="null"/>.</exception>
		/// <exception cref="ArgumentException">Thrown when <paramref name="windowId"/> is <see langword="null"/> or whitespace.</exception>
		/// <remarks>
		/// If the specified window ID does not exist, the operation completes without error.
		/// This method only changes the window status and does not remove the window from storage,
		/// preserving it for auditing and historical purposes.
		/// </remarks>
		Task CancelMaintenanceAsync(TenantId tenantId, string windowId, CancellationToken cancellationToken = default);

		/// <summary>
		/// Schedules a new maintenance window for the specified tenant.
		/// Creates a maintenance period during which the tenant's access may be restricted or blocked.
		/// </summary>
		/// <param name="tenantId">The identifier of the tenant for which maintenance is being scheduled.</param>
		/// <param name="window">The maintenance window definition including start time, end time, status, and notification settings.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
		/// <returns>A task that represents the asynchronous operation.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="tenantId"/> or <paramref name="window"/> is <see langword="null"/>.</exception>
		/// <remarks>
		/// The maintenance window will be enforced by <see cref="TenantMaintenanceMiddleware"/>
		/// based on its scheduled times and status. Overlapping maintenance windows are allowed and
		/// will be evaluated independently.
		/// </remarks>
		Task ScheduleMaintenanceAsync(TenantId tenantId, MaintenanceWindow window, CancellationToken cancellationToken = default);

		/// <summary>
		/// Determines whether the specified tenant is currently under an active maintenance window.
		/// This method is called by middleware to decide whether to block or allow tenant requests.
		/// </summary>
		/// <param name="tenantId">The identifier of the tenant to check.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
		/// <returns>
		/// A task that represents the asynchronous operation. The task result is <see langword="true"/>
		/// if the tenant is currently under maintenance (has at least one active window); otherwise <see langword="false"/>.
		/// </returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="tenantId"/> is <see langword="null"/>.</exception>
		/// <remarks>
		/// This method checks for windows with status <see cref="MaintenanceStatus.Active"/>
		/// or scheduled windows where the current time falls between the start and end times.
		/// The check should be performant as it may be called on every request.
		/// </remarks>
		Task<bool> IsUnderMaintenanceAsync(TenantId tenantId, CancellationToken cancellationToken = default);

		/// <summary>
		/// Retrieves all scheduled maintenance windows for the specified tenant.
		/// Returns windows in all states including scheduled, active, completed, and cancelled.
		/// </summary>
		/// <param name="tenantId">The identifier of the tenant whose maintenance windows should be retrieved.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
		/// <returns>
		/// A task that represents the asynchronous operation. The task result contains a collection
		/// of <see cref="MaintenanceWindow"/> objects, or an empty collection if no windows exist.
		/// </returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="tenantId"/> is <see langword="null"/>.</exception>
		/// <remarks>
		/// This method returns all maintenance windows regardless of status or time range.
		/// Callers should filter the results based on their specific needs (e.g., only active or future windows).
		/// </remarks>
		Task<IEnumerable<MaintenanceWindow>> GetMaintenanceWindowsAsync(TenantId tenantId, CancellationToken cancellationToken = default);

		#endregion
	}
}