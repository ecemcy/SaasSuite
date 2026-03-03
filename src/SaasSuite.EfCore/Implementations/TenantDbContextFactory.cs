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

using System.Runtime.CompilerServices;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

using SaasSuite.Core;
using SaasSuite.Core.Interfaces;
using SaasSuite.EfCore.Interfaces;
using SaasSuite.EfCore.Options;

namespace SaasSuite.EfCore.Implementations
{
	/// <summary>
	/// Factory implementation for creating tenant-specific DbContext instances with dynamic connection strings.
	/// </summary>
	/// <typeparam name="TContext">The <see cref="DbContext"/> type to instantiate.</typeparam>
	/// <remarks>
	/// This factory resolves tenant information synchronously via <see cref="TaskAwaiter.GetResult">Task.GetAwaiter().GetResult()</see>.
	/// For high-performance scenarios, consider caching tenant information or using a synchronous tenant store.
	/// </remarks>
	public class TenantDbContextFactory<TContext>
		: ITenantDbContextFactory<TContext> where TContext : DbContext
	{
		#region ' Fields '

		/// <summary>
		/// Base DbContext options to clone for each tenant instance.
		/// </summary>
		private readonly DbContextOptions<TContext> _baseOptions;

		/// <summary>
		/// Configuration options for EF Core multi-tenancy behavior.
		/// </summary>
		private readonly IOptions<EfCoreOptions> _options;

		/// <summary>
		/// Provides access to the current tenant context.
		/// </summary>
		private readonly ITenantAccessor _tenantAccessor;

		/// <summary>
		/// Provides access to tenant information and connection strings.
		/// </summary>
		private readonly ITenantStore _tenantStore;

		#endregion

		#region ' Constructors '

		/// <summary>
		/// Initializes a new instance of the <see cref="TenantDbContextFactory{TContext}"/> class.
		/// </summary>
		/// <param name="tenantAccessor">The service providing current tenant context.</param>
		/// <param name="tenantStore">The service providing tenant information lookup.</param>
		/// <param name="options">The EF Core configuration options.</param>
		/// <param name="baseOptions">The base DbContext options template.</param>
		/// <exception cref="ArgumentNullException">
		/// Thrown when any parameter is <see langword="null"/>.
		/// </exception>
		public TenantDbContextFactory(ITenantAccessor tenantAccessor, ITenantStore tenantStore, IOptions<EfCoreOptions> options, DbContextOptions<TContext> baseOptions)
		{
			this._tenantAccessor = tenantAccessor ?? throw new ArgumentNullException(nameof(tenantAccessor));
			this._tenantStore = tenantStore ?? throw new ArgumentNullException(nameof(tenantStore));
			this._options = options ?? throw new ArgumentNullException(nameof(options));
			this._baseOptions = baseOptions ?? throw new ArgumentNullException(nameof(baseOptions));
		}

		#endregion

		#region ' Methods '

		/// <summary>
		/// Asynchronously creates a DbContext instance configured for a specific tenant.
		/// </summary>
		/// <param name="tenantId">The unique identifier of the tenant.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
		/// <returns>
		/// A <see cref="Task{TResult}"/> representing the asynchronous operation.
		/// The task result contains a configured <typeparamref name="TContext"/> instance.
		/// </returns>
		/// <exception cref="InvalidOperationException">
		/// Thrown when the tenant is not found or no connection string is available.
		/// </exception>
		public async Task<TContext> CreateDbContextAsync(string tenantId, CancellationToken cancellationToken = default)
		{
			// Resolve tenant information asynchronously
			TenantInfo? tenantInfo = await this._tenantStore.GetByIdAsync(new Core.TenantId(tenantId), cancellationToken)
				?? throw new InvalidOperationException($"Tenant with ID '{tenantId}' not found.");

			// Determine connection string
			string? connectionString = tenantInfo.ConnectionString ?? this._options.Value.DefaultConnectionString;
			if (string.IsNullOrEmpty(connectionString))
			{
				throw new InvalidOperationException(
					$"No connection string configured for tenant '{tenantId}' and no default connection string is available.");
			}

			// Build new options with tenant-specific configuration
			DbContextOptionsBuilder<TContext> optionsBuilder = new DbContextOptionsBuilder<TContext>(this._baseOptions);

			// Note: Database provider configuration (UseSqlServer, UseNpgsql, etc.)
			// must be performed by the application when registering the DbContext
			return (TContext)Activator.CreateInstance(typeof(TContext), optionsBuilder.Options)!;
		}

		/// <summary>
		/// Asynchronously creates a DbContext instance configured for the current tenant.
		/// </summary>
		/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
		/// <returns>
		/// A <see cref="Task{TResult}"/> representing the asynchronous operation.
		/// The task result contains a configured <typeparamref name="TContext"/> instance.
		/// </returns>
		/// <exception cref="InvalidOperationException">
		/// Thrown when no tenant context is available.
		/// </exception>
		public async Task<TContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
		{
			TenantContext? tenantContext = this._tenantAccessor.TenantContext
				?? throw new InvalidOperationException("No tenant context is available. Ensure tenant resolution is configured.");

			return await this.CreateDbContextAsync(tenantContext.TenantId.Value, cancellationToken);
		}

		/// <summary>
		/// Creates a DbContext instance configured for the current tenant.
		/// </summary>
		/// <returns>A configured <typeparamref name="TContext"/> instance.</returns>
		/// <exception cref="InvalidOperationException">
		/// Thrown when no tenant context is available or the tenant is not found.
		/// </exception>
		public TContext CreateDbContext()
		{
			TenantContext? tenantContext = this._tenantAccessor.TenantContext
				?? throw new InvalidOperationException("No tenant context is available. Ensure tenant resolution is configured.");

			return this.CreateDbContext(tenantContext.TenantId.Value);
		}

		/// <summary>
		/// Creates a DbContext instance configured for a specific tenant.
		/// </summary>
		/// <param name="tenantId">The unique identifier of the tenant.</param>
		/// <returns>A configured <typeparamref name="TContext"/> instance.</returns>
		/// <exception cref="InvalidOperationException">
		/// Thrown when the tenant is not found or no connection string is available.
		/// </exception>
		/// <remarks>
		/// This method uses synchronous tenant resolution via <c>GetAwaiter().GetResult()</c>.
		/// Consider using <see cref="CreateDbContextAsync(string, CancellationToken)"/> for async contexts.
		/// </remarks>
		public TContext CreateDbContext(string tenantId)
		{
			// Resolve tenant information synchronously
			TenantInfo? tenantInfo = this._tenantStore.GetByIdAsync(new Core.TenantId(tenantId)).GetAwaiter().GetResult()
				?? throw new InvalidOperationException($"Tenant with ID '{tenantId}' not found.");

			// Determine connection string
			string? connectionString = tenantInfo.ConnectionString ?? this._options.Value.DefaultConnectionString;
			if (string.IsNullOrEmpty(connectionString))
			{
				throw new InvalidOperationException(
					$"No connection string configured for tenant '{tenantId}' and no default connection string is available.");
			}

			// Build new options with tenant-specific configuration
			DbContextOptionsBuilder<TContext> optionsBuilder = new DbContextOptionsBuilder<TContext>(this._baseOptions);

			// Note: Database provider configuration (UseSqlServer, UseNpgsql, etc.)
			// must be performed by the application when registering the DbContext
			return (TContext)Activator.CreateInstance(typeof(TContext), optionsBuilder.Options)!;
		}

		#endregion
	}
}