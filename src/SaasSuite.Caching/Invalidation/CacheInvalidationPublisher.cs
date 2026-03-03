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

using Microsoft.Extensions.DependencyInjection;

using SaasSuite.Caching.Interfaces;

namespace SaasSuite.Caching.Invalidation
{
	/// <summary>
	/// Provides the default implementation of <see cref="ICacheInvalidationPublisher"/> that performs
	/// logical cache invalidation by incrementing namespace versions.
	/// </summary>
	/// <remarks>
	/// This publisher implements a version-based cache invalidation strategy where publishing an invalidation
	/// event increments the namespace version counter in <see cref="INamespaceVersionStore"/>. All cache entries
	/// stored with the previous version become unreachable without requiring expensive delete operations.
	/// The implementation is sealed to prevent inheritance and ensure predictable invalidation behavior.
	/// This class is thread-safe and can be registered as a singleton in the dependency injection container.
	/// </remarks>
	public sealed class CacheInvalidationPublisher
		: ICacheInvalidationPublisher
	{
		#region ' Fields '

		/// <summary>
		/// The version store used to track and increment namespace versions.
		/// </summary>
		/// <remarks>
		/// This store maintains version counters for tenant and area combinations.
		/// Injected via dependency injection during service registration.
		/// </remarks>
		private readonly INamespaceVersionStore _versionStore;

		#endregion

		#region ' Constructors '

		/// <summary>
		/// Initializes a new instance of the <see cref="CacheInvalidationPublisher"/> class.
		/// </summary>
		/// <param name="versionStore">
		/// The <see cref="INamespaceVersionStore"/> used to manage namespace versions. Cannot be <see langword="null"/>.
		/// </param>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="versionStore"/> is <see langword="null"/>.
		/// </exception>
		/// <remarks>
		/// This constructor is invoked by the dependency injection container when resolving
		/// <see cref="ICacheInvalidationPublisher"/>. The version store is typically registered
		/// in service configuration via <see cref="ServiceCollectionExtensions.AddSaasCaching(IServiceCollection)"/>.
		/// </remarks>
		public CacheInvalidationPublisher(INamespaceVersionStore versionStore)
		{
			// Validate that the version store dependency is provided
			this._versionStore = versionStore ?? throw new ArgumentNullException(nameof(versionStore));
		}

		#endregion

		#region ' Methods '

		/// <summary>
		/// Asynchronously publishes a cache invalidation event by incrementing the namespace version.
		/// </summary>
		/// <param name="cacheEvent">
		/// The invalidation event containing tenant and area information. Cannot be <see langword="null"/>.
		/// </param>
		/// <param name="cancellationToken">
		/// A token to monitor for cancellation requests during the asynchronous operation.
		/// </param>
		/// <returns>
		/// A task that represents the asynchronous publish operation.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="cacheEvent"/> is <see langword="null"/>.
		/// </exception>
		/// <remarks>
		/// This method increments the namespace version for the tenant and optional area specified
		/// in <paramref name="cacheEvent"/>. All cache entries stored with the previous version
		/// become unreachable and will result in cache misses. The operation is synchronous despite
		/// returning a Task to maintain interface compatibility for future distributed implementations.
		/// </remarks>
		public Task PublishAsync(ICacheInvalidationEvent cacheEvent, CancellationToken cancellationToken = default)
		{
			// Validate that the cache event is provided
			ArgumentNullException.ThrowIfNull(cacheEvent);

			// Increment the namespace version to logically invalidate all entries with the previous version
			_ = this._versionStore.IncrementVersion(cacheEvent.TenantId, cacheEvent.Area);

			// Return completed task for async interface compatibility
			return Task.CompletedTask;
		}

		#endregion
	}
}