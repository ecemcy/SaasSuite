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

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

using SaasSuite.Core.Interfaces;
using SaasSuite.Core.Options;
using SaasSuite.Core.Services;

namespace SaasSuite.Core.Middleware
{
	/// <summary>
	/// ASP.NET Core middleware that resolves tenant context from incoming HTTP requests.
	/// This middleware extracts tenant identification information, loads tenant metadata,
	/// and populates the tenant context for use throughout the request pipeline.
	/// </summary>
	/// <remarks>
	/// This middleware should be registered early in the pipeline using
	/// <see cref="ApplicationBuilderExtensions.UseSaasResolution"/>. It coordinates
	/// between <see cref="ITenantResolver"/> to identify the tenant, <see cref="ITenantStore"/>
	/// to load tenant metadata, and <see cref="ITenantAccessor"/> to make the context available
	/// to downstream components.
	/// <para>
	/// The middleware performs the following operations:
	/// <list type="number">
	/// <item><description>Uses the registered tenant resolver to extract the tenant ID from the request</description></item>
	/// <item><description>If a tenant ID is found, retrieves full tenant information from the tenant store</description></item>
	/// <item><description>Creates a tenant context and sets it in the tenant accessor for the current request</description></item>
	/// <item><description>Continues the pipeline, making tenant context available to subsequent middleware and endpoints</description></item>
	/// </list>
	/// </para>
	/// </remarks>
	public class SaasResolutionMiddleware
	{
		#region ' Fields '

		/// <summary>
		/// The next middleware delegate in the ASP.NET Core pipeline.
		/// Invoked after tenant resolution is complete to continue request processing.
		/// </summary>
		private readonly RequestDelegate _next;

		#endregion

		#region ' Constructors '

		/// <summary>
		/// Initializes a new instance of the <see cref="SaasResolutionMiddleware"/> class.
		/// </summary>
		/// <param name="next">The next middleware in the request pipeline. Invoked after tenant resolution completes.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="next"/> is <see langword="null"/>.</exception>
		public SaasResolutionMiddleware(RequestDelegate next)
		{
			this._next = next ?? throw new ArgumentNullException(nameof(next));
		}

		#endregion

		#region ' Methods '

		/// <summary>
		/// Invokes the middleware to resolve tenant context for the current HTTP request.
		/// Coordinates tenant resolution, metadata loading, and context population before
		/// continuing to the next middleware in the pipeline.
		/// </summary>
		/// <param name="context">The HTTP context for the current request. Provides access to request and response objects.</param>
		/// <param name="tenantResolver">
		/// The tenant resolver service injected from DI. Used to extract tenant identification from the request.
		/// Must be registered in the service collection before this middleware is invoked.
		/// </param>
		/// <param name="tenantAccessor">
		/// The tenant accessor service injected from DI. Used to store the resolved tenant context for the current request.
		/// Must be registered in the service collection before this middleware is invoked.
		/// </param>
		/// <param name="tenantStore">
		/// The tenant store service injected from DI. Used to load full tenant metadata after tenant ID resolution.
		/// Must be registered in the service collection before this middleware is invoked.
		/// </param>
		/// <returns>A task that represents the asynchronous middleware execution.</returns>
		/// <remarks>
		/// This method uses per-request dependency injection for the resolver, accessor, and store services.
		/// If tenant resolution fails (returns <see langword="null"/>), the request continues without tenant context.
		/// Downstream middleware and endpoints should check for <see langword="null"/> tenant context if strict
		/// tenant requirements are needed, or configure <see cref="SaasCoreOptions.RequireTenant"/>
		/// to enforce tenant presence.
		/// <para>
		/// The middleware requires <paramref name="tenantAccessor"/> to be the mutable <see cref="TenantAccessor"/>
		/// implementation to set the context. If a custom accessor is used, it must provide a way to set the context.
		/// </para>
		/// </remarks>
		public async Task InvokeAsync(HttpContext context, ITenantResolver tenantResolver, ITenantAccessor tenantAccessor, ITenantStore tenantStore)
		{
			// Attempt to resolve tenant ID from the request using the configured strategy
			TenantId? tenantId = await tenantResolver.ResolveAsync(context.RequestAborted);

			if (tenantId.HasValue)
			{
				// Load full tenant metadata from the store using the resolved ID
				TenantInfo? tenantInfo = await tenantStore.GetByIdAsync(tenantId.Value, context.RequestAborted);

				// Only set context if tenant info was successfully loaded and accessor supports mutation
				if (tenantInfo != null && tenantAccessor is TenantAccessor mutableAccessor)
				{
					// Create and populate tenant context for this request
					TenantContext tenantContext = new TenantContext(tenantId.Value, tenantInfo);
					mutableAccessor.SetTenantContext(tenantContext);
				}
			}

			// Continue to the next middleware in the pipeline
			await this._next(context);
		}

		#endregion
	}
}