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

namespace SaasSuite.Seats.Interfaces
{
	/// <summary>
	/// Defines operations for managing seat allocation and usage tracking in multi-tenant environments.
	/// Provides functionality to control concurrent user access based on subscription limits and seat quotas.
	/// This service enables enforcement of user capacity limits per tenant, ensuring tenants do not exceed
	/// their allocated seat counts.
	/// </summary>
	/// <remarks>
	/// Seat management is a critical component of SaaS applications that implement per-user licensing models.
	/// This service supports:
	/// <list type="bullet">
	/// <item><description>Configurable seat limits per tenant based on subscription tiers</description></item>
	/// <item><description>Real-time tracking of active users consuming seats</description></item>
	/// <item><description>Thread-safe seat allocation and release operations</description></item>
	/// <item><description>Prevention of over-subscription beyond allocated seat capacity</description></item>
	/// <item><description>Seat usage monitoring and reporting for billing and analytics</description></item>
	/// </list>
	/// <para>
	/// Common use cases include:
	/// <list type="bullet">
	/// <item><description>Enforcing named-user licensing models (one seat per active user)</description></item>
	/// <item><description>Implementing concurrent user limits for session-based access</description></item>
	/// <item><description>Preventing unauthorized account sharing across multiple users</description></item>
	/// <item><description>Tracking user activity for billing and compliance purposes</description></item>
	/// <item><description>Supporting tiered pricing based on seat count (5 seats, 10 seats, unlimited)</description></item>
	/// </list>
	/// </para>
	/// <para>
	/// Implementations should consider:
	/// <list type="bullet">
	/// <item><description>Thread safety for concurrent seat consumption and release</description></item>
	/// <item><description>Persistence strategies for seat allocations across application restarts</description></item>
	/// <item><description>Distributed locking in multi-instance deployments</description></item>
	/// <item><description>Seat expiration policies for inactive users</description></item>
	/// <item><description>Graceful handling of seat limit changes during active sessions</description></item>
	/// </list>
	/// </para>
	/// </remarks>
	public interface ISeatService
	{
		#region ' Methods '

		/// <summary>
		/// Configures the maximum number of seats allocated to the specified tenant.
		/// This operation sets the seat capacity limit that determines how many concurrent users can access the system.
		/// </summary>
		/// <param name="tenantId">The unique identifier of the tenant for which to allocate seats. Cannot be <see langword="null"/>.</param>
		/// <param name="maxSeats">
		/// The maximum number of seats to allocate. Must be greater than zero. This value determines the maximum
		/// number of concurrent users that can consume seats for this tenant.
		/// </param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests during the asynchronous operation.</param>
		/// <returns>A task that represents the asynchronous operation. The task completes when seat allocation is configured.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="tenantId"/> is <see langword="null"/>.</exception>
		/// <exception cref="ArgumentException">Thrown when <paramref name="maxSeats"/> is less than or equal to zero.</exception>
		/// <remarks>
		/// This method is typically called during:
		/// <list type="bullet">
		/// <item><description>Tenant onboarding and provisioning</description></item>
		/// <item><description>Subscription tier upgrades or downgrades</description></item>
		/// <item><description>Manual seat allocation adjustments by administrators</description></item>
		/// <item><description>Billing period renewals with different seat counts</description></item>
		/// </list>
		/// <para>
		/// When reducing seat allocation (<paramref name="maxSeats"/> is less than current allocation):
		/// <list type="bullet">
		/// <item><description>Existing users currently occupying seats are not automatically disconnected</description></item>
		/// <item><description>The new limit is enforced for subsequent seat consumption attempts</description></item>
		/// <item><description>Administrators should notify affected users before reducing limits</description></item>
		/// <item><description>Consider implementing graceful degradation or user notification mechanisms</description></item>
		/// </list>
		/// </para>
		/// <para>
		/// When increasing seat allocation:
		/// <list type="bullet">
		/// <item><description>Additional seats become immediately available for new users</description></item>
		/// <item><description>Waiting or blocked users may now be able to access the system</description></item>
		/// <item><description>The change takes effect for subsequent <see cref="TryConsumeSeatAsync"/> calls</description></item>
		/// </list>
		/// </para>
		/// <para>
		/// Implementations should persist the allocation to survive application restarts and ensure
		/// consistency across distributed deployments.
		/// </para>
		/// </remarks>
		Task AllocateSeatsAsync(TenantId tenantId, int maxSeats, CancellationToken cancellationToken = default);

		/// <summary>
		/// Releases a seat previously consumed by the specified user, making it available for other users.
		/// This operation is idempotent and safe to call even if the user does not currently hold a seat.
		/// </summary>
		/// <param name="tenantId">The unique identifier of the tenant from which to release the seat. Cannot be <see langword="null"/>.</param>
		/// <param name="userId">
		/// The unique identifier of the user releasing the seat. Must be a non-empty, non-whitespace string.
		/// This should match the identifier used in <see cref="TryConsumeSeatAsync"/>.
		/// </param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests during the asynchronous operation.</param>
		/// <returns>A task that represents the asynchronous operation. The task completes when the seat has been released.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="tenantId"/> is <see langword="null"/>.</exception>
		/// <exception cref="ArgumentException">Thrown when <paramref name="userId"/> is <see langword="null"/>, empty, or contains only whitespace.</exception>
		/// <remarks>
		/// This method should be called when:
		/// <list type="bullet">
		/// <item><description>A user explicitly logs out of the application</description></item>
		/// <item><description>A user's session expires or times out</description></item>
		/// <item><description>A user is disconnected or terminated by administrators</description></item>
		/// <item><description>A user's account is deactivated or deleted</description></item>
		/// <item><description>WebSocket or persistent connections are closed</description></item>
		/// </list>
		/// <para>
		/// The operation is idempotent, meaning:
		/// <list type="bullet">
		/// <item><description>Calling this method multiple times for the same user has no additional effect</description></item>
		/// <item><description>If the user doesn't currently hold a seat, the operation completes without error</description></item>
		/// <item><description>If the tenant has no seat allocations, the operation completes without error</description></item>
		/// </list>
		/// </para>
		/// <para>
		/// Best practices for seat release:
		/// <list type="bullet">
		/// <item><description>Call this method in logout handlers, session expiration events, and cleanup routines</description></item>
		/// <item><description>Implement automatic seat release for inactive users based on timeout policies</description></item>
		/// <item><description>Use try-finally blocks or using statements to ensure seats are released even on exceptions</description></item>
		/// <item><description>Log seat release events for auditing and troubleshooting</description></item>
		/// <item><description>Consider implementing background jobs to clean up orphaned seats from crashed sessions</description></item>
		/// </list>
		/// </para>
		/// <para>
		/// After a seat is released:
		/// <list type="bullet">
		/// <item><description>The seat becomes immediately available for other users to consume</description></item>
		/// <item><description>The user is removed from the active users list in <see cref="SeatUsage"/></description></item>
		/// <item><description>Available seat count increases by one</description></item>
		/// <item><description>The user may consume a new seat by calling <see cref="TryConsumeSeatAsync"/> again</description></item>
		/// </list>
		/// </para>
		/// </remarks>
		Task ReleaseSeatAsync(TenantId tenantId, string userId, CancellationToken cancellationToken = default);

		/// <summary>
		/// Attempts to consume (allocate) a seat for the specified user within the tenant's seat quota.
		/// This operation is atomic and thread-safe, ensuring seats are not over-allocated.
		/// </summary>
		/// <param name="tenantId">The unique identifier of the tenant requesting seat consumption. Cannot be <see langword="null"/>.</param>
		/// <param name="userId">
		/// The unique identifier of the user attempting to consume a seat. Must be a non-empty, non-whitespace string.
		/// This identifier should be unique within the tenant's scope (e.g., user ID, email, or username).
		/// </param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests during the asynchronous operation.</param>
		/// <returns>
		/// A task that represents the asynchronous operation. The task result is <see langword="true"/> if a seat
		/// was successfully allocated to the user; <see langword="false"/> if the tenant has reached its maximum
		/// seat limit and no seats are available.
		/// </returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="tenantId"/> is <see langword="null"/>.</exception>
		/// <exception cref="ArgumentException">Thrown when <paramref name="userId"/> is <see langword="null"/>, empty, or contains only whitespace.</exception>
		/// <remarks>
		/// This method implements the core seat enforcement logic with the following behavior:
		/// <list type="bullet">
		/// <item><description>If the user already has a seat allocated, returns <see langword="true"/> immediately (idempotent)</description></item>
		/// <item><description>If seats are available and the user doesn't have one, allocates a seat and returns <see langword="true"/></description></item>
		/// <item><description>If the seat limit is reached and the user doesn't have a seat, returns <see langword="false"/></description></item>
		/// <item><description>The operation is atomic to prevent race conditions in concurrent scenarios</description></item>
		/// </list>
		/// <para>
		/// This method is typically invoked by:
		/// <list type="bullet">
		/// <item><description><see cref="Middleware.SeatEnforcerMiddleware"/> during request processing to enforce seat limits</description></item>
		/// <item><description>Authentication handlers to validate seat availability during login</description></item>
		/// <item><description>WebSocket or SignalR connection handlers for persistent connections</description></item>
		/// <item><description>API gateways or proxies that enforce seat-based access control</description></item>
		/// </list>
		/// </para>
		/// <para>
		/// When a seat cannot be consumed (returns <see langword="false"/>):
		/// <list type="bullet">
		/// <item><description>The user should be informed that the tenant has reached its seat limit</description></item>
		/// <item><description>Consider returning HTTP 429 Too Many Requests or 403 Forbidden status</description></item>
		/// <item><description>Provide guidance to contact administrators or upgrade subscription</description></item>
		/// <item><description>Log the event for monitoring and alerting purposes</description></item>
		/// </list>
		/// </para>
		/// <para>
		/// Implementations must ensure thread safety and handle concurrent calls correctly, as multiple
		/// users may attempt to consume seats simultaneously. Consider using locks, semaphores, or
		/// atomic database operations to maintain consistency.
		/// </para>
		/// </remarks>
		Task<bool> TryConsumeSeatAsync(TenantId tenantId, string userId, CancellationToken cancellationToken = default);

		/// <summary>
		/// Retrieves the current seat usage statistics for the specified tenant.
		/// Provides a snapshot of seat allocation, consumption, and availability at the time of the request.
		/// </summary>
		/// <param name="tenantId">The unique identifier of the tenant for which to retrieve seat usage information. Cannot be <see langword="null"/>.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests during the asynchronous operation.</param>
		/// <returns>
		/// A task that represents the asynchronous operation. The task result contains a <see cref="SeatUsage"/>
		/// object with comprehensive seat statistics including maximum seats, used seats, available seats, and active users.
		/// </returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="tenantId"/> is <see langword="null"/>.</exception>
		/// <remarks>
		/// This method returns real-time seat usage data that includes:
		/// <list type="bullet">
		/// <item><description>Maximum seats allocated to the tenant (from subscription or manual allocation)</description></item>
		/// <item><description>Number of seats currently in use by active users</description></item>
		/// <item><description>Number of seats remaining available for new users</description></item>
		/// <item><description>List of user identifiers currently occupying seats</description></item>
		/// <item><description>Boolean flag indicating whether all seats are currently occupied</description></item>
		/// </list>
		/// <para>
		/// The returned data represents a point-in-time snapshot and may change immediately after the call
		/// if users log in or out. For consistent seat enforcement, use <see cref="TryConsumeSeatAsync"/>
		/// which performs atomic check-and-allocate operations.
		/// </para>
		/// <para>
		/// Use this method to:
		/// <list type="bullet">
		/// <item><description>Display seat availability in user interfaces</description></item>
		/// <item><description>Generate usage reports for administrators</description></item>
		/// <item><description>Monitor seat utilization for capacity planning</description></item>
		/// <item><description>Trigger alerts when seat usage approaches limits</description></item>
		/// <item><description>Audit active users for security and compliance</description></item>
		/// </list>
		/// </para>
		/// </remarks>
		Task<SeatUsage> GetSeatUsageAsync(TenantId tenantId, CancellationToken cancellationToken = default);

		#endregion
	}
}