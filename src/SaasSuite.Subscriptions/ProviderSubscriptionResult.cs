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

namespace SaasSuite.Subscriptions
{
	/// <summary>
	/// Represents the result of an operation performed on an external subscription provider (e.g., Stripe, PayPal, Paddle).
	/// </summary>
	/// <remarks>
	/// This class standardizes the response format from different payment providers, making it easier to integrate
	/// with multiple billing platforms without changing core subscription logic. It supports both successful operations
	/// (returning subscription details) and failures (returning error information). Factory methods <see cref="Successful"/>
	/// and <see cref="Failure"/> provide a convenient way to construct result objects.
	/// </remarks>
	public class ProviderSubscriptionResult
	{
		#region ' Properties '

		/// <summary>
		/// Gets or sets a value indicating whether the subscription is scheduled to cancel at the end of the current period.
		/// </summary>
		/// <value>
		/// <see langword="true"/> if the subscription will end at <see cref="CurrentPeriodEnd"/> without renewing;
		/// <see langword="false"/> if it will automatically renew. Defaults to <see langword="false"/>.
		/// </value>
		/// <remarks>
		/// This flag indicates "cancel at period end" behavior where the user has cancelled but retains access
		/// until the paid period expires. Providers like Stripe support this mode to honor the customer's
		/// remaining paid time. When <see langword="true"/>, no further billing will occur after the current period.
		/// </remarks>
		public bool CancelAtPeriodEnd { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether the provider operation completed successfully.
		/// </summary>
		/// <value>
		/// <see langword="true"/> if the operation succeeded; <see langword="false"/> if it failed.
		/// </value>
		/// <remarks>
		/// Check this property first to determine whether to process the subscription data or handle the error.
		/// When <see langword="true"/>, <see cref="ProviderSubscriptionId"/> should contain a valid value.
		/// When <see langword="false"/>, <see cref="ErrorMessage"/> should explain the failure reason.
		/// </remarks>
		public bool Success { get; set; }

		/// <summary>
		/// Gets or sets the provider-specific error code if the operation failed.
		/// </summary>
		/// <value>
		/// A string containing the provider's error code (e.g., "card_declined", "resource_missing"),
		/// or <see langword="null"/> if the operation succeeded or no code is available.
		/// </value>
		/// <remarks>
		/// Error codes enable programmatic error handling and specific retry logic based on error type.
		/// Different providers use different code formats. Use this to differentiate between transient
		/// errors (retry) and permanent errors (notify user). Common codes include payment failures,
		/// invalid requests, and authentication errors.
		/// </remarks>
		public string? ErrorCode { get; set; }

		/// <summary>
		/// Gets or sets the human-readable error message if the operation failed.
		/// </summary>
		/// <value>
		/// A string describing what went wrong,
		/// or <see langword="null"/> if the operation succeeded.
		/// </value>
		/// <remarks>
		/// When <see cref="Success"/> is <see langword="false"/>, this property should contain a meaningful
		/// error message suitable for logging or, after sanitization, displaying to users. Common errors include
		/// payment failures, invalid parameters, rate limiting, or network issues. Always check this when
		/// handling failed operations.
		/// </remarks>
		public string? ErrorMessage { get; set; }

		/// <summary>
		/// Gets or sets the unique customer identifier from the external payment provider.
		/// </summary>
		/// <value>
		/// A string containing the provider's customer ID (e.g., Stripe customer ID like "cus_xxxxx"),
		/// or <see langword="null"/> if not applicable or the operation failed.
		/// </value>
		/// <remarks>
		/// Many payment providers organize subscriptions under a customer entity. This ID represents that customer
		/// in the provider's system. Store this alongside the tenant record for future subscription operations.
		/// Useful when creating multiple subscriptions for the same tenant or managing payment methods.
		/// </remarks>
		public string? ProviderCustomerId { get; set; }

		/// <summary>
		/// Gets or sets the unique subscription identifier from the external payment provider.
		/// </summary>
		/// <value>
		/// A string containing the provider's subscription ID (e.g., Stripe subscription ID like "sub_xxxxx"),
		/// or <see langword="null"/> if the operation failed or doesn't return a subscription ID.
		/// </value>
		/// <remarks>
		/// This ID is used for subsequent operations like updates, cancellations, and synchronization with the provider.
		/// Store this value in your local subscription record to maintain the link to the external subscription.
		/// Different providers use different ID formats, so treat this as an opaque string.
		/// </remarks>
		public string? ProviderSubscriptionId { get; set; }

		/// <summary>
		/// Gets or sets the subscription status as reported by the external provider.
		/// </summary>
		/// <value>
		/// A string representing the provider's status (e.g., "active", "trialing", "past_due", "canceled"),
		/// or <see langword="null"/> if not available.
		/// </value>
		/// <remarks>
		/// Provider status values may not directly map to <see cref="SubscriptionStatus"/>.
		/// Translation logic should convert provider-specific statuses to the application's status enum.
		/// Common statuses include variations of active, trial, past due, cancelled, and incomplete.
		/// </remarks>
		public string? Status { get; set; }

		/// <summary>
		/// Gets or sets the end date of the current billing period.
		/// </summary>
		/// <value>
		/// A <see cref="DateTimeOffset"/> indicating when the current billing cycle ends and renewal will occur,
		/// or <see langword="null"/> if not applicable or not provided.
		/// </value>
		/// <remarks>
		/// This date represents when the next billing charge will occur for recurring subscriptions.
		/// For one-time purchases or cancelled subscriptions set to end at period end, this indicates when access terminates.
		/// Useful for calculating days remaining in the current period and scheduling renewal notifications.
		/// </remarks>
		public DateTimeOffset? CurrentPeriodEnd { get; set; }

		/// <summary>
		/// Gets or sets the start date of the current billing period.
		/// </summary>
		/// <value>
		/// A <see cref="DateTimeOffset"/> indicating when the current billing cycle began,
		/// or <see langword="null"/> if not provided by the provider.
		/// </value>
		/// <remarks>
		/// Used to synchronize billing period information between the provider and local subscription records.
		/// Helpful for prorating charges, understanding billing cycles, and displaying billing information to users.
		/// Typically represents the date the subscription started or the most recent renewal date.
		/// </remarks>
		public DateTimeOffset? CurrentPeriodStart { get; set; }

		/// <summary>
		/// Gets or sets additional provider-specific metadata as key-value pairs.
		/// </summary>
		/// <value>
		/// A dictionary containing custom metadata from the provider,
		/// or <see langword="null"/> if no metadata is available.
		/// </value>
		/// <remarks>
		/// Providers often allow custom metadata to be attached to subscriptions for integration purposes.
		/// This can include invoice URLs, payment method details, promotional codes, or any custom fields
		/// your integration requires. The contents are provider-specific and should be documented per integration.
		/// </remarks>
		public Dictionary<string, string>? Metadata { get; set; }

		#endregion

		#region ' Static Methods '

		/// <summary>
		/// Creates a failed provider operation result with error details.
		/// </summary>
		/// <param name="errorMessage">A description of what went wrong. Cannot be <see langword="null"/> or empty.</param>
		/// <param name="errorCode">Optional provider-specific error code. Can be <see langword="null"/>.</param>
		/// <returns>
		/// A <see cref="ProviderSubscriptionResult"/> with <see cref="Success"/> set to <see langword="false"/>
		/// and populated with the provided error information.
		/// </returns>
		/// <remarks>
		/// This factory method simplifies creating failure results and ensures the Success flag is properly set.
		/// Use this when provider operations fail to communicate the error details. The error message should
		/// be descriptive enough for logging and debugging, while the error code enables programmatic handling.
		/// Always log failure results for troubleshooting provider integration issues.
		/// </remarks>
		public static ProviderSubscriptionResult Failure(string errorMessage, string? errorCode = null)
		{
			return new ProviderSubscriptionResult
			{
				Success = false,
				ErrorMessage = errorMessage,
				ErrorCode = errorCode
			};
		}

		/// <summary>
		/// Creates a successful provider operation result with subscription details.
		/// </summary>
		/// <param name="providerSubscriptionId">The provider's subscription identifier. Cannot be <see langword="null"/> or empty.</param>
		/// <param name="providerCustomerId">Optional provider's customer identifier. Can be <see langword="null"/>.</param>
		/// <param name="status">Optional subscription status from the provider. Can be <see langword="null"/>.</param>
		/// <param name="currentPeriodStart">Optional start date of the current billing period. Can be <see langword="null"/>.</param>
		/// <param name="currentPeriodEnd">Optional end date of the current billing period. Can be <see langword="null"/>.</param>
		/// <returns>
		/// A <see cref="ProviderSubscriptionResult"/> with <see cref="Success"/> set to <see langword="true"/>
		/// and populated with the provided subscription details.
		/// </returns>
		/// <remarks>
		/// This factory method simplifies creating success results and ensures the Success flag is properly set.
		/// Use this when provider operations complete successfully to return subscription information.
		/// The provider subscription ID is required as it's essential for tracking the external subscription.
		/// </remarks>
		public static ProviderSubscriptionResult Successful(string providerSubscriptionId, string? providerCustomerId = null, string? status = null, DateTimeOffset? currentPeriodStart = null, DateTimeOffset? currentPeriodEnd = null)
		{
			return new ProviderSubscriptionResult
			{
				Success = true,
				ProviderSubscriptionId = providerSubscriptionId,
				ProviderCustomerId = providerCustomerId,
				Status = status,
				CurrentPeriodStart = currentPeriodStart,
				CurrentPeriodEnd = currentPeriodEnd
			};
		}

		#endregion
	}
}