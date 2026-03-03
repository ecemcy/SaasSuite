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

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

using SaasSuite.Core.Interfaces;

namespace SaasSuite.Core.Services
{
	/// <summary>
	/// Tenant resolver implementation that extracts tenant identification from HTTP request headers.
	/// Provides a simple strategy for tenant resolution suitable for API-first architectures and microservices.
	/// </summary>
	/// <remarks>
	/// This resolver is commonly used in scenarios where:
	/// <list type="bullet">
	/// <item><description>API clients (mobile apps, SPAs, services) can include custom headers</description></item>
	/// <item><description>Tenant context cannot be derived from the URL or domain</description></item>
	/// <item><description>Multiple tenants share the same base URL</description></item>
	/// <item><description>API gateways or proxies can inject tenant headers</description></item>
	/// </list>
	/// <para>
	/// The default header name is "X-Tenant-Id" but can be customized via constructor.
	/// Clients must include the tenant identifier in every request for proper resolution.
	/// </para>
	/// </remarks>
	public class HttpHeaderTenantResolver
		: ITenantResolver
	{
		#region ' Fields '

		/// <summary>
		/// HTTP context accessor for retrieving the current request context.
		/// Provides access to request headers for tenant identification.
		/// </summary>
		private readonly IHttpContextAccessor _httpContextAccessor;

		/// <summary>
		/// The HTTP header name to read the tenant identifier from.
		/// Configurable to support different header naming conventions.
		/// </summary>
		private readonly string _headerName;

		#endregion

		#region ' Constructors '

		/// <summary>
		/// Initializes a new instance of the <see cref="HttpHeaderTenantResolver"/> class
		/// with the specified HTTP context accessor and optional custom header name.
		/// </summary>
		/// <param name="httpContextAccessor">
		/// The HTTP context accessor for accessing request information. Required for reading headers.
		/// Must be registered in the dependency injection container.
		/// </param>
		/// <param name="headerName">
		/// The name of the HTTP header containing the tenant identifier. Defaults to "X-Tenant-Id".
		/// Should follow HTTP header naming conventions and be consistent across API documentation.
		/// </param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="httpContextAccessor"/> is <see langword="null"/>.</exception>
		public HttpHeaderTenantResolver(IHttpContextAccessor httpContextAccessor, string headerName = "X-Tenant-Id")
		{
			this._httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
			this._headerName = headerName;
		}

		#endregion

		#region ' Methods '

		/// <summary>
		/// Resolves the tenant identifier by reading the configured HTTP header from the current request.
		/// Returns <see langword="null"/> if the header is missing, empty, or the HTTP context is unavailable.
		/// </summary>
		/// <param name="cancellationToken">A token to monitor for cancellation requests. Not used in this implementation.</param>
		/// <returns>
		/// A task that represents the asynchronous operation. The task result contains the
		/// resolved <see cref="TenantId"/> if the header is present and contains a valid value;
		/// otherwise <see langword="null"/>.
		/// </returns>
		/// <remarks>
		/// This method performs the following steps:
		/// <list type="number">
		/// <item><description>Retrieves the current HTTP context</description></item>
		/// <item><description>Reads the configured header from the request</description></item>
		/// <item><description>Validates that the header value is not null or whitespace</description></item>
		/// <item><description>Creates and returns a TenantId if validation passes</description></item>
		/// </list>
		/// <para>
		/// Returns <see langword="null"/> in the following cases:
		/// <list type="bullet">
		/// <item><description>No HTTP context is available (non-HTTP scenarios)</description></item>
		/// <item><description>The specified header is not present in the request</description></item>
		/// <item><description>The header value is null, empty, or whitespace</description></item>
		/// </list>
		/// </para>
		/// <para>
		/// The operation is synchronous despite returning a Task for interface compatibility.
		/// No actual async operations are performed.
		/// </para>
		/// </remarks>
		public Task<TenantId?> ResolveAsync(CancellationToken cancellationToken = default)
		{
			// Retrieve the current HTTP context (may be null if not in an HTTP request scope)
			HttpContext httpContext = this._httpContextAccessor.HttpContext;

			if (httpContext == null)
			{
				return Task.FromResult<TenantId?>(null);
			}

			// Attempt to read the tenant identifier from the configured header
			if (httpContext.Request.Headers.TryGetValue(this._headerName, out StringValues headerValue))
			{
				string tenantIdValue = headerValue.ToString();

				// Validate that the header contains a non-empty value
				if (!string.IsNullOrWhiteSpace(tenantIdValue))
				{
					return Task.FromResult<TenantId?>(new TenantId(tenantIdValue));
				}
			}

			// Header not found or empty, tenant cannot be resolved
			return Task.FromResult<TenantId?>(null);
		}

		#endregion
	}
}