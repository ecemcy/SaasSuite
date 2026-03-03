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

using SaasSuite.Migration.Implementations;
using SaasSuite.Migration.Interfaces;
using SaasSuite.Migration.Options;

namespace Microsoft.Extensions.DependencyInjection
{
	/// <summary>
	/// Provides extension methods for registering multi-tenant migration services in the dependency injection container.
	/// </summary>
	/// <remarks>
	/// These extensions simplify the configuration of the migration engine and migration steps,
	/// enabling automated tenant migrations with support for batching, checkpointing, parallel execution,
	/// and rollback capabilities.
	/// </remarks>
	public static class ServiceCollectionExtensions
	{
		#region ' Static Methods '

		/// <summary>
		/// Registers a migration step with the service collection for automatic discovery by the migration engine.
		/// </summary>
		/// <typeparam name="TStep">
		/// The migration step type implementing <see cref="IMigrationStep"/>. Must be a class
		/// with a public constructor compatible with dependency injection.
		/// </typeparam>
		/// <param name="services">The <see cref="IServiceCollection"/> to add services to. Cannot be <see langword="null"/>.</param>
		/// <returns>The <see cref="IServiceCollection"/> for method chaining.</returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="services"/> is <see langword="null"/>.
		/// </exception>
		/// <remarks>
		/// <para>
		/// This method registers the migration step as a scoped service, allowing it to be resolved
		/// and executed by the migration engine. The scoped lifetime ensures that:
		/// <list type="bullet">
		/// <item><description>Each migration operation gets fresh step instances</description></item>
		/// <item><description>Steps can safely maintain state during a single migration run</description></item>
		/// <item><description>Dependencies are properly resolved and disposed</description></item>
		/// </list>
		/// </para>
		/// <para>
		/// Multiple steps can be registered by calling this method multiple times.
		/// Steps are executed in the order they are resolved from the DI container, which
		/// typically matches registration order but is not guaranteed. For explicit ordering,
		/// pass steps to the migration engine in a specific sequence.
		/// </para>
		/// <para>
		/// The method uses <see cref="ServiceCollectionDescriptorExtensions.TryAddEnumerable(IServiceCollection, ServiceDescriptor)"/>
		/// to avoid duplicate registrations of the same step type.
		/// </para>
		/// <para>
		/// Migration steps can inject dependencies through their constructors, such as:
		/// <list type="bullet">
		/// <item><description>Database contexts for data migrations</description></item>
		/// <item><description>HTTP clients for external service migrations</description></item>
		/// <item><description>Logging services for progress tracking</description></item>
		/// <item><description>Configuration services for environment-specific behavior</description></item>
		/// </list>
		/// </para>
		/// </remarks>
		public static IServiceCollection AddMigrationStep<TStep>(this IServiceCollection services)
			where TStep : class, IMigrationStep
		{
			// Validate that services collection is not null
			ArgumentNullException.ThrowIfNull(services);

			// Register the migration step as a scoped enumerable service
			// TryAddEnumerable prevents duplicate registrations of the same type
			services.TryAddEnumerable(ServiceDescriptor.Scoped<IMigrationStep, TStep>());

			return services;
		}

		/// <summary>
		/// Registers the multi-tenant migration engine and related services with the service collection.
		/// </summary>
		/// <param name="services">The <see cref="IServiceCollection"/> to add services to. Cannot be <see langword="null"/>.</param>
		/// <param name="configureOptions">
		/// Optional action to configure <see cref="MigrationOptions"/>. If <see langword="null"/>,
		/// default options are used with 10 tenants per batch and checkpointing enabled.
		/// </param>
		/// <returns>The <see cref="IServiceCollection"/> for method chaining.</returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="services"/> is <see langword="null"/>.
		/// </exception>
		/// <remarks>
		/// <para>
		/// This method registers:
		/// <list type="bullet">
		/// <item><description><see cref="MigrationOptions"/> configured via the options pattern</description></item>
		/// <item><description><see cref="IMigrationEngine"/> implemented by <see cref="MigrationEngine"/> as scoped</description></item>
		/// </list>
		/// </para>
		/// <para>
		/// The scoped lifetime for the migration engine ensures that each migration operation
		/// gets a fresh instance with its own state and dependencies. This is important for
		/// scenarios where multiple migrations might run concurrently or sequentially in different scopes.
		/// </para>
		/// <para>
		/// You must also register:
		/// <list type="bullet">
		/// <item><description>An implementation of <see cref="ITenantProvider"/> to supply tenant lists</description></item>
		/// <item><description>Migration steps using <see cref="AddMigrationStep{TStep}"/> or manual registration</description></item>
		/// </list>
		/// </para>
		/// <para>
		/// Default options include:
		/// <list type="bullet">
		/// <item><description>Batch size: 10 tenants</description></item>
		/// <item><description>Parallel execution: Disabled</description></item>
		/// <item><description>Checkpointing: Enabled with interval of 1 batch</description></item>
		/// <item><description>Continue on failure: Enabled</description></item>
		/// <item><description>Tenant timeout: 5 minutes</description></item>
		/// </list>
		/// </para>
		/// </remarks>
		public static IServiceCollection AddSaasMigration(this IServiceCollection services, Action<MigrationOptions>? configureOptions = null)
		{
			// Validate that services collection is not null
			ArgumentNullException.ThrowIfNull(services);

			// Register migration options with configuration action or defaults
			if (configureOptions != null)
			{
				_ = services.Configure(configureOptions);
			}
			else
			{
				// Register with empty configuration to use all defaults
				_ = services.Configure<MigrationOptions>(options => { });
			}

			// Register migration engine as scoped service
			// TryAddScoped ensures we don't override an existing registration
			services.TryAddScoped<IMigrationEngine, MigrationEngine>();

			return services;
		}

		#endregion
	}
}