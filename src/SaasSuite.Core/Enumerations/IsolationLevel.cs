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

namespace SaasSuite.Core.Enumerations
{
	/// <summary>
	/// Describes how tenant data and resources are isolated within the application or platform.
	/// </summary>
	/// <remarks>
	/// Defines the multi-tenancy strategy employed for resource separation and security boundaries.
	/// This enumeration determines the level of separation between tenants and influences
	/// database design, resource allocation, and security policies.
	/// </remarks>
	public enum IsolationLevel
	{
		/// <summary>
		/// No isolation guarantees are provided between tenants.
		/// </summary>
		/// <remarks>
		/// Commonly used for single-tenant deployments, development environments, or testing scenarios
		/// where tenant separation is not required. In this mode, all tenants share the same resources
		/// without any logical or physical separation, which may be acceptable for non-production environments
		/// or when migrating from single-tenant to multi-tenant architecture.
		/// </remarks>
		None = 0,

		/// <summary>
		/// Tenants share infrastructure and resources with logical separation at the application or data layer.
		/// </summary>
		/// <remarks>
		/// Data isolation is achieved through discriminator columns, row-level security, or filtered queries within shared resources.
		/// This is the most cost-effective approach but requires careful implementation to maintain security.
		/// Example: A shared database with a TenantId column to filter data per tenant, or shared compute instances
		/// with tenant context isolation. Provides good resource utilization and cost efficiency but requires
		/// rigorous query filtering to prevent cross-tenant data access.
		/// </remarks>
		Shared = 1,

		/// <summary>
		/// Tenants have dedicated infrastructure and resources isolated at the infrastructure level.
		/// </summary>
		/// <remarks>
		/// Provides the highest level of isolation and security with complete physical or logical separation.
		/// This approach offers the strongest security guarantees but comes with increased resource costs and operational complexity.
		/// Example: Separate database per tenant, dedicated compute resources, isolated storage accounts, or dedicated network segments.
		/// Ideal for tenants with strict compliance requirements (HIPAA, PCI-DSS, SOC 2) or those requiring
		/// guaranteed performance without noisy neighbor effects.
		/// </remarks>
		Dedicated = 2,

		/// <summary>
		/// A combination of shared and dedicated components depending on the subsystem or resource type.
		/// </summary>
		/// <remarks>
		/// Some resources (e.g., application code, shared services, caching) are shared for efficiency,
		/// while others (e.g., databases, storage, sensitive data) are dedicated for security or compliance.
		/// Provides a flexible balance between cost efficiency and isolation based on specific requirements.
		/// Example: Shared application instances and Redis cache with dedicated databases per tenant,
		/// or shared infrastructure with dedicated storage for sensitive documents.
		/// This model allows optimization of costs while meeting varied tenant requirements for different resource types.
		/// </remarks>
		Hybrid = 3
	}
}