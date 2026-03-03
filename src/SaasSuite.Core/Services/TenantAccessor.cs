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

using SaasSuite.Core.Interfaces;
using SaasSuite.Core.Middleware;

namespace SaasSuite.Core.Services
{
	/// <summary>
	/// Default implementation of <see cref="ITenantAccessor"/> that uses <see cref="AsyncLocal{T}"/>
	/// for thread-safe, async-flow-aware tenant context storage.
	/// Provides isolated tenant context per async execution flow while sharing a single accessor instance.
	/// </summary>
	/// <remarks>
	/// This implementation leverages <see cref="AsyncLocal{T}"/> to store the tenant context, which provides:
	/// <list type="bullet">
	/// <item><description>Thread-safe storage that flows correctly through async/await boundaries</description></item>
	/// <item><description>Automatic isolation between concurrent HTTP requests</description></item>
	/// <item><description>No risk of cross-contamination between different execution contexts</description></item>
	/// <item><description>Compatible with both synchronous and asynchronous code paths</description></item>
	/// </list>
	/// <para>
	/// The accessor itself is registered as a singleton in the DI container, but the stored tenant context
	/// is effectively scoped to each async execution flow (typically corresponding to an HTTP request).
	/// This design combines the performance benefits of singleton registration with the isolation guarantees
	/// of scoped services.
	/// </para>
	/// <para>
	/// The <see cref="SetTenantContext"/> method is internal to the resolution process and should only be
	/// called by <see cref="SaasResolutionMiddleware"/> during tenant resolution.
	/// </para>
	/// </remarks>
	public class TenantAccessor
		: ITenantAccessor
	{
		#region ' Static Fields '

		/// <summary>
		/// Static async-local storage for tenant context. Provides isolated storage per async execution flow
		/// while being accessible across all instances of the accessor (of which there is typically only one).
		/// </summary>
		/// <remarks>
		/// AsyncLocal ensures that:
		/// <list type="bullet">
		/// <item><description>Each async context (request) has its own isolated tenant context</description></item>
		/// <item><description>Context flows correctly through async/await calls</description></item>
		/// <item><description>Child tasks inherit the context from their parent</description></item>
		/// <item><description>Context does not leak between concurrent requests</description></item>
		/// </list>
		/// </remarks>
		private static readonly AsyncLocal<TenantContext?> _tenantContext = new AsyncLocal<TenantContext?>();

		#endregion

		#region ' Properties '

		/// <summary>
		/// Gets the current tenant context for the executing async flow.
		/// Returns <see langword="null"/> if no tenant has been resolved for the current request.
		/// </summary>
		/// <value>
		/// The <see cref="TenantContext"/> for the current async execution flow,
		/// or <see langword="null"/> if tenant resolution has not occurred or failed.
		/// </value>
		/// <remarks>
		/// This property reads from the <see cref="AsyncLocal{T}"/> storage, which automatically
		/// retrieves the context for the current async flow. Multiple concurrent requests will
		/// each see their own isolated tenant context.
		/// </remarks>
		public TenantContext? TenantContext => _tenantContext.Value;

		#endregion

		#region ' Methods '

		/// <summary>
		/// Sets the tenant context for the current async execution flow.
		/// This method should only be called by tenant resolution middleware during request processing.
		/// </summary>
		/// <param name="context">
		/// The tenant context to set for the current request, or <see langword="null"/> to clear the context.
		/// </param>
		/// <remarks>
		/// This method writes to the <see cref="AsyncLocal{T}"/> storage, which automatically
		/// associates the context with the current async flow. The context will remain accessible
		/// throughout the request lifetime and across async boundaries, but will not affect other
		/// concurrent requests.
		/// <para>
		/// This method is public to allow middleware access but should be considered an internal API.
		/// Application code should not call this method directly; use tenant resolution middleware instead.
		/// </para>
		/// </remarks>
		public void SetTenantContext(TenantContext? context)
		{
			_tenantContext.Value = context;
		}

		#endregion
	}
}