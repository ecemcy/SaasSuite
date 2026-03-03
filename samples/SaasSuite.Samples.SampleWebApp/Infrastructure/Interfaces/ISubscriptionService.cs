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
using SaasSuite.Samples.SampleWebApp.Infrastructure.Models;

namespace SaasSuite.Samples.SampleWebApp.Infrastructure.Interfaces
{
	/// <summary>
	/// Provides services for managing tenant subscriptions and plan transitions.
	/// </summary>
	/// <remarks>
	/// This service handles subscription lifecycle operations including plan changes, which typically
	/// trigger updates to features, quotas, and seat allocations associated with the tenant.
	/// </remarks>
	public interface ISubscriptionService
	{
		#region ' Methods '

		/// <summary>
		/// Downgrades a tenant's subscription to a lower-tier plan with reduced features and limits.
		/// </summary>
		/// <param name="tenantId">The identifier of the tenant whose subscription to downgrade.</param>
		/// <param name="newPlan">The target <see cref="PlanType"/> to downgrade to.</param>
		/// <returns>
		/// A task that represents the asynchronous operation and contains the updated <see cref="Subscription"/>.
		/// </returns>
		/// <remarks>
		/// This operation may need to handle scenarios where the tenant's current usage exceeds the limits
		/// of the downgraded plan, potentially requiring cleanup or grace periods.
		/// </remarks>
		Task<Subscription> DowngradePlanAsync(TenantId tenantId, PlanType newPlan);

		/// <summary>
		/// Upgrades a tenant's subscription to a higher-tier plan with expanded features and limits.
		/// </summary>
		/// <param name="tenantId">The identifier of the tenant whose subscription to upgrade.</param>
		/// <param name="newPlan">The target <see cref="PlanType"/> to upgrade to.</param>
		/// <returns>
		/// A task that represents the asynchronous operation and contains the updated <see cref="Subscription"/>.
		/// </returns>
		/// <remarks>
		/// This operation should validate that the new plan is indeed an upgrade, update billing information,
		/// and log an audit event for the plan change.
		/// </remarks>
		Task<Subscription> UpgradePlanAsync(TenantId tenantId, PlanType newPlan);

		/// <summary>
		/// Retrieves the subscription details for a specific tenant.
		/// </summary>
		/// <param name="tenantId">The identifier of the tenant whose subscription to retrieve.</param>
		/// <returns>
		/// A task that represents the asynchronous operation and contains the <see cref="Subscription"/> if found,
		/// or <see langword="null"/> if the tenant has no subscription record.
		/// </returns>
		Task<Subscription?> GetSubscriptionAsync(TenantId tenantId);

		#endregion
	}
}