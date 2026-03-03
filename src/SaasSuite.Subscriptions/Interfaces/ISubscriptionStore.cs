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
using SaasSuite.Subscriptions.Enumerations;
using SaasSuite.Subscriptions.Options;

namespace SaasSuite.Subscriptions.Interfaces
{
	/// <summary>
	/// Defines the contract for persisting and retrieving subscription plans and subscription instances.
	/// </summary>
	/// <remarks>
	/// This interface abstracts the data access layer for subscription management, enabling different
	/// storage implementations (in-memory, SQL, NoSQL, etc.) without changing business logic.
	/// Implementations should ensure thread-safety and data consistency, especially for operations
	/// that check-and-update subscription states. The interface is divided into plan operations
	/// (CRUD for subscription plan templates) and subscription operations (CRUD for tenant subscription instances).
	/// </remarks>
	public interface ISubscriptionStore
	{
		#region ' Plan Operations '

		/// <summary>
		/// Creates and persists a new subscription plan.
		/// </summary>
		/// <param name="plan">The subscription plan to create. Cannot be <see langword="null"/>.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests during the asynchronous operation.</param>
		/// <returns>
		/// A task that represents the asynchronous operation.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="plan"/> is <see langword="null"/>.
		/// </exception>
		/// <exception cref="InvalidOperationException">
		/// Thrown when a plan with the same <see cref="SubscriptionPlan.Id"/> already exists.
		/// </exception>
		/// <remarks>
		/// This method adds a new plan to the catalog. The plan must have a unique ID. Duplicate IDs should
		/// result in an exception to maintain data integrity. After creation, the plan is immediately available
		/// for subscription creation if <see cref="SubscriptionPlan.IsActive"/> is <see langword="true"/>.
		/// No validation is performed on pricing or features; validation should occur in the service layer
		/// before calling this method.
		/// </remarks>
		Task CreatePlanAsync(SubscriptionPlan plan, CancellationToken cancellationToken = default);

		/// <summary>
		/// Deletes a subscription plan from the catalog.
		/// </summary>
		/// <param name="planId">The unique identifier of the plan to delete. Cannot be <see langword="null"/> or empty.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests during the asynchronous operation.</param>
		/// <returns>
		/// A task that represents the asynchronous operation.
		/// </returns>
		/// <exception cref="ArgumentException">
		/// Thrown when <paramref name="planId"/> is <see langword="null"/> or empty.
		/// </exception>
		/// <remarks>
		/// Deletes the plan record from storage. This operation should be used with caution, especially if
		/// subscriptions reference this plan. Best practice is to set <see cref="SubscriptionPlan.IsActive"/>
		/// to <see langword="false"/> instead of deleting plans with active subscriptions. Some implementations
		/// may enforce referential integrity and prevent deletion of plans with dependent subscriptions.
		/// If the plan doesn't exist, the operation completes silently without error (idempotent).
		/// </remarks>
		Task DeletePlanAsync(string planId, CancellationToken cancellationToken = default);

		/// <summary>
		/// Updates an existing subscription plan with new values.
		/// </summary>
		/// <param name="plan">The subscription plan with updated values. Cannot be <see langword="null"/>.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests during the asynchronous operation.</param>
		/// <returns>
		/// A task that represents the asynchronous operation.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="plan"/> is <see langword="null"/>.
		/// </exception>
		/// <exception cref="InvalidOperationException">
		/// Thrown when no plan exists with the specified <see cref="SubscriptionPlan.Id"/>.
		/// </exception>
		/// <remarks>
		/// This method replaces the existing plan data with the provided plan object. The plan ID must match
		/// an existing plan. Changes to plans don't automatically affect existing subscriptions; consider
		/// implementing plan versioning or migration strategies for price or feature changes. Common update
		/// scenarios include changing <see cref="SubscriptionPlan.IsActive"/> to deprecate plans, updating
		/// descriptions or metadata, or adjusting limits for new subscriptions.
		/// </remarks>
		Task UpdatePlanAsync(SubscriptionPlan plan, CancellationToken cancellationToken = default);

		/// <summary>
		/// Retrieves all subscription plans in the catalog with optional filtering.
		/// </summary>
		/// <param name="includeInactive">
		/// <see langword="true"/> to include inactive plans in the results;
		/// <see langword="false"/> to return only active plans. Defaults to <see langword="false"/>.
		/// </param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests during the asynchronous operation.</param>
		/// <returns>
		/// A task that represents the asynchronous operation. The task result contains a collection of
		/// <see cref="SubscriptionPlan"/> instances matching the filter criteria. Returns an empty collection if no plans exist.
		/// </returns>
		/// <remarks>
		/// This method returns all plans, optionally filtered by <see cref="SubscriptionPlan.IsActive"/> status.
		/// When <paramref name="includeInactive"/> is <see langword="false"/> (default), only plans available
		/// for new subscriptions are returned, suitable for plan selection interfaces. When <see langword="true"/>,
		/// all plans including deprecated ones are returned, useful for administrative views and reporting.
		/// The order of results is implementation-specific; consider sorting by price, name, or custom display order
		/// at the service layer if needed.
		/// </remarks>
		Task<IEnumerable<SubscriptionPlan>> GetPlansAsync(bool includeInactive = false, CancellationToken cancellationToken = default);

		/// <summary>
		/// Retrieves a subscription plan by its unique identifier.
		/// </summary>
		/// <param name="planId">The unique identifier of the plan to retrieve. Cannot be <see langword="null"/> or empty.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests during the asynchronous operation.</param>
		/// <returns>
		/// A task that represents the asynchronous operation. The task result contains the <see cref="SubscriptionPlan"/>
		/// if found, or <see langword="null"/> if no plan exists with the specified ID.
		/// </returns>
		/// <exception cref="ArgumentException">
		/// Thrown when <paramref name="planId"/> is <see langword="null"/> or empty.
		/// </exception>
		/// <remarks>
		/// This method performs a lookup by plan ID and returns <see langword="null"/> if not found rather than
		/// throwing an exception, allowing callers to handle missing plans gracefully. Used by subscription
		/// creation and entitlement calculation to retrieve plan details. Implementations may cache frequently
		/// accessed plans for performance optimization.
		/// </remarks>
		Task<SubscriptionPlan?> GetPlanAsync(string planId, CancellationToken cancellationToken = default);

		#endregion

		#region ' Subscription Operations '

		/// <summary>
		/// Creates and persists a new subscription for a tenant.
		/// </summary>
		/// <param name="subscription">The subscription to create. Cannot be <see langword="null"/>.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests during the asynchronous operation.</param>
		/// <returns>
		/// A task that represents the asynchronous operation.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="subscription"/> is <see langword="null"/>.
		/// </exception>
		/// <exception cref="InvalidOperationException">
		/// Thrown when a subscription with the same <see cref="Subscription.Id"/> already exists.
		/// </exception>
		/// <remarks>
		/// This method persists a new subscription instance. The subscription must have a unique ID.
		/// Duplicate IDs result in an exception. Implementations should validate that the referenced
		/// <see cref="Subscription.PlanId"/> exists, though this is typically enforced at the service layer.
		/// The subscription immediately becomes queryable after creation. Set <see cref="Subscription.CreatedAt"/>
		/// and <see cref="Subscription.UpdatedAt"/> to the current UTC time if not already set.
		/// </remarks>
		Task CreateSubscriptionAsync(Subscription subscription, CancellationToken cancellationToken = default);

		/// <summary>
		/// Deletes a subscription record from storage.
		/// </summary>
		/// <param name="subscriptionId">The unique identifier of the subscription to delete. Cannot be <see langword="null"/> or empty.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests during the asynchronous operation.</param>
		/// <returns>
		/// A task that represents the asynchronous operation.
		/// </returns>
		/// <exception cref="ArgumentException">
		/// Thrown when <paramref name="subscriptionId"/> is <see langword="null"/> or empty.
		/// </exception>
		/// <remarks>
		/// Permanently removes the subscription record. This operation should be used cautiously as it destroys
		/// historical data. Best practice is to cancel subscriptions (<see cref="SubscriptionStatus.Cancelled"/>)
		/// rather than deleting them to maintain audit trails and billing history. Deletion might be appropriate for:
		/// <list type="bullet">
		/// <item><description>Test data cleanup</description></item>
		/// <item><description>GDPR/data retention compliance after retention periods</description></item>
		/// <item><description>Removing erroneous records</description></item>
		/// </list>
		/// If the subscription doesn't exist, the operation completes silently without error (idempotent).
		/// Some implementations may soft-delete instead of hard-delete for data recovery purposes.
		/// </remarks>
		Task DeleteSubscriptionAsync(string subscriptionId, CancellationToken cancellationToken = default);

		/// <summary>
		/// Updates an existing subscription with new values.
		/// </summary>
		/// <param name="subscription">The subscription with updated values. Cannot be <see langword="null"/>.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests during the asynchronous operation.</param>
		/// <returns>
		/// A task that represents the asynchronous operation.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="subscription"/> is <see langword="null"/>.
		/// </exception>
		/// <exception cref="InvalidOperationException">
		/// Thrown when no subscription exists with the specified <see cref="Subscription.Id"/>.
		/// </exception>
		/// <remarks>
		/// This method replaces the existing subscription data with the provided subscription object.
		/// The subscription ID must match an existing record. Common update scenarios include:
		/// <list type="bullet">
		/// <item><description>Status changes (trial to active, active to cancelled, etc.)</description></item>
		/// <item><description>Plan changes (upgrades/downgrades)</description></item>
		/// <item><description>Billing date updates after renewals</description></item>
		/// <item><description>Metadata updates for external provider synchronization</description></item>
		/// </list>
		/// Implementations should set <see cref="Subscription.UpdatedAt"/> to the current UTC time automatically.
		/// Ensure updates are atomic to avoid race conditions when multiple processes update subscription state concurrently.
		/// </remarks>
		Task UpdateSubscriptionAsync(Subscription subscription, CancellationToken cancellationToken = default);

		/// <summary>
		/// Retrieves all subscriptions associated with a specific tenant, including historical subscriptions.
		/// </summary>
		/// <param name="tenantId">The tenant identifier whose subscriptions to retrieve. Cannot be <see langword="null"/>.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests during the asynchronous operation.</param>
		/// <returns>
		/// A task that represents the asynchronous operation. The task result contains a collection of
		/// <see cref="Subscription"/> instances for the tenant, ordered by creation date descending (most recent first).
		/// Returns an empty collection if the tenant has no subscriptions.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="tenantId"/> is <see langword="null"/>.
		/// </exception>
		/// <remarks>
		/// This method returns all subscription records for a tenant regardless of status, providing a complete
		/// subscription history. Results are ordered by <see cref="Subscription.CreatedAt"/> in descending order
		/// so the most recent subscription appears first. Useful for subscription history displays, billing
		/// reports, and understanding tenant lifecycle. Filter by <see cref="Subscription.Status"/> at the
		/// service layer if only specific statuses are needed.
		/// </remarks>
		Task<IEnumerable<Subscription>> GetSubscriptionsByTenantAsync(TenantId tenantId, CancellationToken cancellationToken = default);

		/// <summary>
		/// Retrieves the currently active subscription for a specific tenant.
		/// </summary>
		/// <param name="tenantId">The tenant identifier whose active subscription to retrieve. Cannot be <see langword="null"/>.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests during the asynchronous operation.</param>
		/// <returns>
		/// A task that represents the asynchronous operation. The task result contains the active <see cref="Subscription"/>
		/// if one exists, or <see langword="null"/> if the tenant has no active or trial subscription.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="tenantId"/> is <see langword="null"/>.
		/// </exception>
		/// <remarks>
		/// This method returns subscriptions with status <see cref="SubscriptionStatus.Active"/> or
		/// <see cref="SubscriptionStatus.Trial"/>, representing subscriptions that currently grant access.
		/// If multiple active subscriptions exist (when <see cref="SubscriptionOptions.AllowMultipleSubscriptions"/>
		/// is <see langword="true"/>), the most recently created one is returned. This is the primary method
		/// for determining a tenant's current entitlements. Returns <see langword="null"/> if the tenant has
		/// no valid subscription, which should restrict access to paid features.
		/// </remarks>
		Task<Subscription?> GetActiveSubscriptionAsync(TenantId tenantId, CancellationToken cancellationToken = default);

		/// <summary>
		/// Retrieves a subscription by its unique identifier.
		/// </summary>
		/// <param name="subscriptionId">The unique identifier of the subscription to retrieve. Cannot be <see langword="null"/> or empty.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests during the asynchronous operation.</param>
		/// <returns>
		/// A task that represents the asynchronous operation. The task result contains the <see cref="Subscription"/>
		/// if found, or <see langword="null"/> if no subscription exists with the specified ID.
		/// </returns>
		/// <exception cref="ArgumentException">
		/// Thrown when <paramref name="subscriptionId"/> is <see langword="null"/> or empty.
		/// </exception>
		/// <remarks>
		/// This method performs a direct lookup by subscription ID and returns <see langword="null"/> if not found,
		/// allowing graceful handling of missing subscriptions. Used for subscription-specific operations like
		/// cancellation, renewal, and status updates. The returned subscription may have any status (active, cancelled, expired, etc.).
		/// </remarks>
		Task<Subscription?> GetSubscriptionAsync(string subscriptionId, CancellationToken cancellationToken = default);

		#endregion
	}
}