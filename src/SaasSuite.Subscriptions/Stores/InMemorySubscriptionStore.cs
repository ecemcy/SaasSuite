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

using System.Collections.Concurrent;

using SaasSuite.Core;
using SaasSuite.Subscriptions.Enumerations;
using SaasSuite.Subscriptions.Interfaces;
using SaasSuite.Subscriptions.Options;

namespace SaasSuite.Subscriptions.Stores
{
	/// <summary>
	/// Provides a thread-safe, in-memory implementation of <see cref="ISubscriptionStore"/> for development and testing.
	/// </summary>
	/// <remarks>
	/// This implementation uses <see cref="ConcurrentDictionary{TKey, TValue}"/> for thread-safe storage without locking.
	/// All data is stored in memory and will be lost when the application restarts. Suitable for:
	/// <list type="bullet">
	/// <item><description>Development and testing environments</description></item>
	/// <item><description>Unit tests requiring subscription functionality</description></item>
	/// <item><description>Demos and proof-of-concepts</description></item>
	/// <item><description>Single-instance deployments where persistence is not required</description></item>
	/// </list>
	/// For production scenarios requiring durable storage across restarts or multiple instances, implement
	/// a custom <see cref="ISubscriptionStore"/> backed by SQL, NoSQL, or other persistent storage.
	/// </remarks>
	public class InMemorySubscriptionStore
		: ISubscriptionStore
	{
		#region ' Fields '

		/// <summary>
		/// Thread-safe dictionary storing subscriptions keyed by subscription ID.
		/// </summary>
		private readonly ConcurrentDictionary<string, Subscription> _subscriptions = new ConcurrentDictionary<string, Subscription>();

		/// <summary>
		/// Thread-safe dictionary storing subscription plans keyed by plan ID.
		/// </summary>
		private readonly ConcurrentDictionary<string, SubscriptionPlan> _plans = new ConcurrentDictionary<string, SubscriptionPlan>();

		#endregion

		#region ' Plan Operations '

		/// <summary>
		/// Creates and persists a new subscription plan in memory.
		/// </summary>
		/// <param name="plan">The subscription plan to create. Cannot be <see langword="null"/>.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests. Not used in this in-memory implementation but included for interface compliance.</param>
		/// <returns>
		/// A completed task representing the synchronous operation.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="plan"/> is <see langword="null"/>.
		/// </exception>
		/// <exception cref="InvalidOperationException">
		/// Thrown when a plan with the same <see cref="SubscriptionPlan.Id"/> already exists.
		/// </exception>
		/// <remarks>
		/// This method attempts to add the plan to the concurrent dictionary atomically.
		/// If a plan with the same ID already exists, <see cref="ConcurrentDictionary{TKey,TValue}.TryAdd"/>
		/// returns <see langword="false"/> and an exception is thrown to signal the duplicate.
		/// The operation completes synchronously but returns a Task for interface compatibility.
		/// </remarks>
		public Task CreatePlanAsync(SubscriptionPlan plan, CancellationToken cancellationToken = default)
		{
			// Validate the plan parameter is not null
			ArgumentNullException.ThrowIfNull(plan);

			// Attempt to add the plan; throw if duplicate ID exists
			if (!this._plans.TryAdd(plan.Id, plan))
			{
				throw new InvalidOperationException($"Plan with ID '{plan.Id}' already exists.");
			}

			return Task.CompletedTask;
		}

		/// <summary>
		/// Deletes a subscription plan from memory.
		/// </summary>
		/// <param name="planId">The unique identifier of the plan to delete.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests. Not used in this in-memory implementation but included for interface compliance.</param>
		/// <returns>
		/// A completed task representing the synchronous operation.
		/// </returns>
		/// <remarks>
		/// Uses <see cref="ConcurrentDictionary{TKey,TValue}.TryRemove(TKey, out TValue)"/> which is idempotent (safe to call
		/// even if the plan doesn't exist). No exception is thrown if the plan is not found. The out parameter
		/// is discarded as we don't need the removed value. Consider checking for active subscriptions
		/// referencing this plan before deletion in a production implementation.
		/// </remarks>
		public Task DeletePlanAsync(string planId, CancellationToken cancellationToken = default)
		{
			// Remove the plan from the dictionary (idempotent operation)
			_ = this._plans.TryRemove(planId, out _);
			return Task.CompletedTask;
		}

		/// <summary>
		/// Updates an existing subscription plan in memory.
		/// </summary>
		/// <param name="plan">The subscription plan with updated values. Cannot be <see langword="null"/>.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests. Not used in this in-memory implementation but included for interface compliance.</param>
		/// <returns>
		/// A completed task representing the synchronous operation.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="plan"/> is <see langword="null"/>.
		/// </exception>
		/// <exception cref="InvalidOperationException">
		/// Thrown when no plan exists with the specified <see cref="SubscriptionPlan.Id"/>.
		/// </exception>
		/// <remarks>
		/// This method first checks if the plan exists using <see cref="ConcurrentDictionary{TKey,TValue}.ContainsKey"/>,
		/// then replaces the existing plan using the indexer. The check-then-update is not atomic, but race conditions
		/// are unlikely in typical usage. For strict atomicity, consider using <see cref="ConcurrentDictionary{TKey,TValue}.TryUpdate"/>.
		/// </remarks>
		public Task UpdatePlanAsync(SubscriptionPlan plan, CancellationToken cancellationToken = default)
		{
			// Validate the plan parameter is not null
			ArgumentNullException.ThrowIfNull(plan);

			// Verify the plan exists before updating
			if (!this._plans.ContainsKey(plan.Id))
			{
				throw new InvalidOperationException($"Plan with ID '{plan.Id}' does not exist.");
			}

			// Replace the existing plan with the updated one
			this._plans[plan.Id] = plan;
			return Task.CompletedTask;
		}

		/// <summary>
		/// Retrieves all subscription plans with optional filtering by active status.
		/// </summary>
		/// <param name="includeInactive">
		/// <see langword="true"/> to include inactive plans; <see langword="false"/> to return only active plans.
		/// Defaults to <see langword="false"/>.
		/// </param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests. Not used in this in-memory implementation but included for interface compliance.</param>
		/// <returns>
		/// A completed task with a collection of <see cref="SubscriptionPlan"/> instances matching the filter.
		/// </returns>
		/// <remarks>
		/// Starts with all plan values from the concurrent dictionary and applies filtering based on
		/// <see cref="SubscriptionPlan.IsActive"/> if <paramref name="includeInactive"/> is <see langword="false"/>.
		/// The operation iterates the dictionary, which is thread-safe but provides a snapshot that may not
		/// reflect concurrent modifications. Results are not ordered; consider sorting at the service layer if needed.
		/// </remarks>
		public Task<IEnumerable<SubscriptionPlan>> GetPlansAsync(bool includeInactive = false, CancellationToken cancellationToken = default)
		{
			// Start with all plans from the dictionary
			IEnumerable<SubscriptionPlan> plans = this._plans.Values.AsEnumerable();

			// Filter out inactive plans if requested
			if (!includeInactive)
			{
				plans = plans.Where(p => p.IsActive);
			}

			return Task.FromResult(plans);
		}

		/// <summary>
		/// Retrieves a subscription plan by its unique identifier from memory.
		/// </summary>
		/// <param name="planId">The unique identifier of the plan to retrieve.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests. Not used in this in-memory implementation but included for interface compliance.</param>
		/// <returns>
		/// A completed task with the <see cref="SubscriptionPlan"/> if found, or <see langword="null"/> if not found.
		/// </returns>
		/// <remarks>
		/// Uses <see cref="ConcurrentDictionary{TKey,TValue}.TryGetValue"/> for thread-safe lookup.
		/// Returns <see langword="null"/> rather than throwing an exception when the plan doesn't exist,
		/// allowing graceful handling by callers. The operation completes synchronously.
		/// </remarks>
		public Task<SubscriptionPlan?> GetPlanAsync(string planId, CancellationToken cancellationToken = default)
		{
			// Attempt to retrieve the plan from the dictionary
			_ = this._plans.TryGetValue(planId, out SubscriptionPlan? plan);
			return Task.FromResult(plan);
		}

		#endregion

		#region ' Subscription Operations '

		/// <summary>
		/// Creates and persists a new subscription in memory.
		/// </summary>
		/// <param name="subscription">The subscription to create. Cannot be <see langword="null"/>.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests. Not used in this in-memory implementation but included for interface compliance.</param>
		/// <returns>
		/// A completed task representing the synchronous operation.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="subscription"/> is <see langword="null"/>.
		/// </exception>
		/// <exception cref="InvalidOperationException">
		/// Thrown when a subscription with the same <see cref="Subscription.Id"/> already exists.
		/// </exception>
		/// <remarks>
		/// Atomically adds the subscription using <see cref="ConcurrentDictionary{TKey,TValue}.TryAdd"/>.
		/// Throws an exception if a duplicate ID is detected to maintain data integrity.
		/// The operation completes synchronously but returns a Task for interface compatibility.
		/// </remarks>
		public Task CreateSubscriptionAsync(Subscription subscription, CancellationToken cancellationToken = default)
		{
			// Validate the subscription parameter is not null
			ArgumentNullException.ThrowIfNull(subscription);

			// Attempt to add the subscription; throw if duplicate ID exists
			if (!this._subscriptions.TryAdd(subscription.Id, subscription))
			{
				throw new InvalidOperationException($"Subscription with ID '{subscription.Id}' already exists.");
			}

			return Task.CompletedTask;
		}

		/// <summary>
		/// Deletes a subscription from memory.
		/// </summary>
		/// <param name="subscriptionId">The unique identifier of the subscription to delete.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests. Not used in this in-memory implementation but included for interface compliance.</param>
		/// <returns>
		/// A completed task representing the synchronous operation.
		/// </returns>
		/// <remarks>
		/// Performs an idempotent removal using <see cref="ConcurrentDictionary{TKey,TValue}.TryRemove(TKey, out TValue)"/>.
		/// No exception is thrown if the subscription doesn't exist, making this safe to call multiple times.
		/// The discarded out parameter (_) indicates we don't need the removed value. In production systems,
		/// consider soft deletion or archiving instead of hard deletion to maintain audit trails.
		/// </remarks>
		public Task DeleteSubscriptionAsync(string subscriptionId, CancellationToken cancellationToken = default)
		{
			// Remove the subscription from the dictionary (idempotent operation)
			_ = this._subscriptions.TryRemove(subscriptionId, out _);
			return Task.CompletedTask;
		}

		/// <summary>
		/// Updates an existing subscription in memory.
		/// </summary>
		/// <param name="subscription">The subscription with updated values. Cannot be <see langword="null"/>.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests. Not used in this in-memory implementation but included for interface compliance.</param>
		/// <returns>
		/// A completed task representing the synchronous operation.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="subscription"/> is <see langword="null"/>.
		/// </exception>
		/// <exception cref="InvalidOperationException">
		/// Thrown when no subscription exists with the specified <see cref="Subscription.Id"/>.
		/// </exception>
		/// <remarks>
		/// This method verifies the subscription exists before updating, then sets <see cref="Subscription.UpdatedAt"/>
		/// to the current UTC time to track when the modification occurred. Finally, it replaces the existing
		/// subscription using the dictionary indexer. The check-then-update is not atomic but sufficient for
		/// in-memory scenarios. The automatic timestamp update helps track subscription change history.
		/// </remarks>
		public Task UpdateSubscriptionAsync(Subscription subscription, CancellationToken cancellationToken = default)
		{
			// Validate the subscription parameter is not null
			ArgumentNullException.ThrowIfNull(subscription);

			// Verify the subscription exists before updating
			if (!this._subscriptions.ContainsKey(subscription.Id))
			{
				throw new InvalidOperationException($"Subscription with ID '{subscription.Id}' does not exist.");
			}

			// Automatically update the modification timestamp
			subscription.UpdatedAt = DateTimeOffset.UtcNow;

			// Replace the existing subscription with the updated one
			this._subscriptions[subscription.Id] = subscription;
			return Task.CompletedTask;
		}

		/// <summary>
		/// Retrieves all subscriptions for a specific tenant, ordered by creation date descending.
		/// </summary>
		/// <param name="tenantId">The tenant identifier whose subscriptions to retrieve.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests. Not used in this in-memory implementation but included for interface compliance.</param>
		/// <returns>
		/// A completed task with a collection of <see cref="Subscription"/> instances for the tenant.
		/// </returns>
		/// <remarks>
		/// Filters all subscriptions by matching <see cref="Subscription.TenantId"/> with the provided tenant ID.
		/// Results are sorted by <see cref="Subscription.CreatedAt"/> in descending order so the most recent
		/// subscription appears first. This ordering is consistent with the interface contract.
		/// The LINQ query iterates the entire dictionary, which is acceptable for in-memory scenarios but
		/// consider indexing by tenant ID in database implementations for better performance.
		/// </remarks>
		public Task<IEnumerable<Subscription>> GetSubscriptionsByTenantAsync(TenantId tenantId, CancellationToken cancellationToken = default)
		{
			// Filter subscriptions by tenant ID and order by creation date descending
			IEnumerable<Subscription> subscriptions = this._subscriptions.Values
				.Where(s => s.TenantId == tenantId)
				.OrderByDescending(s => s.CreatedAt)
				.AsEnumerable();

			return Task.FromResult(subscriptions);
		}

		/// <summary>
		/// Retrieves the currently active subscription for a specific tenant.
		/// </summary>
		/// <param name="tenantId">The tenant identifier whose active subscription to retrieve.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests. Not used in this in-memory implementation but included for interface compliance.</param>
		/// <returns>
		/// A completed task with the active <see cref="Subscription"/> if found, or <see langword="null"/> if none exists.
		/// </returns>
		/// <remarks>
		/// Filters subscriptions to find those with status <see cref="SubscriptionStatus.Active"/> or
		/// <see cref="SubscriptionStatus.Trial"/> that belong to the specified tenant. If multiple active
		/// subscriptions exist (rare unless <see cref="SubscriptionOptions.AllowMultipleSubscriptions"/> is enabled),
		/// the most recently created one is returned via <see cref="Enumerable.FirstOrDefault{TSource}(IEnumerable{TSource})"/>.
		/// Returns <see langword="null"/> if no active or trial subscription exists for the tenant.
		/// </remarks>
		public Task<Subscription?> GetActiveSubscriptionAsync(TenantId tenantId, CancellationToken cancellationToken = default)
		{
			// Find active or trial subscriptions for the tenant, preferring the most recent
			Subscription? subscription = this._subscriptions.Values
				.Where(s => s.TenantId == tenantId &&
						   (s.Status == SubscriptionStatus.Active || s.Status == SubscriptionStatus.Trial))
				.OrderByDescending(s => s.CreatedAt)
				.FirstOrDefault();

			return Task.FromResult(subscription);
		}

		/// <summary>
		/// Retrieves a subscription by its unique identifier from memory.
		/// </summary>
		/// <param name="subscriptionId">The unique identifier of the subscription to retrieve.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests. Not used in this in-memory implementation but included for interface compliance.</param>
		/// <returns>
		/// A completed task with the <see cref="Subscription"/> if found, or <see langword="null"/> if not found.
		/// </returns>
		/// <remarks>
		/// Performs a thread-safe lookup using <see cref="ConcurrentDictionary{TKey,TValue}.TryGetValue"/>.
		/// Returns <see langword="null"/> when not found rather than throwing, enabling graceful error handling.
		/// The operation completes synchronously.
		/// </remarks>
		public Task<Subscription?> GetSubscriptionAsync(string subscriptionId, CancellationToken cancellationToken = default)
		{
			// Attempt to retrieve the subscription from the dictionary
			_ = this._subscriptions.TryGetValue(subscriptionId, out Subscription? subscription);
			return Task.FromResult(subscription);
		}

		#endregion
	}
}