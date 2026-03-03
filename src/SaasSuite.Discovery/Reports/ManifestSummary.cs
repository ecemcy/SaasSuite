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

using System.Text.Json.Serialization;

namespace SaasSuite.Discovery.Reports
{
	/// <summary>
	/// Represents aggregated summary statistics for a discovery manifest.
	/// </summary>
	/// <remarks>
	/// The summary provides high-level insights into discovered services, including distribution
	/// by lifetime, tenant scope, source, and special configurations. This enables quick
	/// assessment of service registration patterns and potential issues.
	/// </remarks>
	public class ManifestSummary
	{
		#region ' Properties '

		/// <summary>
		/// Gets the count of services with decorators applied.
		/// </summary>
		/// <value>
		/// An integer representing the number of services that have one or more decorators.
		/// </value>
		/// <remarks>
		/// Decorators add cross-cutting concerns like logging, caching, or validation to services.
		/// This count indicates how extensively the decorator pattern is used in the application.
		/// Services can have multiple decorators, which are applied in registration order.
		/// </remarks>
		[JsonPropertyName("withDecorators")]
		public int WithDecorators { get; init; }

		/// <summary>
		/// Gets the count of services with tenant-based conditional predicates.
		/// </summary>
		/// <value>
		/// An integer representing the number of services that have tenant predicates configured.
		/// </value>
		/// <remarks>
		/// Services with tenant predicates are conditionally registered based on tenant context,
		/// enabling features like:
		/// <list type="bullet">
		/// <item><description>Feature flags per tenant</description></item>
		/// <item><description>Tier-based functionality</description></item>
		/// <item><description>A/B testing implementations</description></item>
		/// <item><description>Beta features for specific tenants</description></item>
		/// </list>
		/// A high count indicates extensive use of conditional service registration.
		/// </remarks>
		[JsonPropertyName("withTenantPredicates")]
		public int WithTenantPredicates { get; init; }

		/// <summary>
		/// Gets the count of services grouped by their dependency injection lifetime.
		/// </summary>
		/// <value>
		/// A dictionary mapping lifetime names (Scoped, Singleton, Transient) to counts.
		/// Defaults to an empty dictionary. Cannot be <see langword="null"/>.
		/// </value>
		/// <remarks>
		/// This provides visibility into the lifetime distribution of services, which can help
		/// identify potential issues such as:
		/// <list type="bullet">
		/// <item><description>Too many singleton services that might cause memory leaks</description></item>
		/// <item><description>Too many transient services that could impact performance</description></item>
		/// <item><description>Inappropriate lifetime choices for tenant-aware services</description></item>
		/// </list>
		/// </remarks>
		[JsonPropertyName("byLifetime")]
		public Dictionary<string, int> ByLifetime { get; init; } = new Dictionary<string, int>();

		/// <summary>
		/// Gets the count of services grouped by their registration source.
		/// </summary>
		/// <value>
		/// A dictionary mapping source identifiers (e.g., assembly names, attributes) to counts.
		/// Defaults to an empty dictionary. Cannot be <see langword="null"/>.
		/// </value>
		/// <remarks>
		/// Source grouping shows where services are coming from, such as:
		/// <list type="bullet">
		/// <item><description>Specific assembly names (e.g., "MyApp.Services")</description></item>
		/// <item><description>Attribute-based discovery sources</description></item>
		/// <item><description>Manual registrations vs. auto-discovered services</description></item>
		/// </list>
		/// This helps understand which application layers contribute most services.
		/// </remarks>
		[JsonPropertyName("bySource")]
		public Dictionary<string, int> BySource { get; init; } = new Dictionary<string, int>();

		/// <summary>
		/// Gets the count of services grouped by their tenant scope.
		/// </summary>
		/// <value>
		/// A dictionary mapping tenant scope names (Global, Request, SingletonPerTenant) to counts.
		/// Defaults to an empty dictionary. Cannot be <see langword="null"/>.
		/// </value>
		/// <remarks>
		/// Tenant scope distribution indicates how services are isolated across tenants:
		/// <list type="bullet">
		/// <item><description>Global: Services shared across all tenants</description></item>
		/// <item><description>Request: Services created per request with tenant context</description></item>
		/// <item><description>SingletonPerTenant: Services cached per tenant</description></item>
		/// </list>
		/// A high proportion of global services might indicate insufficient tenant isolation.
		/// </remarks>
		[JsonPropertyName("byTenantScope")]
		public Dictionary<string, int> ByTenantScope { get; init; } = new Dictionary<string, int>();

		#endregion
	}
}