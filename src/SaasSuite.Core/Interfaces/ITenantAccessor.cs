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
using SaasSuite.Core.Services;

namespace SaasSuite.Core.Interfaces
{
	/// <summary>
	/// Provides thread-safe access to the current tenant context for the executing request or async flow.
	/// This service acts as the primary entry point for retrieving tenant information throughout the application.
	/// </summary>
	/// <remarks>
	/// The default implementation (<see cref="TenantAccessor"/>) uses <see cref="AsyncLocal{T}"/>
	/// to store tenant context, ensuring that the context flows correctly through async/await boundaries
	/// and is isolated between concurrent requests. The tenant context is typically set by
	/// <see cref="SaasResolutionMiddleware"/> early in the request pipeline.
	/// </remarks>
	public interface ITenantAccessor
	{
		#region ' Properties '

		/// <summary>
		/// Gets the current tenant context for the executing request or async flow.
		/// Returns <see langword="null"/> if no tenant has been resolved for the current execution context.
		/// </summary>
		/// <value>
		/// The <see cref="Core.TenantContext"/> containing tenant identification and metadata,
		/// or <see langword="null"/> if tenant resolution has not occurred or failed.
		/// </value>
		/// <remarks>
		/// This property is safe to access from any point in the request pipeline after
		/// tenant resolution middleware has executed. Services, controllers, and other
		/// components should inject <see cref="ITenantAccessor"/> and use this property
		/// to access tenant-specific information.
		/// <para>
		/// A <see langword="null"/> value may indicate:
		/// <list type="bullet">
		/// <item><description>The request is executing before tenant resolution middleware</description></item>
		/// <item><description>No tenant identifier could be extracted from the request</description></item>
		/// <item><description>The tenant identifier was invalid or not found in the tenant store</description></item>
		/// <item><description>The application is configured to allow non-tenant requests</description></item>
		/// </list>
		/// </para>
		/// </remarks>
		TenantContext? TenantContext { get; }

		#endregion
	}
}