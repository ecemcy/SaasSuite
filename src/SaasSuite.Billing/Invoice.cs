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

using SaasSuite.Billing.Enumerations;
using SaasSuite.Core;

namespace SaasSuite.Billing
{
	/// <summary>
	/// Represents an invoice in a multi-tenant SaaS application.
	/// </summary>
	/// <remarks>
	/// An invoice contains line items for subscription fees, usage charges, and other billable items,
	/// along with payment tracking information and lifecycle status. Invoices progress through various
	/// states from draft to paid, and track all financial details including taxes, discounts, and payments.
	/// </remarks>
	public class Invoice
	{
		#region ' Properties '

		/// <summary>
		/// Gets or sets the total amount due for this invoice.
		/// </summary>
		/// <value>The total invoice amount in the invoice's currency.</value>
		/// <remarks>
		/// This is calculated as: <see cref="Subtotal"/> - <see cref="DiscountAmount"/> + <see cref="TaxAmount"/>.
		/// </remarks>
		public decimal Amount { get; set; }

		/// <summary>
		/// Gets or sets the remaining balance due on this invoice.
		/// </summary>
		/// <value>The outstanding balance in the invoice's currency.</value>
		/// <remarks>
		/// This is calculated as: <see cref="Amount"/> - <see cref="AmountPaid"/>.
		/// When this value reaches 0, the invoice status changes to <see cref="InvoiceStatus.Paid"/>.
		/// </remarks>
		public decimal AmountDue { get; set; }

		/// <summary>
		/// Gets or sets the amount that has been paid towards this invoice.
		/// </summary>
		/// <value>The amount paid in the invoice's currency. Defaults to 0.</value>
		/// <remarks>
		/// This tracks partial and full payments received, and is used to calculate the remaining balance.
		/// </remarks>
		public decimal AmountPaid { get; set; }

		/// <summary>
		/// Gets or sets the total discount amount applied to this invoice.
		/// </summary>
		/// <value>The discount amount in the invoice's currency. Defaults to 0.</value>
		/// <remarks>
		/// This represents invoice-level discounts that reduce the subtotal before taxes are calculated.
		/// </remarks>
		public decimal DiscountAmount { get; set; }

		/// <summary>
		/// Gets or sets the subtotal amount before taxes and discounts are applied.
		/// </summary>
		/// <value>The subtotal amount in the invoice's currency.</value>
		/// <remarks>
		/// This is the sum of all line item amounts before any invoice-level adjustments.
		/// </remarks>
		public decimal Subtotal { get; set; }

		/// <summary>
		/// Gets or sets the total tax amount applied to this invoice.
		/// </summary>
		/// <value>The tax amount in the invoice's currency. Defaults to 0.</value>
		/// <remarks>
		/// This represents invoice-level taxes calculated on the taxable amount (subtotal minus discounts).
		/// </remarks>
		public decimal TaxAmount { get; set; }

		/// <summary>
		/// Gets or sets the three-letter ISO 4217 currency code for all monetary values in this invoice.
		/// </summary>
		/// <value>The currency code (e.g., "USD", "EUR", "GBP"). Defaults to "USD".</value>
		/// <remarks>
		/// All amounts (<see cref="Amount"/>, <see cref="Subtotal"/>, etc.) are denominated in this currency.
		/// </remarks>
		public string Currency { get; set; } = "USD";

		/// <summary>
		/// Gets or sets the unique identifier for this invoice.
		/// </summary>
		/// <value>A globally unique identifier string. Defaults to a new <see cref="Guid"/> when created.</value>
		/// <remarks>
		/// This identifier is used internally to track and reference the invoice throughout the billing system.
		/// </remarks>
		public string InvoiceId { get; set; } = Guid.NewGuid().ToString();

		/// <summary>
		/// Gets or sets the invoice number, which is a human-readable identifier displayed to customers.
		/// </summary>
		/// <value>The invoice number string. Defaults to <see cref="string.Empty"/>.</value>
		/// <remarks>
		/// This number is typically sequential or formatted according to business requirements (e.g., INV-2024-0001).
		/// </remarks>
		public string InvoiceNumber { get; set; } = string.Empty;

		/// <summary>
		/// Gets or sets optional notes or description text for this invoice.
		/// </summary>
		/// <value>The notes text, or <see langword="null"/> if no notes are attached.</value>
		/// <remarks>
		/// This can include special instructions, payment terms, or other information displayed to the customer.
		/// </remarks>
		public string? Notes { get; set; }

		/// <summary>
		/// Gets or sets the subscription identifier associated with this invoice.
		/// </summary>
		/// <value>
		/// The subscription identifier string, or <see langword="null"/> if the invoice is not associated with a subscription.
		/// </value>
		/// <remarks>
		/// This links the invoice to a specific subscription when billing for recurring services.
		/// </remarks>
		public string? SubscriptionId { get; set; }

		/// <summary>
		/// Gets or sets the date and time when the invoice record was created in the system.
		/// </summary>
		/// <value>The creation date and time in UTC. Defaults to the current UTC time when created.</value>
		/// <remarks>
		/// This timestamp is set when the invoice is first instantiated and does not change.
		/// </remarks>
		public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

		/// <summary>
		/// Gets or sets the date by which payment is due for this invoice.
		/// </summary>
		/// <value>The payment due date and time in UTC.</value>
		/// <remarks>
		/// After this date, the invoice may transition to <see cref="InvoiceStatus.Overdue"/> status.
		/// </remarks>
		public DateTimeOffset DueDate { get; set; }

		/// <summary>
		/// Gets or sets the date and time when this invoice was issued to the customer.
		/// </summary>
		/// <value>The issuance date and time in UTC. Defaults to the current UTC time when created.</value>
		/// <remarks>
		/// This is typically when the invoice transitions from draft status to a finalized state.
		/// </remarks>
		public DateTimeOffset IssuedDate { get; set; } = DateTimeOffset.UtcNow;

		/// <summary>
		/// Gets or sets the date and time when the invoice was last modified.
		/// </summary>
		/// <value>The last update date and time in UTC. Defaults to the current UTC time when created.</value>
		/// <remarks>
		/// This timestamp is updated whenever any invoice property changes, providing an audit trail.
		/// </remarks>
		public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

		/// <summary>
		/// Gets or sets the date when this invoice was fully paid.
		/// </summary>
		/// <value>
		/// The payment date and time in UTC, or <see langword="null"/> if the invoice has not been fully paid.
		/// </value>
		/// <remarks>
		/// This is set when the invoice status changes to <see cref="InvoiceStatus.Paid"/>.
		/// </remarks>
		public DateTimeOffset? PaidDate { get; set; }

		/// <summary>
		/// Gets or sets the end date of the billing period covered by this invoice.
		/// </summary>
		/// <value>
		/// The billing period end date and time in UTC, or <see langword="null"/> if not applicable.
		/// </value>
		/// <remarks>
		/// For recurring subscriptions, this indicates when the billing cycle ended.
		/// </remarks>
		public DateTimeOffset? PeriodEnd { get; set; }

		/// <summary>
		/// Gets or sets the start date of the billing period covered by this invoice.
		/// </summary>
		/// <value>
		/// The billing period start date and time in UTC, or <see langword="null"/> if not applicable.
		/// </value>
		/// <remarks>
		/// For recurring subscriptions, this indicates when the billing cycle began.
		/// </remarks>
		public DateTimeOffset? PeriodStart { get; set; }

		/// <summary>
		/// Gets or sets optional metadata associated with this invoice.
		/// </summary>
		/// <value>
		/// A dictionary of key-value pairs containing custom metadata, or <see langword="null"/> if no metadata exists.
		/// </value>
		/// <remarks>
		/// Metadata can store additional custom information such as purchase order numbers, department codes, or external system references.
		/// </remarks>
		public Dictionary<string, string>? Metadata { get; set; }

		/// <summary>
		/// Gets or sets the current status of this invoice in its lifecycle.
		/// </summary>
		/// <value>The invoice status enum value. Defaults to <see cref="InvoiceStatus.Draft"/>.</value>
		/// <remarks>
		/// The status determines what operations can be performed on the invoice (e.g., payments can only be applied to pending invoices).
		/// </remarks>
		public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;

		/// <summary>
		/// Gets or sets the collection of line items that make up the charges on this invoice.
		/// </summary>
		/// <value>A list of invoice line items. Defaults to an empty list.</value>
		/// <remarks>
		/// Each line item represents a specific charge for a product, service, or usage.
		/// </remarks>
		public List<InvoiceLineItem> LineItems { get; set; } = new List<InvoiceLineItem>();

		/// <summary>
		/// Gets or sets the tenant identifier for the organization or customer this invoice belongs to.
		/// </summary>
		/// <value>The unique tenant identifier.</value>
		/// <remarks>
		/// This associates the invoice with a specific tenant in the multi-tenant system.
		/// </remarks>
		public TenantId TenantId { get; set; }

		#endregion
	}
}