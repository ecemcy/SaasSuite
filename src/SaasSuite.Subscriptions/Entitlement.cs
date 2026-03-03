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

namespace SaasSuite.Subscriptions
{
	/// <summary>
	/// Represents the computed access rights, features, and resource limits a tenant is entitled to based on their active subscription.
	/// </summary>
	/// <remarks>
	/// Entitlements are derived from the combination of a tenant's subscription and the associated plan definition.
	/// They provide a read-only snapshot of what a tenant can access at a given point in time, making it easy
	/// to check feature flags and enforce usage limits throughout the application without repeatedly querying
	/// subscription and plan data. Entitlements should be recomputed when subscriptions change or plans are updated.
	/// </remarks>
	public class Entitlement
	{
		#region ' Properties '

		/// <summary>
		/// Gets or sets a value indicating whether the subscription is currently in a trial period.
		/// </summary>
		/// <value>
		/// <see langword="true"/> if the tenant is using a trial subscription with an active trial end date in the future;
		/// otherwise, <see langword="false"/>.
		/// </value>
		/// <remarks>
		/// This computed flag helps distinguish between trial and paid subscriptions for UI display,
		/// analytics, and feature access decisions. Trial users may have different upgrade prompts or
		/// limitations compared to paying customers. A subscription is considered in trial if its status
		/// is Trial and the trial end date has not yet passed.
		/// </remarks>
		public bool IsInTrial { get; set; }

		/// <summary>
		/// Gets or sets the subscription plan identifier that defines the entitled features and limits.
		/// </summary>
		/// <value>
		/// A string containing the plan ID from which features and limits are copied.
		/// </value>
		/// <remarks>
		/// The plan ID allows correlation with the plan definition for reference.
		/// Features and limits in this entitlement are snapshots from the plan at the time of computation.
		/// </remarks>
		public string PlanId { get; set; } = string.Empty;

		/// <summary>
		/// Gets or sets the unique identifier of the subscription granting these entitlements.
		/// </summary>
		/// <value>
		/// A string containing the subscription ID that this entitlement is derived from.
		/// </value>
		/// <remarks>
		/// This creates a traceable link back to the source subscription for auditing and debugging.
		/// When the subscription status changes, entitlements should be recomputed.
		/// </remarks>
		public string SubscriptionId { get; set; } = string.Empty;

		/// <summary>
		/// Gets or sets the UTC date and time when the subscription (and thus the entitlement) ends.
		/// </summary>
		/// <value>
		/// A <see cref="DateTimeOffset"/> indicating when subscription access terminates,
		/// or <see langword="null"/> for ongoing subscriptions without a defined end date.
		/// </value>
		/// <remarks>
		/// For cancelled subscriptions, this is typically set to the end of the current billing period.
		/// For expired subscriptions, this marks when access was revoked. For active recurring subscriptions,
		/// this may be <see langword="null"/> as they continue until cancelled. Use this date to enforce
		/// access cutoff and trigger subscription expiration workflows.
		/// </remarks>
		public DateTimeOffset? EndDate { get; set; }

		/// <summary>
		/// Gets or sets the UTC date and time when the trial period ends.
		/// </summary>
		/// <value>
		/// A <see cref="DateTimeOffset"/> indicating when trial access expires,
		/// or <see langword="null"/> if the subscription is not in a trial or the trial has already ended.
		/// </value>
		/// <remarks>
		/// When this date is reached, trial subscriptions should typically transition to Active (if payment
		/// is successful) or Expired (if no payment method is available). This value is copied from
		/// <see cref="Subscription.TrialEndDate"/> and is useful for displaying countdown timers or
		/// upgrade prompts to trial users.
		/// </remarks>
		public DateTimeOffset? TrialEndDate { get; set; }

		/// <summary>
		/// Gets or sets the resource usage limits the tenant is entitled to.
		/// </summary>
		/// <value>
		/// A dictionary mapping limit names to numeric values (e.g., "api-calls" -> 10000, "storage-gb" -> 100).
		/// Defaults to an empty dictionary.
		/// </value>
		/// <remarks>
		/// Limits are copied from the associated <see cref="SubscriptionPlan.Limits"/> at the time
		/// of entitlement computation. These values should be enforced by quota systems, metering services,
		/// or other resource management components. The decimal type accommodates both whole numbers and
		/// fractional limits. Common limit types include API call counts, storage quotas, user seats,
		/// and bandwidth allocations.
		/// </remarks>
		public Dictionary<string, decimal> Limits { get; set; } = new Dictionary<string, decimal>();

		/// <summary>
		/// Gets or sets the collection of feature flags the tenant is entitled to access.
		/// </summary>
		/// <value>
		/// A list of feature names (e.g., "advanced-analytics", "priority-support", "api-access").
		/// Defaults to an empty list.
		/// </value>
		/// <remarks>
		/// Features are copied from the associated <see cref="SubscriptionPlan.Features"/> at the time
		/// of entitlement computation. Use this collection to check feature access throughout the application
		/// via <c>entitlement.Features.Contains("feature-name")</c> or through the subscription service's
		/// <c>HasFeatureAsync</c> method. Feature names should follow a consistent naming convention.
		/// </remarks>
		public List<string> Features { get; set; } = new List<string>();

		/// <summary>
		/// Gets or sets the current status of the underlying subscription.
		/// </summary>
		/// <value>
		/// A <see cref="SubscriptionStatus"/> value indicating the subscription's lifecycle state.
		/// </value>
		/// <remarks>
		/// The status determines whether the entitlement is currently valid and enforceable.
		/// Only Active and Trial subscriptions typically grant full access to features.
		/// Other statuses (Cancelled, Expired, PastDue, Suspended) may restrict access partially or completely.
		/// </remarks>
		public SubscriptionStatus Status { get; set; }

		/// <summary>
		/// Gets or sets the tenant identifier for which this entitlement applies.
		/// </summary>
		/// <value>
		/// A <see cref="Core.TenantId"/> uniquely identifying the tenant who owns this entitlement.
		/// </value>
		/// <remarks>
		/// The tenant ID ensures entitlements are properly scoped in multi-tenant scenarios.
		/// This value must match the tenant associated with the underlying subscription.
		/// </remarks>
		public TenantId TenantId { get; set; }

		#endregion
	}
}