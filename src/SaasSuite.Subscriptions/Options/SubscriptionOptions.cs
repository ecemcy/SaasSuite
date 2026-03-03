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

namespace SaasSuite.Subscriptions.Options
{
	/// <summary>
	/// Provides configuration options that control subscription service behavior throughout the application.
	/// </summary>
	/// <remarks>
	/// These options are typically configured in application startup (via appsettings.json or code) and injected
	/// into services using the options pattern (<c>IOptions&lt;SubscriptionOptions&gt;</c>). They define global
	/// policies for subscription lifecycle management, trial handling, and tenant subscription limits.
	/// Changes to these options affect all subscriptions and should be carefully considered.
	/// </remarks>
	public class SubscriptionOptions
	{
		#region ' Properties '

		/// <summary>
		/// Gets or sets a value indicating whether tenants can have multiple active subscriptions simultaneously.
		/// </summary>
		/// <value>
		/// <see langword="true"/> to allow multiple concurrent active subscriptions per tenant;
		/// <see langword="false"/> to enforce a single active subscription. Defaults to <see langword="false"/>.
		/// </value>
		/// <remarks>
		/// Most SaaS applications enforce a single active subscription per tenant (the most common model).
		/// When <see langword="false"/>, attempting to create a new subscription for a tenant with an existing
		/// active subscription should either fail or automatically cancel the previous subscription (based on
		/// business logic). When <see langword="true"/>, tenants can have multiple active subscriptions, which
		/// might be useful for:
		/// <list type="bullet">
		/// <item><description>Different product lines or modules</description></item>
		/// <item><description>Separate billing for different departments</description></item>
		/// <item><description>Addons or supplementary subscriptions</description></item>
		/// </list>
		/// Enabling multiple subscriptions requires additional entitlement aggregation logic.
		/// </remarks>
		public bool AllowMultipleSubscriptions { get; set; } = false;

		/// <summary>
		/// Gets or sets a value indicating whether expired subscriptions should be automatically cancelled.
		/// </summary>
		/// <value>
		/// <see langword="true"/> to automatically transition expired subscriptions to Cancelled status;
		/// <see langword="false"/> to leave them in Expired status. Defaults to <see langword="true"/>.
		/// </value>
		/// <remarks>
		/// When enabled, background jobs or scheduled tasks should periodically check for subscriptions
		/// that have passed their <see cref="Subscription.EndDate"/> and update their status to Cancelled.
		/// This keeps the subscription state machine clean and enables accurate reporting of active vs cancelled
		/// subscriptions. When disabled, expired subscriptions remain in Expired status until manually handled,
		/// which may be useful for manual review workflows or grace periods beyond the end date.
		/// </remarks>
		public bool AutoCancelExpired { get; set; } = true;

		/// <summary>
		/// Gets or sets the default trial period in days when no plan-specific trial is defined.
		/// </summary>
		/// <value>
		/// An integer representing the fallback trial duration in days.
		/// A value of <c>0</c> means no default trial. Defaults to <c>0</c>.
		/// </value>
		/// <remarks>
		/// This provides a system-wide default trial period when <see cref="SubscriptionPlan.TrialPeriodDays"/>
		/// is <c>0</c> or not explicitly set. Useful for:
		/// <list type="bullet">
		/// <item><description>Offering consistent trials across all plans</description></item>
		/// <item><description>Running promotional trial periods without modifying each plan</description></item>
		/// <item><description>Providing trials for legacy plans that predate the trial feature</description></item>
		/// </list>
		/// Plan-specific trial periods take precedence over this default. If both this and the plan's trial
		/// period are <c>0</c>, no trial is offered. Common default values are 7, 14, or 30 days.
		/// Setting to <c>0</c> disables default trials, requiring explicit plan configuration.
		/// </remarks>
		public int DefaultTrialPeriodDays { get; set; } = 0;

		/// <summary>
		/// Gets or sets the number of days to wait after a payment failure before suspending a subscription.
		/// </summary>
		/// <value>
		/// An integer representing the grace period duration in days. Defaults to <c>7</c> days.
		/// </value>
		/// <remarks>
		/// When a subscription payment fails, it typically transitions to PastDue status. This grace period
		/// defines how long to wait before moving to Suspended status (which may restrict or revoke access).
		/// During the grace period:
		/// <list type="bullet">
		/// <item><description>Retry billing attempts should continue</description></item>
		/// <item><description>Access may be maintained or limited based on business policy</description></item>
		/// <item><description>Customers can update payment methods to recover</description></item>
		/// </list>
		/// Common grace periods range from 3 to 14 days. After the grace period expires without successful payment,
		/// subscriptions should be suspended or cancelled. A value of <c>0</c> means immediate suspension on failure.
		/// </remarks>
		public int GracePeriodDays { get; set; } = 7;

		#endregion
	}
}