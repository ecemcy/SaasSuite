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
using SaasSuite.Samples.SampleWebApp.Infrastructure.Interfaces;
using SaasSuite.Samples.SampleWebApp.Infrastructure.Models;

namespace SaasSuite.Samples.SampleWebApp.Infrastructure.Services
{
	/// <summary>
	/// In-memory implementation of <see cref="ISubscriptionService"/> for demonstration purposes.
	/// </summary>
	/// <remarks>
	/// This implementation stores subscriptions in memory. In production, subscription data should be persisted
	/// to a database and integrated with a billing provider (e.g., Stripe, PayPal) for payment processing.
	/// </remarks>
	public class InMemorySubscriptionService
		: ISubscriptionService
	{
		#region ' Fields '

		/// <summary>
		/// In-memory dictionary storing subscriptions keyed by tenant ID.
		/// </summary>
		private readonly Dictionary<string, Subscription> _subscriptions = new Dictionary<string, Subscription>();

		/// <summary>
		/// Audit service for logging subscription changes.
		/// </summary>
		private readonly IAuditService _auditService;

		/// <summary>
		/// Time provider for date and time operations.
		/// </summary>
		private readonly ITimeProvider _timeProvider;

		#endregion

		#region ' Constructors '

		/// <summary>
		/// Initializes a new instance of the <see cref="InMemorySubscriptionService"/> class.
		/// </summary>
		/// <param name="timeProvider">The time provider for date/time operations.</param>
		/// <param name="auditService">The audit service for logging subscription events.</param>
		public InMemorySubscriptionService(ITimeProvider timeProvider, IAuditService auditService)
		{
			this._timeProvider = timeProvider;
			this._auditService = auditService;
		}

		#endregion

		#region ' Methods '

		/// <summary>
		/// Calculates the monthly price for a given subscription plan.
		/// </summary>
		/// <param name="plan">The plan type to price.</param>
		/// <returns>The monthly price in dollars as a decimal value.</returns>
		private decimal GetPlanPrice(PlanType plan)
		{
			return plan switch
			{
				PlanType.Free => 0,
				PlanType.Starter => 29,
				PlanType.Professional => 99,
				PlanType.Enterprise => 299,
				_ => 0
			};
		}

		/// <inheritdoc/>
		public async Task<Subscription> DowngradePlanAsync(TenantId tenantId, PlanType newPlan)
		{
			Subscription? subscription = await this.GetSubscriptionAsync(tenantId);

			// Create new subscription if one doesn't exist
			if (subscription == null)
			{
				subscription = new Subscription
				{
					TenantId = tenantId,
					Plan = newPlan,
					State = SubscriptionState.Active,
					CurrentPeriodEnds = this._timeProvider.UtcNow.AddMonths(1),
					MonthlyPrice = this.GetPlanPrice(newPlan)
				};
			}
			else
			{
				// Update existing subscription with new plan details
				subscription.Plan = newPlan;
				subscription.MonthlyPrice = this.GetPlanPrice(newPlan);
			}

			this._subscriptions[tenantId.Value] = subscription;

			// Log the downgrade event for audit trail and compliance
			await this._auditService.LogAsync(
				tenantId,
				"subscription.downgraded",
				"Subscription",
				$"Plan downgraded to {newPlan}",
				new Dictionary<string, string> { { "plan", newPlan.ToString() } });

			return subscription;
		}

		/// <inheritdoc/>
		public async Task<Subscription> UpgradePlanAsync(TenantId tenantId, PlanType newPlan)
		{
			Subscription? subscription = await this.GetSubscriptionAsync(tenantId);

			// Create new subscription if one doesn't exist
			if (subscription == null)
			{
				subscription = new Subscription
				{
					TenantId = tenantId,
					Plan = newPlan,
					State = SubscriptionState.Active,
					CurrentPeriodEnds = this._timeProvider.UtcNow.AddMonths(1),
					MonthlyPrice = this.GetPlanPrice(newPlan)
				};
			}
			else
			{
				// Update existing subscription with new plan details
				subscription.Plan = newPlan;
				subscription.MonthlyPrice = this.GetPlanPrice(newPlan);
			}

			this._subscriptions[tenantId.Value] = subscription;

			// Log the upgrade event for audit trail and compliance
			await this._auditService.LogAsync(
				tenantId,
				"subscription.upgraded",
				"Subscription",
				$"Plan upgraded to {newPlan}",
				new Dictionary<string, string> { { "plan", newPlan.ToString() } });

			return subscription;
		}

		/// <inheritdoc/>
		public Task<Subscription?> GetSubscriptionAsync(TenantId tenantId)
		{
			this._subscriptions.TryGetValue(tenantId.Value, out Subscription? subscription);
			return Task.FromResult(subscription);
		}

		#endregion
	}
}