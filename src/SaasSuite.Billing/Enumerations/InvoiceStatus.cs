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

namespace SaasSuite.Billing.Enumerations
{
	/// <summary>
	/// Represents the lifecycle status of an invoice in the billing system.
	/// </summary>
	/// <remarks>
	/// The status determines what operations are allowed on the invoice and how it should be treated by the billing workflow.
	/// </remarks>
	public enum InvoiceStatus
	{
		/// <summary>
		/// Invoice is in draft state and has not yet been finalized.
		/// </summary>
		/// <remarks>
		/// Draft invoices can be modified, have taxes and discounts applied, and are not visible to customers.
		/// This is the initial state when an invoice is created.
		/// </remarks>
		Draft = 0,

		/// <summary>
		/// Invoice has been finalized and is awaiting payment from the customer.
		/// </summary>
		/// <remarks>
		/// Pending invoices are visible to customers, cannot be modified, and are actively due for payment.
		/// This state is reached after an invoice is finalized from draft status.
		/// </remarks>
		Pending = 1,

		/// <summary>
		/// Invoice has been paid in full.
		/// </summary>
		/// <remarks>
		/// The full invoice amount has been received, and no further payment is required.
		/// Paid invoices are archived and serve as payment receipts.
		/// </remarks>
		Paid = 2,

		/// <summary>
		/// Invoice payment is overdue beyond the due date.
		/// </summary>
		/// <remarks>
		/// The customer has not paid by the due date, and the invoice may be subject to late fees or collection actions.
		/// This status is typically set automatically by a reconciliation process.
		/// </remarks>
		Overdue = 3,

		/// <summary>
		/// Invoice has been cancelled and is no longer valid.
		/// </summary>
		/// <remarks>
		/// Cancelled invoices are voided and do not require payment.
		/// This may occur when a subscription is cancelled immediately or an invoice was created in error.
		/// </remarks>
		Cancelled = 4,

		/// <summary>
		/// Invoice payment has been refunded to the customer.
		/// </summary>
		/// <remarks>
		/// The previously paid amount has been returned, either in full or partially.
		/// Refunded invoices may show a credit balance or zero balance depending on the refund amount.
		/// </remarks>
		Refunded = 5
	}
}