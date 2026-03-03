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
	/// Represents a billing cycle period for recurring charges in a subscription-based system.
	/// </summary>
	/// <remarks>
	/// A billing cycle defines a time range during which usage is tracked and charges are accumulated,
	/// typically resulting in a single invoice at the end of the period.
	/// </remarks>
	public class BillingCycle
	{
		#region ' Properties '

		/// <summary>
		/// Gets or sets a value indicating whether this billing cycle has been closed.
		/// </summary>
		/// <remarks>
		/// A closed cycle has completed its invoicing process and should not accept new charges.
		/// </remarks>
		/// <value>
		/// <see langword="true"/> if the billing cycle is closed; otherwise, <see langword="false"/>.
		/// Defaults to <see langword="false"/>.
		/// </value>
		public bool IsClosed { get; set; }

		/// <summary>
		/// Gets or sets the unique identifier for this billing cycle.
		/// </summary>
		/// <remarks>
		/// This identifier is used to track and reference the billing cycle throughout the system.
		/// </remarks>
		/// <value>A globally unique identifier string. Defaults to a new <see cref="Guid"/> when created.</value>
		public string Id { get; set; } = Guid.NewGuid().ToString();

		/// <summary>
		/// Gets or sets the billing period type describing the recurrence frequency.
		/// </summary>
		/// <remarks>
		/// Common values include "Monthly", "Quarterly", "Yearly", or custom period descriptions.
		/// </remarks>
		/// <value>A string describing the billing period type. Defaults to "Monthly".</value>
		public string Period { get; set; } = "Monthly";

		/// <summary>
		/// Gets or sets the identifier of the invoice generated for this billing cycle.
		/// </summary>
		/// <remarks>
		/// This links the billing cycle to its corresponding invoice document.
		/// </remarks>
		/// <value>
		/// The invoice identifier string, or <see langword="null"/> if no invoice has been generated yet.
		/// </value>
		public string? InvoiceId { get; set; }

		/// <summary>
		/// Gets or sets the end date and time of this billing cycle.
		/// </summary>
		/// <remarks>
		/// This marks the conclusion of the billing period, after which an invoice is typically generated.
		/// </remarks>
		/// <value>The cycle end date and time in UTC.</value>
		public DateTimeOffset EndDate { get; set; }

		/// <summary>
		/// Gets or sets the start date and time of this billing cycle.
		/// </summary>
		/// <remarks>
		/// This marks the beginning of the period during which charges and usage are accumulated.
		/// </remarks>
		/// <value>The cycle start date and time in UTC.</value>
		public DateTimeOffset StartDate { get; set; }

		/// <summary>
		/// Gets or sets the date and time when this billing cycle was closed.
		/// </summary>
		/// <remarks>
		/// This is set when the cycle completes and an invoice is finalized.
		/// </remarks>
		/// <value>
		/// The closure date and time in UTC, or <see langword="null"/> if the cycle is still open.
		/// </value>
		public DateTimeOffset? ClosedDate { get; set; }

		/// <summary>
		/// Gets or sets optional metadata associated with this billing cycle.
		/// </summary>
		/// <remarks>
		/// Metadata can store additional custom information such as billing triggers, period adjustments, or external references.
		/// </remarks>
		/// <value>
		/// A dictionary of key-value pairs containing custom metadata, or <see langword="null"/> if no metadata exists.
		/// </value>
		public Dictionary<string, string>? Metadata { get; set; }

		#endregion

		#region ' Methods '

		/// <summary>
		/// Calculates the proration factor for a given period within this billing cycle.
		/// </summary>
		/// <remarks>
		/// The proration factor represents what fraction of the full cycle is covered by the specified date range,
		/// useful for calculating partial charges when subscriptions start or end mid-cycle.
		/// </remarks>
		/// <param name="start">The start date of the period to prorate. If before the cycle start, the cycle start is used.</param>
		/// <param name="end">The end date of the period to prorate. If after the cycle end, the cycle end is used.</param>
		/// <returns>
		/// A decimal value between 0 and 1 representing the proportion of the cycle covered.
		/// Returns 0 if the period does not overlap with the cycle or if the cycle has no duration.
		/// </returns>
		public decimal CalculateProrationFactor(DateTimeOffset start, DateTimeOffset end)
		{
			// Clamp the start and end dates to the billing cycle boundaries
			DateTimeOffset cycleStart = this.StartDate > start ? this.StartDate : start;
			DateTimeOffset cycleEnd = this.EndDate < end ? this.EndDate : end;

			// Return 0 if there's no overlap
			if (cycleEnd <= cycleStart)
			{
				return 0m;
			}

			double usedDays = (cycleEnd - cycleStart).TotalDays;
			int totalDays = this.GetDurationInDays();

			return totalDays > 0 ? (decimal)(usedDays / totalDays) : 0m;
		}

		/// <summary>
		/// Calculates the total number of days in this billing cycle.
		/// </summary>
		/// <remarks>
		/// This is useful for prorating charges and understanding cycle duration.
		/// </remarks>
		/// <returns>The number of calendar days between <see cref="StartDate"/> and <see cref="EndDate"/>.</returns>
		public int GetDurationInDays()
		{
			return (this.EndDate - this.StartDate).Days;
		}

		#endregion
	}
}