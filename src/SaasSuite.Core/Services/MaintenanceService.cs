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

using SaasSuite.Core.Enumerations;
using SaasSuite.Core.Interfaces;

namespace SaasSuite.Core.Services
{
	/// <summary>
	/// In-memory implementation of the <see cref="IMaintenanceService"/> interface.
	/// Provides maintenance window management using thread-safe collections for testing and development scenarios.
	/// </summary>
	/// <remarks>
	/// This implementation stores maintenance windows in memory using <see cref="ConcurrentDictionary{TKey, TValue}"/>.
	/// It is suitable for development, testing, and single-instance deployments. For production multi-instance
	/// deployments, replace this with a persistent implementation using a database or distributed cache.
	/// <para>
	/// Key characteristics:
	/// <list type="bullet">
	/// <item><description>Data is lost when the application restarts</description></item>
	/// <item><description>Not synchronized across multiple application instances</description></item>
	/// <item><description>Thread-safe for concurrent access within a single process</description></item>
	/// <item><description>Lightweight with no external dependencies</description></item>
	/// </list>
	/// </para>
	/// </remarks>
	public class MaintenanceService
		: IMaintenanceService
	{
		#region ' Fields '

		/// <summary>
		/// Lock object for synchronizing access to the maintenance window lists within the dictionary.
		/// Ensures thread-safety when modifying individual tenant window collections.
		/// </summary>
		private readonly object _lock = new object();

		/// <summary>
		/// Thread-safe dictionary storing maintenance windows by tenant ID.
		/// Key is the tenant ID string, value is a list of maintenance windows for that tenant.
		/// </summary>
		private readonly ConcurrentDictionary<string, List<MaintenanceWindow>> _tenantWindows = new ConcurrentDictionary<string, List<MaintenanceWindow>>();

		#endregion

		#region ' Methods '

		/// <summary>
		/// Cancels a scheduled maintenance window by updating its status to <see cref="MaintenanceStatus.Cancelled"/>.
		/// The window remains in storage for audit purposes but will not be enforced.
		/// </summary>
		/// <param name="tenantId">The identifier of the tenant whose maintenance window should be cancelled.</param>
		/// <param name="windowId">The unique identifier of the maintenance window to cancel.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests. Not used in this implementation.</param>
		/// <returns>A completed task representing the synchronous operation.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="tenantId"/> is <see langword="null"/>.</exception>
		/// <exception cref="ArgumentException">Thrown when <paramref name="windowId"/> is <see langword="null"/> or whitespace.</exception>
		/// <remarks>
		/// If the specified window ID does not exist for the tenant, the operation completes without error.
		/// Only the status is changed; all other window properties remain unchanged.
		/// Cancelled windows will not be considered during maintenance enforcement checks.
		/// </remarks>
		public Task CancelMaintenanceAsync(TenantId tenantId, string windowId, CancellationToken cancellationToken = default)
		{
			ArgumentNullException.ThrowIfNull(tenantId.Value, nameof(tenantId));

			if (string.IsNullOrWhiteSpace(windowId))
			{
				throw new ArgumentException("Window ID cannot be null or whitespace", nameof(windowId));
			}

			string key = tenantId.Value;

			if (this._tenantWindows.TryGetValue(key, out List<MaintenanceWindow>? windows))
			{
				lock (this._lock)
				{
					// Find the window by ID and update its status
					MaintenanceWindow? window = windows.FirstOrDefault(w => w.Id == windowId);
					window?.Status = MaintenanceStatus.Cancelled;
				}
			}

			return Task.CompletedTask;
		}

		/// <summary>
		/// Schedules a new maintenance window for the specified tenant by adding it to the in-memory store.
		/// </summary>
		/// <param name="tenantId">The identifier of the tenant for which maintenance is being scheduled.</param>
		/// <param name="window">The maintenance window to schedule with all required details.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests. Not used in this implementation.</param>
		/// <returns>A completed task representing the synchronous operation.</returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="tenantId"/> or <paramref name="window"/> is <see langword="null"/>.
		/// </exception>
		/// <remarks>
		/// The maintenance window is added to a per-tenant list in the concurrent dictionary.
		/// Multiple maintenance windows can be scheduled for the same tenant.
		/// The window is stored with its current status and will be evaluated during maintenance checks.
		/// </remarks>
		public Task ScheduleMaintenanceAsync(TenantId tenantId, MaintenanceWindow window, CancellationToken cancellationToken = default)
		{
			ArgumentNullException.ThrowIfNull(tenantId.Value, nameof(tenantId));
			ArgumentNullException.ThrowIfNull(window);

			string key = tenantId.Value;

			lock (this._lock)
			{
				// Get or create the list of maintenance windows for this tenant
				List<MaintenanceWindow> windows = this._tenantWindows.GetOrAdd(key, _ => new List<MaintenanceWindow>());
				windows.Add(window);
			}

			return Task.CompletedTask;
		}

		/// <summary>
		/// Determines whether the specified tenant is currently under an active maintenance window.
		/// Checks for windows that are explicitly active or scheduled windows that are currently in effect.
		/// </summary>
		/// <param name="tenantId">The identifier of the tenant to check.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests. Not used in this implementation.</param>
		/// <returns>
		/// A completed task with result <see langword="true"/> if the tenant has at least one active
		/// maintenance window; otherwise <see langword="false"/>.
		/// </returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="tenantId"/> is <see langword="null"/>.</exception>
		/// <remarks>
		/// A tenant is considered under maintenance if any window meets one of these conditions:
		/// <list type="bullet">
		/// <item><description>Status is explicitly <see cref="MaintenanceStatus.Active"/></description></item>
		/// <item><description>Status is <see cref="MaintenanceStatus.Scheduled"/> and current time is between start and end times</description></item>
		/// </list>
		/// <para>
		/// Windows with status <see cref="MaintenanceStatus.Completed"/> or <see cref="MaintenanceStatus.Cancelled"/>
		/// are not considered active. If the tenant has no maintenance windows, returns <see langword="false"/>.
		/// </para>
		/// </remarks>
		public Task<bool> IsUnderMaintenanceAsync(TenantId tenantId, CancellationToken cancellationToken = default)
		{
			ArgumentNullException.ThrowIfNull(tenantId.Value, nameof(tenantId));

			string key = tenantId.Value;

			if (this._tenantWindows.TryGetValue(key, out List<MaintenanceWindow>? windows))
			{
				lock (this._lock)
				{
					DateTimeOffset now = DateTimeOffset.UtcNow;

					// Check if any window is explicitly active or implicitly active based on time
					bool isUnderMaintenance = windows.Any(w =>
						w.Status == MaintenanceStatus.Active ||
						(w.Status == MaintenanceStatus.Scheduled &&
						 w.ScheduledStart <= now &&
						 w.ScheduledEnd >= now));

					return Task.FromResult(isUnderMaintenance);
				}
			}

			// No maintenance windows exist for this tenant
			return Task.FromResult(false);
		}

		/// <summary>
		/// Retrieves all maintenance windows for the specified tenant from the in-memory store.
		/// Returns windows in all states including scheduled, active, completed, and cancelled.
		/// </summary>
		/// <param name="tenantId">The identifier of the tenant whose maintenance windows should be retrieved.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests. Not used in this implementation.</param>
		/// <returns>
		/// A completed task containing a collection of <see cref="MaintenanceWindow"/> objects,
		/// or an empty collection if no windows exist for the tenant.
		/// </returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="tenantId"/> is <see langword="null"/>.</exception>
		/// <remarks>
		/// The returned collection is a snapshot copy to prevent external modification of the internal list.
		/// If the tenant has no maintenance windows, an empty array is returned rather than <see langword="null"/>.
		/// </remarks>
		public Task<IEnumerable<MaintenanceWindow>> GetMaintenanceWindowsAsync(TenantId tenantId, CancellationToken cancellationToken = default)
		{
			ArgumentNullException.ThrowIfNull(tenantId.Value, nameof(tenantId));

			string key = tenantId.Value;

			if (this._tenantWindows.TryGetValue(key, out List<MaintenanceWindow>? windows))
			{
				lock (this._lock)
				{
					// Return a copy to prevent external modification
					return Task.FromResult<IEnumerable<MaintenanceWindow>>(windows.ToList());
				}
			}

			// Return empty collection if tenant has no maintenance windows
			return Task.FromResult<IEnumerable<MaintenanceWindow>>(Array.Empty<MaintenanceWindow>());
		}

		#endregion
	}
}