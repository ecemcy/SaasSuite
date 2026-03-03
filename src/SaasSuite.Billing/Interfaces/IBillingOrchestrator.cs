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

namespace SaasSuite.Billing.Interfaces
{
	/// <summary>
	/// Defines the contract for orchestrating complex billing workflows across subscriptions, metering, and payments.
	/// </summary>
	/// <remarks>
	/// The billing orchestrator coordinates invoice generation, payment processing, tax application, discounts,
	/// and reconciliation operations to provide a complete billing system.
	/// </remarks>
	public interface IBillingOrchestrator
	{
		#region ' Methods '

		/// <summary>
		/// Processes a payment for an invoice using the specified payment method.
		/// </summary>
		/// <param name="invoiceId">The unique identifier of the invoice to apply payment to.</param>
		/// <param name="amount">The payment amount in the invoice's currency.</param>
		/// <param name="paymentMethodId">The identifier of the payment method used for the transaction.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests during the asynchronous operation.</param>
		/// <returns>
		/// A task that represents the asynchronous operation.
		/// The task result contains <see langword="true"/> if payment was successfully processed; otherwise, <see langword="false"/>.
		/// </returns>
		/// <exception cref="InvalidOperationException">Thrown when the invoice is not found or is in a status that cannot accept payments.</exception>
		/// <remarks>
		/// This operation updates the invoice's payment tracking, adjusts the amount due,
		/// and transitions the invoice status to paid if the full amount is received.
		/// </remarks>
		Task<bool> ProcessPaymentAsync(string invoiceId, decimal amount, string paymentMethodId, CancellationToken cancellationToken = default);

		/// <summary>
		/// Reconciles payments and invoices, updating their statuses based on current date and payment status.
		/// </summary>
		/// <param name="tenantId">
		/// The unique identifier of the tenant to reconcile, or <see langword="null"/> to reconcile all tenants.
		/// </param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests during the asynchronous operation.</param>
		/// <returns>
		/// A task that represents the asynchronous operation.
		/// The task result contains the number of invoices that were updated during reconciliation.
		/// </returns>
		/// <remarks>
		/// This operation identifies overdue invoices, validates payment records, and ensures invoice statuses
		/// accurately reflect the current payment state.
		/// </remarks>
		Task<int> ReconcileAsync(TenantId? tenantId = null, CancellationToken cancellationToken = default);

		/// <summary>
		/// Applies a discount to a draft invoice, reducing the amount due.
		/// </summary>
		/// <param name="invoiceId">The unique identifier of the invoice to apply the discount to.</param>
		/// <param name="discountAmount">The discount amount to subtract from the subtotal, in the invoice's currency.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests during the asynchronous operation.</param>
		/// <returns>
		/// A task that represents the asynchronous operation.
		/// The task result contains the updated <see cref="Invoice"/> with the discount applied.
		/// </returns>
		/// <exception cref="InvalidOperationException">Thrown when the invoice is not found or is not in draft status.</exception>
		/// <remarks>
		/// The discount is applied to the subtotal before taxes are calculated.
		/// This operation can only be performed on invoices in draft status.
		/// </remarks>
		Task<Invoice> ApplyDiscountAsync(string invoiceId, decimal discountAmount, CancellationToken cancellationToken = default);

		/// <summary>
		/// Applies taxes to a draft invoice based on the specified tax rate.
		/// </summary>
		/// <param name="invoiceId">The unique identifier of the invoice to apply taxes to.</param>
		/// <param name="taxRate">The tax rate to apply as a percentage (e.g., 10 for 10%, 8.5 for 8.5%).</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests during the asynchronous operation.</param>
		/// <returns>
		/// A task that represents the asynchronous operation.
		/// The task result contains the updated <see cref="Invoice"/> with taxes applied.
		/// </returns>
		/// <exception cref="InvalidOperationException">Thrown when the invoice is not found or is not in draft status.</exception>
		/// <remarks>
		/// The tax is calculated on the taxable amount (subtotal minus discounts) and added to the invoice total.
		/// This operation can only be performed on invoices in draft status.
		/// </remarks>
		Task<Invoice> ApplyTaxAsync(string invoiceId, decimal taxRate, CancellationToken cancellationToken = default);

		/// <summary>
		/// Finalizes a draft invoice, making it ready for payment and visible to customers.
		/// </summary>
		/// <param name="invoiceId">The unique identifier of the invoice to finalize.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests during the asynchronous operation.</param>
		/// <returns>
		/// A task that represents the asynchronous operation.
		/// The task result contains the finalized <see cref="Invoice"/>.
		/// </returns>
		/// <exception cref="InvalidOperationException">Thrown when the invoice is not found or is not in draft status.</exception>
		/// <remarks>
		/// This operation applies default taxes if configured, locks the invoice from further modifications,
		/// and transitions the status from draft to pending.
		/// </remarks>
		Task<Invoice> FinalizeInvoiceAsync(string invoiceId, CancellationToken cancellationToken = default);

		/// <summary>
		/// Generates an invoice for a tenant based on their active subscription and usage data.
		/// </summary>
		/// <param name="tenantId">The unique identifier of the tenant for whom to generate the invoice.</param>
		/// <param name="billingCycle">The billing cycle defining the time period covered by the invoice.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests during the asynchronous operation.</param>
		/// <returns>
		/// A task that represents the asynchronous operation.
		/// The task result contains the newly generated <see cref="Invoice"/>.
		/// </returns>
		/// <exception cref="InvalidOperationException">Thrown when no active subscription exists for the tenant.</exception>
		/// <remarks>
		/// This operation aggregates subscription fees, usage-based charges, applies proration if configured,
		/// and creates a complete invoice for the specified billing cycle.
		/// </remarks>
		Task<Invoice> GenerateInvoiceAsync(TenantId tenantId, BillingCycle billingCycle, CancellationToken cancellationToken = default);

		/// <summary>
		/// Refunds a paid invoice, returning the specified amount to the customer.
		/// </summary>
		/// <param name="invoiceId">The unique identifier of the invoice to refund.</param>
		/// <param name="amount">
		/// The amount to refund in the invoice's currency, or <see langword="null"/> to issue a full refund
		/// of the entire amount paid.
		/// </param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests during the asynchronous operation.</param>
		/// <returns>
		/// A task that represents the asynchronous operation.
		/// The task result contains the updated <see cref="Invoice"/> after the refund.
		/// </returns>
		/// <exception cref="InvalidOperationException">
		/// Thrown when the invoice is not found, is not in paid status, or the refund amount exceeds the amount paid.
		/// </exception>
		/// <remarks>
		/// This operation reverses payment tracking, adjusts the amount due, and transitions the invoice
		/// to refunded status if the full amount is refunded.
		/// </remarks>
		Task<Invoice> RefundInvoiceAsync(string invoiceId, decimal? amount = null, CancellationToken cancellationToken = default);

		#endregion
	}
}