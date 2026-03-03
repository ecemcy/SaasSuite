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
using SaasSuite.Core.Options;
using SaasSuite.Core.Services;

namespace Microsoft.Extensions.DependencyInjection
{
	/// <summary>
	/// Provides dependency injection registration extension methods for SaasSuite Core services.
	/// These extensions configure the core multi-tenancy infrastructure components in the DI container,
	/// including tenant accessor, maintenance service, and configuration options.
	/// </summary>
	public static class ServiceCollectionExtensions
	{
		#region ' Static Methods '

		/// <summary>
		/// Registers the SaasSuite Core default service implementations in the dependency injection container.
		/// Configures core services including tenant accessor and maintenance service with sensible default implementations
		/// suitable for most scenarios.
		/// </summary>
		/// <param name="services">The service collection to add registrations to. Cannot be <see langword="null"/>.</param>
		/// <returns>The same <paramref name="services"/> instance for method chaining, enabling fluent configuration.</returns>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="services"/> is <see langword="null"/>.</exception>
		/// <remarks>
		/// This method registers the following core services:
		/// <list type="bullet">
		/// <item>
		/// <description>
		/// <see cref="IMaintenanceService"/> as <see cref="MaintenanceService"/> with <see cref="ServiceLifetime.Singleton"/> lifetime.
		/// This in-memory implementation is suitable for development and testing but should be replaced with a persistent
		/// implementation for production multi-instance deployments.
		/// </description>
		/// </item>
		/// <item>
		/// <description>
		/// <see cref="ITenantAccessor"/> as <see cref="TenantAccessor"/> with <see cref="ServiceLifetime.Singleton"/> lifetime.
		/// Although registered as a singleton, the accessor uses <see cref="AsyncLocal{T}"/> internally to store
		/// tenant context per async flow, effectively providing request-scoped behavior while maintaining singleton registration.
		/// </description>
		/// </item>
		/// </list>
		/// <para>
		/// Library consumers can override these default implementations by registering their own implementations
		/// after calling this method. Later registrations take precedence in the DI container.
		/// </para>
		/// <para>
		/// Additional services that must be registered separately:
		/// <list type="bullet">
		/// <item><description><see cref="ITenantResolver"/> - Required for tenant identification from requests</description></item>
		/// <item><description><see cref="ITenantStore"/> - Required for loading tenant metadata</description></item>
		/// <item><description><see cref="ITelemetryEnricher"/> - Optional for enriching telemetry with tenant context</description></item>
		/// <item><description><see cref="IIsolationPolicy"/> - Optional for custom isolation logic</description></item>
		/// </list>
		/// </para>
		/// </remarks>
		public static IServiceCollection AddSaasCore(this IServiceCollection services)
		{
			ArgumentNullException.ThrowIfNull(services, nameof(services));

			// Register maintenance service as singleton for application-wide maintenance window tracking
			// In production, consider replacing with a database-backed implementation for multi-instance deployments
			_ = services.AddSingleton<IMaintenanceService, MaintenanceService>();

			// Register tenant accessor as singleton, but it stores TenantContext in AsyncLocal
			// so it behaves as request-scoped per async flow, providing isolation between concurrent requests
			_ = services.AddSingleton<ITenantAccessor>(_ => new TenantAccessor());

			return services;
		}

		/// <summary>
		/// Registers SaasSuite Core services and applies custom configuration options.
		/// Allows detailed configuration of multi-tenancy behavior including tenant requirements,
		/// default isolation levels, and telemetry enrichment settings before registering core services.
		/// </summary>
		/// <param name="services">The service collection to add registrations to. Cannot be <see langword="null"/>.</param>
		/// <param name="configureOptions">
		/// Action delegate used to configure <see cref="SaasCoreOptions"/>. Cannot be <see langword="null"/>.
		/// This delegate receives a default-initialized options object that can be modified.
		/// </param>
		/// <returns>The same <paramref name="services"/> instance for method chaining, enabling fluent configuration.</returns>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="services"/> or <paramref name="configureOptions"/> is <see langword="null"/>.</exception>
		/// <remarks>
		/// This overload allows customization of SaasSuite Core behavior before registering services.
		/// The configured options instance is registered as a singleton and can be injected into middleware
		/// and services that need access to the configuration.
		/// <para>
		/// Configurable options include:
		/// <list type="bullet">
		/// <item>
		/// <description>
		/// <see cref="SaasCoreOptions.RequireTenant"/> - Whether requests must successfully resolve a tenant.
		/// Set to <see langword="false"/> to allow anonymous or non-tenant requests. Default is <see langword="true"/>.
		/// </description>
		/// </item>
		/// <item>
		/// <description>
		/// <see cref="SaasCoreOptions.DefaultIsolationLevel"/> - Fallback isolation level for tenants without explicit configuration.
		/// Default is <see cref="IsolationLevel.Shared"/>.
		/// </description>
		/// </item>
		/// <item>
		/// <description>
		/// <see cref="SaasCoreOptions.EnableTelemetryEnrichment"/> - Whether to automatically enrich telemetry with tenant context.
		/// Requires an <see cref="ITelemetryEnricher"/> implementation to be registered. Default is <see langword="true"/>.
		/// </description>
		/// </item>
		/// </list>
		/// </para>
		/// </remarks>
		public static IServiceCollection AddSaasCore(this IServiceCollection services, Action<SaasCoreOptions> configureOptions)
		{
			ArgumentNullException.ThrowIfNull(services, nameof(services));
			ArgumentNullException.ThrowIfNull(configureOptions, nameof(configureOptions));

			// Create and configure options instance with defaults, then apply custom configuration
			SaasCoreOptions options = new SaasCoreOptions();
			configureOptions(options);

			// Register the configured options as a singleton for simple consumption in middleware/services
			// This allows middleware and services to access configuration without using IOptions<T> pattern
			_ = services.AddSingleton(options);

			// Register core services with default implementations
			_ = services.AddSaasCore();

			return services;
		}

		#endregion
	}
}