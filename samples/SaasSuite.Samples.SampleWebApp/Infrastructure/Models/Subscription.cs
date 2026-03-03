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
using SaasSuite.Samples.SampleWebApp.Infrastructure.Enumerations;

namespace SaasSuite.Samples.SampleWebApp.Infrastructure.Models
{
	/// <summary>
	/// Represents a tenant's subscription including plan details, billing status, and access permissions.
	/// </summary>
	/// <remarks>
	/// The subscription determines what features, quotas, and seat allocations are available to the tenant.
	/// Changes to subscription plans typically trigger updates to associated tenant limits and permissions.
	/// </remarks>
	public class Subscription
	{
		#region ' Properties '

		/// <summary>
		/// Gets a value indicating whether the subscription is currently active and grants access to tenant features.
		/// </summary>
		/// <value>
		/// <see langword="true"/> if the state is <see cref="SubscriptionState.Active"/> or <see cref="SubscriptionState.Trialing"/>;
		/// otherwise, <see langword="false"/>.
		/// </value>
		/// <remarks>
		/// This computed property simplifies access control checks by consolidating active states.
		/// </remarks>
		public bool IsActive => this.State == SubscriptionState.Active || this.State == SubscriptionState.Trialing;

		/// <summary>
		/// Gets or sets the monthly price for this subscription in the billing currency.
		/// </summary>
		/// <value>
		/// A decimal representing the monthly recurring charge. Zero for free plans.
		/// </value>
		public decimal MonthlyPrice { get; set; }

		/// <summary>
		/// Gets or sets the current subscription plan type.
		/// </summary>
		/// <value>
		/// The <see cref="PlanType"/> indicating the tier and feature set available to the tenant.
		/// </value>
		public required PlanType Plan { get; set; }

		/// <summary>
		/// Gets or sets the current lifecycle state of the subscription.
		/// </summary>
		/// <value>
		/// The <see cref="SubscriptionState"/> indicating whether the subscription is active, trialing, overdue, or canceled.
		/// </value>
		public required SubscriptionState State { get; set; }

		/// <summary>
		/// Gets or initializes the tenant this subscription belongs to.
		/// </summary>
		/// <value>
		/// The <see cref="TenantId"/> identifying the owning tenant.
		/// </value>
		public required TenantId TenantId { get; init; }

		/// <summary>
		/// Gets or sets when the current billing period ends.
		/// </summary>
		/// <value>
		/// A <see cref="DateTimeOffset"/> indicating when the subscription renews or expires, or <see langword="null"/> if not set.
		/// </value>
		public DateTimeOffset? CurrentPeriodEnds { get; set; }

		/// <summary>
		/// Gets or sets when the trial period ends, if applicable.
		/// </summary>
		/// <value>
		/// A <see cref="DateTimeOffset"/> indicating when the trial expires, or <see langword="null"/> if not in trial.
		/// </value>
		public DateTimeOffset? TrialEnds { get; set; }

		#endregion
	}
}