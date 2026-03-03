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

using SaasSuite.Core.Enumerations;
using SaasSuite.Core.Interfaces;

namespace SaasSuite.Core.Options
{
	/// <summary>
	/// Configuration options for customizing SaasSuite Core multi-tenancy behavior.
	/// </summary>
	/// <remarks>
	/// Controls tenant resolution requirements, default isolation strategies, and integration features
	/// such as telemetry enrichment. These options are applied globally across the application.
	/// </remarks>
	public class SaasCoreOptions
	{
		#region ' Properties '

		/// <summary>
		/// Gets or sets a value indicating whether tenant context information should be automatically attached to telemetry events.
		/// When <see langword="true"/>, telemetry will be enriched with tenant ID, name, isolation level, and other
		/// context information for enhanced observability and tenant-specific monitoring.
		/// </summary>
		/// <value>
		/// <see langword="true"/> to enable automatic telemetry enrichment with tenant context;
		/// <see langword="false"/> to disable enrichment.
		/// Defaults to <see langword="true"/> to enable tenant-aware monitoring and diagnostics.
		/// </value>
		/// <remarks>
		/// This feature requires an implementation of <see cref="ITelemetryEnricher"/> to be registered
		/// in the dependency injection container. If no enricher is registered, this setting has no effect.
		/// <para>
		/// Telemetry enrichment provides:
		/// <list type="bullet">
		/// <item><description>Tenant-specific log filtering and analysis</description></item>
		/// <item><description>Per-tenant metrics and monitoring dashboards</description></item>
		/// <item><description>Tenant-scoped distributed tracing</description></item>
		/// <item><description>Easier troubleshooting and support for specific tenants</description></item>
		/// </list>
		/// </para>
		/// <para>
		/// Consider disabling enrichment if tenant information contains sensitive data that should not
		/// appear in telemetry systems, or if telemetry volume needs to be reduced for cost optimization.
		/// </para>
		/// </remarks>
		public bool EnableTelemetryEnrichment { get; set; } = true;

		/// <summary>
		/// Gets or sets a value indicating whether requests must successfully resolve a tenant to proceed through the pipeline.
		/// When <see langword="true"/>, requests without a valid tenant will be rejected or require special handling.
		/// When <see langword="false"/>, requests may proceed without tenant context, allowing anonymous or system-level operations.
		/// </summary>
		/// <value>
		/// <see langword="true"/> to enforce strict tenant resolution for all requests;
		/// <see langword="false"/> to allow requests without tenant context.
		/// Defaults to <see langword="true"/> for strict multi-tenant enforcement.
		/// </value>
		/// <remarks>
		/// Setting this to <see langword="false"/> is useful for:
		/// <list type="bullet">
		/// <item><description>Public endpoints that don't require tenant context (health checks, static content)</description></item>
		/// <item><description>Tenant provisioning or onboarding workflows</description></item>
		/// <item><description>System administration endpoints</description></item>
		/// <item><description>Migration scenarios from single-tenant to multi-tenant architecture</description></item>
		/// </list>
		/// When <see langword="true"/>, consider implementing authorization policies or custom middleware
		/// to handle requests where tenant resolution fails.
		/// </remarks>
		public bool RequireTenant { get; set; } = true;

		/// <summary>
		/// Gets or sets the default isolation level to use when tenant metadata does not explicitly specify one.
		/// This fallback isolation level is applied to tenants without specific isolation configuration in their
		/// <see cref="TenantInfo"/> settings.
		/// </summary>
		/// <value>
		/// An <see cref="IsolationLevel"/> enumeration value specifying the default tenant isolation strategy.
		/// Defaults to <see cref="IsolationLevel.Shared"/> for resource-efficient multi-tenancy.
		/// </value>
		/// <remarks>
		/// The default isolation level is used when:
		/// <list type="bullet">
		/// <item><description>A tenant is newly created without explicit isolation configuration</description></item>
		/// <item><description>Tenant metadata is incomplete or corrupted</description></item>
		/// <item><description>Fallback behavior is needed during tenant migration</description></item>
		/// </list>
		/// Consider setting this to <see cref="IsolationLevel.Dedicated"/> for high-security applications
		/// or <see cref="IsolationLevel.Shared"/> for cost-optimized deployments.
		/// </remarks>
		public IsolationLevel DefaultIsolationLevel { get; set; } = IsolationLevel.Shared;

		#endregion
	}
}