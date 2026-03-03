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

using SaasSuite.Core;
using SaasSuite.Core.Interfaces;

namespace Microsoft.Extensions.DependencyInjection
{
	/// <summary>
	/// Provides extension methods for applying the decorator pattern to tenant-aware services.
	/// </summary>
	/// <remarks>
	/// The decorator pattern allows runtime behavior modification of services based on tenant context
	/// without changing the original service implementation. This is useful for:
	/// <list type="bullet">
	/// <item><description>Adding tenant-specific caching, logging, or validation</description></item>
	/// <item><description>Implementing feature flags or A/B testing per tenant</description></item>
	/// <item><description>Adding audit trails or monitoring for specific tenants</description></item>
	/// <item><description>Wrapping services with tenant-specific business rules</description></item>
	/// </list>
	/// <para>
	/// <strong>Version 1 Limitation:</strong> Decorator support is currently limited to scoped and transient
	/// services. Singleton services cannot be decorated in v1. This limitation exists because singleton
	/// decorators would need to maintain state across all tenants, which conflicts with tenant isolation principles.
	/// </para>
	/// </remarks>
	public static class DecoratorExtensions
	{
		#region ' Static Methods '

		/// <summary>
		/// Decorates all registrations of a service type with the specified decorator, optionally applying the decorator conditionally per tenant.
		/// </summary>
		/// <typeparam name="TService">The service interface type to decorate. Must be a class.</typeparam>
		/// <typeparam name="TDecorator">
		/// The decorator type that implements <typeparamref name="TService"/>. Must be a class
		/// with a constructor that accepts the decorated service as a parameter.
		/// </typeparam>
		/// <param name="services">The <see cref="IServiceCollection"/> to modify. Cannot be <see langword="null"/>.</param>
		/// <param name="tenantPredicate">
		/// Optional predicate to conditionally apply the decorator based on tenant context.
		/// If <see langword="null"/>, the decorator is always applied.
		/// </param>
		/// <returns>The <see cref="IServiceCollection"/> for method chaining.</returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="services"/> is <see langword="null"/>.
		/// </exception>
		/// <exception cref="NotSupportedException">
		/// Thrown when attempting to decorate a singleton service (v1 limitation).
		/// </exception>
		/// <remarks>
		/// <para>
		/// This method is similar to <see cref="DecorateTenantScoped{TService, TDecorator}"/> but applies
		/// the decorator to all registrations of the service type, not just the most recent one.
		/// This is useful when multiple implementations are registered (e.g., for collection injection)
		/// and all should be decorated uniformly.
		/// </para>
		/// <para>
		/// Use cases include:
		/// <list type="bullet">
		/// <item><description>Decorating all message handlers in a collection</description></item>
		/// <item><description>Adding logging to all repository implementations</description></item>
		/// <item><description>Wrapping all validators with tenant-specific rules</description></item>
		/// </list>
		/// </para>
		/// <para>
		/// <strong>v1 Limitation:</strong> Only scoped and transient services can be decorated.
		/// If any registration is a singleton, the entire operation fails with <see cref="NotSupportedException"/>.
		/// </para>
		/// </remarks>
		public static IServiceCollection DecorateAllTenantScoped<TService, TDecorator>(this IServiceCollection services, Func<TenantContext, bool>? tenantPredicate = null)
			where TService : class
			where TDecorator : class, TService
		{
			// Validate that services collection is not null
			ArgumentNullException.ThrowIfNull(services);

			// Find all descriptors for the service type
			List<ServiceDescriptor> descriptors = services.Where(d => d.ServiceType == typeof(TService)).ToList();

			// Iterate through each descriptor and apply decorator
			foreach (ServiceDescriptor descriptor in descriptors)
			{
				// v1 limitation check: ensure it's not a singleton
				if (descriptor.Lifetime == ServiceLifetime.Singleton)
				{
					throw new NotSupportedException(
						"Decorator support for singleton services is not available in v1. " +
						"Only scoped and transient services can be decorated.");
				}

				// Remove the original descriptor
				_ = services.Remove(descriptor);

				// Determine implementation type
				Type implementationType = descriptor.ImplementationType
					?? descriptor.ImplementationInstance?.GetType()
					?? throw new InvalidOperationException("Cannot determine implementation type");

				// Re-register original implementation with its concrete type
				services.Add(new ServiceDescriptor(
					implementationType,
					implementationType,
					descriptor.Lifetime));

				// Add decorated registration
				services.Add(new ServiceDescriptor(
					typeof(TService),
					provider =>
					{
						// Create instance of original implementation
						object decorated = ActivatorUtilities.CreateInstance(provider, implementationType);

						// Apply tenant predicate if specified
						if (tenantPredicate != null)
						{
							ITenantAccessor? tenantAccessor = provider.GetService<ITenantAccessor>();
							TenantContext? tenantContext = tenantAccessor?.TenantContext;

							// Return undecorated if predicate fails
							if (tenantContext == null || !tenantPredicate(tenantContext))
							{
								return (TService)decorated;
							}
						}

						// Apply decorator
						return ActivatorUtilities.CreateInstance<TDecorator>(provider, decorated);
					},
					descriptor.Lifetime));
			}

			return services;
		}

		/// <summary>
		/// Decorates a tenant-scoped service with the specified decorator type, optionally applying the decorator conditionally per tenant.
		/// </summary>
		/// <typeparam name="TService">The service interface type to decorate. Must be a class.</typeparam>
		/// <typeparam name="TDecorator">
		/// The decorator type that implements <typeparamref name="TService"/>. Must be a class
		/// with a constructor that accepts the decorated service as a parameter.
		/// </typeparam>
		/// <param name="services">The <see cref="IServiceCollection"/> to modify. Cannot be <see langword="null"/>.</param>
		/// <param name="tenantPredicate">
		/// Optional predicate to conditionally apply the decorator based on tenant context.
		/// If <see langword="null"/>, the decorator is always applied. If provided and returns <see langword="false"/>,
		/// the original undecorated service is returned.
		/// </param>
		/// <returns>The <see cref="IServiceCollection"/> for method chaining.</returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="services"/> is <see langword="null"/>.
		/// </exception>
		/// <exception cref="InvalidOperationException">
		/// Thrown when no service is registered for type <typeparamref name="TService"/>.
		/// </exception>
		/// <exception cref="NotSupportedException">
		/// Thrown when attempting to decorate a singleton service (v1 limitation).
		/// </exception>
		/// <remarks>
		/// <para>
		/// This method modifies the service registration to wrap the original implementation with the decorator.
		/// The decorator must have a constructor that accepts the original service as a parameter, typically
		/// as the first constructor parameter followed by any additional dependencies.
		/// </para>
		/// <para>
		/// The tenant predicate allows conditional decoration based on tenant properties such as:
		/// <list type="bullet">
		/// <item><description>Tenant tier or subscription level</description></item>
		/// <item><description>Feature flags enabled for the tenant</description></item>
		/// <item><description>Tenant region or compliance requirements</description></item>
		/// <item><description>Experimental features enabled for specific tenants</description></item>
		/// </list>
		/// </para>
		/// <para>
		/// <strong>v1 Limitation:</strong> Only scoped and transient services can be decorated.
		/// Attempting to decorate a singleton service throws <see cref="NotSupportedException"/>.
		/// This limitation will be addressed in a future version.
		/// </para>
		/// </remarks>
		public static IServiceCollection DecorateTenantScoped<TService, TDecorator>(this IServiceCollection services, Func<TenantContext, bool>? tenantPredicate = null)
			where TService : class
			where TDecorator : class, TService
		{
			// Validate that services collection is not null
			ArgumentNullException.ThrowIfNull(services);

			// Find the most recent registration for the service type
			ServiceDescriptor? descriptor = services.LastOrDefault(d => d.ServiceType == typeof(TService))
				?? throw new InvalidOperationException($"No service registered for type {typeof(TService).Name}");

			// v1 limitation: Only support scoped and transient services
			if (descriptor.Lifetime == ServiceLifetime.Singleton)
			{
				throw new NotSupportedException(
					"Decorator support for singleton services is not available in v1. " +
					"Only scoped and transient services can be decorated. " +
					"This limitation will be addressed in a future version.");
			}

			// Remove the original service descriptor
			_ = services.Remove(descriptor);

			// Determine the implementation type from the descriptor
			Type implementationType = descriptor.ImplementationType
				?? descriptor.ImplementationInstance?.GetType()
				?? throw new InvalidOperationException("Cannot determine implementation type");

			// Re-register the original implementation with its concrete type
			// This allows the decorator to resolve the original implementation
			services.Add(new ServiceDescriptor(
				implementationType,
				implementationType,
				descriptor.Lifetime));

			// Add the decorated service registration
			services.Add(new ServiceDescriptor(
				typeof(TService),
				provider =>
				{
					// Create instance of the original implementation
					object decorated = ActivatorUtilities.CreateInstance(provider, implementationType);

					// If tenant predicate is specified, evaluate it
					if (tenantPredicate != null)
					{
						// Attempt to get tenant context from the accessor
						ITenantAccessor? tenantAccessor = provider.GetService<ITenantAccessor>();
						TenantContext? tenantContext = tenantAccessor?.TenantContext;

						// If no tenant context or predicate fails, return original undecorated service
						if (tenantContext == null || !tenantPredicate(tenantContext))
						{
							return (TService)decorated;
						}
					}

					// Apply decorator by creating instance with decorated service as dependency
					return ActivatorUtilities.CreateInstance<TDecorator>(
						provider,
						decorated);
				},
				descriptor.Lifetime));

			return services;
		}

		#endregion
	}
}