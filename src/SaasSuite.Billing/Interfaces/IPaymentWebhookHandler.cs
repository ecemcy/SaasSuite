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

namespace SaasSuite.Billing.Interfaces
{
	/// <summary>
	/// Defines a contract for handling payment webhook notifications from external payment providers.
	/// </summary>
	/// <remarks>
	/// Implementations of this interface process incoming webhook events such as payment confirmations,
	/// failures, refunds, or subscription changes from services like Stripe, PayPal, or other payment gateways.
	/// </remarks>
	public interface IPaymentWebhookHandler
	{
		#region ' Methods '

		/// <summary>
		/// Handles a webhook notification from a payment provider.
		/// </summary>
		/// <param name="webhookPayload">The raw webhook payload received from the payment provider, typically in JSON format.</param>
		/// <param name="signature">The signature header provided by the payment provider for webhook verification.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests during the asynchronous operation.</param>
		/// <returns>
		/// A task that represents the asynchronous operation.
		/// The task result contains a <see cref="WebhookHandlingResult"/> indicating whether the webhook was successfully processed, failed, or skipped.
		/// </returns>
		/// <remarks>
		/// This method should verify the webhook authenticity, parse the payload, and update the system accordingly.
		/// </remarks>
		Task<WebhookHandlingResult> HandleAsync(string webhookPayload, string signature, CancellationToken cancellationToken = default);

		#endregion
	}
}