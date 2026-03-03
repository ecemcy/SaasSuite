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

namespace SaasSuite.Samples.SampleWebApp.Infrastructure.Enumerations
{
	/// <summary>
	/// Defines the available subscription plan tiers with increasing feature sets and pricing levels.
	/// </summary>
	/// <remarks>
	/// Plans are ordered from lowest to highest tier, with each higher tier including all features
	/// from lower tiers plus additional capabilities.
	/// </remarks>
	public enum PlanType
	{
		/// <summary>
		/// Free tier with basic features at no cost.
		/// </summary>
		/// <remarks>
		/// Typically includes limited seats, basic features, and minimal quotas suitable for evaluation or small-scale use.
		/// </remarks>
		Free = 0,

		/// <summary>
		/// Entry-level paid plan designed for small teams with essential features.
		/// </summary>
		/// <remarks>
		/// Provides expanded quotas and seat limits compared to the Free tier.
		/// </remarks>
		Starter = 1,

		/// <summary>
		/// Mid-tier plan for growing businesses requiring advanced features and higher limits.
		/// </summary>
		/// <remarks>
		/// Includes advanced reporting, analytics, and significantly higher quotas and seat allocations.
		/// </remarks>
		Professional = 2,

		/// <summary>
		/// Premium plan offering full features, unlimited or very high limits, and priority support.
		/// </summary>
		/// <remarks>
		/// Designed for large organizations with custom requirements and enterprise-grade needs.
		/// </remarks>
		Enterprise = 3
	}
}