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

using Microsoft.Extensions.Options;

using SaasSuite.Billing.Interfaces;
using SaasSuite.Billing.Options;

namespace SaasSuite.Billing.Services
{
	/// <summary>
	/// Background job for reconciling invoices, processing payments, and handling webhook notifications.
	/// </summary>
	/// <remarks>
	/// This service coordinates invoice status updates, payment reconciliation, and webhook verification
	/// for a complete payment processing workflow.
	/// </remarks>
	public class ReconciliationJob
	{
		#region ' Fields '

		/// <summary>
		/// Configuration options controlling reconciliation behavior.
		/// </summary>
		private readonly BillingOptions _options;

		/// <summary>
		/// The billing orchestrator for executing invoice and payment operations.
		/// </summary>
		private readonly IBillingOrchestrator _billingOrchestrator;

		/// <summary>
		/// Handler for processing payment webhooks from external providers.
		/// </summary>
		private readonly IPaymentWebhookHandler _webhookHandler;

		/// <summary>
		/// Verifier for validating webhook signatures.
		/// </summary>
		private readonly IWebhookSignatureVerifier _signatureVerifier;

		#endregion

		#region ' Constructors '

		/// <summary>
		/// Initializes a new instance of the <see cref="ReconciliationJob"/> class.
		/// </summary>
		/// <param name="billingOrchestrator">The billing orchestrator for managing billing operations. Cannot be <see langword="null"/>.</param>
		/// <param name="options">The billing configuration options. Cannot be <see langword="null"/>.</param>
		/// <param name="webhookHandler">The webhook handler for processing payment notifications. Cannot be <see langword="null"/>.</param>
		/// <param name="signatureVerifier">The signature verifier for validating webhooks. Cannot be <see langword="null"/>.</param>
		/// <exception cref="ArgumentNullException">Thrown when any parameter is <see langword="null"/>.</exception>
		public ReconciliationJob(
			IBillingOrchestrator billingOrchestrator,
			IOptions<BillingOptions> options,
			IPaymentWebhookHandler webhookHandler,
			IWebhookSignatureVerifier signatureVerifier)
		{
			this._billingOrchestrator = billingOrchestrator ?? throw new ArgumentNullException(nameof(billingOrchestrator));
			this._options = options?.Value ?? throw new ArgumentNullException(nameof(options));
			this._webhookHandler = webhookHandler ?? throw new ArgumentNullException(nameof(webhookHandler));
			this._signatureVerifier = signatureVerifier ?? throw new ArgumentNullException(nameof(signatureVerifier));
		}

		#endregion

		#region ' Methods '

		/// <summary>
		/// Processes a payment notification received from a webhook, applying the payment to the specified invoice.
		/// </summary>
		/// <param name="invoiceId">The unique identifier of the invoice to apply payment to.</param>
		/// <param name="amount">The payment amount received, in the invoice's currency.</param>
		/// <param name="paymentMethodId">The identifier of the payment method used for the transaction.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests during the asynchronous operation.</param>
		/// <returns>A task that represents the asynchronous operation.</returns>
		/// <remarks>
		/// This method updates the invoice's payment tracking and status based on the payment amount received.
		/// </remarks>
		public async Task ProcessPaymentNotificationAsync(string invoiceId, decimal amount, string paymentMethodId, CancellationToken cancellationToken = default)
		{
			_ = await this._billingOrchestrator.ProcessPaymentAsync(
				invoiceId,
				amount,
				paymentMethodId,
				cancellationToken);
		}

		/// <summary>
		/// Executes the reconciliation job, updating invoice statuses based on current date and payment status.
		/// </summary>
		/// <param name="cancellationToken">A token to monitor for cancellation requests during the asynchronous operation.</param>
		/// <returns>
		/// A task that represents the asynchronous operation.
		/// The task result contains the number of invoices that were reconciled and updated,
		/// or 0 if auto-reconciliation is disabled.
		/// </returns>
		/// <remarks>
		/// This method checks all invoices and marks pending invoices as overdue if past their due date.
		/// The reconciliation only runs if <see cref="BillingOptions.AutoReconcilePayments"/> is enabled.
		/// </remarks>
		public async Task<int> ExecuteAsync(CancellationToken cancellationToken = default)
		{
			// Skip reconciliation if not configured
			if (!this._options.AutoReconcilePayments)
			{
				return 0;
			}

			// Reconcile all tenants
			int reconciledCount = await this._billingOrchestrator.ReconcileAsync(null, cancellationToken);
			return reconciledCount;
		}

		/// <summary>
		/// Verifies the cryptographic signature of a webhook payload to ensure authenticity.
		/// </summary>
		/// <param name="payload">The raw webhook payload string to verify.</param>
		/// <param name="signature">The signature value to validate against the payload.</param>
		/// <returns>
		/// <see langword="true"/> if the signature is valid and the webhook is authentic;
		/// <see langword="false"/> if the signature is invalid or no webhook secret is configured.
		/// </returns>
		/// <remarks>
		/// This method uses the configured webhook secret from <see cref="BillingOptions"/> to validate the signature.
		/// </remarks>
		public bool VerifyWebhookSignature(string payload, string signature)
		{
			// Cannot verify without a configured secret
			if (string.IsNullOrWhiteSpace(this._options.WebhookSecret))
			{
				return false;
			}

			return this._signatureVerifier.VerifySignature(payload, signature, this._options.WebhookSecret);
		}

		/// <summary>
		/// Handles a payment webhook notification from an external payment provider.
		/// </summary>
		/// <param name="webhookPayload">The raw webhook payload received from the payment provider, typically in JSON format.</param>
		/// <param name="signature">The signature header provided by the payment provider for webhook verification.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests during the asynchronous operation.</param>
		/// <returns>
		/// A task that represents the asynchronous operation.
		/// The task result contains a <see cref="WebhookHandlingResult"/> indicating the outcome of webhook processing.
		/// </returns>
		/// <remarks>
		/// This method delegates webhook processing to the configured <see cref="IPaymentWebhookHandler"/> implementation.
		/// </remarks>
		public Task<WebhookHandlingResult> HandleWebhookAsync(string webhookPayload, string signature, CancellationToken cancellationToken = default)
		{
			return this._webhookHandler.HandleAsync(webhookPayload, signature, cancellationToken);
		}

		#endregion
	}
}