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
using SaasSuite.Subscriptions.Services;
using SaasSuite.Subscriptions.Stores;

namespace SaasSuite.Subscriptions
{
	/// <summary>
	/// Represents an active or historical subscription instance for a tenant in a multi-tenant SaaS application.
	/// </summary>
	/// <remarks>
	/// Subscriptions link tenants to subscription plans and track the lifecycle of that relationship including
	/// start dates, renewal dates, trials, cancellations, and status changes. Each subscription is uniquely identified
	/// and maintains its own timeline and metadata. Tenants may have multiple subscriptions over time (historical records)
	/// but typically only one active subscription, depending on <see cref="SubscriptionOptions.AllowMultipleSubscriptions"/>.
	/// </remarks>
	public class Subscription
	{
		#region ' Properties '

		/// <summary>
		/// Gets or sets the unique identifier for this subscription instance.
		/// </summary>
		/// <value>
		/// A globally unique string generated using <see cref="Guid.NewGuid"/>.
		/// Defaults to a new GUID string.
		/// </value>
		/// <remarks>
		/// This ID uniquely identifies the subscription record and is used for retrieval, updates, and as a
		/// foreign key in related systems (billing, analytics, etc.). The default GUID ensures uniqueness
		/// across distributed systems and simplifies data synchronization.
		/// </remarks>
		public string Id { get; set; } = Guid.NewGuid().ToString();

		/// <summary>
		/// Gets or sets the subscription plan identifier that defines the features and pricing for this subscription.
		/// </summary>
		/// <value>
		/// A string referencing a <see cref="SubscriptionPlan.Id"/>.
		/// Defaults to an empty string.
		/// </value>
		/// <remarks>
		/// This links the subscription to its plan definition, which contains features, limits, pricing, and billing period.
		/// Changes to the plan definition don't automatically affect existing subscriptions; plan upgrades/downgrades
		/// typically create new subscription records or explicitly update this field. Use this to retrieve the associated
		/// plan for entitlement calculations and billing operations.
		/// </remarks>
		public string PlanId { get; set; } = string.Empty;

		/// <summary>
		/// Gets or sets the UTC date and time when this subscription record was created.
		/// </summary>
		/// <value>
		/// A <see cref="DateTimeOffset"/> marking the record creation timestamp.
		/// Defaults to <see cref="DateTimeOffset.UtcNow"/>.
		/// </value>
		/// <remarks>
		/// This immutable timestamp records when the subscription was initially created in the system.
		/// Useful for auditing, analytics, and chronological ordering. Should not be changed after creation.
		/// Differs from <see cref="StartDate"/> which represents when subscription access began.
		/// </remarks>
		public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

		/// <summary>
		/// Gets or sets the UTC date and time when the subscription began.
		/// </summary>
		/// <value>
		/// A <see cref="DateTimeOffset"/> marking the subscription's start.
		/// Defaults to <see cref="DateTimeOffset.UtcNow"/>.
		/// </value>
		/// <remarks>
		/// For trial subscriptions, this is when the trial started. For paid subscriptions, this is when
		/// the first payment was made or when access was granted. This date is used to calculate billing
		/// anniversaries, subscription age for analytics, and is important for revenue recognition.
		/// All timestamps use UTC to avoid time zone ambiguity.
		/// </remarks>
		public DateTimeOffset StartDate { get; set; } = DateTimeOffset.UtcNow;

		/// <summary>
		/// Gets or sets the UTC date and time when this subscription record was last modified.
		/// </summary>
		/// <value>
		/// A <see cref="DateTimeOffset"/> marking the most recent update.
		/// Defaults to <see cref="DateTimeOffset.UtcNow"/>.
		/// </value>
		/// <remarks>
		/// This timestamp is automatically updated whenever the subscription is modified (status changes,
		/// plan upgrades, metadata updates, etc.). Implementations should set this to the current UTC time
		/// in update operations. Useful for synchronization, caching invalidation, and change tracking.
		/// The <see cref="InMemorySubscriptionStore"/> automatically updates this field in
		/// <c>UpdateSubscriptionAsync</c>.
		/// </remarks>
		public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

		/// <summary>
		/// Gets or sets the UTC date and time when the subscription was cancelled by the tenant.
		/// </summary>
		/// <value>
		/// A <see cref="DateTimeOffset"/> marking when cancellation was requested,
		/// or <see langword="null"/> if the subscription has not been cancelled.
		/// </value>
		/// <remarks>
		/// This timestamp records when the user initiated cancellation, which may be different from
		/// <see cref="EndDate"/> if the subscription continues until the end of the billing period.
		/// Useful for cancellation analytics, retention campaigns, and auditing. When set, the
		/// <see cref="Status"/> should typically be <see cref="SubscriptionStatus.Cancelled"/>.
		/// </remarks>
		public DateTimeOffset? CancellationDate { get; set; }

		/// <summary>
		/// Gets or sets the UTC date and time when the subscription ends or ended.
		/// </summary>
		/// <value>
		/// A <see cref="DateTimeOffset"/> marking when access terminates,
		/// or <see langword="null"/> for ongoing subscriptions without a defined end date.
		/// </value>
		/// <remarks>
		/// For cancelled subscriptions, this typically represents the end of the current billing period.
		/// For expired subscriptions, this is when access was revoked. For active recurring subscriptions,
		/// this is usually <see langword="null"/> as they continue indefinitely. One-time subscriptions
		/// may have this set to a specific expiration date. Use this to enforce access cutoffs and trigger
		/// expiration workflows.
		/// </remarks>
		public DateTimeOffset? EndDate { get; set; }

		/// <summary>
		/// Gets or sets the UTC date and time when the next billing charge will occur for recurring subscriptions.
		/// </summary>
		/// <value>
		/// A <see cref="DateTimeOffset"/> indicating the next billing date,
		/// or <see langword="null"/> for one-time subscriptions or cancelled subscriptions that won't renew.
		/// </value>
		/// <remarks>
		/// For recurring subscriptions (Monthly, Quarterly, Annual), this date is calculated based on the
		/// billing period and represents when the next payment should be processed. It's updated after each
		/// successful billing cycle. Use this to schedule billing jobs, send renewal reminders, and display
		/// billing information to users. Calculated by <see cref="SubscriptionService.CalculateNextBillingDate"/>.
		/// </remarks>
		public DateTimeOffset? NextBillingDate { get; set; }

		/// <summary>
		/// Gets or sets the UTC date and time when the trial period ends.
		/// </summary>
		/// <value>
		/// A <see cref="DateTimeOffset"/> marking the trial expiration,
		/// or <see langword="null"/> if the subscription is not in trial or never had a trial.
		/// </value>
		/// <remarks>
		/// Trial end dates are calculated by adding <see cref="SubscriptionPlan.TrialPeriodDays"/> to the
		/// <see cref="StartDate"/> when creating a trial subscription. When this date is reached, the subscription
		/// should transition to Active (with successful payment) or Expired (without payment). Use this to
		/// display trial countdown timers and trigger conversion workflows. After trial ends, this value remains
		/// for historical reference.
		/// </remarks>
		public DateTimeOffset? TrialEndDate { get; set; }

		/// <summary>
		/// Gets or sets custom metadata associated with this subscription as key-value pairs.
		/// </summary>
		/// <value>
		/// A dictionary of string keys and values containing application-specific metadata,
		/// or <see langword="null"/> if no metadata is associated.
		/// </value>
		/// <remarks>
		/// Metadata provides extensibility for storing additional subscription-specific information without
		/// schema changes. Common uses include:
		/// <list type="bullet">
		/// <item><description>External provider IDs (Stripe subscription ID, PayPal agreement ID)</description></item>
		/// <item><description>Promotional codes or discount information</description></item>
		/// <item><description>Referral sources or campaign tracking</description></item>
		/// <item><description>Custom billing notes or special terms</description></item>
		/// <item><description>Integration-specific fields</description></item>
		/// </list>
		/// Keep metadata lightweight and avoid storing large or sensitive data.
		/// </remarks>
		public Dictionary<string, string>? Metadata { get; set; }

		/// <summary>
		/// Gets or sets the current lifecycle status of this subscription.
		/// </summary>
		/// <value>
		/// A <see cref="SubscriptionStatus"/> enum value indicating the subscription's state.
		/// </value>
		/// <remarks>
		/// The status controls access to features and drives business logic throughout the application.
		/// Status transitions should be tracked for auditing. Common transitions include:
		/// <list type="bullet">
		/// <item><description>Trial -> Active (successful payment)</description></item>
		/// <item><description>Active -> Cancelled (user cancellation)</description></item>
		/// <item><description>Active -> PastDue (payment failure)</description></item>
		/// <item><description>PastDue -> Suspended (grace period expired)</description></item>
		/// <item><description>Suspended -> Active (payment recovered)</description></item>
		/// <item><description>Any state -> Expired (subscription ended)</description></item>
		/// </list>
		/// </remarks>
		public SubscriptionStatus Status { get; set; }

		/// <summary>
		/// Gets or sets the tenant identifier that owns this subscription.
		/// </summary>
		/// <value>
		/// A <see cref="Core.TenantId"/> linking this subscription to a specific tenant.
		/// </value>
		/// <remarks>
		/// This establishes the subscription-tenant relationship and is used for tenant-scoped queries,
		/// access control, and billing. All subscription operations should enforce tenant isolation using this value.
		/// A tenant may have multiple subscriptions (historical or concurrent based on configuration).
		/// </remarks>
		public TenantId TenantId { get; set; }

		#endregion
	}
}