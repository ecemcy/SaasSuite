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

using SaasSuite.Metering.Interfaces;
using SaasSuite.Metering.Options;
using SaasSuite.Metering.Services;
using SaasSuite.Metering.Stores;

namespace Microsoft.Extensions.DependencyInjection
{
	/// <summary>
	/// Provides extension methods for registering SaasSuite metering services in the dependency injection container.
	/// These extensions simplify the configuration of usage tracking and billing infrastructure in multi-tenant ASP.NET Core applications.
	/// </summary>
	/// <remarks>
	/// This static class extends <see cref="IServiceCollection"/> to enable fluent registration of metering services
	/// using method chaining. The extensions are defined in the <see cref="Microsoft.Extensions.DependencyInjection"/>
	/// namespace to make them automatically available when the DI namespace is imported, following the .NET convention
	/// for extension method discoverability in ASP.NET Core applications.
	/// </remarks>
	public static class ServiceCollectionExtensions
	{
		#region ' Static Methods '

		/// <summary>
		/// Adds SaaS metering services to the service collection with default in-memory storage and default configuration.
		/// This is the simplest registration method suitable for getting started quickly without custom configuration.
		/// </summary>
		/// <param name="services">The service collection to add the metering services to. Cannot be <see langword="null"/>.</param>
		/// <returns>The same <paramref name="services"/> instance for method chaining, enabling fluent configuration.</returns>
		/// <remarks>
		/// This method registers:
		/// <list type="bullet">
		/// <item><description><see cref="IMeteringStore"/> as <see cref="InMemoryMeteringStore"/> with singleton lifetime</description></item>
		/// <item><description><see cref="MeteringService"/> with scoped lifetime</description></item>
		/// <item><description>Default <see cref="MeteringOptions"/> (90 day retention, no metric validation)</description></item>
		/// </list>
		/// <para>
		/// This is equivalent to calling:
		/// <code>
		/// services.AddSaasMetering(options => { });
		/// </code>
		/// </para>
		/// <para>
		/// The in-memory store is suitable for:
		/// <list type="bullet">
		/// <item><description>Development and testing environments</description></item>
		/// <item><description>Proof-of-concept applications</description></item>
		/// <item><description>Single-instance deployments with acceptable data loss on restart</description></item>
		/// </list>
		/// For production multi-instance deployments, use the generic overload to specify a persistent store implementation.
		/// </para>
		/// </remarks>
		public static IServiceCollection AddSaasMetering(this IServiceCollection services)
		{
			// Delegate to the overload with an empty configuration action
			return services.AddSaasMetering(_ => { });
		}

		/// <summary>
		/// Adds SaaS metering services to the service collection with default in-memory storage and custom configuration.
		/// Allows configuring metering options such as retention period, metric validation, and auto-aggregation.
		/// </summary>
		/// <param name="services">The service collection to add the metering services to. Cannot be <see langword="null"/>.</param>
		/// <param name="configureOptions">
		/// An action delegate to configure <see cref="MeteringOptions"/>. Cannot be <see langword="null"/>.
		/// This delegate receives a default-initialized options object that can be modified to customize metering behavior.
		/// </param>
		/// <returns>The same <paramref name="services"/> instance for method chaining, enabling fluent configuration.</returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="services"/> is <see langword="null"/> or when <paramref name="configureOptions"/> is <see langword="null"/>.
		/// </exception>
		/// <remarks>
		/// This method registers:
		/// <list type="bullet">
		/// <item><description><see cref="MeteringOptions"/> configured via the options pattern</description></item>
		/// <item><description><see cref="IMeteringStore"/> as <see cref="InMemoryMeteringStore"/> with singleton lifetime</description></item>
		/// <item><description><see cref="MeteringService"/> with scoped lifetime</description></item>
		/// </list>
		/// <para>
		/// The <see cref="ServiceCollectionDescriptorExtensions.TryAddSingleton{TService, TImplementation}(IServiceCollection)"/> and <see cref="ServiceCollectionDescriptorExtensions.TryAddScoped{TService}(IServiceCollection)"/> methods are used to allow
		/// external registrations to override the defaults if registered before calling this method.
		/// </para>
		/// <para>
		/// Configuration example:
		/// <code>
		/// services.AddSaasMetering(options =>
		/// {
		///     options.RetentionPeriod = TimeSpan.FromDays(365);
		///     options.ValidateMetrics = true;
		///     options.ValidMetrics = new HashSet&lt;string&gt; { "api-calls", "storage-gb" };
		///     options.EnableAutoAggregation = true;
		///     options.AggregationInterval = TimeSpan.FromHours(1);
		/// });
		/// </code>
		/// </para>
		/// <para>
		/// Service lifetimes explained:
		/// <list type="bullet">
		/// <item><description>Singleton store: Shared instance maintains all events in memory across requests</description></item>
		/// <item><description>Scoped service: New instance per HTTP request/scope for better resource management</description></item>
		/// </list>
		/// </para>
		/// </remarks>
		public static IServiceCollection AddSaasMetering(this IServiceCollection services, Action<MeteringOptions> configureOptions)
		{
			// Validate that services collection is not null
			ArgumentNullException.ThrowIfNull(services);

			// Validate that configuration action is not null
			ArgumentNullException.ThrowIfNull(configureOptions);

			// Register options using the standard .NET options pattern
			// This allows options to be injected as IOptions<MeteringOptions> in services
			_ = services.Configure(configureOptions);

			// Register the in-memory store as singleton (only if not already registered)
			// Singleton ensures all requests share the same event collection
			services.TryAddSingleton<IMeteringStore, InMemoryMeteringStore>();

			// Register the metering service as scoped (only if not already registered)
			// Scoped lifetime creates one instance per HTTP request/scope
			services.TryAddScoped<MeteringService>();

			// Return services for method chaining
			return services;
		}

		/// <summary>
		/// Adds SaaS metering services with a custom metering store implementation and default configuration.
		/// Use this overload to replace the in-memory store with a persistent implementation such as a database or time-series store.
		/// </summary>
		/// <typeparam name="TStore">
		/// The custom implementation of <see cref="IMeteringStore"/> to use. Must be a class implementing the interface.
		/// The type should have a constructor compatible with dependency injection resolution.
		/// </typeparam>
		/// <param name="services">The service collection to add the metering services to. Cannot be <see langword="null"/>.</param>
		/// <returns>The same <paramref name="services"/> instance for method chaining, enabling fluent configuration.</returns>
		/// <remarks>
		/// This method registers:
		/// <list type="bullet">
		/// <item><description>Default <see cref="MeteringOptions"/> (90 day retention, no metric validation)</description></item>
		/// <item><description><see cref="IMeteringStore"/> as <typeparamref name="TStore"/> with singleton lifetime</description></item>
		/// <item><description><see cref="MeteringService"/> with scoped lifetime</description></item>
		/// </list>
		/// <para>
		/// This is equivalent to calling:
		/// <code>
		/// services.AddSaasMetering&lt;TStore&gt;(options => { });
		/// </code>
		/// </para>
		/// <para>
		/// Example usage with SQL Server store:
		/// <code>
		/// services.AddSaasMetering&lt;SqlServerMeteringStore&gt;();
		/// </code>
		/// </para>
		/// <para>
		/// Custom store considerations:
		/// <list type="bullet">
		/// <item><description>Ensure the store type is registered with appropriate dependencies in its constructor</description></item>
		/// <item><description>The store should handle connection management and resource disposal</description></item>
		/// <item><description>Consider implementing retry policies for transient failures</description></item>
		/// <item><description>Implement proper indexing and query optimization for performance</description></item>
		/// </list>
		/// </para>
		/// </remarks>
		public static IServiceCollection AddSaasMetering<TStore>(this IServiceCollection services)
			where TStore : class, IMeteringStore
		{
			// Delegate to the overload with custom store and empty configuration
			return services.AddSaasMetering<TStore>(_ => { });
		}

		/// <summary>
		/// Adds SaaS metering services with a custom metering store implementation and custom configuration.
		/// This is the most flexible registration method, allowing full customization of both storage and options.
		/// </summary>
		/// <typeparam name="TStore">
		/// The custom implementation of <see cref="IMeteringStore"/> to use. Must be a class implementing the interface.
		/// Common implementations include database stores (SQL, NoSQL), time-series databases, or cloud services.
		/// </typeparam>
		/// <param name="services">The service collection to add the metering services to. Cannot be <see langword="null"/>.</param>
		/// <param name="configureOptions">
		/// An action delegate to configure <see cref="MeteringOptions"/>. Cannot be <see langword="null"/>.
		/// Allows customization of retention, validation, and aggregation settings.
		/// </param>
		/// <returns>The same <paramref name="services"/> instance for method chaining, enabling fluent configuration.</returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="services"/> is <see langword="null"/> or when <paramref name="configureOptions"/> is <see langword="null"/>.
		/// </exception>
		/// <remarks>
		/// This method registers:
		/// <list type="bullet">
		/// <item><description><see cref="MeteringOptions"/> configured via the provided action</description></item>
		/// <item><description><see cref="IMeteringStore"/> as <typeparamref name="TStore"/> with singleton lifetime</description></item>
		/// <item><description><see cref="MeteringService"/> with scoped lifetime</description></item>
		/// </list>
		/// <para>
		/// Complete configuration example with PostgreSQL store:
		/// <code>
		/// services.AddSaasMetering&lt;PostgresMeteringStore&gt;(options =>
		/// {
		///     options.RetentionPeriod = TimeSpan.FromDays(730); // 2 years
		///     options.ValidateMetrics = true;
		///     options.ValidMetrics = new HashSet&lt;string&gt;
		///     {
		///         "api-calls",
		///         "storage-gb",
		///         "compute-hours",
		///         "data-transfer-gb"
		///     };
		///     options.EnableAutoAggregation = true;
		///     options.AggregationInterval = TimeSpan.FromMinutes(15);
		/// });
		/// </code>
		/// </para>
		/// <para>
		/// The <see cref="ServiceCollectionDescriptorExtensions.TryAddSingleton{TService, TImplementation}(IServiceCollection)"/> and <see cref="ServiceCollectionDescriptorExtensions.TryAddScoped{TService}(IServiceCollection)"/> methods ensure that:
		/// <list type="bullet">
		/// <item><description>Services registered before this call take precedence</description></item>
		/// <item><description>Multiple calls to AddSaasMetering won't create duplicate registrations</description></item>
		/// <item><description>Test projects can override registrations with mocks or fakes</description></item>
		/// </list>
		/// </para>
		/// <para>
		/// After registration, inject services as needed:
		/// <code>
		/// public class UsageController : Controller
		/// {
		///     private readonly MeteringService _meteringService;
		///
		///     public UsageController(MeteringService meteringService)
		///     {
		///         _meteringService = meteringService;
		///     }
		///
		///     [HttpGet]
		///     public async Task&lt;IActionResult&gt; GetUsage(string tenantId)
		///     {
		///         var usage = await _meteringService.GetCurrentMonthAggregatedUsageAsync(
		///             new TenantId(tenantId),
		///             AggregationPeriod.Daily);
		///         return Ok(usage);
		///     }
		/// }
		/// </code>
		/// </para>
		/// </remarks>
		public static IServiceCollection AddSaasMetering<TStore>(this IServiceCollection services, Action<MeteringOptions> configureOptions)
			where TStore : class, IMeteringStore
		{
			// Validate that services collection is not null
			ArgumentNullException.ThrowIfNull(services);

			// Validate that configuration action is not null
			ArgumentNullException.ThrowIfNull(configureOptions);

			// Register options using the standard .NET options pattern
			_ = services.Configure(configureOptions);

			// Register the custom store implementation as singleton (only if not already registered)
			// TryAdd allows test projects to register mocks before calling this method
			services.TryAddSingleton<IMeteringStore, TStore>();

			// Register the metering service as scoped (only if not already registered)
			services.TryAddScoped<MeteringService>();

			// Return services for method chaining
			return services;
		}

		#endregion
	}
}