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
using SaasSuite.Core.Interfaces;
using SaasSuite.Samples.SampleWebApp.Infrastructure.Enumerations;
using SaasSuite.Samples.SampleWebApp.Infrastructure.Interfaces;
using SaasSuite.Samples.SampleWebApp.Infrastructure.Models;

namespace SaasSuite.Samples.SampleWebApp.Infrastructure.Services
{
	/// <summary>
	/// Demonstration implementation of <see cref="ITenantAuthService"/> for tenant authorization.
	/// </summary>
	/// <remarks>
	/// <para>
	/// This implementation simulates authentication using HTTP headers for demo purposes.
	/// </para>
	/// <para>
	/// In production environments, this should integrate with your authentication system:
	/// </para>
	/// <list type="bullet">
	/// <item><description>JWT token claims from bearer authentication</description></item>
	/// <item><description>Cookie-based authentication with ASP.NET Core Identity</description></item>
	/// <item><description>OAuth/OpenID Connect providers (Azure AD, Auth0, etc.)</description></item>
	/// </list>
	/// </remarks>
	public class TenantAuthService
		: ITenantAuthService
	{
		#region ' Fields '

		/// <summary>
		/// Accessor for retrieving the current HTTP context.
		/// </summary>
		private readonly IHttpContextAccessor _httpContextAccessor;

		/// <summary>
		/// Accessor for retrieving the current tenant context.
		/// </summary>
		private readonly ITenantAccessor _tenantAccessor;

		/// <summary>
		/// Store for retrieving tenant user information.
		/// </summary>
		private readonly ITenantUserStore _userStore;

		#endregion

		#region ' Constructors '

		/// <summary>
		/// Initializes a new instance of the <see cref="TenantAuthService"/> class.
		/// </summary>
		/// <param name="tenantAccessor">The tenant context accessor.</param>
		/// <param name="httpContextAccessor">The HTTP context accessor.</param>
		/// <param name="userStore">The user store for retrieving user data.</param>
		public TenantAuthService(ITenantAccessor tenantAccessor, IHttpContextAccessor httpContextAccessor, ITenantUserStore userStore)
		{
			this._tenantAccessor = tenantAccessor;
			this._httpContextAccessor = httpContextAccessor;
			this._userStore = userStore;
		}

		#endregion

		#region ' Methods '

		/// <inheritdoc/>
		public bool HasRole(TenantRole minRole)
		{
			TenantAuthContext? context = this.GetCurrentContext();
			if (context == null)
			{
				return false;
			}

			// Owner has all permissions (highest level)
			if (context.IsOwner)
			{
				return true;
			}

			// Admin has admin and member permissions
			if (minRole == TenantRole.TenantAdmin && context.IsAdmin)
			{
				return true;
			}

			// Member only has member-level permissions
			if (minRole == TenantRole.TenantMember && context.IsMember)
			{
				return true;
			}

			return false;
		}

		/// <inheritdoc/>
		public string GetCurrentUserId()
		{
			HttpContext? httpContext = this._httpContextAccessor.HttpContext;
			if (httpContext == null)
			{
				return "demo-user";
			}

			// In demo mode, use X-User-Id header to simulate authentication
			// In production, extract from ClaimsPrincipal: httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
			return httpContext.Request.Headers["X-User-Id"].FirstOrDefault() ?? "demo-user";
		}

		/// <inheritdoc/>
		public TenantAuthContext? GetCurrentContext()
		{
			TenantContext? tenantContext = this._tenantAccessor.TenantContext;
			if (tenantContext == null)
			{
				return null;
			}

			// In demo mode, use X-User-Id header to simulate authentication
			// In production, extract user ID from authenticated claims (e.g., JWT, cookies)
			string userId = this.GetCurrentUserId();

			// Attempt to retrieve user from tenant-scoped store
			TenantUser? user = this._userStore.GetByIdAsync(tenantContext.TenantId, userId).GetAwaiter().GetResult();
			if (user == null)
			{
				// Default to member role for demo when user not found in store
				return new TenantAuthContext
				{
					UserId = userId,
					TenantId = tenantContext.TenantId,
					Role = TenantRole.TenantMember,
					Email = $"{userId}@example.com",
					DisplayName = $"User {userId}"
				};
			}

			// Return context populated from stored user information
			return new TenantAuthContext
			{
				UserId = user.Id,
				TenantId = tenantContext.TenantId,
				Role = user.Role,
				Email = user.Email,
				DisplayName = user.DisplayName
			};
		}

		#endregion
	}
}