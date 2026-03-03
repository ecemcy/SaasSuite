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

using SaasSuite.Core.Attributes;

namespace SaasSuite.Core.Enumerations
{
	/// <summary>
	/// Defines how a tenant-aware service should be scoped or cached relative to tenants.
	/// </summary>
	/// <remarks>
	/// Determines the granularity of service instance creation and lifetime in multi-tenant scenarios.
	/// This enumeration is used in conjunction with <see cref="TenantServiceAttribute"/> to specify
	/// how service instances should be managed across tenant boundaries.
	/// </remarks>
	public enum TenantScope
	{
		/// <summary>
		/// Service is not tied to a specific tenant and is shared globally across all tenants.
		/// </summary>
		/// <remarks>
		/// A single instance is used regardless of which tenant is making the request.
		/// This scope is appropriate for services that do not contain tenant-specific state or configuration,
		/// such as logging services, utility classes, or stateless infrastructure services.
		/// Using this scope for tenant-aware services requires careful design to ensure thread safety
		/// and prevent cross-tenant data contamination.
		/// </remarks>
		Global = 0,

		/// <summary>
		/// Service instance is scoped to the current HTTP request or async execution flow.
		/// </summary>
		/// <remarks>
		/// A new instance is created for each request, ensuring complete tenant isolation at the request level.
		/// This is the default and most common scoping strategy for tenant-aware services.
		/// The service instance lives for the duration of the request and is disposed afterward,
		/// ensuring no cross-tenant state leakage between concurrent requests. This scope provides
		/// the best isolation guarantees and is recommended for most tenant-specific business logic.
		/// </remarks>
		Request = 1,

		/// <summary>
		/// One service instance is created and cached per tenant across multiple requests.
		/// </summary>
		/// <remarks>
		/// The instance is reused for all requests from the same tenant, providing better performance
		/// for services that maintain expensive-to-initialize tenant-specific state or configuration.
		/// This scope requires custom caching or factory patterns and careful consideration of thread safety,
		/// as the same instance may be accessed by multiple concurrent requests from the same tenant.
		/// Useful for services that load tenant-specific configuration, maintain tenant-scoped connections,
		/// or cache tenant-specific data. Care must be taken to ensure proper disposal and cache invalidation
		/// when tenant configuration changes or when tenants are removed from the system.
		/// </remarks>
		SingletonPerTenant = 2
	}
}