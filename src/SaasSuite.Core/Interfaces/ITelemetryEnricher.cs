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

namespace SaasSuite.Core.Interfaces
{
	/// <summary>
	/// Enriches telemetry data with tenant-specific context information for enhanced observability.
	/// Implementations of this interface integrate with logging, metrics, and tracing systems
	/// to automatically tag telemetry with tenant identifiers and related metadata.
	/// </summary>
	/// <remarks>
	/// This interface enables tenant-aware observability by allowing telemetry systems
	/// (such as Application Insights, OpenTelemetry, or custom logging frameworks) to
	/// automatically include tenant context in all logs, metrics, and traces. This is
	/// essential for troubleshooting, monitoring, and analyzing tenant-specific behavior
	/// in multi-tenant applications.
	/// </remarks>
	public interface ITelemetryEnricher
	{
		#region ' Methods '

		/// <summary>
		/// Enriches the provided telemetry properties dictionary with tenant-specific context information.
		/// This method adds tenant-related key-value pairs to enable filtering and analysis of telemetry by tenant.
		/// </summary>
		/// <param name="tenantContext">The current tenant context containing tenant identification and metadata. May be <see langword="null"/> for non-tenant requests.</param>
		/// <param name="properties">
		/// The properties dictionary to be enriched with tenant information. This dictionary is typically
		/// provided by a telemetry framework and will be attached to logs, metrics, or traces.
		/// </param>
		/// <remarks>
		/// Common enrichment properties include:
		/// <list type="bullet">
		/// <item><description>Tenant ID - The unique identifier of the tenant</description></item>
		/// <item><description>Tenant Name - The display name of the tenant</description></item>
		/// <item><description>Isolation Level - The tenant's data isolation strategy</description></item>
		/// <item><description>Tenant Tier - The service plan or tier of the tenant</description></item>
		/// <item><description>Is Active - Whether the tenant is currently active</description></item>
		/// </list>
		/// <para>
		/// Implementations should handle <see langword="null"/> tenant contexts gracefully by either
		/// skipping enrichment or adding a marker indicating no tenant context is available.
		/// </para>
		/// <para>
		/// The method should avoid throwing exceptions to prevent disrupting the application's
		/// primary operations. Any errors during enrichment should be logged separately.
		/// </para>
		/// </remarks>
		void Enrich(TenantContext tenantContext, IDictionary<string, object> properties);

		#endregion
	}
}