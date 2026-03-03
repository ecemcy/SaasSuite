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

namespace SaasSuite.EfCore.Interfaces
{
	/// <summary>
	/// Defines the contract for creating tenant-specific DbContext instances with appropriate configuration.
	/// </summary>
	/// <typeparam name="TContext">The <see cref="DbContext"/> type to create.</typeparam>
	public interface ITenantDbContextFactory<TContext>
		where TContext : DbContext
	{
		#region ' Methods '

		/// <summary>
		/// Creates a DbContext instance configured for the current tenant.
		/// </summary>
		/// <returns>A configured <typeparamref name="TContext"/> instance.</returns>
		/// <exception cref="InvalidOperationException">
		/// Thrown when no tenant context is available.
		/// </exception>
		TContext CreateDbContext();

		/// <summary>
		/// Creates a DbContext instance configured for a specific tenant.
		/// </summary>
		/// <param name="tenantId">The unique identifier of the tenant.</param>
		/// <returns>A configured <typeparamref name="TContext"/> instance.</returns>
		/// <exception cref="InvalidOperationException">
		/// Thrown when the tenant is not found.
		/// </exception>
		TContext CreateDbContext(string tenantId);

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
		/// Thrown when the tenant is not found.
		/// </exception>
		Task<TContext> CreateDbContextAsync(string tenantId, CancellationToken cancellationToken = default);

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
		Task<TContext> CreateDbContextAsync(CancellationToken cancellationToken = default);

		#endregion
	}
}