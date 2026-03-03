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

namespace SaasSuite.Billing.Options
{
	/// <summary>
	/// Configuration options for the billing service.
	/// </summary>
	/// <remarks>
	/// These options control invoice generation, payment processing, and reconciliation behavior.
	/// They can be configured through dependency injection and affect all billing operations throughout the application.
	/// </remarks>
	public class BillingOptions
	{
		#region ' Properties '

		/// <summary>
		/// Gets or sets a value indicating whether invoices should be automatically finalized immediately after generation.
		/// </summary>
		/// <value>
		/// <see langword="true"/> to automatically finalize invoices; otherwise, <see langword="false"/>.
		/// Defaults to <see langword="true"/>.
		/// </value>
		/// <remarks>
		/// When <see langword="true"/>, invoices transition from draft to pending status automatically.
		/// When <see langword="false"/>, invoices remain in draft and must be manually finalized.
		/// </remarks>
		public bool AutoFinalizeInvoices { get; set; } = true;

		/// <summary>
		/// Gets or sets a value indicating whether payments should be automatically reconciled against invoices.
		/// </summary>
		/// <value>
		/// <see langword="true"/> to enable automatic reconciliation; otherwise, <see langword="false"/>.
		/// Defaults to <see langword="true"/>.
		/// </value>
		/// <remarks>
		/// When <see langword="true"/>, the reconciliation process runs automatically to match payments to invoices.
		/// When <see langword="false"/>, payment reconciliation must be triggered manually.
		/// </remarks>
		public bool AutoReconcilePayments { get; set; } = true;

		/// <summary>
		/// Gets or sets a value indicating whether usage-based charges should be included in generated invoices.
		/// </summary>
		/// <value>
		/// <see langword="true"/> to include usage charges; otherwise, <see langword="false"/>.
		/// Defaults to <see langword="true"/>.
		/// </value>
		/// <remarks>
		/// When <see langword="true"/>, metered usage data is retrieved and added as line items.
		/// When <see langword="false"/>, only subscription charges are included.
		/// </remarks>
		public bool IncludeUsageCharges { get; set; } = true;

		/// <summary>
		/// Gets or sets a value indicating whether subscription charges should be prorated based on the billing cycle.
		/// </summary>
		/// <value>
		/// <see langword="true"/> to enable proration; otherwise, <see langword="false"/>.
		/// Defaults to <see langword="true"/>.
		/// </value>
		/// <remarks>
		/// When <see langword="true"/>, charges are adjusted proportionally if a subscription doesn't cover the full billing period.
		/// When <see langword="false"/>, full subscription amounts are charged regardless of period coverage.
		/// </remarks>
		public bool ProrationEnabled { get; set; } = true;

		/// <summary>
		/// Gets or sets the default tax rate applied to invoices as a percentage.
		/// </summary>
		/// <value>The tax rate percentage (e.g., 10 for 10%, 8.5 for 8.5%). Defaults to 0 (no tax).</value>
		/// <remarks>
		/// This tax rate is automatically applied when invoices are finalized if no custom tax has been set.
		/// </remarks>
		public decimal DefaultTaxRate { get; set; } = 0m;

		/// <summary>
		/// Gets or sets the default number of days after the invoice date when payment is due.
		/// </summary>
		/// <value>The number of days. Defaults to 30 days.</value>
		/// <remarks>
		/// This value is used to calculate the <see cref="Invoice.DueDate"/> when generating invoices.
		/// </remarks>
		public int DefaultPaymentTermsDays { get; set; } = 30;

		/// <summary>
		/// Gets or sets the number of days after the due date before marking an invoice as overdue.
		/// </summary>
		/// <value>The number of grace period days. Defaults to 1 day.</value>
		/// <remarks>
		/// This provides a grace period before invoices transition to overdue status.
		/// </remarks>
		public int OverdueDaysAfterDue { get; set; } = 1;

		/// <summary>
		/// Gets or sets the number of days before the due date to send payment reminder notifications to customers.
		/// </summary>
		/// <value>The number of days before due date. Defaults to 7 days.</value>
		/// <remarks>
		/// This is used by notification systems to schedule reminder communications.
		/// </remarks>
		public int ReminderDaysBeforeDue { get; set; } = 7;

		/// <summary>
		/// Gets or sets the default three-letter ISO 4217 currency code used for all invoices.
		/// </summary>
		/// <value>The currency code (e.g., "USD", "EUR", "GBP"). Defaults to "USD".</value>
		/// <remarks>
		/// This currency is applied to new invoices unless explicitly overridden.
		/// </remarks>
		public string DefaultCurrency { get; set; } = "USD";

		/// <summary>
		/// Gets or sets the prefix prepended to generated invoice numbers.
		/// </summary>
		/// <value>The invoice number prefix string. Defaults to "INV-".</value>
		/// <remarks>
		/// This helps create recognizable invoice numbers and can be customized for branding or organizational requirements.
		/// </remarks>
		public string InvoiceNumberPrefix { get; set; } = "INV-";

		/// <summary>
		/// Gets or sets the URL endpoint where payment provider webhooks should be sent.
		/// </summary>
		/// <value>The webhook endpoint URL, or <see langword="null"/> if webhook integration is not configured.</value>
		/// <remarks>
		/// This endpoint receives notifications about payment events such as successful payments or failed charges.
		/// </remarks>
		public string? WebhookEndpoint { get; set; }

		/// <summary>
		/// Gets or sets the secret key used to verify webhook signatures from payment providers.
		/// </summary>
		/// <value>The webhook secret string, or <see langword="null"/> if webhook verification is not configured.</value>
		/// <remarks>
		/// This ensures webhook requests are authentic and come from the configured payment provider.
		/// </remarks>
		public string? WebhookSecret { get; set; }

		#endregion
	}
}