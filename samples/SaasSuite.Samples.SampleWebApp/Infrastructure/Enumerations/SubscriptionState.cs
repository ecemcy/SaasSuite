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

using SaasSuite.Samples.SampleWebApp.Infrastructure.Models;

namespace SaasSuite.Samples.SampleWebApp.Infrastructure.Enumerations
{
	/// <summary>
	/// Represents the current lifecycle state of a tenant's subscription.
	/// </summary>
	/// <remarks>
	/// The state determines whether the subscription is active and what actions are permitted.
	/// </remarks>
	public enum SubscriptionState
	{
		/// <summary>
		/// Subscription is active and in good standing with full access to features.
		/// </summary>
		/// <remarks>
		/// This is the normal operational state after successful payment processing.
		/// </remarks>
		Active = 0,

		/// <summary>
		/// Subscription is in trial period before payment is required.
		/// </summary>
		/// <remarks>
		/// Trial subscriptions typically have full or limited feature access for a specified duration.
		/// The <see cref="Subscription.TrialEnds"/> property indicates when the trial expires.
		/// </remarks>
		Trialing = 1,

		/// <summary>
		/// Subscription payment is overdue and may be suspended or canceled if not resolved.
		/// </summary>
		/// <remarks>
		/// Features may be limited or access may be restricted until payment is updated.
		/// Grace periods may apply before transition to <see cref="Canceled"/> state.
		/// </remarks>
		PastDue = 2,

		/// <summary>
		/// Subscription has been canceled and will not renew at the period end.
		/// </summary>
		/// <remarks>
		/// Access is typically maintained until the current billing period ends, after which
		/// the tenant may be downgraded or deactivated based on business rules.
		/// </remarks>
		Canceled = 3
	}
}