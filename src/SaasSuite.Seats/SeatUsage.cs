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

using SaasSuite.Seats.Interfaces;

namespace SaasSuite.Seats
{
	/// <summary>
	/// Represents a snapshot of current seat allocation and usage statistics for a tenant.
	/// Provides comprehensive information about seat capacity, consumption, and active users
	/// for monitoring, reporting, and decision-making purposes.
	/// </summary>
	/// <remarks>
	/// This model is returned by <see cref="ISeatService.GetSeatUsageAsync"/> to provide
	/// a point-in-time view of seat utilization. The data reflects the state at the time of the query
	/// and may change immediately afterward as users log in or out.
	/// <para>
	/// Use cases for seat usage data include:
	/// <list type="bullet">
	/// <item><description>Displaying seat availability in administrative dashboards</description></item>
	/// <item><description>Generating usage reports for billing and capacity planning</description></item>
	/// <item><description>Triggering alerts when seat utilization reaches thresholds</description></item>
	/// <item><description>Monitoring active users for security and compliance auditing</description></item>
	/// <item><description>Informing users about available capacity before attempting login</description></item>
	/// <item><description>Supporting subscription upgrade decisions based on utilization trends</description></item>
	/// </list>
	/// </para>
	/// </remarks>
	public class SeatUsage
	{
		#region ' Properties '

		/// <summary>
		/// Gets a value indicating whether all allocated seats are currently in use, indicating the tenant is at full capacity.
		/// This is a calculated property that evaluates to <see langword="true"/> when no additional seats are available.
		/// </summary>
		/// <value>
		/// <see langword="true"/> if <see cref="UsedSeats"/> is greater than or equal to <see cref="MaxSeats"/>,
		/// meaning all seats are occupied and no capacity remains for new users;
		/// <see langword="false"/> if seats are still available for allocation.
		/// </value>
		/// <remarks>
		/// This property provides a convenient boolean indicator for:
		/// <list type="bullet">
		/// <item><description>Quickly determining if new user access should be blocked</description></item>
		/// <item><description>Displaying "Full Capacity" warnings in user interfaces</description></item>
		/// <item><description>Triggering capacity alerts or notifications to administrators</description></item>
		/// <item><description>Conditional logic in admission control flows</description></item>
		/// <item><description>Reporting on tenants that have reached their subscription limits</description></item>
		/// </list>
		/// <para>
		/// When this property returns <see langword="true"/>:
		/// <list type="bullet">
		/// <item><description>New users attempting to log in will be rejected with seat limit errors</description></item>
		/// <item><description><see cref="ISeatService.TryConsumeSeatAsync"/> will return <see langword="false"/> for new users</description></item>
		/// <item><description><see cref="AvailableSeats"/> will be zero</description></item>
		/// <item><description>Existing users who already hold seats can continue accessing the system</description></item>
		/// </list>
		/// </para>
		/// <para>
		/// Note: This property may return <see langword="true"/> even if <see cref="UsedSeats"/> exceeds <see cref="MaxSeats"/>,
		/// which can occur temporarily when seat allocation is reduced while users are active.
		/// </para>
		/// </remarks>
		public bool IsFull => this.UsedSeats >= this.MaxSeats;

		/// <summary>
		/// Gets the number of seats currently available for new users to consume.
		/// This is a calculated property derived from the difference between <see cref="MaxSeats"/> and <see cref="UsedSeats"/>.
		/// </summary>
		/// <value>
		/// A non-negative integer representing the number of seats that can be allocated to new users.
		/// Returns zero when the tenant is at full capacity (<see cref="UsedSeats"/> equals or exceeds <see cref="MaxSeats"/>).
		/// The calculation ensures the value never becomes negative even if <see cref="UsedSeats"/> exceeds <see cref="MaxSeats"/>
		/// (which could occur during transitional states when reducing seat allocation).
		/// </value>
		/// <remarks>
		/// This property provides immediate visibility into capacity availability for:
		/// <list type="bullet">
		/// <item><description>User interfaces displaying how many users can still join</description></item>
		/// <item><description>Admission control logic determining whether to allow new logins</description></item>
		/// <item><description>Capacity planning and alerting when availability is low</description></item>
		/// <item><description>API responses indicating whether additional users can be onboarded</description></item>
		/// </list>
		/// <para>
		/// Example capacity scenarios:
		/// <list type="bullet">
		/// <item><description>MaxSeats = 10, UsedSeats = 7 ??? AvailableSeats = 3</description></item>
		/// <item><description>MaxSeats = 10, UsedSeats = 10 ??? AvailableSeats = 0 (full capacity)</description></item>
		/// <item><description>MaxSeats = 5, UsedSeats = 8 ??? AvailableSeats = 0 (over-allocated, protected by Max)</description></item>
		/// </list>
		/// </para>
		/// </remarks>
		public int AvailableSeats => Math.Max(0, this.MaxSeats - this.UsedSeats);

		/// <summary>
		/// Gets or sets the maximum number of seats allocated to the tenant.
		/// This represents the seat capacity limit based on the tenant's subscription tier or manual allocation.
		/// </summary>
		/// <value>
		/// A positive integer representing the total number of seats available to the tenant.
		/// This value is set via <see cref="ISeatService.AllocateSeatsAsync"/> and determines
		/// the maximum number of concurrent users that can consume seats.
		/// </value>
		/// <remarks>
		/// The maximum seats value is determined by:
		/// <list type="bullet">
		/// <item><description>Subscription tier configuration (e.g., 5 seats for Basic, 25 for Pro, unlimited for Enterprise)</description></item>
		/// <item><description>Manual allocation by administrators for custom arrangements</description></item>
		/// <item><description>Billing period and payment status</description></item>
		/// <item><description>Trial period allocations or promotional offers</description></item>
		/// </list>
		/// This value can be compared against <see cref="UsedSeats"/> to calculate utilization percentage
		/// and determine how close the tenant is to reaching capacity.
		/// </remarks>
		public int MaxSeats { get; set; }

		/// <summary>
		/// Gets or sets the number of seats currently consumed by active users.
		/// Represents the count of users who have successfully allocated seats and are actively using the system.
		/// </summary>
		/// <value>
		/// A non-negative integer representing the number of seats in use. This value ranges from 0 to <see cref="MaxSeats"/>.
		/// A value equal to <see cref="MaxSeats"/> indicates full capacity utilization.
		/// </value>
		/// <remarks>
		/// This count includes:
		/// <list type="bullet">
		/// <item><description>Users who have successfully logged in and consumed a seat</description></item>
		/// <item><description>Users with active sessions holding seat allocations</description></item>
		/// <item><description>Users who have not yet released their seats via logout or timeout</description></item>
		/// </list>
		/// The count does not include:
		/// <list type="bullet">
		/// <item><description>Users who attempted to log in but were rejected due to seat limits</description></item>
		/// <item><description>Users who have logged out and released their seats</description></item>
		/// <item><description>Inactive users whose seats have been reclaimed by timeout policies</description></item>
		/// </list>
		/// This value corresponds to the count of user identifiers in the <see cref="ActiveUsers"/> collection.
		/// </remarks>
		public int UsedSeats { get; set; }

		/// <summary>
		/// Gets or sets the collection of user identifiers currently occupying seats.
		/// Provides a complete list of active users who have consumed seats within the tenant's allocation.
		/// </summary>
		/// <value>
		/// A read-only list of user identifier strings representing all users currently holding seats.
		/// Defaults to an empty array if no users are active. The count of this collection should match <see cref="UsedSeats"/>.
		/// User identifiers are the same values used in <see cref="ISeatService.TryConsumeSeatAsync"/>
		/// and <see cref="ISeatService.ReleaseSeatAsync"/> calls.
		/// </value>
		/// <remarks>
		/// This collection is useful for:
		/// <list type="bullet">
		/// <item><description>Displaying a list of currently logged-in users in administrative interfaces</description></item>
		/// <item><description>Security auditing and access monitoring for compliance requirements</description></item>
		/// <item><description>Identifying specific users who can be asked to log out when capacity is needed</description></item>
		/// <item><description>Generating activity reports showing who is actively using the system</description></item>
		/// <item><description>Detecting and cleaning up orphaned seats from crashed or abandoned sessions</description></item>
		/// <item><description>Implementing "kick user" functionality to forcibly release seats</description></item>
		/// </list>
		/// <para>
		/// The collection is read-only to prevent external modification of the seat state.
		/// To modify seat allocations, use the appropriate <see cref="ISeatService"/> methods.
		/// </para>
		/// <para>
		/// User identifiers in this list match those extracted from:
		/// <list type="bullet">
		/// <item><description>Authentication claims (e.g., "sub" claim from JWT tokens)</description></item>
		/// <item><description>HTTP headers (e.g., X-User-Id header for API key authentication)</description></item>
		/// <item><description>Custom user identification mechanisms specific to the application</description></item>
		/// </list>
		/// </para>
		/// <para>
		/// Privacy and security considerations:
		/// <list type="bullet">
		/// <item><description>Ensure appropriate authorization checks before exposing this list to users</description></item>
		/// <item><description>Consider masking or truncating user IDs in non-administrative contexts</description></item>
		/// <item><description>Log access to this data for security auditing purposes</description></item>
		/// <item><description>Comply with privacy regulations when storing or transmitting user identifiers</description></item>
		/// </list>
		/// </para>
		/// </remarks>
		public IReadOnlyList<string> ActiveUsers { get; set; } = Array.Empty<string>();

		#endregion
	}
}