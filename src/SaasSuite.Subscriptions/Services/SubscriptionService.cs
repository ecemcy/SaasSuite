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
using Microsoft.Extensions.Options;

using SaasSuite.Core;
using SaasSuite.Subscriptions.Enumerations;
using SaasSuite.Subscriptions.Interfaces;
using SaasSuite.Subscriptions.Options;

namespace SaasSuite.Subscriptions.Services
{
	/// <summary>
	/// Provides business logic for managing subscription plans and tenant subscriptions in a multi-tenant SaaS application.
	/// </summary>
	/// <remarks>
	/// This service acts as the primary facade for subscription operations, coordinating between the subscription store
	/// and configuration options. It handles plan management (CRUD), subscription lifecycle (create, cancel, renew),
	/// and entitlement calculation. The service enforces business rules like plan validation, trial period handling,
	/// and billing date calculations. All operations are tenant-aware and support proper isolation in multi-tenant scenarios.
	/// </remarks>
	public class SubscriptionService
	{
		#region ' Fields '

		/// <summary>
		/// The subscription store for data persistence operations.
		/// </summary>
		private readonly ISubscriptionStore _store;

		/// <summary>
		/// Configuration options controlling subscription behavior.
		/// </summary>
		private readonly SubscriptionOptions _options;

		#endregion

		#region ' Constructors '

		/// <summary>
		/// Initializes a new instance of the <see cref="SubscriptionService"/> class.
		/// </summary>
		/// <param name="store">The subscription store implementation for data access. Cannot be <see langword="null"/>.</param>
		/// <param name="options">The subscription configuration options. Cannot be <see langword="null"/>.</param>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="store"/> or <paramref name="options"/> is <see langword="null"/>.
		/// </exception>
		/// <remarks>
		/// The service is registered as scoped in the dependency injection container, typically per HTTP request
		/// in web applications. The store and options are injected automatically by the DI framework when configured
		/// via <see cref="ServiceCollectionExtensions.AddSaasSubscriptions(IServiceCollection)"/>.
		/// </remarks>
		public SubscriptionService(ISubscriptionStore store, IOptions<SubscriptionOptions> options)
		{
			// Validate and store the subscription store dependency
			this._store = store ?? throw new ArgumentNullException(nameof(store));

			// Validate and extract options value
			this._options = options?.Value ?? throw new ArgumentNullException(nameof(options));
		}

		#endregion

		#region ' Plan Management '

		/// <summary>
		/// Creates a new subscription plan after validation.
		/// </summary>
		/// <param name="plan">The subscription plan to create. Cannot be <see langword="null"/>.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
		/// <returns>
		/// A task that represents the asynchronous operation. The task result contains the created <see cref="SubscriptionPlan"/>.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="plan"/> is <see langword="null"/>.
		/// </exception>
		/// <exception cref="ArgumentException">
		/// Thrown when the plan name is empty or the price is negative.
		/// </exception>
		/// <remarks>
		/// This method performs validation before delegating to the store:
		/// <list type="bullet">
		/// <item><description>Ensures the plan name is not empty (required for display)</description></item>
		/// <item><description>Ensures the price is non-negative (free plans use 0, paid plans use positive values)</description></item>
		/// </list>
		/// After validation passes, the plan is persisted and returned unchanged. The plan ID should be
		/// set before calling this method (defaults to a new GUID). Consider additional validation like
		/// checking for duplicate names or validating feature/limit configurations based on business rules.
		/// </remarks>
		public async Task<SubscriptionPlan> CreatePlanAsync(SubscriptionPlan plan, CancellationToken cancellationToken = default)
		{
			// Validate plan is not null
			ArgumentNullException.ThrowIfNull(plan);

			// Ensure plan has a name for display and identification
			if (string.IsNullOrWhiteSpace(plan.Name))
			{
				throw new ArgumentException("Plan name cannot be null or whitespace.", nameof(plan));
			}

			// Ensure price is non-negative (0 for free plans, positive for paid)
			if (plan.Price < 0)
			{
				throw new ArgumentException("Plan price cannot be negative.", nameof(plan));
			}

			// Persist the validated plan
			await this._store.CreatePlanAsync(plan, cancellationToken);
			return plan;
		}

		/// <summary>
		/// Deletes a subscription plan.
		/// </summary>
		/// <param name="planId">The unique plan identifier. Cannot be <see langword="null"/> or empty.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
		/// <returns>
		/// A task that represents the asynchronous operation.
		/// </returns>
		/// <remarks>
		/// Pass-through to the store. Use with caution; prefer marking plans inactive instead of deleting them
		/// if active subscriptions reference the plan. Deletion is idempotent and won't fail if the plan doesn't exist.
		/// </remarks>
		public Task DeletePlanAsync(string planId, CancellationToken cancellationToken = default)
		{
			return this._store.DeletePlanAsync(planId, cancellationToken);
		}

		/// <summary>
		/// Updates an existing subscription plan.
		/// </summary>
		/// <param name="plan">The subscription plan with updated values. Cannot be <see langword="null"/>.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
		/// <returns>
		/// A task that represents the asynchronous operation.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="plan"/> is <see langword="null"/>.
		/// </exception>
		/// <remarks>
		/// Validates the plan is not null before delegating to the store. Changes to plan definitions
		/// don't automatically affect existing subscriptions; consider implementing plan migration workflows
		/// if needed. Common update scenarios include marking plans inactive, changing descriptions or metadata,
		/// or adjusting limits for new subscriptions.
		/// </remarks>
		public Task UpdatePlanAsync(SubscriptionPlan plan, CancellationToken cancellationToken = default)
		{
			// Validate plan is not null before updating
			ArgumentNullException.ThrowIfNull(plan);

			return this._store.UpdatePlanAsync(plan, cancellationToken);
		}

		/// <summary>
		/// Retrieves all subscription plans with optional filtering.
		/// </summary>
		/// <param name="includeInactive">
		/// <see langword="true"/> to include inactive plans in the results;
		/// <see langword="false"/> to return only active plans. Defaults to <see langword="false"/>.
		/// </param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
		/// <returns>
		/// A task that represents the asynchronous operation. The task result contains a collection of
		/// <see cref="SubscriptionPlan"/> instances matching the filter criteria.
		/// </returns>
		/// <remarks>
		/// Pass-through to the store. When <paramref name="includeInactive"/> is <see langword="false"/>,
		/// only plans available for new subscriptions are returned (suitable for plan selection UIs).
		/// When <see langword="true"/>, all plans including deprecated ones are returned (useful for
		/// admin interfaces and reporting).
		/// </remarks>
		public Task<IEnumerable<SubscriptionPlan>> GetPlansAsync(bool includeInactive = false, CancellationToken cancellationToken = default)
		{
			return this._store.GetPlansAsync(includeInactive, cancellationToken);
		}

		/// <summary>
		/// Retrieves a subscription plan by its identifier.
		/// </summary>
		/// <param name="planId">The unique plan identifier. Cannot be <see langword="null"/> or empty.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
		/// <returns>
		/// A task that represents the asynchronous operation. The task result contains the <see cref="SubscriptionPlan"/>
		/// if found, or <see langword="null"/> if no plan exists with the specified ID.
		/// </returns>
		/// <remarks>
		/// This is a pass-through method to the store with no additional logic. Returns <see langword="null"/>
		/// when the plan doesn't exist, allowing callers to handle missing plans gracefully.
		/// </remarks>
		public Task<SubscriptionPlan?> GetPlanAsync(string planId, CancellationToken cancellationToken = default)
		{
			return this._store.GetPlanAsync(planId, cancellationToken);
		}

		#endregion

		#region ' Subscription Management '

		/// <summary>
		/// Calculates the next billing date based on the current date and billing period.
		/// </summary>
		/// <param name="currentDate">The starting date from which to calculate the next billing date.</param>
		/// <param name="period">The billing period defining the renewal frequency.</param>
		/// <returns>
		/// A <see cref="DateTimeOffset"/> representing the next billing date.
		/// For OneTime billing, returns the current date unchanged.
		/// </returns>
		/// <remarks>
		/// This private helper method implements billing date calculation logic:
		/// <list type="bullet">
		/// <item><description>Monthly: Adds 1 month (e.g., Jan 15 to Feb 15)</description></item>
		/// <item><description>Quarterly: Adds 3 months (e.g., Jan 15 to Apr 15)</description></item>
		/// <item><description>Annual: Adds 1 year (e.g., Jan 15 2024 to Jan 15 2025)</description></item>
		/// <item><description>OneTime: Returns input unchanged (no renewal)</description></item>
		/// </list>
		/// Date arithmetic handles month-end edge cases automatically (e.g., Jan 31 + 1 month = Feb 28/29).
		/// All calculations preserve the time component and time zone offset.
		/// </remarks>
		private DateTimeOffset CalculateNextBillingDate(DateTimeOffset currentDate, BillingPeriod period)
		{
			return period switch
			{
				// Add appropriate time period for each billing cycle
				BillingPeriod.Monthly => currentDate.AddMonths(1),
				BillingPeriod.Quarterly => currentDate.AddMonths(3),
				BillingPeriod.Annual => currentDate.AddYears(1),
				// One-time billing has no next date
				_ => currentDate
			};
		}

		/// <summary>
		/// Checks if a tenant has access to a specific feature based on their active subscription.
		/// </summary>
		/// <param name="tenantId">The tenant identifier to check. Cannot be <see langword="null"/>.</param>
		/// <param name="feature">The feature name to check for access. Cannot be <see langword="null"/> or empty.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
		/// <returns>
		/// A task that represents the asynchronous operation. The task result contains <see langword="true"/>
		/// if the tenant has access to the specified feature; otherwise, <see langword="false"/>.
		/// </returns>
		/// <remarks>
		/// This is a convenience method that retrieves the tenant's entitlement and checks if the specified
		/// feature is in the Features collection. Returns <see langword="false"/> if the tenant has no active
		/// subscription or the feature is not included in their plan. Feature names are case-sensitive.
		/// Use this for feature flag checks throughout the application.
		/// </remarks>
		public async Task<bool> HasFeatureAsync(TenantId tenantId, string feature, CancellationToken cancellationToken = default)
		{
			// Retrieve entitlements and check for feature presence
			Entitlement? entitlement = await this.GetEntitlementAsync(tenantId, cancellationToken);
			return entitlement?.Features.Contains(feature) ?? false;
		}

		/// <summary>
		/// Gets the usage limit for a specific metric for a tenant based on their active subscription.
		/// </summary>
		/// <param name="tenantId">The tenant identifier to check. Cannot be <see langword="null"/>.</param>
		/// <param name="metric">The metric name (e.g., "api-calls", "storage-gb"). Cannot be <see langword="null"/> or empty.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
		/// <returns>
		/// A task that represents the asynchronous operation. The task result contains the numeric limit
		/// if defined, or <see langword="null"/> if the tenant has no active subscription or no limit exists for the metric.
		/// </returns>
		/// <remarks>
		/// This convenience method retrieves the tenant's entitlement and extracts the limit value for the
		/// specified metric from the Limits dictionary. Returns <see langword="null"/> if the tenant has
		/// no active subscription or the metric is not defined in their plan. A missing limit typically means
		/// unlimited or no quota enforcement for that metric. Metric names are case-sensitive.
		/// </remarks>
		public async Task<decimal?> GetLimitAsync(TenantId tenantId, string metric, CancellationToken cancellationToken = default)
		{
			// Retrieve entitlements and extract the limit for the specified metric
			Entitlement? entitlement = await this.GetEntitlementAsync(tenantId, cancellationToken);
			if (entitlement?.Limits.TryGetValue(metric, out decimal limit) == true)
			{
				return limit;
			}

			return null;
		}

		/// <summary>
		/// Computes the entitlements for a specific tenant based on their active subscription.
		/// </summary>
		/// <param name="tenantId">The tenant identifier whose entitlements to compute. Cannot be <see langword="null"/>.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
		/// <returns>
		/// A task that represents the asynchronous operation. The task result contains the tenant's <see cref="Entitlement"/>
		/// if they have an active subscription, or <see langword="null"/> if they have no active subscription.
		/// </returns>
		/// <remarks>
		/// This method builds an entitlement object by:
		/// <list type="number">
		/// <item><description>Retrieving the tenant's active subscription (Active or Trial status)</description></item>
		/// <item><description>Retrieving the associated plan definition</description></item>
		/// <item><description>Copying features and limits from the plan to the entitlement</description></item>
		/// <item><description>Computing trial status based on trial end date</description></item>
		/// <item><description>Including subscription metadata like status, dates, and identifiers</description></item>
		/// </list>
		/// The returned entitlement provides a complete snapshot of what the tenant can access.
		/// Returns <see langword="null"/> if no active subscription exists, indicating no paid access.
		/// </remarks>
		public async Task<Entitlement?> GetEntitlementAsync(TenantId tenantId, CancellationToken cancellationToken = default)
		{
			// Retrieve the active subscription for the tenant
			Subscription? subscription = await this._store.GetActiveSubscriptionAsync(tenantId, cancellationToken);
			if (subscription == null)
			{
				// No active subscription means no entitlements
				return null;
			}

			// Retrieve the plan to get features and limits
			SubscriptionPlan? plan = await this._store.GetPlanAsync(subscription.PlanId, cancellationToken);
			if (plan == null)
			{
				// Plan is required for entitlements
				return null;
			}

			// Determine if currently in trial period
			DateTimeOffset now = DateTimeOffset.UtcNow;
			bool isInTrial = subscription.Status == SubscriptionStatus.Trial &&
							 subscription.TrialEndDate.HasValue &&
							 subscription.TrialEndDate.Value > now;

			// Build the entitlement object
			return new Entitlement
			{
				TenantId = tenantId,
				SubscriptionId = subscription.Id,
				PlanId = subscription.PlanId,
				Status = subscription.Status,
				Features = new List<string>(plan.Features), // Copy features from plan
				Limits = new Dictionary<string, decimal>(plan.Limits), // Copy limits from plan
				IsInTrial = isInTrial,
				TrialEndDate = subscription.TrialEndDate,
				EndDate = subscription.EndDate
			};
		}

		/// <summary>
		/// Cancels a subscription, transitioning it to Cancelled status with a cancellation date.
		/// </summary>
		/// <param name="subscriptionId">The unique subscription identifier. Cannot be <see langword="null"/> or empty.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
		/// <returns>
		/// A task that represents the asynchronous operation. The task result contains the cancelled <see cref="Subscription"/>.
		/// </returns>
		/// <exception cref="ArgumentException">
		/// Thrown when the subscription doesn't exist.
		/// </exception>
		/// <remarks>
		/// This method retrieves the subscription, updates its status to Cancelled, sets the cancellation date
		/// to the current UTC time, and sets the end date to the cancellation date (immediate termination).
		/// For "cancel at period end" behavior, modify this logic to set EndDate to the NextBillingDate instead.
		/// The updated subscription is persisted and returned. Access to features should be revoked after
		/// the end date is reached.
		/// </remarks>
		public async Task<Subscription> CancelSubscriptionAsync(string subscriptionId, CancellationToken cancellationToken = default)
		{
			// Retrieve the subscription to cancel
			Subscription? subscription = await this._store.GetSubscriptionAsync(subscriptionId, cancellationToken)
				?? throw new ArgumentException($"Subscription with ID '{subscriptionId}' does not exist.", nameof(subscriptionId));

			// Update subscription state for cancellation
			subscription.Status = SubscriptionStatus.Cancelled;
			subscription.CancellationDate = DateTimeOffset.UtcNow;
			subscription.EndDate = subscription.CancellationDate; // Immediate termination

			// Persist the cancellation
			await this._store.UpdateSubscriptionAsync(subscription, cancellationToken);
			return subscription;
		}

		/// <summary>
		/// Creates a new subscription for a tenant with the specified plan and optional trial.
		/// </summary>
		/// <param name="tenantId">The tenant identifier for whom to create the subscription. Cannot be <see langword="null"/>.</param>
		/// <param name="planId">The subscription plan identifier. Cannot be <see langword="null"/> or empty.</param>
		/// <param name="startTrial">
		/// <see langword="true"/> to start the subscription with a trial period if the plan offers one;
		/// <see langword="false"/> to start as an active paid subscription. Defaults to <see langword="false"/>.
		/// </param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
		/// <returns>
		/// A task that represents the asynchronous operation. The task result contains the newly created <see cref="Subscription"/>.
		/// </returns>
		/// <exception cref="ArgumentException">
		/// Thrown when the plan doesn't exist or is inactive.
		/// </exception>
		/// <remarks>
		/// This method orchestrates subscription creation with the following logic:
		/// <list type="number">
		/// <item><description>Retrieves and validates the plan (must exist and be active)</description></item>
		/// <item><description>Creates a new subscription with a unique ID and current UTC start date</description></item>
		/// <item><description>Sets status to Trial or Active based on <paramref name="startTrial"/> and plan's trial period</description></item>
		/// <item><description>Calculates trial end date if starting with a trial</description></item>
		/// <item><description>Calculates next billing date for recurring plans (not for one-time)</description></item>
		/// <item><description>Persists the subscription and returns it</description></item>
		/// </list>
		/// The subscription is immediately active/trial and queryable after creation.
		/// </remarks>
		public async Task<Subscription> CreateSubscriptionAsync(TenantId tenantId, string planId, bool startTrial = false, CancellationToken cancellationToken = default)
		{
			// Retrieve the plan to validate it exists
			SubscriptionPlan? plan = await this._store.GetPlanAsync(planId, cancellationToken)
				?? throw new ArgumentException($"Plan with ID '{planId}' does not exist.", nameof(planId));

			// Ensure the plan is available for new subscriptions
			if (!plan.IsActive)
			{
				throw new ArgumentException($"Plan '{plan.Name}' is not currently available.", nameof(planId));
			}

			// Capture the current time for consistent calculations
			DateTimeOffset now = DateTimeOffset.UtcNow;

			// Build the subscription object with initial values
			Subscription subscription = new Subscription
			{
				TenantId = tenantId,
				PlanId = planId,
				// Set status based on trial request and plan configuration
				Status = startTrial && plan.TrialPeriodDays > 0 ? SubscriptionStatus.Trial : SubscriptionStatus.Active,
				StartDate = now
			};

			// Calculate trial end date if starting with a trial
			if (startTrial && plan.TrialPeriodDays > 0)
			{
				subscription.TrialEndDate = now.AddDays(plan.TrialPeriodDays);
			}

			// Calculate next billing date for recurring subscriptions
			if (plan.BillingPeriod != BillingPeriod.OneTime)
			{
				subscription.NextBillingDate = this.CalculateNextBillingDate(now, plan.BillingPeriod);
			}

			// Persist the subscription
			await this._store.CreateSubscriptionAsync(subscription, cancellationToken);
			return subscription;
		}

		/// <summary>
		/// Renews a subscription for another billing period, updating billing dates and handling trial-to-active transitions.
		/// </summary>
		/// <param name="subscriptionId">The unique subscription identifier. Cannot be <see langword="null"/> or empty.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
		/// <returns>
		/// A task that represents the asynchronous operation. The task result contains the renewed <see cref="Subscription"/>.
		/// </returns>
		/// <exception cref="ArgumentException">
		/// Thrown when the subscription or its plan doesn't exist, the plan is one-time (non-renewable),
		/// or the subscription status prevents renewal.
		/// </exception>
		/// <remarks>
		/// This method handles subscription renewal with the following logic:
		/// <list type="number">
		/// <item><description>Retrieves and validates the subscription and its plan exist</description></item>
		/// <item><description>Ensures the plan is renewable (not OneTime billing period)</description></item>
		/// <item><description>If status is Trial, transitions to Active and clears trial end date</description></item>
		/// <item><description>If status is Active or PastDue, allows renewal (other statuses throw exception)</description></item>
		/// <item><description>Calculates next billing date based on billing period</description></item>
		/// <item><description>Persists and returns the updated subscription</description></item>
		/// </list>
		/// Typically called after successful payment processing to extend the subscription period.
		/// </remarks>
		public async Task<Subscription> RenewSubscriptionAsync(string subscriptionId, CancellationToken cancellationToken = default)
		{
			// Retrieve the subscription to renew
			Subscription? subscription = await this._store.GetSubscriptionAsync(subscriptionId, cancellationToken)
				?? throw new ArgumentException($"Subscription with ID '{subscriptionId}' does not exist.", nameof(subscriptionId));

			// Retrieve the plan to access billing period information
			SubscriptionPlan? plan = await this._store.GetPlanAsync(subscription.PlanId, cancellationToken)
				?? throw new ArgumentException($"Plan with ID '{subscription.PlanId}' does not exist.");

			// Ensure the plan supports renewal
			if (plan.BillingPeriod == BillingPeriod.OneTime)
			{
				throw new ArgumentException("One-time subscriptions cannot be renewed.");
			}

			// Handle trial-to-active conversion
			if (subscription.Status == SubscriptionStatus.Trial)
			{
				subscription.Status = SubscriptionStatus.Active;
				subscription.TrialEndDate = null;
			}
			// Ensure subscription is in a renewable state
			else if (subscription.Status != SubscriptionStatus.Active && subscription.Status != SubscriptionStatus.PastDue)
			{
				throw new ArgumentException($"Cannot renew subscription with status '{subscription.Status}'.");
			}

			// Calculate next billing date from current billing date or now
			DateTimeOffset now = DateTimeOffset.UtcNow;
			DateTimeOffset billingDate = subscription.NextBillingDate ?? now;
			subscription.NextBillingDate = this.CalculateNextBillingDate(billingDate, plan.BillingPeriod);

			// Persist the renewal
			await this._store.UpdateSubscriptionAsync(subscription, cancellationToken);
			return subscription;
		}

		/// <summary>
		/// Retrieves all subscriptions for a specific tenant.
		/// </summary>
		/// <param name="tenantId">The tenant identifier whose subscriptions to retrieve. Cannot be <see langword="null"/>.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
		/// <returns>
		/// A task that represents the asynchronous operation. The task result contains a collection of
		/// <see cref="Subscription"/> instances for the tenant.
		/// </returns>
		/// <remarks>
		/// Pass-through to the store. Returns all subscriptions (active, cancelled, expired) ordered by
		/// creation date descending, providing a complete subscription history for the tenant.
		/// </remarks>
		public Task<IEnumerable<Subscription>> GetSubscriptionsByTenantAsync(TenantId tenantId, CancellationToken cancellationToken = default)
		{
			return this._store.GetSubscriptionsByTenantAsync(tenantId, cancellationToken);
		}

		/// <summary>
		/// Retrieves the currently active subscription for a specific tenant.
		/// </summary>
		/// <param name="tenantId">The tenant identifier whose active subscription to retrieve. Cannot be <see langword="null"/>.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
		/// <returns>
		/// A task that represents the asynchronous operation. The task result contains the active <see cref="Subscription"/>
		/// if one exists, or <see langword="null"/> if the tenant has no active or trial subscription.
		/// </returns>
		/// <remarks>
		/// Pass-through to the store. Returns subscriptions with Active or Trial status, representing
		/// subscriptions that currently grant access. Returns <see langword="null"/> if no valid subscription exists.
		/// </remarks>
		public Task<Subscription?> GetActiveSubscriptionAsync(TenantId tenantId, CancellationToken cancellationToken = default)
		{
			return this._store.GetActiveSubscriptionAsync(tenantId, cancellationToken);
		}

		/// <summary>
		/// Retrieves a subscription by its identifier.
		/// </summary>
		/// <param name="subscriptionId">The unique subscription identifier. Cannot be <see langword="null"/> or empty.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
		/// <returns>
		/// A task that represents the asynchronous operation. The task result contains the <see cref="Subscription"/>
		/// if found, or <see langword="null"/> if no subscription exists with the specified ID.
		/// </returns>
		/// <remarks>
		/// Pass-through to the store with no additional logic. Returns <see langword="null"/> when not found.
		/// </remarks>
		public Task<Subscription?> GetSubscriptionAsync(string subscriptionId, CancellationToken cancellationToken = default)
		{
			return this._store.GetSubscriptionAsync(subscriptionId, cancellationToken);
		}

		#endregion
	}
}