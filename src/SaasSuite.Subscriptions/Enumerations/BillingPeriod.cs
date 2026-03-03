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

namespace SaasSuite.Subscriptions.Enumerations
{
	/// <summary>
	/// Defines the recurring billing cycle frequency for subscription plans.
	/// </summary>
	/// <remarks>
	/// The billing period determines when subscriptions are automatically charged and renewed.
	/// Different periods enable various pricing strategies and customer preferences.
	/// This enumeration is used in conjunction with <see cref="SubscriptionPlan"/>
	/// to define subscription pricing and renewal schedules.
	/// </remarks>
	public enum BillingPeriod
	{
		/// <summary>
		/// Subscription is billed every month on the same day.
		/// </summary>
		/// <remarks>
		/// Monthly billing provides the most flexibility for customers with shorter commitment periods.
		/// The subscription renews automatically on the same day each month (e.g., if created on the 15th,
		/// it renews on the 15th of each subsequent month). For months with fewer days than the original
		/// billing day, the renewal occurs on the last day of the month.
		/// </remarks>
		Monthly = 0,

		/// <summary>
		/// Subscription is billed every three months (90 days).
		/// </summary>
		/// <remarks>
		/// Quarterly billing offers a middle ground between monthly and annual commitments.
		/// Common for B2B subscriptions where customers prefer predictable quarterly budget cycles.
		/// The subscription renews every 3 months from the start date, providing a balance between
		/// commitment length and payment frequency.
		/// </remarks>
		Quarterly = 1,

		/// <summary>
		/// Subscription is billed once per year (annually).
		/// </summary>
		/// <remarks>
		/// Annual billing typically offers a discounted rate compared to monthly billing and
		/// provides longer-term revenue predictability. The subscription renews on the same day
		/// each year. This is common for enterprise plans and often includes incentives like
		/// additional features or cost savings for the longer commitment.
		/// </remarks>
		Annual = 2,

		/// <summary>
		/// One-time payment with no automatic renewal or recurring charges.
		/// </summary>
		/// <remarks>
		/// One-time billing is used for lifetime access, perpetual licenses, or single purchase items
		/// that don't require ongoing billing. Subscriptions with this billing period do not have a
		/// next billing date and cannot be automatically renewed. Suitable for pay-once models or
		/// addons that don't recur.
		/// </remarks>
		OneTime = 3
	}
}