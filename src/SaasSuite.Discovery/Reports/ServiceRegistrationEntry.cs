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
	/// Represents a single service registration entry in a discovery manifest.
	/// </summary>
	/// <remarks>
	/// Each entry captures complete metadata about how a service is registered, including
	/// its implementation type, service types, lifetime, tenant scope, and any special
	/// configurations like predicates or decorators.
	/// </remarks>
	public class ServiceRegistrationEntry
	{
		#region ' Constructors '

		/// <summary>
		/// Initializes a new instance of the <see cref="ServiceRegistrationEntry"/> class.
		/// </summary>
		/// <param name="implementationType">The full name of the implementation type. Cannot be <see langword="null"/>.</param>
		/// <param name="serviceTypes">The list of service type names this implementation provides. Cannot be <see langword="null"/>.</param>
		/// <param name="lifetime">The service lifetime as a string (Scoped, Singleton, Transient). Cannot be <see langword="null"/>.</param>
		/// <param name="tenantScope">The tenant scope as a string (Global, Request, SingletonPerTenant). Cannot be <see langword="null"/>.</param>
		/// <exception cref="ArgumentNullException">
		/// Thrown when any parameter is <see langword="null"/>.
		/// </exception>
		public ServiceRegistrationEntry(string implementationType, List<string> serviceTypes, string lifetime, string tenantScope)
		{
			this.ImplementationType = implementationType ?? throw new ArgumentNullException(nameof(implementationType));
			this.ServiceTypes = serviceTypes ?? throw new ArgumentNullException(nameof(serviceTypes));
			this.Lifetime = lifetime ?? throw new ArgumentNullException(nameof(lifetime));
			this.TenantScope = tenantScope ?? throw new ArgumentNullException(nameof(tenantScope));
		}

		#endregion

		#region ' Properties '

		/// <summary>
		/// Gets a value indicating whether a tenant predicate is configured for this service.
		/// </summary>
		/// <value>
		/// <see langword="true"/> if a conditional registration predicate is configured; otherwise, <see langword="false"/>.
		/// </value>
		/// <remarks>
		/// When <see langword="true"/>, the service is only registered for tenants matching the predicate condition.
		/// This enables feature flags, tier-based features, and other conditional functionality.
		/// </remarks>
		[JsonPropertyName("hasTenantPredicate")]
		public bool HasTenantPredicate { get; init; }

		/// <summary>
		/// Gets the fully qualified name of the implementation type.
		/// </summary>
		/// <value>
		/// A string containing the full type name including namespace (e.g., "MyApp.Services.UserService").
		/// Cannot be <see langword="null"/>.
		/// </value>
		/// <remarks>
		/// The full type name allows unambiguous identification of the implementing class
		/// and can be used to locate source code or generate documentation links.
		/// </remarks>
		[JsonPropertyName("implementationType")]
		public string ImplementationType { get; }

		/// <summary>
		/// Gets the dependency injection lifetime of the service.
		/// </summary>
		/// <value>
		/// A string representation of the service lifetime: "Scoped", "Singleton", or "Transient".
		/// Cannot be <see langword="null"/>.
		/// </value>
		/// <remarks>
		/// The lifetime determines when service instances are created and disposed:
		/// <list type="bullet">
		/// <item><description>Scoped: One instance per request/scope</description></item>
		/// <item><description>Singleton: One instance for the application lifetime</description></item>
		/// <item><description>Transient: New instance every time it's resolved</description></item>
		/// </list>
		/// </remarks>
		[JsonPropertyName("lifetime")]
		public string Lifetime { get; }

		/// <summary>
		/// Gets the tenant scope indicating how the service handles multi-tenancy.
		/// </summary>
		/// <value>
		/// A string representation of the tenant scope: "Global", "Request", or "SingletonPerTenant".
		/// Cannot be <see langword="null"/>.
		/// </value>
		/// <remarks>
		/// Tenant scope defines the isolation level for multi-tenant scenarios:
		/// <list type="bullet">
		/// <item><description>Global: Shared across all tenants</description></item>
		/// <item><description>Request: Tenant-aware, created per request</description></item>
		/// <item><description>SingletonPerTenant: Cached separately for each tenant</description></item>
		/// </list>
		/// </remarks>
		[JsonPropertyName("tenantScope")]
		public string TenantScope { get; }

		/// <summary>
		/// Gets the source that triggered this service registration.
		/// </summary>
		/// <value>
		/// A string identifying where the service was discovered (e.g., assembly name, attribute type).
		/// Can be <see langword="null"/> if the source is unknown.
		/// </value>
		/// <remarks>
		/// The source helps trace which discovery mechanism registered the service:
		/// <list type="bullet">
		/// <item><description>"Assembly: MyApp.Services" - Discovered from an assembly search</description></item>
		/// <item><description>"Attribute: TenantServiceAttribute" - Discovered via attribute decoration</description></item>
		/// <item><description>"Manual" - Manually registered outside discovery</description></item>
		/// </list>
		/// </remarks>
		[JsonPropertyName("source")]
		public string? Source { get; init; }

		/// <summary>
		/// Gets a human-readable description of the tenant predicate, if configured.
		/// </summary>
		/// <value>
		/// A string describing the predicate condition, or <see langword="null"/> if no predicate is configured.
		/// </value>
		/// <remarks>
		/// This provides context about when the service is registered. Since predicates are
		/// lambda functions that can't be easily serialized, this description offers human-readable
		/// information about the condition.
		/// </remarks>
		[JsonPropertyName("tenantPredicateDescription")]
		public string? TenantPredicateDescription { get; init; }

		/// <summary>
		/// Gets the list of decorator type names applied to this service.
		/// </summary>
		/// <value>
		/// A list of fully qualified decorator type names. Defaults to an empty list.
		/// Cannot be <see langword="null"/>.
		/// </value>
		/// <remarks>
		/// Decorators wrap the service to add cross-cutting concerns like:
		/// <list type="bullet">
		/// <item><description>Logging and diagnostics</description></item>
		/// <item><description>Caching</description></item>
		/// <item><description>Validation</description></item>
		/// <item><description>Authorization checks</description></item>
		/// </list>
		/// Decorators are applied in the order they appear in the list.
		/// </remarks>
		[JsonPropertyName("decorators")]
		public List<string> Decorators { get; init; } = new List<string>();

		/// <summary>
		/// Gets the list of service type names that this implementation is registered for.
		/// </summary>
		/// <value>
		/// A list of fully qualified type names representing all service types (interfaces or classes)
		/// this implementation is registered as. Cannot be <see langword="null"/>.
		/// </value>
		/// <remarks>
		/// A single implementation can be registered for multiple service types,
		/// allowing resolution through any of its implemented interfaces or base types.
		/// For example, a repository might be registered as both IRepository and IQueryableRepository.
		/// </remarks>
		[JsonPropertyName("serviceTypes")]
		public List<string> ServiceTypes { get; }

		#endregion
	}
}