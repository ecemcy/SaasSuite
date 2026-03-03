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

using SaasSuite.Subscriptions.Options;

namespace SaasSuite.Subscriptions.Enumerations
{
	/// <summary>
	/// Defines the lifecycle state of a subscription in a multi-tenant SaaS application.
	/// </summary>
	/// <remarks>
	/// The subscription status drives access control, billing behavior, and user experience throughout
	/// the subscription lifecycle. Status transitions typically flow from Trial → Active → (Cancelled/Expired),
	/// with PastDue and Suspended as intermediate states during payment issues.
	/// </remarks>
	public enum SubscriptionStatus
	{
		/// <summary>
		/// The subscription is currently active with full access to all entitled features and resources.
		/// </summary>
		/// <remarks>
		/// Active subscriptions have successfully passed any trial period, are paid up to date,
		/// and grant full access to the plan's features and limits. This is the normal operating state
		/// for a healthy subscription. Automatic billing occurs on the next billing date.
		/// </remarks>
		Active = 0,

		/// <summary>
		/// The subscription has been cancelled and will not renew at the end of the current billing period.
		/// </summary>
		/// <remarks>
		/// Cancelled subscriptions may continue to provide access until the end of the paid period,
		/// depending on the cancellation policy. No further billing will occur. The cancellation date
		/// is recorded in <see cref="Subscription.CancellationDate"/>.
		/// Tenants with cancelled subscriptions should be prevented from accessing paid features after
		/// the end date, though data retention policies may vary.
		/// </remarks>
		Cancelled = 1,

		/// <summary>
		/// The subscription has reached its end date and is no longer providing access.
		/// </summary>
		/// <remarks>
		/// Expired subscriptions have passed their natural end date without renewal, typically due to
		/// failed payment, intentional non-renewal, or reaching the end of a one-time subscription.
		/// Access to subscription features should be revoked. Expired subscriptions may be eligible
		/// for reactivation depending on business rules and grace periods.
		/// </remarks>
		Expired = 2,

		/// <summary>
		/// The subscription is in a free trial period with temporary access to paid features.
		/// </summary>
		/// <remarks>
		/// Trial subscriptions provide limited-time access to evaluate features before committing to payment.
		/// The trial end date is stored in <see cref="Subscription.TrialEndDate"/>.
		/// Trials automatically transition to Active upon successful payment or Expired if not converted.
		/// Trial periods are defined in <see cref="SubscriptionPlan.TrialPeriodDays"/>.
		/// </remarks>
		Trial = 3,

		/// <summary>
		/// The subscription payment is overdue but access may still be provided during a grace period.
		/// </summary>
		/// <remarks>
		/// Past due subscriptions have failed payment attempts but haven't been suspended yet.
		/// A grace period (configured in <see cref="SubscriptionOptions.GracePeriodDays"/>)
		/// allows time for customers to update payment methods. After the grace period expires,
		/// subscriptions typically transition to Suspended or Cancelled. Retry billing attempts
		/// should continue during this period.
		/// </remarks>
		PastDue = 4,

		/// <summary>
		/// The subscription has been temporarily suspended due to payment failure or administrative action.
		/// </summary>
		/// <remarks>
		/// Suspended subscriptions have restricted or no access to paid features and services.
		/// This status is typically reached after the PastDue grace period expires without successful payment.
		/// Suspensions can also be applied administratively for policy violations or fraud prevention.
		/// Suspended subscriptions may be reactivated upon successful payment or administrative approval.
		/// Data is typically retained to allow for reactivation.
		/// </remarks>
		Suspended = 5
	}
}