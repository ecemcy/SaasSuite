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

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

using SaasSuite.Core.Interfaces;
using SaasSuite.EfCore.Implementations;
using SaasSuite.EfCore.Interceptors;
using SaasSuite.EfCore.Interfaces;
using SaasSuite.EfCore.Options;

namespace Microsoft.Extensions.DependencyInjection
{
	/// <summary>
	/// Provides extension methods for configuring multi-tenant Entity Framework Core services.
	/// </summary>
	public static class ServiceCollectionExtensions
	{
		#region ' Static Methods '

		/// <summary>
		/// Registers a tenant-scoped DbContext with both EF Core and DbContext configuration options.
		/// </summary>
		/// <typeparam name="TContext">The <see cref="DbContext"/> type to register.</typeparam>
		/// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
		/// <param name="efCoreOptionsAction">An action to configure <see cref="EfCoreOptions"/> for multi-tenancy behavior.</param>
		/// <param name="dbContextOptionsAction">
		/// An action to configure <see cref="DbContextOptionsBuilder"/> for database provider setup
		/// (e.g., UseSqlServer, UseNpgsql, UseSqlite).
		/// </param>
		/// <returns>The <see cref="IServiceCollection"/> for method chaining.</returns>
		/// <remarks>
		/// This overload provides complete control over both multi-tenancy features and database provider configuration.
		/// Use this when you need to customize both aspects of the DbContext.
		/// </remarks>
		public static IServiceCollection AddSaasTenantDbContext<TContext>(this IServiceCollection services, Action<EfCoreOptions> efCoreOptionsAction, Action<IServiceProvider, DbContextOptionsBuilder> dbContextOptionsAction)
			where TContext : DbContext
		{
			_ = services.Configure(efCoreOptionsAction);
			return services.AddSaasTenantDbContext<TContext>(dbContextOptionsAction);
		}

		/// <summary>
		/// Registers a tenant-scoped DbContext with EF Core configuration options.
		/// </summary>
		/// <typeparam name="TContext">The <see cref="DbContext"/> type to register.</typeparam>
		/// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
		/// <param name="optionsAction">An action to configure <see cref="EfCoreOptions"/> such as query filters and caching behavior.</param>
		/// <returns>The <see cref="IServiceCollection"/> for method chaining.</returns>
		/// <remarks>
		/// This overload applies custom EF Core multi-tenancy options before registering the DbContext.
		/// Use this when you need to customize tenant isolation behavior.
		/// </remarks>
		public static IServiceCollection AddSaasTenantDbContext<TContext>(this IServiceCollection services, Action<EfCoreOptions> optionsAction)
			where TContext : DbContext
		{
			_ = services.Configure(optionsAction);
			return services.AddSaasTenantDbContext<TContext>();
		}

		/// <summary>
		/// Registers a tenant-scoped DbContext with automatic tenant isolation and multi-tenancy features.
		/// </summary>
		/// <typeparam name="TContext">The <see cref="DbContext"/> type to register.</typeparam>
		/// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
		/// <param name="optionsAction">
		/// An optional action to configure DbContext options such as database provider and connection string.
		/// </param>
		/// <param name="contextLifetime">
		/// The service lifetime for the DbContext. Defaults to <see cref="ServiceLifetime.Scoped"/>.
		/// </param>
		/// <param name="optionsLifetime">
		/// The service lifetime for DbContextOptions. Defaults to <see cref="ServiceLifetime.Scoped"/>.
		/// </param>
		/// <returns>The <see cref="IServiceCollection"/> for method chaining.</returns>
		/// <remarks>
		/// This method automatically configures:
		/// <list type="bullet">
		/// <item><description>Tenant ID auto-assignment via <see cref="TenantSaveChangesInterceptor"/></description></item>
		/// <item><description>Per-tenant model caching via <see cref="TenantModelCacheKeyFactory"/></description></item>
		/// <item><description>Tenant-scoped DbContext factory via <see cref="ITenantDbContextFactory{TContext}"/></description></item>
		/// </list>
		/// </remarks>
		public static IServiceCollection AddSaasTenantDbContext<TContext>(this IServiceCollection services, Action<IServiceProvider, DbContextOptionsBuilder>? optionsAction = null, ServiceLifetime contextLifetime = ServiceLifetime.Scoped, ServiceLifetime optionsLifetime = ServiceLifetime.Scoped)
			where TContext : DbContext
		{
			// Register EF Core configuration options
			_ = services.AddOptions<EfCoreOptions>();

			// Register tenant save changes interceptor
			services.TryAddScoped<TenantSaveChangesInterceptor>();

			// Register model cache key factory for per-tenant caching
			services.TryAddSingleton<IModelCacheKeyFactory, TenantModelCacheKeyFactory>();

			// Register tenant DbContext factory
			services.TryAddScoped<ITenantDbContextFactory<TContext>, TenantDbContextFactory<TContext>>();

			// Register the DbContext with tenant-aware configuration
			_ = services.AddDbContext<TContext>(
				(serviceProvider, options) =>
				{
					IOptions<EfCoreOptions>? efCoreOptions = serviceProvider.GetService<Microsoft.Extensions.Options.IOptions<EfCoreOptions>>();
					ITenantAccessor tenantAccessor = serviceProvider.GetRequiredService<ITenantAccessor>();

					// Apply user-provided options first
					optionsAction?.Invoke(serviceProvider, options);

					// Add tenant save changes interceptor if enabled
					if (efCoreOptions?.Value.AutoSetTenantId ?? true)
					{
						TenantSaveChangesInterceptor interceptor = serviceProvider.GetRequiredService<TenantSaveChangesInterceptor>();
						_ = options.AddInterceptors(interceptor);
					}

					// Configure per-tenant model caching if enabled
					if (efCoreOptions?.Value.UsePerTenantModelCache ?? true)
					{
						_ = options.ReplaceService<IModelCacheKeyFactory, TenantModelCacheKeyFactory>();
					}
				},
				contextLifetime,
				optionsLifetime);

			return services;
		}

		#endregion
	}
}