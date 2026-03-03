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

namespace SaasSuite.Samples.SampleWebApp.Infrastructure
{
	/// <summary>
	/// Resolves tenant identifiers from URL path segments using the pattern /t/{tenantId}/...
	/// </summary>
	/// <remarks>
	/// <para>
	/// This path-based resolution strategy is ideal for demos and testing as it makes the tenant context
	/// visible and easily testable via standard HTTP requests.
	/// </para>
	/// <para>
	/// Alternative tenant resolution strategies include:
	/// </para>
	/// <list type="bullet">
	/// <item><description><strong>Subdomain-based:</strong> Extract from host (e.g., tenant1.myapp.com)</description></item>
	/// <item><description><strong>Header-based:</strong> Read from custom header (e.g., X-Tenant-Id)</description></item>
	/// <item><description><strong>Claim-based:</strong> Extract from JWT token claims for authenticated requests</description></item>
	/// <item><description><strong>Database-mapped:</strong> Look up tenant by custom domain mapping</description></item>
	/// </list>
	/// <para>
	/// See the project README for implementation examples of these alternative strategies.
	/// </para>
	/// </remarks>
	public class PathTenantResolver
		: ITenantResolver
	{
		#region ' Fields '

		/// <summary>
		/// HTTP context accessor for retrieving the current request path.
		/// </summary>
		private readonly IHttpContextAccessor _httpContextAccessor;

		#endregion

		#region ' Constructors '

		/// <summary>
		/// Initializes a new instance of the <see cref="PathTenantResolver"/> class.
		/// </summary>
		/// <param name="httpContextAccessor">The HTTP context accessor.</param>
		public PathTenantResolver(IHttpContextAccessor httpContextAccessor)
		{
			this._httpContextAccessor = httpContextAccessor;
		}

		#endregion

		#region ' Methods '

		/// <inheritdoc/>
		public Task<TenantId?> ResolveAsync(CancellationToken cancellationToken = default)
		{
			HttpContext? httpContext = this._httpContextAccessor.HttpContext;
			if (httpContext == null)
			{
				return Task.FromResult<TenantId?>(null);
			}

			string? path = httpContext.Request.Path.Value;
			if (string.IsNullOrEmpty(path))
			{
				return Task.FromResult<TenantId?>(null);
			}

			// Parse URL path expecting format: /t/{tenantId}/...
			string[] segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
			if (segments.Length >= 2 && segments[0].Equals("t", StringComparison.OrdinalIgnoreCase))
			{
				string tenantId = segments[1];
				return Task.FromResult<TenantId?>(new TenantId(tenantId));
			}

			// No tenant found in path
			return Task.FromResult<TenantId?>(null);
		}

		#endregion
	}
}