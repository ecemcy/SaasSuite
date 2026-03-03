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

using SaasSuite.Core.Middleware;

namespace SaasSuite.Core.Interfaces
{
	/// <summary>
	/// Extracts and resolves tenant identification information from the current request or execution context.
	/// Implementations define the strategy for determining which tenant is associated with an incoming request.
	/// </summary>
	/// <remarks>
	/// Common resolution strategies include:
	/// <list type="bullet">
	/// <item><description>HTTP Headers - Reading tenant ID from custom headers (e.g., X-Tenant-Id)</description></item>
	/// <item><description>Subdomain - Extracting tenant from the subdomain portion of the host</description></item>
	/// <item><description>Route Values - Reading tenant ID from URL route parameters</description></item>
	/// <item><description>Claims - Extracting tenant from authenticated user claims</description></item>
	/// <item><description>Query String - Reading tenant ID from query parameters</description></item>
	/// <item><description>Host Header - Mapping entire host names to tenant identifiers</description></item>
	/// </list>
	/// <para>
	/// Multiple resolvers can be chained together using a composite pattern to try different
	/// strategies in order until a tenant is successfully resolved.
	/// </para>
	/// </remarks>
	public interface ITenantResolver
	{
		#region ' Methods '

		/// <summary>
		/// Attempts to resolve the tenant identifier from the current request or execution context.
		/// Returns <see langword="null"/> if no tenant identifier can be determined.
		/// </summary>
		/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
		/// <returns>
		/// A task that represents the asynchronous operation. The task result contains the
		/// resolved <see cref="TenantId"/> if successful, or <see langword="null"/> if tenant
		/// resolution failed or no tenant context is available.
		/// </returns>
		/// <remarks>
		/// This method is called by <see cref="SaasResolutionMiddleware"/> early
		/// in the request pipeline. Implementations should:
		/// <list type="bullet">
		/// <item><description>Be performant as they execute on every request</description></item>
		/// <item><description>Return <see langword="null"/> rather than throwing exceptions for missing tenants</description></item>
		/// <item><description>Validate that extracted tenant identifiers are not empty or whitespace</description></item>
		/// <item><description>Log resolution failures for diagnostics</description></item>
		/// </list>
		/// <para>
		/// The resolved tenant ID is used to load full tenant metadata from <see cref="ITenantStore"/>
		/// and populate the <see cref="TenantContext"/> for the current request.
		/// </para>
		/// </remarks>
		Task<TenantId?> ResolveAsync(CancellationToken cancellationToken = default);

		#endregion
	}
}