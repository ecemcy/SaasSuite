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

using SaasSuite.Core;
using SaasSuite.Seats.Interfaces;

namespace SaasSuite.Seats.Services
{
	/// <summary>
	/// In-memory implementation of <see cref="ISeatService"/> using thread-safe collections for seat management.
	/// Provides seat allocation, consumption, and release operations suitable for development, testing,
	/// and single-instance production deployments.
	/// </summary>
	/// <remarks>
	/// This implementation stores all seat allocation data in memory using thread-safe concurrent collections
	/// and explicit locking for atomic operations. Key characteristics:
	/// <list type="bullet">
	/// <item><description>All data is lost when the application restarts - not persistent</description></item>
	/// <item><description>Thread-safe for concurrent access within a single process</description></item>
	/// <item><description>Not synchronized across multiple application instances</description></item>
	/// <item><description>Suitable for development, testing, and single-instance deployments</description></item>
	/// <item><description>Zero external dependencies or configuration required</description></item>
	/// <item><description>Default allocation of 5 seats per tenant if not explicitly configured</description></item>
	/// </list>
	/// <para>
	/// For production multi-instance deployments, consider replacing this implementation with a persistent
	/// version that uses:
	/// <list type="bullet">
	/// <item><description>Database storage (SQL Server, PostgreSQL, etc.) for durability and consistency</description></item>
	/// <item><description>Distributed cache (Redis, Memcached) for performance and cross-instance synchronization</description></item>
	/// <item><description>Distributed locking mechanisms (Redis locks, database pessimistic locks) for atomic operations</description></item>
	/// <item><description>Event sourcing or change tracking for audit trails</description></item>
	/// </list>
	/// </para>
	/// <para>
	/// The implementation uses a two-level locking strategy:
	/// <list type="number">
	/// <item><description>Outer concurrent dictionary for tenant-level thread safety</description></item>
	/// <item><description>Inner per-tenant locks for atomic seat consumption and release operations</description></item>
	/// </list>
	/// This approach minimizes lock contention by isolating locks to individual tenants rather than
	/// using a global lock for all seat operations.
	/// </para>
	/// </remarks>
	public class SeatService
		: ISeatService
	{
		#region ' Fields '

		/// <summary>
		/// Thread-safe dictionary storing seat allocation data by tenant ID.
		/// Key is the tenant ID string, value is a <see cref="TenantSeats"/> object containing
		/// seat limits, active users, and synchronization primitives for that tenant.
		/// </summary>
		/// <remarks>
		/// The concurrent dictionary provides thread-safe operations for adding and retrieving tenant data.
		/// Each tenant has its own independent <see cref="TenantSeats"/> instance, enabling isolated
		/// seat management without cross-tenant interference. The dictionary grows dynamically as new
		/// tenants consume seats, with no pre-allocation required.
		/// </remarks>
		private readonly ConcurrentDictionary<string, TenantSeats> _tenantSeats = new ConcurrentDictionary<string, TenantSeats>();

		#endregion

		#region ' Methods '

		/// <summary>
		/// Retrieves or creates the <see cref="TenantSeats"/> data structure for the specified tenant.
		/// Ensures a tenant entry exists in the dictionary with default values if not already present.
		/// </summary>
		/// <param name="tenantId">The unique identifier of the tenant for which to get or create seat data.</param>
		/// <returns>
		/// The existing or newly created <see cref="TenantSeats"/> instance for the specified tenant.
		/// New tenants are initialized with a default allocation of 5 seats and an empty active users set.
		/// </returns>
		/// <remarks>
		/// This method is idempotent and thread-safe due to the atomic GetOrAdd operation on the concurrent dictionary.
		/// Multiple concurrent calls for the same tenant will result in a single <see cref="TenantSeats"/> instance.
		/// The default allocation of 5 seats can be changed via <see cref="AllocateSeatsAsync"/> after creation.
		/// </remarks>
		private TenantSeats GetOrCreateTenantSeats(TenantId tenantId)
		{
			// Atomically get existing or add new TenantSeats for this tenant
			// The factory function only executes if the key doesn't exist, ensuring single initialization
			return this._tenantSeats.GetOrAdd(tenantId.Value, _ => new TenantSeats
			{
				MaxSeats = 5, // Default to 5 seats for new tenants without explicit allocation
				ActiveUsers = new HashSet<string>(), // Initialize empty set of active users
				Lock = new object() // Create dedicated lock object for this tenant's atomic operations
			});
		}

		/// <summary>
		/// Configures the maximum number of seats allocated to the specified tenant.
		/// Updates the seat capacity limit that determines how many concurrent users can access the system.
		/// </summary>
		/// <param name="tenantId">The unique identifier of the tenant for which to allocate seats. Cannot be <see langword="null"/>.</param>
		/// <param name="maxSeats">
		/// The maximum number of seats to allocate. Must be greater than zero.
		/// This value sets the upper limit on concurrent user access for the tenant.
		/// </param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests. Not used in this implementation but included for interface compliance.</param>
		/// <returns>A completed task indicating the allocation has been configured.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="tenantId"/> is <see langword="null"/>.</exception>
		/// <exception cref="ArgumentException">Thrown when <paramref name="maxSeats"/> is less than or equal to zero.</exception>
		/// <remarks>
		/// This method updates the seat allocation and takes effect immediately for subsequent seat consumption attempts.
		/// Reducing the seat count does not automatically disconnect or remove existing users who have already consumed seats;
		/// they will retain their seats until they explicitly release them or their sessions expire.
		/// The new limit is enforced only for new seat consumption attempts after this call.
		/// </remarks>
		public Task AllocateSeatsAsync(TenantId tenantId, int maxSeats, CancellationToken cancellationToken = default)
		{
			// Validate that tenant ID is not null
			ArgumentNullException.ThrowIfNull(tenantId.Value, nameof(tenantId));

			// Validate that max seats is a positive number
			if (maxSeats <= 0)
			{
				throw new ArgumentException("Max seats must be greater than zero", nameof(maxSeats));
			}

			// Get or create the seat data for this tenant
			TenantSeats seats = this.GetOrCreateTenantSeats(tenantId);

			// Update the maximum seat allocation
			// Note: This is a simple assignment and doesn't require locking because it's an atomic operation
			seats.MaxSeats = maxSeats;

			return Task.CompletedTask;
		}

		/// <summary>
		/// Releases a seat previously consumed by the specified user, making it available for other users.
		/// This operation is idempotent and safe to call even if the user does not currently hold a seat.
		/// </summary>
		/// <param name="tenantId">The unique identifier of the tenant from which to release the seat. Cannot be <see langword="null"/>.</param>
		/// <param name="userId">
		/// The unique identifier of the user releasing the seat. Must be a non-empty, non-whitespace string.
		/// This should match the identifier used in <see cref="TryConsumeSeatAsync"/>.
		/// </param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests. Not used in this implementation but included for interface compliance.</param>
		/// <returns>A completed task indicating the seat has been released or was not held by the user.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="tenantId"/> is <see langword="null"/>.</exception>
		/// <exception cref="ArgumentException">Thrown when <paramref name="userId"/> is <see langword="null"/>, empty, or contains only whitespace.</exception>
		/// <remarks>
		/// This method uses per-tenant locking to ensure atomic seat release operations:
		/// <list type="bullet">
		/// <item><description>Checks if the tenant has any seat data (skips operation if no data exists)</description></item>
		/// <item><description>Acquires exclusive lock on the tenant's seat data</description></item>
		/// <item><description>Removes the user from the active users set</description></item>
		/// <item><description>Releases the lock automatically when exiting the locked block</description></item>
		/// </list>
		/// <para>
		/// The operation is idempotent:
		/// <list type="bullet">
		/// <item><description>If the user doesn't hold a seat, the remove operation has no effect</description></item>
		/// <item><description>If the tenant doesn't exist in the dictionary, the method completes without error</description></item>
		/// <item><description>Multiple calls with the same user ID are safe and have no additional effect</description></item>
		/// </list>
		/// </para>
		/// <para>
		/// After successful release:
		/// <list type="bullet">
		/// <item><description>The seat becomes immediately available for other users to consume</description></item>
		/// <item><description>Used seat count decreases by one</description></item>
		/// <item><description>Available seat count increases by one</description></item>
		/// <item><description>The user is removed from the active users list</description></item>
		/// </list>
		/// </para>
		/// </remarks>
		public Task ReleaseSeatAsync(TenantId tenantId, string userId, CancellationToken cancellationToken = default)
		{
			// Validate that tenant ID is not null
			ArgumentNullException.ThrowIfNull(tenantId.Value, nameof(tenantId));

			// Validate that user ID is not null, empty, or whitespace
			if (string.IsNullOrWhiteSpace(userId))
			{
				throw new ArgumentException("User ID cannot be null or whitespace", nameof(userId));
			}

			// Attempt to get the seat data for this tenant (without creating if it doesn't exist)
			if (this._tenantSeats.TryGetValue(tenantId.Value, out TenantSeats? seats))
			{
				// Use per-tenant lock to ensure atomic seat release operation
				lock (seats.Lock)
				{
					// Remove the user from active users set
					// HashSet.Remove returns false if element not present, which is fine (idempotent behavior)
					_ = seats.ActiveUsers.Remove(userId);
				}
			}

			// If tenant doesn't exist in dictionary, nothing to release (idempotent behavior)
			return Task.CompletedTask;
		}

		/// <summary>
		/// Attempts to consume (allocate) a seat for the specified user within the tenant's seat quota.
		/// This operation is atomic and thread-safe using per-tenant locking to prevent race conditions.
		/// </summary>
		/// <param name="tenantId">The unique identifier of the tenant requesting seat consumption. Cannot be <see langword="null"/>.</param>
		/// <param name="userId">
		/// The unique identifier of the user attempting to consume a seat. Must be a non-empty, non-whitespace string.
		/// This identifier should be unique within the tenant's scope.
		/// </param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests. Not used in this implementation but included for interface compliance.</param>
		/// <returns>
		/// A completed task with result <see langword="true"/> if a seat was successfully allocated to the user
		/// or the user already had a seat; <see langword="false"/> if the tenant has reached its maximum seat limit
		/// and no seats are available for new users.
		/// </returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="tenantId"/> is <see langword="null"/>.</exception>
		/// <exception cref="ArgumentException">Thrown when <paramref name="userId"/> is <see langword="null"/>, empty, or contains only whitespace.</exception>
		/// <remarks>
		/// This method implements atomic check-and-allocate logic using a per-tenant lock:
		/// <list type="number">
		/// <item><description>Acquire exclusive lock on the tenant's seat data</description></item>
		/// <item><description>Check if user already has a seat (idempotent - return true immediately)</description></item>
		/// <item><description>Check if seat capacity is available (used seats less than max seats)</description></item>
		/// <item><description>If available, add user to active users set and return true</description></item>
		/// <item><description>If not available, return false without modifying state</description></item>
		/// <item><description>Release lock automatically when exiting the locked block</description></item>
		/// </list>
		/// <para>
		/// The per-tenant locking strategy ensures that:
		/// <list type="bullet">
		/// <item><description>No two users can simultaneously consume the last available seat</description></item>
		/// <item><description>Seat limit is never exceeded by concurrent requests</description></item>
		/// <item><description>Idempotent behavior is guaranteed for repeated calls with the same user ID</description></item>
		/// <item><description>Different tenants can consume seats concurrently without lock contention</description></item>
		/// </list>
		/// </para>
		/// </remarks>
		public Task<bool> TryConsumeSeatAsync(TenantId tenantId, string userId, CancellationToken cancellationToken = default)
		{
			// Validate that tenant ID is not null
			ArgumentNullException.ThrowIfNull(tenantId.Value, nameof(tenantId));

			// Validate that user ID is not null, empty, or whitespace
			if (string.IsNullOrWhiteSpace(userId))
			{
				throw new ArgumentException("User ID cannot be null or whitespace", nameof(userId));
			}

			// Get or create the seat data for this tenant
			TenantSeats seats = this.GetOrCreateTenantSeats(tenantId);

			// Use per-tenant lock to ensure atomic check-and-allocate operation
			lock (seats.Lock)
			{
				// Check if this user already has a seat allocated (idempotent behavior)
				if (seats.ActiveUsers.Contains(userId))
				{
					return Task.FromResult(true); // User already has a seat, return success
				}

				// Check if there are available seats (current usage below the limit)
				if (seats.ActiveUsers.Count >= seats.MaxSeats)
				{
					return Task.FromResult(false); // No seats available, return failure
				}

				// Seat is available and user doesn't have one - allocate the seat
				_ = seats.ActiveUsers.Add(userId);
				return Task.FromResult(true); // Seat successfully allocated
			}
		}

		/// <summary>
		/// Retrieves the current seat usage statistics for the specified tenant.
		/// Provides a snapshot of seat allocation, consumption, and active users.
		/// </summary>
		/// <param name="tenantId">The unique identifier of the tenant for which to retrieve seat usage information. Cannot be <see langword="null"/>.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests. Not used in this implementation but included for interface compliance.</param>
		/// <returns>
		/// A completed task containing a <see cref="SeatUsage"/> object with comprehensive seat statistics
		/// including maximum seats, used seats, available seats, and the list of active user identifiers.
		/// </returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="tenantId"/> is <see langword="null"/>.</exception>
		/// <remarks>
		/// This method is synchronous despite returning a <see cref="Task"/> to maintain interface compatibility
		/// with asynchronous implementations that may perform I/O operations. The returned data represents
		/// a point-in-time snapshot and may change immediately after the call as users consume or release seats.
		/// The active users list is a copy to prevent external modification of the internal state.
		/// </remarks>
		public Task<SeatUsage> GetSeatUsageAsync(TenantId tenantId, CancellationToken cancellationToken = default)
		{
			// Validate that tenant ID is not null
			ArgumentNullException.ThrowIfNull(tenantId.Value, nameof(tenantId));

			// Get or create the seat data for this tenant (ensures tenant exists with defaults)
			TenantSeats seats = this.GetOrCreateTenantSeats(tenantId);

			// Create a snapshot of current seat usage
			// Note: We don't lock here because we're reading atomic properties and creating a copy of the list
			SeatUsage usage = new SeatUsage
			{
				MaxSeats = seats.MaxSeats, // Current seat allocation limit
				UsedSeats = seats.ActiveUsers.Count, // Number of users currently holding seats
				ActiveUsers = seats.ActiveUsers.ToList() // Copy of active user IDs (prevents external modification)
			};

			// Return the usage snapshot as a completed task
			return Task.FromResult(usage);
		}

		#endregion

		#region ' Classes '

		/// <summary>
		/// Internal data structure holding seat allocation information for a single tenant.
		/// Encapsulates the maximum seat limit, active users set, and synchronization primitive
		/// used for thread-safe seat operations.
		/// </summary>
		/// <remarks>
		/// This class is private to the <see cref="SeatService"/> implementation and is not exposed
		/// to external consumers. It serves as a container for:
		/// <list type="bullet">
		/// <item><description>Maximum seat allocation configured for the tenant</description></item>
		/// <item><description>HashSet of user identifiers currently occupying seats</description></item>
		/// <item><description>Lock object for synchronizing concurrent seat consumption and release</description></item>
		/// </list>
		/// Each tenant has its own independent instance stored in the <see cref="_tenantSeats"/> dictionary.
		/// </remarks>
		private class TenantSeats
		{
			#region ' Properties '

			/// <summary>
			/// Gets or sets the maximum number of seats allocated to this tenant.
			/// Determines the upper limit on concurrent users that can consume seats.
			/// </summary>
			/// <value>
			/// A positive integer representing the seat capacity. Defaults to 5 for new tenants
			/// if not explicitly configured via <see cref="AllocateSeatsAsync"/>.
			/// </value>
			public int MaxSeats { get; set; }

			/// <summary>
			/// Gets or sets the synchronization primitive used for thread-safe access to this tenant's seat data.
			/// Ensures atomic operations when checking availability and modifying the <see cref="ActiveUsers"/> set.
			/// </summary>
			/// <value>
			/// A dedicated lock object instance used exclusively for this tenant. Using per-tenant locks
			/// instead of a global lock improves concurrency by allowing different tenants to perform
			/// seat operations simultaneously without lock contention. This object should never be
			/// <see langword="null"/> and is initialized during <see cref="TenantSeats"/> construction.
			/// </value>
			public object Lock { get; set; } = new object();

			/// <summary>
			/// Gets or sets the collection of user identifiers currently holding seats.
			/// Each entry represents a user who has successfully consumed a seat via <see cref="TryConsumeSeatAsync"/>.
			/// </summary>
			/// <value>
			/// A mutable <see cref="HashSet{T}"/> containing unique user identifiers. HashSet is used for:
			/// <list type="bullet">
			/// <item><description>O(1) lookup time to check if a user already has a seat</description></item>
			/// <item><description>O(1) insertion time to add new users</description></item>
			/// <item><description>O(1) removal time to release seats</description></item>
			/// <item><description>Automatic prevention of duplicate user entries</description></item>
			/// </list>
			/// The count of this set represents the number of used seats and should never exceed <see cref="MaxSeats"/>
			/// due to the atomic check-and-allocate logic in <see cref="TryConsumeSeatAsync"/>.
			/// </value>
			public HashSet<string> ActiveUsers { get; set; } = new HashSet<string>();

			#endregion
		}

		#endregion
	}
}