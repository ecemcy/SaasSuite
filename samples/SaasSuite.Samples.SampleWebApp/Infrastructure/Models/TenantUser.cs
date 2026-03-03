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
using SaasSuite.Samples.SampleWebApp.Infrastructure.Enumerations;

namespace SaasSuite.Samples.SampleWebApp.Infrastructure.Models
{
	/// <summary>
	/// Represents a user account associated with a specific tenant in a multi-tenant application.
	/// </summary>
	/// <remarks>
	/// This model maintains tenant-scoped user information separate from the application's central authentication,
	/// enabling per-tenant user management, role assignment, and access control.
	/// </remarks>
	public class TenantUser
	{
		#region ' Properties '

		/// <summary>
		/// Gets a value indicating whether the user account is currently active.
		/// </summary>
		/// <value>
		/// <see langword="true"/> if the user can access the tenant; <see langword="false"/> if the account is disabled.
		/// </value>
		/// <remarks>
		/// Inactive users cannot authenticate or perform actions within the tenant regardless of their role.
		/// </remarks>
		public bool IsActive { get; init; }

		/// <summary>
		/// Gets or initializes the user's display name for presentation in user interfaces.
		/// </summary>
		/// <value>
		/// A human-readable string representing the user's name.
		/// </value>
		public required string DisplayName { get; init; }

		/// <summary>
		/// Gets or initializes the user's email address.
		/// </summary>
		/// <value>
		/// A string containing the user's email address used for identification and communication.
		/// </value>
		public required string Email { get; init; }

		/// <summary>
		/// Gets or initializes the unique identifier for this user.
		/// </summary>
		/// <value>
		/// A string uniquely identifying the user within the tenant. This typically correlates with the authentication system's user ID.
		/// </value>
		public required string Id { get; init; }

		/// <summary>
		/// Gets or initializes the tenant this user belongs to.
		/// </summary>
		/// <value>
		/// The <see cref="TenantId"/> associating this user with a specific tenant.
		/// </value>
		public required TenantId TenantId { get; init; }

		/// <summary>
		/// Gets or initializes the user's role within the tenant for authorization purposes.
		/// </summary>
		/// <value>
		/// A <see cref="TenantRole"/> determining the user's permission level within the tenant.
		/// </value>
		public required TenantRole Role { get; init; }

		/// <summary>
		/// Gets or initializes when this user record was created.
		/// </summary>
		/// <value>
		/// A <see cref="DateTime"/> in UTC representing when the user was added to the tenant. Defaults to current UTC time.
		/// </value>
		public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

		#endregion
	}
}