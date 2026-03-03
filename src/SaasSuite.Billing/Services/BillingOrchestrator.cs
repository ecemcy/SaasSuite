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

using SaasSuite.Billing.Enumerations;
using SaasSuite.Billing.Interfaces;
using SaasSuite.Billing.Options;
using SaasSuite.Core;
using SaasSuite.Metering;
using SaasSuite.Metering.Enumerations;
using SaasSuite.Metering.Services;
using SaasSuite.Subscriptions;
using SaasSuite.Subscriptions.Services;

namespace SaasSuite.Billing.Services
{
	/// <summary>
	/// Implementation of billing orchestration that coordinates subscriptions, metering, and payments.
	/// </summary>
	/// <remarks>
	/// This orchestrator serves as the central coordination point for all billing operations, managing
	/// the workflow from invoice generation through payment processing and reconciliation.
	/// </remarks>
	public class BillingOrchestrator
		: IBillingOrchestrator
	{
		#region ' Fields '

		/// <summary>
		/// Configuration options controlling billing behavior.
		/// </summary>
		private readonly BillingOptions _options;

		/// <summary>
		/// Service for managing invoice persistence and retrieval.
		/// </summary>
		private readonly InvoiceService _invoiceService;

		/// <summary>
		/// Service for retrieving metered usage data.
		/// </summary>
		private readonly MeteringService _meteringService;

		/// <summary>
		/// Service for managing subscription data and operations.
		/// </summary>
		private readonly SubscriptionService _subscriptionService;

		#endregion

		#region ' Constructors '

		/// <summary>
		/// Initializes a new instance of the <see cref="BillingOrchestrator"/> class.
		/// </summary>
		/// <param name="invoiceService">The invoice service for managing invoice data. Cannot be <see langword="null"/>.</param>
		/// <param name="subscriptionService">The subscription service for accessing subscription data. Cannot be <see langword="null"/>.</param>
		/// <param name="meteringService">The metering service for accessing usage data. Cannot be <see langword="null"/>.</param>
		/// <param name="options">The billing configuration options. Cannot be <see langword="null"/>.</param>
		/// <exception cref="ArgumentNullException">Thrown when any parameter is <see langword="null"/>.</exception>
		public BillingOrchestrator(InvoiceService invoiceService, SubscriptionService subscriptionService, MeteringService meteringService, IOptions<BillingOptions> options)
		{
			this._invoiceService = invoiceService ?? throw new ArgumentNullException(nameof(invoiceService));
			this._subscriptionService = subscriptionService ?? throw new ArgumentNullException(nameof(subscriptionService));
			this._meteringService = meteringService ?? throw new ArgumentNullException(nameof(meteringService));
			this._options = options?.Value ?? throw new ArgumentNullException(nameof(options));
		}

		#endregion

		#region ' Methods '

		/// <summary>
		/// Retrieves the unit price for a specific usage metric.
		/// </summary>
		/// <param name="metric">The metric identifier to get pricing for.</param>
		/// <returns>The unit price for the metric in the default currency.</returns>
		/// <remarks>
		/// This is a simple lookup implementation using a switch expression with hardcoded prices.
		/// Production systems should use configurable pricing tables stored in a database or configuration system,
		/// allowing for dynamic pricing updates without code changes. Consider implementing tiered pricing,
		/// volume discounts, and regional pricing variations.
		/// </remarks>
		private decimal GetUsagePrice(string metric)
		{
			return metric switch
			{
				"api-calls" => 0.001m, // $0.001 per API call
				"storage-gb" => 0.10m, // $0.10 per GB stored
				"bandwidth-gb" => 0.05m, // $0.05 per GB transferred
				"compute-hours" => 0.50m, // $0.50 per compute hour
				_ => 0.01m // Default fallback price for unknown metrics
			};
		}

		/// <summary>
		/// Generates a unique invoice number using the configured prefix and current timestamp.
		/// </summary>
		/// <returns>A unique invoice number string.</returns>
		/// <remarks>
		/// This is a simple timestamp-based implementation using Unix milliseconds to ensure uniqueness.
		/// Production systems should consider using more sophisticated numbering schemes such as sequential
		/// numbers with database sequences, or integrating with existing accounting system numbering conventions.
		/// </remarks>
		private string GenerateInvoiceNumber()
		{
			long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
			return $"{this._options.InvoiceNumberPrefix}{timestamp}";
		}

		/// <summary>
		/// Processes a payment for an invoice using the specified payment method.
		/// </summary>
		/// <param name="invoiceId">The unique identifier of the invoice to apply payment to.</param>
		/// <param name="amount">The payment amount in the invoice's currency.</param>
		/// <param name="paymentMethodId">The identifier of the payment method used for the transaction.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests during the asynchronous operation.</param>
		/// <returns>
		/// A task that represents the asynchronous operation.
		/// The task result contains <see langword="true"/> if payment was successfully processed.
		/// </returns>
		/// <exception cref="InvalidOperationException">
		/// Thrown when the invoice is not found or is in a status that cannot accept payments (not pending or overdue).
		/// </exception>
		/// <remarks>
		/// This operation validates the invoice state, applies the payment amount, updates the invoice status,
		/// and marks it as paid if the full amount is received. Partial payments are supported, with the invoice
		/// remaining in pending or overdue status until fully paid.
		/// </remarks>
		public async Task<bool> ProcessPaymentAsync(string invoiceId, decimal amount, string paymentMethodId, CancellationToken cancellationToken = default)
		{
			// Retrieve the invoice
			Invoice? invoice = await this._invoiceService.GetByIdAsync(invoiceId, cancellationToken)
				?? throw new InvalidOperationException($"Invoice {invoiceId} not found");

			// Validate invoice status allows payment
			if (invoice.Status != InvoiceStatus.Pending && invoice.Status != InvoiceStatus.Overdue)
			{
				throw new InvalidOperationException($"Cannot process payment for invoice in {invoice.Status} status");
			}

			// Apply the payment amount
			invoice.AmountPaid += amount;
			invoice.AmountDue = invoice.Amount - invoice.AmountPaid;

			// Update status if fully paid
			if (invoice.AmountDue <= 0)
			{
				invoice.Status = InvoiceStatus.Paid;
				invoice.PaidDate = DateTimeOffset.UtcNow;
				invoice.AmountDue = 0; // Ensure no negative balance
			}

			invoice.UpdatedAt = DateTimeOffset.UtcNow;
			await this._invoiceService.UpdateAsync(invoice, cancellationToken);

			return true;
		}

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
		/// This operation identifies pending invoices that are past their due date and marks them as overdue.
		/// The reconciliation process helps maintain accurate invoice states and can be run periodically
		/// as a background job to keep all invoice statuses current.
		/// </remarks>
		public async Task<int> ReconcileAsync(TenantId? tenantId = null, CancellationToken cancellationToken = default)
		{
			// Get invoices to reconcile based on tenant filter
			List<Invoice> invoices = tenantId.HasValue
				? await this._invoiceService.GetByTenantIdAsync(tenantId.Value, cancellationToken)
				: await this._invoiceService.GetAllAsync(cancellationToken);

			int reconciledCount = 0;
			DateTimeOffset now = DateTimeOffset.UtcNow;

			// Process each invoice
			foreach (Invoice invoice in invoices)
			{
				bool wasUpdated = false;

				// Mark pending invoices as overdue if past due date
				if (invoice.Status == InvoiceStatus.Pending && invoice.DueDate < now)
				{
					invoice.Status = InvoiceStatus.Overdue;
					wasUpdated = true;
				}

				// Persist updates
				if (wasUpdated)
				{
					invoice.UpdatedAt = now;
					await this._invoiceService.UpdateAsync(invoice, cancellationToken);
					reconciledCount++;
				}
			}

			return reconciledCount;
		}

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
		/// The discount is subtracted from the subtotal before taxes are calculated, and the invoice total is recalculated.
		/// This operation can only be performed on invoices in draft status. Once an invoice is finalized, discounts
		/// can no longer be applied.
		/// </remarks>
		public async Task<Invoice> ApplyDiscountAsync(string invoiceId, decimal discountAmount, CancellationToken cancellationToken = default)
		{
			Invoice? invoice = await this._invoiceService.GetByIdAsync(invoiceId, cancellationToken)
				?? throw new InvalidOperationException($"Invoice {invoiceId} not found");

			// Only draft invoices can be modified
			if (invoice.Status != InvoiceStatus.Draft)
			{
				throw new InvalidOperationException("Cannot apply discount to finalized invoice");
			}

			// Apply discount and recalculate amounts
			invoice.DiscountAmount = discountAmount;
			invoice.Amount = invoice.Subtotal - invoice.DiscountAmount + invoice.TaxAmount;
			invoice.AmountDue = invoice.Amount - invoice.AmountPaid;
			invoice.UpdatedAt = DateTimeOffset.UtcNow;

			await this._invoiceService.UpdateAsync(invoice, cancellationToken);
			return invoice;
		}

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
		/// This operation can only be performed on invoices in draft status. Tax rates should be provided as percentages,
		/// so a 10% tax rate should be passed as 10, not 0.10.
		/// </remarks>
		public async Task<Invoice> ApplyTaxAsync(string invoiceId, decimal taxRate, CancellationToken cancellationToken = default)
		{
			Invoice? invoice = await this._invoiceService.GetByIdAsync(invoiceId, cancellationToken)
				?? throw new InvalidOperationException($"Invoice {invoiceId} not found");

			// Only draft invoices can be modified
			if (invoice.Status != InvoiceStatus.Draft)
			{
				throw new InvalidOperationException("Cannot apply tax to finalized invoice");
			}

			// Calculate tax on taxable amount and update totals
			decimal taxableAmount = invoice.Subtotal - invoice.DiscountAmount;
			invoice.TaxAmount = taxableAmount * (taxRate / 100m);
			invoice.Amount = taxableAmount + invoice.TaxAmount;
			invoice.AmountDue = invoice.Amount - invoice.AmountPaid;
			invoice.UpdatedAt = DateTimeOffset.UtcNow;

			await this._invoiceService.UpdateAsync(invoice, cancellationToken);
			return invoice;
		}

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
		/// This operation applies default taxes if configured and not already applied, locks the invoice from further modifications,
		/// and transitions the status from draft to pending. Once finalized, the invoice can no longer be modified and is
		/// ready to be sent to the customer for payment.
		/// </remarks>
		public async Task<Invoice> FinalizeInvoiceAsync(string invoiceId, CancellationToken cancellationToken = default)
		{
			Invoice? invoice = await this._invoiceService.GetByIdAsync(invoiceId, cancellationToken)
				?? throw new InvalidOperationException($"Invoice {invoiceId} not found");

			if (invoice.Status != InvoiceStatus.Draft)
			{
				throw new InvalidOperationException($"Cannot finalize invoice in {invoice.Status} status");
			}

			// Apply default tax if configured and not already applied
			if (this._options.DefaultTaxRate > 0 && invoice.TaxAmount == 0)
			{
				await this.ApplyTaxAsync(invoiceId, this._options.DefaultTaxRate, cancellationToken);
				// Retrieve updated invoice after tax application
				invoice = await this._invoiceService.GetByIdAsync(invoiceId, cancellationToken)
					?? throw new InvalidOperationException($"Invoice {invoiceId} not found after applying tax");
			}

			// Transition to pending status
			invoice.Status = InvoiceStatus.Pending;
			invoice.UpdatedAt = DateTimeOffset.UtcNow;

			await this._invoiceService.UpdateAsync(invoice, cancellationToken);
			return invoice;
		}

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
		/// This operation creates line items for subscription fees and usage charges, applies proration if configured,
		/// and optionally finalizes the invoice based on settings. The invoice is initially created in draft status
		/// and may be automatically finalized based on the <see cref="BillingOptions.AutoFinalizeInvoices"/> setting.
		/// </remarks>
		public async Task<Invoice> GenerateInvoiceAsync(TenantId tenantId, BillingCycle billingCycle, CancellationToken cancellationToken = default)
		{
			// Retrieve the tenant's active subscription
			Subscription? subscription = await this._subscriptionService.GetActiveSubscriptionAsync(tenantId, cancellationToken)
				?? throw new InvalidOperationException($"No active subscription found for tenant {tenantId}");

			// Create the invoice with basic information
			Invoice invoice = new Invoice
			{
				TenantId = tenantId,
				SubscriptionId = subscription.Id,
				InvoiceNumber = this.GenerateInvoiceNumber(),
				PeriodStart = billingCycle.StartDate,
				PeriodEnd = billingCycle.EndDate,
				DueDate = billingCycle.EndDate.AddDays(this._options.DefaultPaymentTermsDays),
				Currency = this._options.DefaultCurrency,
				Status = InvoiceStatus.Draft
			};

			// Add subscription line item if plan exists
			SubscriptionPlan? subscriptionPlan = await this._subscriptionService.GetPlanAsync(subscription.PlanId, cancellationToken);
			if (subscriptionPlan != null)
			{
				InvoiceLineItem subscriptionLineItem = new InvoiceLineItem
				{
					Description = $"Subscription: {subscriptionPlan.Name}",
					Quantity = 1,
					UnitPrice = subscriptionPlan.Price,
					Amount = subscriptionPlan.Price,
					PeriodStart = billingCycle.StartDate,
					PeriodEnd = billingCycle.EndDate
				};

				// Apply proration if enabled
				if (this._options.ProrationEnabled)
				{
					decimal prorationFactor = billingCycle.CalculateProrationFactor(
						subscription.StartDate,
						subscription.EndDate ?? billingCycle.EndDate);
					subscriptionLineItem.Amount = subscriptionLineItem.UnitPrice * prorationFactor;
				}

				invoice.LineItems.Add(subscriptionLineItem);
			}

			// Add usage-based charges if enabled
			if (this._options.IncludeUsageCharges)
			{
				IEnumerable<UsageAggregation> usageData = await this._meteringService.GetAggregatedUsageAsync(
					tenantId,
					billingCycle.StartDate,
					billingCycle.EndDate,
					AggregationPeriod.Daily,
					null,
					cancellationToken);

				// Create line items for each metered usage type
				foreach (UsageAggregation usage in usageData)
				{
					InvoiceLineItem usageLineItem = new InvoiceLineItem
					{
						Description = $"Usage: {usage.Metric}",
						Quantity = usage.TotalValue,
						UnitPrice = this.GetUsagePrice(usage.Metric),
						Amount = usage.TotalValue * this.GetUsagePrice(usage.Metric),
						Metric = usage.Metric,
						PeriodStart = billingCycle.StartDate,
						PeriodEnd = billingCycle.EndDate
					};

					invoice.LineItems.Add(usageLineItem);
				}
			}

			// Calculate invoice totals
			invoice.Subtotal = invoice.LineItems.Sum(li => li.Amount);
			invoice.Amount = invoice.Subtotal;
			invoice.AmountDue = invoice.Amount;

			// Persist the invoice
			await this._invoiceService.CreateAsync(invoice, cancellationToken);

			// Auto-finalize if configured
			if (this._options.AutoFinalizeInvoices)
			{
				_ = await this.FinalizeInvoiceAsync(invoice.InvoiceId, cancellationToken);
			}

			return invoice;
		}

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
		/// This operation reverses payment tracking, increases the amount due, and transitions the invoice
		/// to refunded status if the full amount is refunded. Partial refunds are supported, and the invoice
		/// will remain in paid status if only a portion of the payment is refunded.
		/// </remarks>
		public async Task<Invoice> RefundInvoiceAsync(string invoiceId, decimal? amount = null, CancellationToken cancellationToken = default)
		{
			Invoice? invoice = await this._invoiceService.GetByIdAsync(invoiceId, cancellationToken)
				?? throw new InvalidOperationException($"Invoice {invoiceId} not found");

			// Only paid invoices can be refunded
			if (invoice.Status != InvoiceStatus.Paid)
			{
				throw new InvalidOperationException($"Cannot refund invoice in {invoice.Status} status");
			}

			// Default to full refund if no amount specified
			decimal refundAmount = amount ?? invoice.AmountPaid;
			if (refundAmount > invoice.AmountPaid)
			{
				throw new InvalidOperationException("Refund amount cannot exceed amount paid");
			}

			// Apply refund and update balances
			invoice.AmountPaid -= refundAmount;
			invoice.AmountDue += refundAmount;

			// Mark as refunded if fully refunded
			if (invoice.AmountPaid == 0)
			{
				invoice.Status = InvoiceStatus.Refunded;
			}

			invoice.UpdatedAt = DateTimeOffset.UtcNow;
			await this._invoiceService.UpdateAsync(invoice, cancellationToken);

			return invoice;
		}

		#endregion
	}
}