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

using SaasSuite.Subscriptions.Enumerations;
using SaasSuite.Subscriptions.Interfaces;

namespace SaasSuite.Subscriptions
{
	/// <summary>
	/// Defines a subscription tier or package offering in a multi-tenant SaaS application.
	/// </summary>
	/// <remarks>
	/// Subscription plans are templates that define what features and limits are available at a given price point
	/// and billing frequency. Plans are typically defined by administrators and selected by tenants during signup
	/// or upgrade flows. Each plan can include feature flags, resource limits, pricing, trial periods, and custom
	/// metadata. Multiple subscriptions can reference the same plan. Changes to a plan definition don't automatically
	/// affect existing subscriptions unless explicitly propagated through plan migration logic.
	/// </remarks>
	public class SubscriptionPlan
	{
		#region ' Properties '

		/// <summary>
		/// Gets or sets a value indicating whether this plan is currently available for new subscriptions.
		/// </summary>
		/// <value>
		/// <see langword="true"/> if the plan can be selected by new customers;
		/// <see langword="false"/> if it's hidden or deprecated. Defaults to <see langword="true"/>.
		/// </value>
		/// <remarks>
		/// Inactive plans are hidden from plan selection interfaces but existing subscriptions remain valid.
		/// This allows for graceful plan deprecation without disrupting current customers ("grandfathering").
		/// Use this to sunset old plans, run limited-time promotions, or manage plan catalog visibility.
		/// Queries using <see cref="ISubscriptionStore.GetPlansAsync"/> can filter by this flag.
		/// Never delete plan records that have active subscriptions; mark them inactive instead.
		/// </remarks>
		public bool IsActive { get; set; } = true;

		/// <summary>
		/// Gets or sets the price charged for this subscription plan.
		/// </summary>
		/// <value>
		/// A decimal value representing the cost in the system's base currency.
		/// </value>
		/// <remarks>
		/// The price is charged according to the <see cref="BillingPeriod"/>. For example, if Price is 99.99 and
		/// BillingPeriod is Monthly, the customer is charged 99.99 every month. The decimal type supports fractional
		/// currency values and accommodates various pricing strategies. For multi-currency support, additional
		/// currency fields or conversion logic may be needed. Pricing changes typically require creating new plan
		/// versions rather than modifying existing plans to avoid disrupting active subscriptions.
		/// </remarks>
		public decimal Price { get; set; }

		/// <summary>
		/// Gets or sets the duration of the free trial period in days.
		/// </summary>
		/// <value>
		/// An integer representing the number of days customers can use the plan for free before billing begins.
		/// A value of <c>0</c> means no trial is offered. Defaults to <c>0</c>.
		/// </value>
		/// <remarks>
		/// Trial periods allow customers to evaluate the plan before committing to payment. Common trial lengths
		/// are 7, 14, or 30 days. When a subscription is created with <c>startTrial</c> set to <see langword="true"/>,
		/// the <see cref="Subscription.TrialEndDate"/> is calculated by adding this value to the start date.
		/// Trials typically require a payment method upfront (to enable automatic conversion) or start without one
		/// (requiring manual upgrade). Different plans can have different trial periods to reflect their complexity
		/// or value. Setting to <c>0</c> disables trials for this plan.
		/// </remarks>
		public int TrialPeriodDays { get; set; }

		/// <summary>
		/// Gets or sets the unique identifier for this subscription plan.
		/// </summary>
		/// <value>
		/// A globally unique string generated using <see cref="Guid.NewGuid"/>.
		/// Defaults to a new GUID string.
		/// </value>
		/// <remarks>
		/// This ID uniquely identifies the plan and is referenced by subscriptions via <see cref="Subscription.PlanId"/>.
		/// The default GUID ensures uniqueness, but you may prefer human-readable IDs like "basic", "pro", "enterprise"
		/// for easier plan management and configuration. Plan IDs should remain stable even if plan names or pricing change.
		/// </remarks>
		public string Id { get; set; } = Guid.NewGuid().ToString();

		/// <summary>
		/// Gets or sets the display name of the subscription plan.
		/// </summary>
		/// <value>
		/// A human-readable string identifying the plan (e.g., "Basic", "Professional", "Enterprise").
		/// Defaults to an empty string.
		/// </value>
		/// <remarks>
		/// The name is displayed to users during plan selection, on invoices, and in subscription management interfaces.
		/// Should be clear, concise, and consistent with your product's branding and tier naming conventions.
		/// Names can be changed without affecting existing subscriptions, though historical reporting may need
		/// to account for plan name changes over time.
		/// </remarks>
		public string Name { get; set; } = string.Empty;

		/// <summary>
		/// Gets or sets the detailed description of the subscription plan.
		/// </summary>
		/// <value>
		/// A string containing markdown, HTML, or plain text describing the plan's value proposition,
		/// or <see langword="null"/> if no description is provided.
		/// </value>
		/// <remarks>
		/// The description is displayed on pricing pages and plan selection interfaces to help customers
		/// understand what's included and make informed decisions. Can include:
		/// <list type="bullet">
		/// <item><description>Feature highlights and benefits</description></item>
		/// <item><description>Use case recommendations ("Perfect for small teams")</description></item>
		/// <item><description>Limitations or restrictions</description></item>
		/// <item><description>Support level details</description></item>
		/// </list>
		/// Support for markdown or HTML enables rich formatting. Keep descriptions concise but informative.
		/// </remarks>
		public string? Description { get; set; }

		/// <summary>
		/// Gets or sets the billing frequency for this subscription plan.
		/// </summary>
		/// <value>
		/// A <see cref="Enumerations.BillingPeriod"/> value defining how often charges occur.
		/// </value>
		/// <remarks>
		/// The billing period determines subscription renewal frequency and is critical for revenue forecasting,
		/// pricing display, and billing system integration. Common strategies include offering the same plan
		/// at different periods with discounts for longer commitments (e.g., $10/month or $100/year).
		/// OneTime billing is used for perpetual licenses or single-purchase items that don't renew.
		/// </remarks>
		public BillingPeriod BillingPeriod { get; set; }

		/// <summary>
		/// Gets or sets the resource usage limits for this plan.
		/// </summary>
		/// <value>
		/// A dictionary mapping limit names to numeric thresholds
		/// (e.g., "api-calls" -> 10000, "storage-gb" -> 100, "users" -> 5).
		/// Defaults to an empty dictionary.
		/// </value>
		/// <remarks>
		/// Limits define quantitative restrictions on resource usage for this plan tier. Common limit types include:
		/// <list type="bullet">
		/// <item><description>API call quotas per period</description></item>
		/// <item><description>Storage capacity in GB or TB</description></item>
		/// <item><description>Number of users or seats</description></item>
		/// <item><description>Bandwidth or data transfer limits</description></item>
		/// <item><description>Number of projects, workspaces, or other entities</description></item>
		/// </list>
		/// These limits are enforced by quota systems and copied to <see cref="Entitlement.Limits"/> for runtime checks.
		/// The decimal type accommodates both whole numbers and fractional limits. Empty dictionary means unlimited
		/// or no specific limits defined.
		/// </remarks>
		public Dictionary<string, decimal> Limits { get; set; } = new Dictionary<string, decimal>();

		/// <summary>
		/// Gets or sets custom metadata associated with this plan as key-value pairs.
		/// </summary>
		/// <value>
		/// A dictionary of string keys and values containing additional plan information,
		/// or <see langword="null"/> if no metadata is needed.
		/// </value>
		/// <remarks>
		/// Metadata provides extensibility for plan-specific configuration without schema changes. Common uses include:
		/// <list type="bullet">
		/// <item><description>Display information (colors, icons, badges like "Most Popular")</description></item>
		/// <item><description>Marketing attributes (target audience, recommended for)</description></item>
		/// <item><description>External provider mapping (Stripe price ID, PayPal plan ID)</description></item>
		/// <item><description>Sorting order or categorization</description></item>
		/// <item><description>Special terms or conditions</description></item>
		/// <item><description>Localization keys for internationalization</description></item>
		/// </list>
		/// Keep metadata lightweight and avoid storing complex or frequently changing data.
		/// </remarks>
		public Dictionary<string, string>? Metadata { get; set; }

		/// <summary>
		/// Gets or sets the collection of feature flags included in this plan.
		/// </summary>
		/// <value>
		/// A list of feature names that tenants with this plan can access.
		/// Defaults to an empty list.
		/// </value>
		/// <remarks>
		/// Features represent capabilities or modules available to subscribers (e.g., "advanced-analytics",
		/// "priority-support", "api-access", "white-labeling"). Use consistent naming conventions across the
		/// application for feature checks. These features are copied to <see cref="Entitlement.Features"/> when
		/// computing tenant entitlements. Feature names should be treated as identifiers rather than display text;
		/// maintain a separate mapping for user-facing feature descriptions. Empty list means no special features
		/// beyond base functionality.
		/// </remarks>
		public List<string> Features { get; set; } = new List<string>();

		#endregion
	}
}