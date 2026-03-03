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

using SaasSuite.Billing.Interfaces;

namespace SaasSuite.Billing.Webhooks
{
	/// <summary>
	/// A no-operation implementation of <see cref="IPaymentWebhookHandler"/> that indicates webhooks are not configured.
	/// </summary>
	/// <remarks>
	/// This is the default implementation registered when no specific payment provider webhook handler is provided.
	/// All webhook requests are skipped with an informational message directing developers to register a proper handler.
	/// </remarks>
	public class NoOpPaymentWebhookHandler
		: IPaymentWebhookHandler
	{
		#region ' Methods '

		/// <summary>
		/// Handles a webhook notification by skipping it and returning an informational message.
		/// </summary>
		/// <param name="webhookPayload">The raw webhook payload (ignored by this implementation).</param>
		/// <param name="signature">The webhook signature (ignored by this implementation).</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests during the asynchronous operation (ignored by this implementation).</param>
		/// <returns>
		/// A task that represents the asynchronous operation.
		/// The task result contains a <see cref="WebhookHandlingResult"/> with <see cref="WebhookHandlingResult.Skipped"/>
		/// set to <see langword="true"/> and a message explaining that no handler is configured.
		/// </returns>
		/// <remarks>
		/// This method does not process any webhook data and always returns a skipped result.
		/// </remarks>
		public Task<WebhookHandlingResult> HandleAsync(string webhookPayload, string signature, CancellationToken cancellationToken = default)
		{
			return Task.FromResult(WebhookHandlingResult.SkippedResult(
				"No payment webhook handler is configured. " +
				"Register a provider-specific implementation of IPaymentWebhookHandler to handle webhooks."));
		}

		#endregion
	}
}