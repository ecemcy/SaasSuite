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

using SaasSuite.Samples.SampleWebApp.Infrastructure.Enumerations;

namespace SaasSuite.Samples.SampleWebApp.Infrastructure.Interfaces
{
	/// <summary>
	/// Provides services for tenant-scoped authentication and role-based authorization.
	/// </summary>
	/// <remarks>
	/// This service bridges the gap between the application's authentication system and tenant-specific
	/// authorization needs, enabling role-based access control within tenant boundaries.
	/// </remarks>
	public interface ITenantAuthService
	{
		#region ' Methods '

		/// <summary>
		/// Determines whether the current user has at least the specified role level within their tenant.
		/// </summary>
		/// <param name="minRole">The minimum role level required for authorization.</param>
		/// <returns>
		/// <see langword="true"/> if the user has the specified role or higher; otherwise, <see langword="false"/>.
		/// </returns>
		/// <remarks>
		/// <para>Role hierarchy is evaluated such that higher roles include permissions of lower roles:</para>
		/// <list type="bullet">
		/// <item><description><see cref="TenantRole.TenantOwner"/> has all permissions</description></item>
		/// <item><description><see cref="TenantRole.TenantAdmin"/> has admin and member permissions</description></item>
		/// <item><description><see cref="TenantRole.TenantMember"/> has only member permissions</description></item>
		/// </list>
		/// </remarks>
		bool HasRole(TenantRole minRole);

		/// <summary>
		/// Retrieves the unique identifier of the current authenticated user.
		/// </summary>
		/// <returns>
		/// The user's unique identifier string, or a default value (e.g., "demo-user") if no user is authenticated.
		/// </returns>
		/// <remarks>
		/// In production, this should be extracted from authenticated claims (e.g., JWT tokens, authentication cookies).
		/// In this demo, the value is simulated from the X-User-Id HTTP header.
		/// </remarks>
		string GetCurrentUserId();

		/// <summary>
		/// Retrieves the current authenticated user's context within their tenant.
		/// </summary>
		/// <returns>
		/// A <see cref="TenantAuthContext"/> containing user identity, tenant association, and role information,
		/// or <see langword="null"/> if no authenticated context is available.
		/// </returns>
		/// <remarks>
		/// The context is resolved from the current HTTP request and combines tenant resolution with user authentication.
		/// </remarks>
		TenantAuthContext? GetCurrentContext();

		#endregion
	}
}