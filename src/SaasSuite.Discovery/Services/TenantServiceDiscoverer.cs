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

using SaasSuite.Core.Attributes;
using SaasSuite.Discovery.Interfaces;
using SaasSuite.Discovery.Options;

namespace SaasSuite.Discovery.Services
{
	/// <summary>
	/// Provides reflection-based discovery of tenant services marked with <see cref="TenantServiceAttribute"/>.
	/// </summary>
	/// <remarks>
	/// This implementation searches assemblies using reflection to find types decorated with
	/// <see cref="TenantServiceAttribute"/> and automatically registers them with the dependency
	/// injection container. The discoverer maintains a list of all discovered registrations
	/// for reporting and diagnostic purposes.
	/// </remarks>
	public class TenantServiceDiscoverer
		: ITenantServiceDiscoverer
	{
		#region ' Fields '

		/// <summary>
		/// Collection of all service registrations discovered during searching.
		/// </summary>
		/// <remarks>
		/// This list is populated during the discovery process and can be accessed via
		/// <see cref="GetRegistrations"/> for reporting, diagnostics, or manifest generation.
		/// </remarks>
		private readonly List<ServiceRegistration> _registrations = new List<ServiceRegistration>();

		#endregion

		#region ' Methods '

		/// <summary>
		/// Discovers and registers tenant services from a single assembly.
		/// </summary>
		/// <param name="services">The service collection to register services into.</param>
		/// <param name="assembly">The assembly to search for tenant services.</param>
		/// <param name="options">Discovery options controlling filtering and registration behavior.</param>
		/// <remarks>
		/// This method searches all types in the assembly, looking for those decorated with
		/// <see cref="TenantServiceAttribute"/>. For each discovered type:
		/// <list type="number">
		/// <item><description>Validates the type is a valid service candidate</description></item>
		/// <item><description>Applies namespace filters if configured</description></item>
		/// <item><description>Determines which service types to register (interfaces, concrete types, or both)</description></item>
		/// <item><description>Creates a registration metadata object</description></item>
		/// <item><description>Registers each service type with the DI container</description></item>
		/// </list>
		/// If a type implements no interfaces and concrete type registration is disabled,
		/// the type is skipped. This prevents registration of services with no resolvable types.
		/// </remarks>
		private void DiscoverFromAssembly(IServiceCollection services, Assembly assembly, DiscoveryOptions options)
		{
			// Get all types from assembly, handling reflection exceptions
			IEnumerable<Type> types = GetTypesFromAssembly(assembly);

			foreach (Type type in types)
			{
				// Check if type has TenantServiceAttribute
				TenantServiceAttribute? attribute = type.GetCustomAttribute<TenantServiceAttribute>();
				if (attribute == null)
				{
					continue;
				}

				// Skip invalid types (abstract, generic definitions, non-public)
				if (!IsValidServiceType(type))
				{
					continue;
				}

				// Apply namespace filters if configured
				if (options.NamespaceFilters.Count != 0)
				{
					// Skip if type's namespace doesn't match any filter
					if (type.Namespace == null || !options.NamespaceFilters.Any(ns =>
						type.Namespace.StartsWith(ns, StringComparison.Ordinal)))
					{
						continue;
					}
				}

				// Determine which service types to register
				List<Type> serviceTypes = new List<Type>();

				// Add interfaces if option is enabled
				if (options.RegisterInterfaces)
				{
					List<Type> interfaces = type.GetInterfaces()
						.Where(i => !i.IsGenericTypeDefinition)
						.ToList();
					serviceTypes.AddRange(interfaces);
				}

				// Add concrete type if option is enabled
				if (options.RegisterConcreteTypes)
				{
					serviceTypes.Add(type);
				}

				// Skip if no service types to register
				if (serviceTypes.Count == 0)
				{
					continue;
				}

				// Create registration metadata
				ServiceRegistration registration = new ServiceRegistration(
					type,
					serviceTypes,
					attribute.Lifetime,
					attribute.TenantScope)
				{
					Source = $"Attribute: {assembly.GetName().Name}.{type.FullName}"
				};

				// Store registration for reporting
				this._registrations.Add(registration);

				// Register each service type with the DI container
				foreach (Type serviceType in serviceTypes)
				{
					ServiceDescriptor descriptor = new ServiceDescriptor(
						serviceType,
						type,
						attribute.Lifetime);

					services.Add(descriptor);
				}
			}
		}

		/// <summary>
		/// Discovers tenant services from assemblies and registers them in the service collection.
		/// </summary>
		/// <param name="services">The <see cref="IServiceCollection"/> to register discovered services into. Cannot be <see langword="null"/>.</param>
		/// <param name="options">
		/// Configuration options controlling discovery behavior including assembly selection,
		/// namespace filtering, and registration strategies. Cannot be <see langword="null"/>.
		/// </param>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="services"/> or <paramref name="options"/> is <see langword="null"/>.
		/// </exception>
		/// <remarks>
		/// <para>
		/// This method performs the following steps:
		/// <list type="number">
		/// <item><description>Determines which assemblies to search based on <see cref="DiscoveryOptions.Assemblies"/></description></item>
		/// <item><description>Iterates through each assembly and discovers types with <see cref="TenantServiceAttribute"/></description></item>
		/// <item><description>Applies namespace filters if configured</description></item>
		/// <item><description>Determines service types based on <see cref="DiscoveryOptions.RegisterInterfaces"/> and <see cref="DiscoveryOptions.RegisterConcreteTypes"/></description></item>
		/// <item><description>Creates <see cref="ServiceRegistration"/> metadata for each discovered service</description></item>
		/// <item><description>Registers services with the DI container using the lifetime and scope from the attribute</description></item>
		/// </list>
		/// </para>
		/// <para>
		/// If no assemblies are specified in options, all loaded assemblies are searched except:
		/// <list type="bullet">
		/// <item><description>System.* assemblies</description></item>
		/// <item><description>Microsoft.* assemblies</description></item>
		/// <item><description>mscorlib and netstandard</description></item>
		/// </list>
		/// </para>
		/// <para>
		/// Types must meet the following criteria to be discovered:
		/// <list type="bullet">
		/// <item><description>Decorated with <see cref="TenantServiceAttribute"/></description></item>
		/// <item><description>Public, concrete classes (not abstract or interfaces)</description></item>
		/// <item><description>Not generic type definitions</description></item>
		/// <item><description>Within configured namespace filters (if any)</description></item>
		/// </list>
		/// </para>
		/// </remarks>
		public void DiscoverAndRegister(IServiceCollection services, DiscoveryOptions options)
		{
			// Validate that services collection is not null
			ArgumentNullException.ThrowIfNull(services);

			// Validate that options is not null
			ArgumentNullException.ThrowIfNull(options);

			// Determine which assemblies to search
			IEnumerable<Assembly> assembliesToDiscover = options.Assemblies.Count != 0
				? options.Assemblies
				: AppDomain.CurrentDomain.GetAssemblies().Where(a => !IsSystemAssembly(a));

			// Search each assembly for tenant services
			foreach (Assembly? assembly in assembliesToDiscover)
			{
				this.DiscoverFromAssembly(services, assembly, options);
			}
		}

		/// <summary>
		/// Gets all service registrations discovered during the searching process.
		/// </summary>
		/// <returns>A read-only list of all discovered service registrations with complete metadata.</returns>
		/// <remarks>
		/// This method provides access to the complete list of registrations for:
		/// <list type="bullet">
		/// <item><description>Generating discovery manifests for documentation</description></item>
		/// <item><description>Diagnostic and troubleshooting purposes</description></item>
		/// <item><description>Validating service registration in tests</description></item>
		/// <item><description>Creating reports of application service inventory</description></item>
		/// </list>
		/// The returned list includes metadata such as implementation types, service types,
		/// lifetimes, tenant scopes, and source information for each registration.
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
		/// System assemblies are excluded to improve performance and avoid discovering framework types.
		/// The following assembly name patterns are considered system assemblies:
		/// <list type="bullet">
		/// <item><description>Assemblies starting with "System." (e.g., System.Linq, System.Collections)</description></item>
		/// <item><description>Assemblies starting with "Microsoft." (e.g., Microsoft.Extensions.*, Microsoft.AspNetCore.*)</description></item>
		/// <item><description>The "mscorlib" assembly (core runtime library)</description></item>
		/// <item><description>The "netstandard" assembly (.NET Standard library)</description></item>
		/// </list>
		/// All comparisons are case-insensitive to handle different naming conventions.
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
		/// A type is considered valid if it meets all of the following criteria:
		/// <list type="bullet">
		/// <item><description>Is a class (not an interface, enum, or value type)</description></item>
		/// <item><description>Is not abstract</description></item>
		/// <item><description>Is not a generic type definition (open generic)</description></item>
		/// <item><description>Is public</description></item>
		/// </list>
		/// These constraints ensure that the type can be instantiated by the DI container.
		/// Generic type definitions are excluded because they cannot be directly instantiated;
		/// closed generic types (e.g., Repository&lt;User&gt;) are valid if they meet other criteria.
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
		/// This method uses <see cref="ReflectionTypeLoadException"/> handling to gracefully
		/// handle scenarios where some types in an assembly fail to load due to missing dependencies
		/// or other reflection issues. Only successfully loaded types are returned.
		/// This prevents the entire discovery process from failing due to a single problematic type.
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