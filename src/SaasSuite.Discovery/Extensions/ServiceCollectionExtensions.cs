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
using SaasSuite.Discovery.Implementations;
using SaasSuite.Discovery.Interfaces;
using SaasSuite.Discovery.Options;
using SaasSuite.Discovery.Services;

namespace Microsoft.Extensions.DependencyInjection
{
	/// <summary>
	/// Provides extension methods for configuring tenant service discovery in the dependency injection container.
	/// </summary>
	/// <remarks>
	/// Service discovery automatically searches assemblies for types decorated with <see cref="TenantServiceAttribute"/>
	/// and registers them with the appropriate lifetime and scope. This eliminates manual service registration
	/// and ensures consistent registration patterns across the application.
	/// </remarks>
	public static class ServiceCollectionExtensions
	{
		#region ' Static Methods '

		/// <summary>
		/// Registers tenant service discovery and automatically discovers and registers services from assemblies.
		/// </summary>
		/// <param name="services">The <see cref="IServiceCollection"/> to add services to. Cannot be <see langword="null"/>.</param>
		/// <param name="configureOptions">
		/// Optional action to configure discovery options such as assembly filters, namespace filters,
		/// and registration behavior.
		/// </param>
		/// <returns>The <see cref="IServiceCollection"/> for method chaining.</returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="services"/> is <see langword="null"/>.
		/// </exception>
		/// <remarks>
		/// <para>
		/// This method performs the following steps:
		/// <list type="number">
		/// <item><description>Creates a <see cref="DiscoveryOptions"/> instance</description></item>
		/// <item><description>Applies the configuration action if provided</description></item>
		/// <item><description>Searches configured assemblies (or all loaded assemblies if none specified)</description></item>
		/// <item><description>Discovers types with <see cref="TenantServiceAttribute"/></description></item>
		/// <item><description>Registers discovered services with their specified lifetime and tenant scope</description></item>
		/// <item><description>Registers the discoverer itself as a singleton for runtime access</description></item>
		/// </list>
		/// </para>
		/// <para>
		/// By default, if no assemblies are specified in options:
		/// <list type="bullet">
		/// <item><description>All loaded non-system assemblies are searched</description></item>
		/// <item><description>Services are registered with their implemented interfaces</description></item>
		/// <item><description>Concrete types are also registered if <see cref="DiscoveryOptions.RegisterConcreteTypes"/> is <see langword="true"/></description></item>
		/// </list>
		/// </para>
		/// </remarks>
		public static IServiceCollection AddSaasDiscovery(this IServiceCollection services, Action<DiscoveryOptions>? configureOptions = null)
		{
			// Validate that services collection is not null
			ArgumentNullException.ThrowIfNull(services);

			// Create and configure discovery options
			DiscoveryOptions options = new DiscoveryOptions();
			configureOptions?.Invoke(options);

			// Create discoverer and perform discovery and registration
			TenantServiceDiscoverer discoverer = new TenantServiceDiscoverer();
			discoverer.DiscoverAndRegister(services, options);

			// Register the discoverer itself as a singleton for potential runtime use
			// This allows access to registration metadata and discovered services
			_ = services.AddSingleton<ITenantServiceDiscoverer>(discoverer);

			return services;
		}

		/// <summary>
		/// Registers tenant service discovery with explicit discovery options instance.
		/// </summary>
		/// <param name="services">The <see cref="IServiceCollection"/> to add services to. Cannot be <see langword="null"/>.</param>
		/// <param name="options">The pre-configured <see cref="DiscoveryOptions"/> to use. Cannot be <see langword="null"/>.</param>
		/// <returns>The <see cref="IServiceCollection"/> for method chaining.</returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="services"/> or <paramref name="options"/> is <see langword="null"/>.
		/// </exception>
		/// <remarks>
		/// This overload allows passing a fully configured options instance, which is useful when:
		/// <list type="bullet">
		/// <item><description>Options are loaded from configuration files (appsettings.json)</description></item>
		/// <item><description>Options are shared across multiple service registrations</description></item>
		/// <item><description>Options are constructed programmatically based on runtime conditions</description></item>
		/// </list>
		/// The behavior is identical to <see cref="AddSaasDiscovery(IServiceCollection, Action{DiscoveryOptions}?)"/>
		/// but accepts a pre-configured options object instead of a configuration action.
		/// </remarks>
		public static IServiceCollection AddSaasDiscovery(this IServiceCollection services, DiscoveryOptions options)
		{
			// Validate that services collection is not null
			ArgumentNullException.ThrowIfNull(services);

			// Validate that options is not null
			ArgumentNullException.ThrowIfNull(options);

			// Create discoverer and perform discovery and registration
			TenantServiceDiscoverer discoverer = new TenantServiceDiscoverer();
			discoverer.DiscoverAndRegister(services, options);

			// Register the discoverer as a singleton
			_ = services.AddSingleton<ITenantServiceDiscoverer>(discoverer);

			return services;
		}

		/// <summary>
		/// Creates a fluent discovery builder for advanced service registration scenarios with fine-grained control.
		/// </summary>
		/// <param name="services">The <see cref="IServiceCollection"/> to add services to. Cannot be <see langword="null"/>.</param>
		/// <param name="configure">
		/// Action to configure the <see cref="DiscoveryBuilder"/> using a fluent API.
		/// Cannot be <see langword="null"/>.
		/// </param>
		/// <returns>The <see cref="IServiceCollection"/> for method chaining.</returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="services"/> or <paramref name="configure"/> is <see langword="null"/>.
		/// </exception>
		/// <remarks>
		/// <para>
		/// The discovery builder provides a fluent API for complex discovery scenarios, including:
		/// <list type="bullet">
		/// <item><description>Selecting specific assemblies to search</description></item>
		/// <item><description>Filtering types by attributes, namespaces, or custom predicates</description></item>
		/// <item><description>Choosing registration strategies (interfaces, concrete types, matching interfaces)</description></item>
		/// <item><description>Configuring service lifetimes (scoped, singleton, transient)</description></item>
		/// <item><description>Setting tenant scopes (global, per-request, singleton-per-tenant)</description></item>
		/// <item><description>Adding conditional registration based on tenant predicates</description></item>
		/// </list>
		/// </para>
		/// <para>
		/// The builder must be activated by calling <see cref="DiscoveryBuilder.Activate"/> at the end
		/// of the configuration action to actually register the discovered services.
		/// </para>
		/// </remarks>
		public static IServiceCollection AddSaasDiscoveryBuilder(this IServiceCollection services, Action<DiscoveryBuilder> configure)
		{
			// Validate that services collection is not null
			ArgumentNullException.ThrowIfNull(services);

			// Validate that configure action is not null
			ArgumentNullException.ThrowIfNull(configure);

			// Create discovery builder and apply configuration
			DiscoveryBuilder builder = new DiscoveryBuilder(services);
			configure(builder);

			// Activate the builder to perform registration
			// This must be called explicitly to ensure all configuration is complete
			builder.Activate();

			return services;
		}

		#endregion
	}
}