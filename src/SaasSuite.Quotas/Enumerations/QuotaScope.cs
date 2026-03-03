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

namespace SaasSuite.Quotas.Enumerations
{
	/// <summary>
	/// Defines the granularity level at which quota enforcement is applied in multi-tenant scenarios.
	/// </summary>
	/// <remarks>
	/// The scope determines which entity the quota limit applies to, enabling different
	/// levels of resource isolation and control within the application architecture.
	/// Each scope level provides increasingly fine-grained tracking and enforcement:
	/// <list type="bullet">
	/// <item><description><see cref="Tenant"/> scope provides organization-level limits (most common for SaaS)</description></item>
	/// <item><description><see cref="Resource"/> scope enables per-resource or per-project limits within tenants</description></item>
	/// <item><description><see cref="User"/> scope implements individual user quotas for fairness and accountability</description></item>
	/// </list>
	/// Scope selection impacts both the usage tracking key generation and the enforcement semantics.
	/// </remarks>
	public enum QuotaScope
	{
		/// <summary>
		/// Quota is enforced at the tenant level, shared across all users and resources within the tenant.
		/// </summary>
		/// <remarks>
		/// Tenant-scoped quotas ensure that a single tenant organization cannot exceed allocated limits
		/// regardless of how many users or resources are consuming the quota. This is the most common
		/// scope for multi-tenant SaaS applications as it aligns with billing and subscription models.
		/// Usage is aggregated across all activities within the tenant boundary, providing organization-wide
		/// resource management. For example, a tenant with a 10,000 API calls/month quota shares that limit
		/// among all its users and applications. When using tenant scope, the scopeKey parameter in quota
		/// methods is typically null or ignored since the tenant ID itself provides sufficient isolation.
		/// </remarks>
		Tenant = 0,

		/// <summary>
		/// Quota is enforced per individual resource or entity within a tenant.
		/// </summary>
		/// <remarks>
		/// Resource-scoped quotas allow different limits for different types of resources or entities
		/// within the same tenant. This enables fine-grained control where different projects, applications,
		/// or resource types have independent quota allocations. For example, a tenant might have separate
		/// quotas for API calls versus storage usage, or different quotas for different projects or workspaces.
		/// When using resource scope, the scopeKey parameter identifies the specific resource (e.g., "project-123",
		/// "app-api", "storage-bucket-xyz"). Each unique scopeKey maintains its own usage counter and limit
		/// enforcement, allowing flexible multi-dimensional quota management within a single tenant.
		/// </remarks>
		Resource = 1,

		/// <summary>
		/// Quota is enforced per individual user within a tenant.
		/// </summary>
		/// <remarks>
		/// User-scoped quotas provide the finest level of control, ensuring individual users cannot
		/// exceed their personal allocation. This scope is essential for implementing fair usage policies
		/// and preventing single users from consuming all available tenant resources. User-scoped quotas
		/// are commonly used for per-user API rate limiting, individual storage allocations, or features
		/// where accountability and isolation at the user level are required. When using user scope, the
		/// scopeKey parameter should contain the user identifier (e.g., user ID, email, or username).
		/// Each user maintains independent usage tracking, enabling scenarios like "100 API calls per hour
		/// per user" or "5 GB storage per user". This scope is particularly useful in collaborative
		/// environments where multiple users share a tenant but require individual resource limits.
		/// </remarks>
		User = 2
	}
}