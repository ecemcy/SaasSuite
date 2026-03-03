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

using Microsoft.Extensions.DependencyInjection.Extensions;

using SaasSuite.Audit.Interfaces;
using SaasSuite.Audit.Services;

namespace Microsoft.Extensions.DependencyInjection
{
	/// <summary>
	/// Provides extension methods for configuring SaasSuite audit services in the dependency injection container.
	/// </summary>
	/// <remarks>
	/// These extensions simplify the registration of audit services by providing fluent API methods
	/// that can be chained in the application startup configuration. They support both the default
	/// in-memory implementation and custom implementations for production scenarios.
	/// </remarks>
	public static class ServiceCollectionExtensions
	{
		#region ' Static Methods '

		/// <summary>
		/// Registers the default SaasSuite audit services with the service collection using the in-memory implementation.
		/// </summary>
		/// <param name="services">The <see cref="IServiceCollection"/> to add services to. Cannot be <see langword="null"/>.</param>
		/// <returns>The <see cref="IServiceCollection"/> for method chaining.</returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="services"/> is <see langword="null"/>.
		/// </exception>
		/// <remarks>
		/// This method registers <see cref="AuditService"/> as the singleton implementation of <see cref="IAuditService"/>.
		/// The singleton lifetime ensures a single shared instance is used throughout the application, which is appropriate
		/// for the in-memory implementation where all audit events are stored in a single collection.
		/// The method uses <see cref="ServiceCollectionDescriptorExtensions.TryAddSingleton{TService, TImplementation}"/>
		/// to avoid duplicate registrations if the service has already been added.
		/// This is suitable for development, testing, and single-instance deployments. For production scenarios
		/// requiring durable storage, use the generic overload to register a custom implementation.
		/// </remarks>
		public static IServiceCollection AddSaasAudit(this IServiceCollection services)
		{
			// Validate that services collection is not null
			ArgumentNullException.ThrowIfNull(services);

			// Register the default in-memory audit service as a singleton
			// TryAddSingleton ensures we don't override an existing registration
			services.TryAddSingleton<IAuditService, AuditService>();

			// Return the service collection for fluent chaining
			return services;
		}

		/// <summary>
		/// Registers SaasSuite audit services with a custom implementation of <see cref="IAuditService"/>.
		/// </summary>
		/// <typeparam name="TImplementation">
		/// The concrete type implementing <see cref="IAuditService"/>. Must be a class with a public constructor
		/// compatible with dependency injection.
		/// </typeparam>
		/// <param name="services">The <see cref="IServiceCollection"/> to add services to. Cannot be <see langword="null"/>.</param>
		/// <returns>The <see cref="IServiceCollection"/> for method chaining.</returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="services"/> is <see langword="null"/>.
		/// </exception>
		/// <remarks>
		/// This method allows substitution of the default in-memory implementation with a custom one,
		/// such as a database-backed audit service, distributed logging implementation, or integration
		/// with external audit systems like Azure Monitor, AWS CloudTrail, or Splunk.
		/// The custom implementation is registered as a singleton, which is typically appropriate for
		/// audit services that maintain shared state or connections. If your implementation requires
		/// scoped or transient lifetime, register it manually instead of using this extension.
		/// The implementation type must have a constructor that can be satisfied by the DI container.
		/// Common dependencies include database contexts, HTTP clients, logging services, or configuration options.
		/// The method uses <see cref="ServiceCollectionDescriptorExtensions.TryAddSingleton{TService, TImplementation}"/>
		/// to avoid duplicate registrations.
		/// </remarks>
		public static IServiceCollection AddSaasAudit<TImplementation>(this IServiceCollection services)
			where TImplementation : class, IAuditService
		{
			// Validate that services collection is not null
			ArgumentNullException.ThrowIfNull(services);

			// Register the custom audit service implementation as a singleton
			// TryAddSingleton ensures we don't override an existing registration
			services.TryAddSingleton<IAuditService, TImplementation>();

			// Return the service collection for fluent chaining
			return services;
		}

		#endregion
	}
}