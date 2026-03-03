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

namespace SaasSuite.Samples.SampleWebApp.Infrastructure
{
	/// <summary>
	/// Represents the authenticated user's context within a tenant, including identity and authorization information.
	/// </summary>
	/// <remarks>
	/// This context combines tenant resolution with user authentication to provide complete authorization information
	/// for the current request, enabling role-based access control within tenant boundaries.
	/// </remarks>
	public class TenantAuthContext
	{
		#region ' Properties '

		/// <summary>
		/// Gets a value indicating whether the user is an administrator with elevated permissions.
		/// </summary>
		/// <value>
		/// <see langword="true"/> if the user's role is <see cref="TenantRole.TenantAdmin"/> or higher (includes owners); otherwise, <see langword="false"/>.
		/// </value>
		/// <remarks>
		/// This property follows the role hierarchy where owners are also considered admins.
		/// </remarks>
		public bool IsAdmin => this.Role == TenantRole.TenantAdmin || this.IsOwner;

		/// <summary>
		/// Gets a value indicating whether the user is a member with basic access to tenant resources.
		/// </summary>
		/// <value>
		/// <see langword="true"/> if the user has any valid role (member, admin, or owner); otherwise, <see langword="false"/>.
		/// </value>
		/// <remarks>
		/// This property follows the role hierarchy where all authenticated tenant users are at least members.
		/// </remarks>
		public bool IsMember => this.Role == TenantRole.TenantMember || this.IsAdmin;

		/// <summary>
		/// Gets a value indicating whether the user is a tenant owner with full administrative access.
		/// </summary>
		/// <value>
		/// <see langword="true"/> if the user's role is <see cref="TenantRole.TenantOwner"/>; otherwise, <see langword="false"/>.
		/// </value>
		public bool IsOwner => this.Role == TenantRole.TenantOwner;

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
		/// A string containing the user's email for identification and communication.
		/// </value>
		public required string Email { get; init; }

		/// <summary>
		/// Gets or initializes the unique identifier of the authenticated user.
		/// </summary>
		/// <value>
		/// A string containing the user's unique identifier.
		/// </value>
		public required string UserId { get; init; }

		/// <summary>
		/// Gets or initializes the tenant the user is currently operating within.
		/// </summary>
		/// <value>
		/// The <see cref="Core.TenantId"/> identifying the tenant context for this request.
		/// </value>
		public required TenantId TenantId { get; init; }

		/// <summary>
		/// Gets or initializes the user's role within the tenant for authorization decisions.
		/// </summary>
		/// <value>
		/// The <see cref="TenantRole"/> determining the user's permission level.
		/// </value>
		public required TenantRole Role { get; init; }

		#endregion
	}
}