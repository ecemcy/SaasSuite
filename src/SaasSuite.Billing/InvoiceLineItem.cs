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

namespace SaasSuite.Billing
{
	/// <summary>
	/// Represents an individual line item on an invoice.
	/// </summary>
	/// <remarks>
	/// Line items contain details about a single charge or service on an invoice.
	/// They can represent subscription fees, usage-based charges, one-time fees, or any other billable item.
	/// Each line item tracks quantity, pricing, discounts, and the billing period.
	/// </remarks>
	public class InvoiceLineItem
	{
		#region ' Properties '

		/// <summary>
		/// Gets or sets the total amount for this line item.
		/// </summary>
		/// <value>The total amount in the invoice's currency before discounts and taxes.</value>
		/// <remarks>
		/// This is typically calculated as <see cref="Quantity"/> × <see cref="UnitPrice"/>,
		/// but may be adjusted for prorated charges or custom pricing.
		/// </remarks>
		public decimal Amount { get; set; }

		/// <summary>
		/// Gets or sets the discount amount applied to this specific line item.
		/// </summary>
		/// <value>The discount amount in the invoice's currency. Defaults to 0.</value>
		/// <remarks>
		/// This represents a reduction in the charge for this individual item, separate from invoice-level discounts.
		/// </remarks>
		public decimal Discount { get; set; }

		/// <summary>
		/// Gets or sets the quantity of units for this line item.
		/// </summary>
		/// <value>The number of units. Can be a whole number or decimal value for fractional quantities.</value>
		/// <remarks>
		/// For usage-based billing, this represents the total units consumed (e.g., API calls, gigabytes).
		/// For subscription billing, this is typically 1.
		/// </remarks>
		public decimal Quantity { get; set; }

		/// <summary>
		/// Gets or sets the tax amount calculated for this line item.
		/// </summary>
		/// <value>The tax amount in the invoice's currency. Defaults to 0.</value>
		/// <remarks>
		/// This represents taxes applied specifically to this item, separate from invoice-level taxes.
		/// </remarks>
		public decimal Tax { get; set; }

		/// <summary>
		/// Gets or sets the price per unit for this line item.
		/// </summary>
		/// <value>The unit price in the invoice's currency.</value>
		/// <remarks>
		/// This value is multiplied by <see cref="Quantity"/> to calculate the base amount before discounts and taxes.
		/// </remarks>
		public decimal UnitPrice { get; set; }

		/// <summary>
		/// Gets or sets the human-readable description of this line item.
		/// </summary>
		/// <value>A descriptive text explaining the charge. Defaults to <see cref="string.Empty"/>.</value>
		/// <remarks>
		/// This description appears on the invoice and should clearly explain what the charge is for.
		/// </remarks>
		public string Description { get; set; } = string.Empty;

		/// <summary>
		/// Gets or sets the unique identifier for this line item.
		/// </summary>
		/// <value>A globally unique identifier string. Defaults to a new <see cref="Guid"/> when created.</value>
		/// <remarks>
		/// This identifier is automatically generated and used to track the line item throughout its lifecycle.
		/// </remarks>
		public string Id { get; set; } = Guid.NewGuid().ToString();

		/// <summary>
		/// Gets or sets the metric name associated with this line item for usage-based billing.
		/// </summary>
		/// <value>
		/// The metric identifier string, or <see langword="null"/> if this line item is not usage-based.
		/// </value>
		/// <remarks>
		/// This identifies which metered resource or API this charge is for (e.g., "api-calls", "storage-gb").
		/// </remarks>
		public string? Metric { get; set; }

		/// <summary>
		/// Gets or sets the end date of the billing period covered by this line item.
		/// </summary>
		/// <value>
		/// The period end date and time in UTC, or <see langword="null"/> if not applicable.
		/// </value>
		/// <remarks>
		/// For recurring charges, this indicates when the service period ended.
		/// </remarks>
		public DateTimeOffset? PeriodEnd { get; set; }

		/// <summary>
		/// Gets or sets the start date of the billing period covered by this line item.
		/// </summary>
		/// <value>
		/// The period start date and time in UTC, or <see langword="null"/> if not applicable.
		/// </value>
		/// <remarks>
		/// For recurring charges, this indicates when the service period began.
		/// </remarks>
		public DateTimeOffset? PeriodStart { get; set; }

		/// <summary>
		/// Gets or sets optional metadata associated with this line item.
		/// </summary>
		/// <value>
		/// A dictionary of key-value pairs containing custom metadata, or <see langword="null"/> if no metadata exists.
		/// </value>
		/// <remarks>
		/// Metadata can store additional custom information such as product SKUs, cost centers, or tracking codes.
		/// </remarks>
		public Dictionary<string, string>? Metadata { get; set; }

		#endregion
	}
}