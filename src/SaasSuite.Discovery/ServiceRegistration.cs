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

using Microsoft.Extensions.DependencyInjection;

using SaasSuite.Core;
using SaasSuite.Core.Enumerations;

namespace SaasSuite.Discovery
{
	/// <summary>
	/// Represents metadata about a discovered service registration.
	/// </summary>
	/// <remarks>
	/// Service registrations capture all information needed to register a service with the
	/// dependency injection container, including implementation type, service types, lifetime,
	/// tenant scope, and optional predicates or decorators.
	/// </remarks>
	public class ServiceRegistration
	{
		#region ' Constructors '

		/// <summary>
		/// Initializes a new instance of the <see cref="ServiceRegistration"/> class.
		/// </summary>
		/// <param name="implementationType">The concrete type that implements the service. Cannot be <see langword="null"/>.</param>
		/// <param name="serviceTypes">The service types (interfaces or base classes) to register for. Cannot be <see langword="null"/>.</param>
		/// <param name="lifetime">The dependency injection lifetime for the service.</param>
		/// <param name="tenantScope">The tenant scope defining multi-tenant isolation behavior.</param>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="implementationType"/> or <paramref name="serviceTypes"/> is <see langword="null"/>.
		/// </exception>
		public ServiceRegistration(Type implementationType, IReadOnlyList<Type> serviceTypes, ServiceLifetime lifetime, TenantScope tenantScope)
		{
			this.ImplementationType = implementationType ?? throw new ArgumentNullException(nameof(implementationType));
			this.ServiceTypes = serviceTypes ?? throw new ArgumentNullException(nameof(serviceTypes));
			this.Lifetime = lifetime;
			this.TenantScope = tenantScope;
		}

		#endregion

		#region ' Properties '

		/// <summary>
		/// Gets the source identifier indicating where this registration originated.
		/// </summary>
		/// <value>
		/// A string describing the registration source (e.g., assembly name, attribute type, "Manual").
		/// Can be <see langword="null"/> if the source is unknown or not tracked.
		/// </value>
		/// <remarks>
		/// The source helps with diagnostics and understanding service registration origins:
		/// <list type="bullet">
		/// <item><description>"Assembly: MyApp.Services" - Discovered from assembly searching</description></item>
		/// <item><description>"Attribute: TenantServiceAttribute" - Registered via attribute decoration</description></item>
		/// <item><description>"Builder: Fluent API" - Registered through discovery builder</description></item>
		/// <item><description>"Manual" - Manually registered outside discovery mechanisms</description></item>
		/// </list>
		/// </remarks>
		public string? Source { get; init; }

		/// <summary>
		/// Gets an optional predicate for conditional registration based on tenant context.
		/// </summary>
		/// <value>
		/// A function that evaluates <see cref="TenantContext"/> and returns <see langword="true"/> if the
		/// service should be registered for that tenant. Can be <see langword="null"/> for unconditional registration.
		/// </value>
		/// <remarks>
		/// Tenant predicates enable conditional service registration for scenarios like:
		/// <list type="bullet">
		/// <item><description>Feature flags: Register services only for tenants with specific features enabled</description></item>
		/// <item><description>Tier-based functionality: Provide premium services only to premium tenants</description></item>
		/// <item><description>Beta features: Enable experimental features for selected tenants</description></item>
		/// <item><description>Regional compliance: Register services based on tenant's geographic region</description></item>
		/// </list>
		/// When <see langword="null"/>, the service is registered for all tenants.
		/// </remarks>
		public Func<TenantContext, bool>? TenantPredicate { get; init; }

		/// <summary>
		/// Gets the list of decorator types to apply to this service.
		/// </summary>
		/// <value>
		/// A mutable list of <see cref="Type"/> objects representing decorators. Defaults to an empty list.
		/// Cannot be <see langword="null"/>.
		/// </value>
		/// <remarks>
		/// Decorators wrap the service to add cross-cutting concerns without modifying the original implementation.
		/// Multiple decorators can be applied and are executed in the order they appear in the list.
		/// Each decorator must implement the same interface as the service and accept the decorated service
		/// as a constructor parameter. Common decorator patterns include:
		/// <list type="bullet">
		/// <item><description>Logging decorators that log method calls and parameters</description></item>
		/// <item><description>Caching decorators that cache method results</description></item>
		/// <item><description>Validation decorators that validate inputs</description></item>
		/// <item><description>Authorization decorators that check permissions</description></item>
		/// </list>
		/// </remarks>
		public IList<Type> Decorators { get; init; } = new List<Type>();

		/// <summary>
		/// Gets the service types that this implementation is registered for.
		/// </summary>
		/// <value>
		/// A read-only list of <see cref="Type"/> objects representing interfaces or base classes
		/// that this implementation provides. Cannot be <see langword="null"/>.
		/// </value>
		/// <remarks>
		/// A single implementation can be registered for multiple service types, enabling resolution
		/// through any of its contracts. For example, a repository might implement both IRepository&lt;T&gt;
		/// and IQueryable&lt;T&gt;, and be registered for both.
		/// </remarks>
		public IReadOnlyList<Type> ServiceTypes { get; }

		/// <summary>
		/// Gets the dependency injection lifetime for this service.
		/// </summary>
		/// <value>
		/// A <see cref="ServiceLifetime"/> enumeration value (Scoped, Singleton, or Transient).
		/// </value>
		/// <remarks>
		/// The lifetime determines when service instances are created and disposed:
		/// <list type="bullet">
		/// <item><description>Scoped: One instance per request/scope, disposed at scope end</description></item>
		/// <item><description>Singleton: One instance for application lifetime, never disposed until shutdown</description></item>
		/// <item><description>Transient: New instance every time it's resolved, disposed immediately after use</description></item>
		/// </list>
		/// </remarks>
		public ServiceLifetime Lifetime { get; }

		/// <summary>
		/// Gets the tenant scope defining how this service handles multi-tenancy.
		/// </summary>
		/// <value>
		/// A <see cref="TenantScope"/> enumeration value (Global, Request, or SingletonPerTenant).
		/// </value>
		/// <remarks>
		/// Tenant scope controls service isolation in multi-tenant applications:
		/// <list type="bullet">
		/// <item><description>Global: Service is shared across all tenants (tenant-agnostic)</description></item>
		/// <item><description>Request: Service is tenant-aware and created per request with tenant context</description></item>
		/// <item><description>SingletonPerTenant: Service instance is cached separately for each tenant</description></item>
		/// </list>
		/// </remarks>
		public TenantScope TenantScope { get; }

		/// <summary>
		/// Gets the concrete implementation type for this service registration.
		/// </summary>
		/// <value>
		/// A <see cref="Type"/> representing the class that provides the service implementation.
		/// Cannot be <see langword="null"/>.
		/// </value>
		/// <remarks>
		/// The implementation type must be a concrete, instantiable class with a public constructor
		/// compatible with dependency injection. It cannot be abstract, an interface, or a generic type definition.
		/// </remarks>
		public Type ImplementationType { get; }

		#endregion
	}
}