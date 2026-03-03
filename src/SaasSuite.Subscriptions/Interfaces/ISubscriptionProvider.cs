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

namespace SaasSuite.Subscriptions.Interfaces
{
	/// <summary>
	/// Defines the contract for integrating with external subscription and payment providers such as Stripe, PayPal, Paddle, or Chargebee.
	/// </summary>
	/// <remarks>
	/// This interface abstracts external provider operations, enabling the subscription system to work with multiple
	/// payment platforms without changing core business logic. Implementations handle provider-specific API calls,
	/// authentication, error handling, and data mapping. Common implementations include:
	/// <list type="bullet">
	/// <item><description>Stripe provider for credit card subscriptions</description></item>
	/// <item><description>PayPal provider for PayPal-based subscriptions</description></item>
	/// <item><description>Paddle provider for merchant-of-record model</description></item>
	/// <item><description>Mock provider for testing without actual payment processing</description></item>
	/// </list>
	/// Each method returns <see cref="ProviderSubscriptionResult"/> to standardize responses across providers.
	/// </remarks>
	public interface ISubscriptionProvider
	{
		#region ' Methods '

		/// <summary>
		/// Creates a customer record with the external payment provider to enable subscription management.
		/// </summary>
		/// <param name="tenantId">The tenant identifier that will be associated with this customer. Cannot be <see langword="null"/>.</param>
		/// <param name="email">The customer's email address for billing and communication. Cannot be <see langword="null"/> or empty.</param>
		/// <param name="name">Optional customer name or organization name for the billing account. Can be <see langword="null"/>.</param>
		/// <param name="metadata">Optional custom metadata to attach to the customer record as key-value pairs. Can be <see langword="null"/>.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests during the asynchronous operation.</param>
		/// <returns>
		/// A task that represents the asynchronous operation. The task result contains a string with
		/// the provider's unique customer identifier (e.g., Stripe customer ID "cus_xxxxx").
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="tenantId"/> is <see langword="null"/>.
		/// </exception>
		/// <exception cref="ArgumentException">
		/// Thrown when <paramref name="email"/> is <see langword="null"/> or empty.
		/// </exception>
		/// <remarks>
		/// Creating a customer record in the provider's system is typically the first step before creating subscriptions.
		/// The customer object acts as a container for:
		/// <list type="bullet">
		/// <item><description>Payment methods (credit cards, bank accounts, etc.)</description></item>
		/// <item><description>Multiple subscriptions</description></item>
		/// <item><description>Billing history and invoices</description></item>
		/// <item><description>Tax information and addresses</description></item>
		/// </list>
		/// Store the returned customer ID in your tenant record for reuse when creating additional subscriptions
		/// or managing payment methods. The <paramref name="metadata"/> parameter allows linking the provider's
		/// customer record back to your tenant ID and storing additional context. Many providers support
		/// idempotency to prevent duplicate customer creation when retrying failed requests.
		/// </remarks>
		Task<string> CreateCustomerAsync(TenantId tenantId, string email, string? name = null, Dictionary<string, string>? metadata = null, CancellationToken cancellationToken = default);

		/// <summary>
		/// Cancels a subscription with the external payment provider.
		/// </summary>
		/// <param name="providerSubscriptionId">The provider's unique subscription identifier. Cannot be <see langword="null"/> or empty.</param>
		/// <param name="immediately">
		/// <see langword="true"/> to cancel immediately and revoke access;
		/// <see langword="false"/> to cancel at the end of the current billing period (allowing continued access until then).
		/// Defaults to <see langword="false"/>.
		/// </param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests during the asynchronous operation.</param>
		/// <returns>
		/// A task that represents the asynchronous operation. The task result contains a <see cref="ProviderSubscriptionResult"/>
		/// indicating the cancellation outcome and final subscription state.
		/// </returns>
		/// <exception cref="ArgumentException">
		/// Thrown when <paramref name="providerSubscriptionId"/> is <see langword="null"/> or empty.
		/// </exception>
		/// <remarks>
		/// Cancellation behavior depends on the <paramref name="immediately"/> parameter:
		/// <list type="bullet">
		/// <item>
		/// <term>When <see langword="false"/> (default)</term>
		/// <description>
		/// The subscription is marked to cancel at period end.
		/// The customer retains access until <see cref="ProviderSubscriptionResult.CurrentPeriodEnd"/>.
		/// No refund is issued. <see cref="ProviderSubscriptionResult.CancelAtPeriodEnd"/> will be <see langword="true"/>.
		/// </description>
		/// </item>
		/// <item>
		/// <term>When <see langword="true"/></term>
		/// <description>
		/// The subscription ends immediately, access is revoked, and
		/// a partial refund may be issued depending on provider policy and configuration.
		/// </description>
		/// </item>
		/// </list>
		/// After cancellation, no further billing attempts occur. Some providers allow reactivation of
		/// cancelled subscriptions before the period ends. Always update the local subscription status
		/// and cancellation date after this operation completes successfully.
		/// </remarks>
		Task<ProviderSubscriptionResult> CancelSubscriptionAsync(string providerSubscriptionId, bool immediately = false, CancellationToken cancellationToken = default);

		/// <summary>
		/// Creates a new subscription with the external payment provider.
		/// </summary>
		/// <param name="tenantId">The tenant identifier for whom the subscription is being created. Cannot be <see langword="null"/>.</param>
		/// <param name="plan">The subscription plan containing pricing, features, and billing period information. Cannot be <see langword="null"/>.</param>
		/// <param name="customerId">Optional external customer identifier if the customer already exists in the provider's system. Can be <see langword="null"/> to create a new customer.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests during the asynchronous operation.</param>
		/// <returns>
		/// A task that represents the asynchronous operation. The task result contains a <see cref="ProviderSubscriptionResult"/>
		/// with the provider's subscription ID and metadata if successful, or error details if the operation failed.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="tenantId"/> or <paramref name="plan"/> is <see langword="null"/>.
		/// </exception>
		/// <remarks>
		/// This method initiates a subscription with the external provider, typically involving:
		/// <list type="number">
		/// <item><description>Creating or retrieving a customer record in the provider's system</description></item>
		/// <item><description>Setting up the subscription with the specified plan and pricing</description></item>
		/// <item><description>Configuring billing cycle and trial periods if applicable</description></item>
		/// <item><description>Returning the provider's subscription identifier for future reference</description></item>
		/// </list>
		/// If a trial period is defined in the plan, the provider may defer billing until the trial ends.
		/// Payment method collection may happen inline or separately depending on the provider and integration type.
		/// The returned <see cref="ProviderSubscriptionResult.ProviderSubscriptionId"/> should be stored locally
		/// for subsequent operations like updates, cancellations, and webhook processing.
		/// </remarks>
		Task<ProviderSubscriptionResult> CreateSubscriptionAsync(TenantId tenantId, SubscriptionPlan plan, string? customerId = null, CancellationToken cancellationToken = default);

		/// <summary>
		/// Retrieves the current state of a subscription from the external payment provider.
		/// </summary>
		/// <param name="providerSubscriptionId">The provider's unique subscription identifier. Cannot be <see langword="null"/> or empty.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests during the asynchronous operation.</param>
		/// <returns>
		/// A task that represents the asynchronous operation. The task result contains a <see cref="ProviderSubscriptionResult"/>
		/// with the current subscription details including status, billing dates, and metadata.
		/// </returns>
		/// <exception cref="ArgumentException">
		/// Thrown when <paramref name="providerSubscriptionId"/> is <see langword="null"/> or empty.
		/// </exception>
		/// <remarks>
		/// This method fetches fresh subscription data from the provider's API for synchronization purposes.
		/// Use cases include:
		/// <list type="bullet">
		/// <item><description>Webhook validation (verify webhook data matches provider state)</description></item>
		/// <item><description>Periodic synchronization to catch missed webhooks</description></item>
		/// <item><description>Displaying real-time billing information to users</description></item>
		/// <item><description>Reconciliation and auditing of subscription states</description></item>
		/// </list>
		/// The returned result includes current status, billing period dates, cancellation flags, and any
		/// provider-specific metadata. Compare this with local subscription records to detect drift or
		/// manual changes made directly in the provider's dashboard. Implement caching to avoid excessive
		/// API calls when displaying subscription details.
		/// </remarks>
		Task<ProviderSubscriptionResult> GetSubscriptionAsync(string providerSubscriptionId, CancellationToken cancellationToken = default);

		/// <summary>
		/// Updates an existing subscription with the external payment provider, typically to change plans or pricing.
		/// </summary>
		/// <param name="providerSubscriptionId">The provider's unique subscription identifier obtained from <see cref="CreateSubscriptionAsync"/>. Cannot be <see langword="null"/> or empty.</param>
		/// <param name="plan">The new subscription plan to apply. Cannot be <see langword="null"/>.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests during the asynchronous operation.</param>
		/// <returns>
		/// A task that represents the asynchronous operation. The task result contains a <see cref="ProviderSubscriptionResult"/>
		/// indicating success with updated subscription details, or failure with error information.
		/// </returns>
		/// <exception cref="ArgumentException">
		/// Thrown when <paramref name="providerSubscriptionId"/> is <see langword="null"/> or empty.
		/// </exception>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="plan"/> is <see langword="null"/>.
		/// </exception>
		/// <remarks>
		/// Updates typically involve plan changes (upgrades or downgrades) which may affect:
		/// <list type="bullet">
		/// <item><description>Pricing and billing amount</description></item>
		/// <item><description>Billing period and next billing date</description></item>
		/// <item><description>Feature access and limits</description></item>
		/// <item><description>Proration calculations for mid-cycle changes</description></item>
		/// </list>
		/// Most providers support immediate plan changes with proration (charging or crediting the difference)
		/// or scheduled changes at the next billing cycle. The behavior depends on provider configuration
		/// and implementation. After updating, the local subscription record should be synchronized with
		/// the provider's state including updated billing dates and pricing.
		/// </remarks>
		Task<ProviderSubscriptionResult> UpdateSubscriptionAsync(string providerSubscriptionId, SubscriptionPlan plan, CancellationToken cancellationToken = default);

		#endregion
	}
}