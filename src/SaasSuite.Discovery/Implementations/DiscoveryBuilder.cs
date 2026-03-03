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

using System.Reflection;

using Microsoft.Extensions.DependencyInjection;

using SaasSuite.Core;
using SaasSuite.Core.Enumerations;

namespace SaasSuite.Discovery.Implementations
{
	/// <summary>
	/// Provides a fluent API for configuring and executing tenant service discovery with fine-grained control.
	/// </summary>
	/// <remarks>
	/// The discovery builder supports complex service discovery scenarios through a chainable fluent interface.
	/// It allows precise control over which types are discovered, how they're registered, and what lifetimes
	/// and scopes are applied. The builder pattern ensures all configuration is complete before registration occurs.
	/// </remarks>
	public class DiscoveryBuilder
	{
		#region ' Fields '

		/// <summary>
		/// The service collection to register discovered services into.
		/// </summary>
		private readonly IServiceCollection _services;

		/// <summary>
		/// Collection of assemblies to search for discoverable services.
		/// </summary>
		private readonly List<Assembly> _assemblies = new List<Assembly>();

		/// <summary>
		/// Collection of type filter predicates to control which types are discovered.
		/// </summary>
		private readonly List<Func<Type, bool>> _typeFilters = new List<Func<Type, bool>>();

		/// <summary>
		/// Collection of service registrations discovered during the build process.
		/// </summary>
		private readonly List<ServiceRegistration> _registrations = new List<ServiceRegistration>();

		/// <summary>
		/// Optional predicate for conditional registration based on tenant context.
		/// </summary>
		private Func<TenantContext, bool>? _tenantPredicate;

		/// <summary>
		/// Function that determines which service types to register for each implementation type.
		/// </summary>
		private Func<Type, IEnumerable<Type>>? _serviceTypeSelector;

		/// <summary>
		/// The service lifetime to apply to discovered services.
		/// </summary>
		private ServiceLifetime _lifetime = ServiceLifetime.Scoped;

		/// <summary>
		/// The tenant scope to apply to discovered services.
		/// </summary>
		private TenantScope _tenantScope = TenantScope.Request;

		#endregion

		#region ' Constructors '

		/// <summary>
		/// Initializes a new instance of the <see cref="DiscoveryBuilder"/> class.
		/// </summary>
		/// <param name="services">The service collection to register services into. Cannot be <see langword="null"/>.</param>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="services"/> is <see langword="null"/>.
		/// </exception>
		public DiscoveryBuilder(IServiceCollection services)
		{
			this._services = services ?? throw new ArgumentNullException(nameof(services));
		}

		#endregion

		#region ' Methods '

		/// <summary>
		/// Registers a service with the dependency injection container based on the registration metadata.
		/// </summary>
		/// <param name="registration">The registration containing service types and lifetime information.</param>
		private void RegisterService(ServiceRegistration registration)
		{
			// Register each service type that the implementation provides
			foreach (Type serviceType in registration.ServiceTypes)
			{
				ServiceDescriptor descriptor = new ServiceDescriptor(
					serviceType,
					registration.ImplementationType,
					registration.Lifetime);

				this._services.Add(descriptor);
			}
		}

		/// <summary>
		/// Completes the discovery process and registers all discovered services with the service collection.
		/// </summary>
		/// <remarks>
		/// This is the terminal operation that applies all configured filters and registration strategies.
		/// It must be called explicitly to trigger the actual service registration. The method:
		/// <list type="number">
		/// <item><description>Searches configured assemblies (or all non-system assemblies if none specified)</description></item>
		/// <item><description>Applies all type filters</description></item>
		/// <item><description>Determines service types using the configured selector</description></item>
		/// <item><description>Creates <see cref="ServiceRegistration"/> objects</description></item>
		/// <item><description>Registers services with the DI container</description></item>
		/// </list>
		/// After activation, the builder's registration list can be accessed via <see cref="GetRegistrations"/>
		/// for inspection or reporting purposes.
		/// </remarks>
		public void Activate()
		{
			// Determine which assemblies to search
			IEnumerable<Assembly> assembliesToDiscover = this._assemblies.Count != 0
				? this._assemblies
				: AppDomain.CurrentDomain.GetAssemblies().Where(a => !IsSystemAssembly(a));

			// Search each assembly
			foreach (Assembly? assembly in assembliesToDiscover)
			{
				IEnumerable<Type> types = GetTypesFromAssembly(assembly);

				foreach (Type type in types)
				{
					// Skip invalid types (abstract, generic definitions, non-public)
					if (!IsValidServiceType(type))
					{
						continue;
					}

					// Apply all type filters; all must pass
					if (this._typeFilters.Count != 0 && !this._typeFilters.All(filter => filter(type)))
					{
						continue;
					}

					// Determine service types to register
					List<Type> serviceTypes = this._serviceTypeSelector?.Invoke(type)?.ToList() ?? new List<Type>();
					if (serviceTypes.Count == 0)
					{
						continue;
					}

					// Create registration object
					ServiceRegistration registration = new ServiceRegistration(
						type,
						serviceTypes,
						this._lifetime,
						this._tenantScope)
					{
						TenantPredicate = this._tenantPredicate,
						Source = $"Assembly: {assembly.GetName().Name}"
					};

					this._registrations.Add(registration);

					// Register the service with DI container
					this.RegisterService(registration);
				}
			}
		}

		/// <summary>
		/// Registers discovered services using a custom service type selector.
		/// </summary>
		/// <param name="selector">
		/// A function that determines which service types to register for each implementation.
		/// Cannot be <see langword="null"/>.
		/// </param>
		/// <returns>The <see cref="DiscoveryBuilder"/> for method chaining.</returns>
		/// <remarks>
		/// Use this for advanced scenarios where the built-in strategies don't meet your needs,
		/// such as registering services with base classes, generic interfaces, or complex type hierarchies.
		/// </remarks>
		public DiscoveryBuilder As(Func<Type, IEnumerable<Type>> selector)
		{
			this._serviceTypeSelector = selector;
			return this;
		}

		/// <summary>
		/// Registers discovered services as their concrete types (self-registration).
		/// </summary>
		/// <returns>The <see cref="DiscoveryBuilder"/> for method chaining.</returns>
		/// <remarks>
		/// With this strategy, services are registered as themselves, allowing direct resolution
		/// of concrete types. This is useful when services don't implement specific interfaces
		/// or when you need to resolve concrete implementations directly.
		/// </remarks>
		public DiscoveryBuilder AsConcreteTypes()
		{
			this._serviceTypeSelector = type => new[] { type };
			return this;
		}

		/// <summary>
		/// Registers discovered services with all their implemented interfaces.
		/// </summary>
		/// <returns>The <see cref="DiscoveryBuilder"/> for method chaining.</returns>
		/// <remarks>
		/// This strategy registers services for all public interfaces they implement,
		/// excluding generic type definitions. This is the most common registration pattern
		/// for dependency injection, allowing resolution through interface types.
		/// </remarks>
		public DiscoveryBuilder AsInterfaces()
		{
			this._serviceTypeSelector = type => type.GetInterfaces()
				.Where(i => !i.IsGenericTypeDefinition);
			return this;
		}

		/// <summary>
		/// Registers discovered services with matching interfaces using a naming convention.
		/// </summary>
		/// <returns>The <see cref="DiscoveryBuilder"/> for method chaining.</returns>
		/// <remarks>
		/// This strategy matches services to interfaces by convention: a class named "Service"
		/// is registered for interface "IService". This enforces a consistent naming pattern
		/// across the codebase. If no matching interface is found, the service is not registered.
		/// </remarks>
		public DiscoveryBuilder AsMatchingInterface()
		{
			this._serviceTypeSelector = type =>
			{
				// Generate expected interface name by prefixing with 'I'
				string interfaceName = $"I{type.Name}";
				return type.GetInterfaces()
					.Where(i => i.Name.Equals(interfaceName, StringComparison.Ordinal));
			};
			return this;
		}

		/// <summary>
		/// Adds multiple assemblies to search for discoverable services.
		/// </summary>
		/// <param name="assemblies">The assemblies to search. Cannot be <see langword="null"/> or contain null elements.</param>
		/// <returns>The <see cref="DiscoveryBuilder"/> for method chaining.</returns>
		public DiscoveryBuilder FromAssemblies(params Assembly[] assemblies)
		{
			foreach (Assembly assembly in assemblies)
			{
				_ = this.FromAssembly(assembly);
			}
			return this;
		}

		/// <summary>
		/// Adds multiple assemblies to search for discoverable services.
		/// </summary>
		/// <param name="assemblies">The assemblies to search. Cannot be <see langword="null"/>.</param>
		/// <returns>The <see cref="DiscoveryBuilder"/> for method chaining.</returns>
		public DiscoveryBuilder FromAssemblies(IEnumerable<Assembly> assemblies)
		{
			foreach (Assembly assembly in assemblies)
			{
				_ = this.FromAssembly(assembly);
			}
			return this;
		}

		/// <summary>
		/// Adds a specific assembly to search for discoverable services.
		/// </summary>
		/// <param name="assembly">The assembly to search. Cannot be <see langword="null"/>.</param>
		/// <returns>The <see cref="DiscoveryBuilder"/> for method chaining.</returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="assembly"/> is <see langword="null"/>.
		/// </exception>
		public DiscoveryBuilder FromAssembly(Assembly assembly)
		{
			ArgumentNullException.ThrowIfNull(assembly);
			this._assemblies.Add(assembly);
			return this;
		}

		/// <summary>
		/// Adds the assembly containing the specified type to the search list.
		/// </summary>
		/// <typeparam name="T">A type whose assembly should be searched.</typeparam>
		/// <returns>The <see cref="DiscoveryBuilder"/> for method chaining.</returns>
		/// <remarks>
		/// This is a convenience method to avoid explicitly referencing assemblies by name.
		/// Useful when you want to search assemblies from different layers of your application.
		/// </remarks>
		public DiscoveryBuilder FromAssemblyOf<T>()
		{
			return this.FromAssembly(typeof(T).Assembly);
		}

		/// <summary>
		/// Adds a custom filter predicate to control which types are discovered.
		/// </summary>
		/// <param name="filter">
		/// A predicate that returns <see langword="true"/> for types that should be discovered.
		/// Cannot be <see langword="null"/>.
		/// </param>
		/// <returns>The <see cref="DiscoveryBuilder"/> for method chaining.</returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="filter"/> is <see langword="null"/>.
		/// </exception>
		/// <remarks>
		/// Multiple filters can be added; all filters must return <see langword="true"/> for a type to be discovered.
		/// Filters are applied after basic type validation (public, concrete, non-generic).
		/// </remarks>
		public DiscoveryBuilder IncludeTypes(Func<Type, bool> filter)
		{
			ArgumentNullException.ThrowIfNull(filter);
			this._typeFilters.Add(filter);
			return this;
		}

		/// <summary>
		/// Filters discovery to types in the specified namespaces or their sub-namespaces.
		/// </summary>
		/// <param name="namespaces">
		/// The namespace prefixes to filter by. Types in these namespaces or sub-namespaces are included.
		/// </param>
		/// <returns>The <see cref="DiscoveryBuilder"/> for method chaining.</returns>
		/// <remarks>
		/// Namespace filtering uses prefix matching, so "MyApp.Services" will match both
		/// "MyApp.Services" and "MyApp.Services.Implementation".
		/// This is useful for limiting discovery to specific application layers.
		/// </remarks>
		public DiscoveryBuilder InNamespaces(params string[] namespaces)
		{
			return this.IncludeTypes(type =>
				type.Namespace != null &&
				namespaces.Any(ns => type.Namespace.StartsWith(ns, StringComparison.Ordinal)));
		}

		/// <summary>
		/// Adds a tenant-based predicate for conditional service registration.
		/// </summary>
		/// <param name="predicate">
		/// A predicate that determines whether services should be registered for a given tenant.
		/// Cannot be <see langword="null"/>.
		/// </param>
		/// <returns>The <see cref="DiscoveryBuilder"/> for method chaining.</returns>
		/// <remarks>
		/// Use this for feature flags, A/B testing, or tenant tier-specific functionality.
		/// Services are only registered when the predicate returns <see langword="true"/> for the current tenant.
		/// </remarks>
		public DiscoveryBuilder WhenTenant(Func<TenantContext, bool> predicate)
		{
			this._tenantPredicate = predicate;
			return this;
		}

		/// <summary>
		/// Filters discovery to types decorated with the specified attribute.
		/// </summary>
		/// <typeparam name="TAttribute">The attribute type to filter by. Must inherit from <see cref="Attribute"/>.</typeparam>
		/// <returns>The <see cref="DiscoveryBuilder"/> for method chaining.</returns>
		/// <remarks>
		/// This is commonly used to discover types marked with custom service attributes
		/// or marker interfaces that indicate service registration requirements.
		/// </remarks>
		public DiscoveryBuilder WithAttribute<TAttribute>() where TAttribute : Attribute
		{
			return this.IncludeTypes(type => type.GetCustomAttribute<TAttribute>() != null);
		}

		/// <summary>
		/// Sets the tenant scope to <see cref="TenantScope.Global"/> for discovered services.
		/// </summary>
		/// <returns>The <see cref="DiscoveryBuilder"/> for method chaining.</returns>
		/// <remarks>
		/// Global scope indicates services are not tenant-specific and operate across all tenants.
		/// Use for services that provide system-wide functionality or infrastructure concerns.
		/// </remarks>
		public DiscoveryBuilder WithGlobalScope()
		{
			this._tenantScope = TenantScope.Global;
			return this;
		}

		/// <summary>
		/// Sets the tenant scope to <see cref="TenantScope.Request"/> for discovered services.
		/// </summary>
		/// <returns>The <see cref="DiscoveryBuilder"/> for method chaining.</returns>
		/// <remarks>
		/// Request scope creates tenant-aware instances per request. This is the default and
		/// most common scope for tenant-specific services. Each request gets a fresh instance
		/// scoped to the current tenant context.
		/// </remarks>
		public DiscoveryBuilder WithRequestScope()
		{
			this._tenantScope = TenantScope.Request;
			return this;
		}

		/// <summary>
		/// Sets the service lifetime to <see cref="ServiceLifetime.Scoped"/> for discovered services.
		/// </summary>
		/// <returns>The <see cref="DiscoveryBuilder"/> for method chaining.</returns>
		/// <remarks>
		/// Scoped lifetime creates one instance per request/scope. This is the default and
		/// recommended lifetime for most tenant-aware services.
		/// </remarks>
		public DiscoveryBuilder WithScopedLifetime()
		{
			this._lifetime = ServiceLifetime.Scoped;
			return this;
		}

		/// <summary>
		/// Sets the service lifetime to <see cref="ServiceLifetime.Singleton"/> for discovered services.
		/// </summary>
		/// <returns>The <see cref="DiscoveryBuilder"/> for method chaining.</returns>
		/// <remarks>
		/// Singleton lifetime creates one instance for the entire application lifetime.
		/// Use with caution for tenant-aware services as they must be thread-safe and
		/// handle multiple tenants concurrently.
		/// </remarks>
		public DiscoveryBuilder WithSingletonLifetime()
		{
			this._lifetime = ServiceLifetime.Singleton;
			return this;
		}

		/// <summary>
		/// Sets the tenant scope to <see cref="TenantScope.SingletonPerTenant"/> for discovered services.
		/// </summary>
		/// <returns>The <see cref="DiscoveryBuilder"/> for method chaining.</returns>
		/// <remarks>
		/// SingletonPerTenant scope creates one instance per tenant that is reused across requests.
		/// Use for expensive-to-create services that can be safely cached per tenant, such as
		/// tenant-specific configuration or connection pools.
		/// </remarks>
		public DiscoveryBuilder WithSingletonPerTenantScope()
		{
			this._tenantScope = TenantScope.SingletonPerTenant;
			return this;
		}

		/// <summary>
		/// Sets the service lifetime to <see cref="ServiceLifetime.Transient"/> for discovered services.
		/// </summary>
		/// <returns>The <see cref="DiscoveryBuilder"/> for method chaining.</returns>
		/// <remarks>
		/// Transient lifetime creates a new instance each time the service is resolved.
		/// Use for stateless, lightweight services that don't hold resources.
		/// </remarks>
		public DiscoveryBuilder WithTransientLifetime()
		{
			this._lifetime = ServiceLifetime.Transient;
			return this;
		}

		/// <summary>
		/// Builds a list of discovered service registrations without activating them.
		/// </summary>
		/// <returns>A read-only list of <see cref="ServiceRegistration"/> objects representing discovered services.</returns>
		/// <remarks>
		/// Use this method to preview what would be registered without actually modifying the service collection.
		/// This is useful for diagnostics, testing discovery rules, or generating documentation.
		/// The registrations are created during previous <see cref="Activate"/> calls.
		/// </remarks>
		public IReadOnlyList<ServiceRegistration> BuildManifest()
		{
			return this._registrations;
		}

		/// <summary>
		/// Gets all service registrations discovered during the build process.
		/// </summary>
		/// <returns>A read-only list of all discovered service registrations.</returns>
		/// <remarks>
		/// This provides access to registration metadata for inspection, reporting, or diagnostic purposes.
		/// The list is populated during <see cref="Activate"/> calls.
		/// </remarks>
		public IReadOnlyList<ServiceRegistration> GetRegistrations()
		{
			return this._registrations;
		}

		#endregion

		#region ' Static Methods '

		/// <summary>
		/// Determines whether an assembly is a system assembly that should be excluded from discovery.
		/// </summary>
		/// <param name="assembly">The assembly to check.</param>
		/// <returns><see langword="true"/> if the assembly is a system assembly; otherwise, <see langword="false"/>.</returns>
		/// <remarks>
		/// System assemblies include .NET Framework/Core assemblies (System.*, Microsoft.*) and runtime assemblies
		/// (mscorlib, netstandard). These are excluded to improve performance and avoid discovering system types.
		/// </remarks>
		private static bool IsSystemAssembly(Assembly assembly)
		{
			string name = assembly.GetName().Name ?? string.Empty;
			return name.StartsWith("System.", StringComparison.OrdinalIgnoreCase) ||
				   name.StartsWith("Microsoft.", StringComparison.OrdinalIgnoreCase) ||
				   name.Equals("mscorlib", StringComparison.OrdinalIgnoreCase) ||
				   name.Equals("netstandard", StringComparison.OrdinalIgnoreCase);
		}

		/// <summary>
		/// Determines whether a type is a valid candidate for service registration.
		/// </summary>
		/// <param name="type">The type to validate.</param>
		/// <returns><see langword="true"/> if the type can be registered; otherwise, <see langword="false"/>.</returns>
		/// <remarks>
		/// A type is valid if it is a public, concrete class that is not a generic type definition.
		/// Abstract classes, interfaces, and generic definitions are excluded.
		/// </remarks>
		private static bool IsValidServiceType(Type type)
		{
			return type.IsClass &&
				   !type.IsAbstract &&
				   !type.IsGenericTypeDefinition &&
				   type.IsPublic;
		}

		/// <summary>
		/// Safely retrieves types from an assembly, handling reflection exceptions.
		/// </summary>
		/// <param name="assembly">The assembly to get types from.</param>
		/// <returns>An enumerable of types that could be loaded successfully.</returns>
		/// <remarks>
		/// Uses <see cref="ReflectionTypeLoadException"/> handling to return partial results
		/// when some types in an assembly fail to load (e.g., due to missing dependencies).
		/// </remarks>
		private static IEnumerable<Type> GetTypesFromAssembly(Assembly assembly)
		{
			try
			{
				return assembly.GetTypes();
			}
			catch (ReflectionTypeLoadException ex)
			{
				// Return types that loaded successfully, filtering out nulls
				return ex.Types.Where(t => t != null)!;
			}
		}

		#endregion
	}
}