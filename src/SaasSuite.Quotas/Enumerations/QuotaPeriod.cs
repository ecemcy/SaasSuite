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

namespace SaasSuite.Quotas.Enumerations
{
	/// <summary>
	/// Defines the time period over which quota usage is measured and automatically reset.
	/// </summary>
	/// <remarks>
	/// The quota period determines when usage counters are automatically reset to zero.
	/// Different periods enable various rate limiting and resource allocation strategies.
	/// Reset times are always calculated and enforced in UTC to ensure consistency across time zones.
	/// The period value directly impacts quota enforcement behavior:
	/// <list type="bullet">
	/// <item><description><see cref="Hourly"/> quotas are ideal for fine-grained rate limiting and preventing burst traffic</description></item>
	/// <item><description><see cref="Daily"/> quotas provide balanced control suitable for most API scenarios</description></item>
	/// <item><description><see cref="Monthly"/> quotas align with billing cycles and subscription periods</description></item>
	/// <item><description><see cref="Total"/> quotas implement lifetime limits requiring manual intervention</description></item>
	/// </list>
	/// </remarks>
	public enum QuotaPeriod
	{
		/// <summary>
		/// Quota resets every hour at the top of the hour (e.g., 01:00:00, 02:00:00, 03:00:00 UTC).
		/// </summary>
		/// <remarks>
		/// Hourly quotas provide the finest temporal granularity for automatic resets, making them
		/// ideal for scenarios requiring tight rate limiting to prevent abuse and ensure fair resource distribution.
		/// The reset occurs at precisely the top of each hour (minute and second are 00) in UTC.
		/// Usage accumulated during one hour does not carry over to the next hour.
		/// Common use cases include API request throttling, temporary resource limits, and burst protection.
		/// </remarks>
		Hourly = 0,

		/// <summary>
		/// Quota resets every day at midnight UTC (00:00:00).
		/// </summary>
		/// <remarks>
		/// Daily quotas provide a balance between flexibility and control, suitable for most
		/// API rate limiting and resource allocation scenarios. The reset occurs at precisely
		/// midnight (00:00:00) UTC each day, regardless of the user's local time zone.
		/// This period is commonly used for daily API call limits, daily upload quotas,
		/// and similar 24-hour cyclical constraints. Usage does not roll over between days.
		/// </remarks>
		Daily = 1,

		/// <summary>
		/// Quota resets on the first day of each calendar month at midnight UTC (00:00:00).
		/// </summary>
		/// <remarks>
		/// Monthly quotas align with billing cycles and subscription periods, making them
		/// ideal for SaaS applications with monthly subscription plans. The reset occurs at
		/// precisely midnight (00:00:00) UTC on the first day of each calendar month.
		/// Monthly quotas naturally accommodate varying month lengths (28-31 days) and
		/// are commonly used for subscription-based API limits, monthly storage allocations,
		/// and billing-period resource caps. Usage does not carry forward to the next month.
		/// </remarks>
		Monthly = 2,

		/// <summary>
		/// Quota never resets automatically and accumulates indefinitely until manually reset.
		/// </summary>
		/// <remarks>
		/// Total quotas implement lifetime limits that persist until explicitly reset through
		/// administrative action or code. These quotas are used for permanent restrictions,
		/// one-time allocations, or features requiring manual intervention to refresh.
		/// Common use cases include trial period limits, feature access counters, lifetime
		/// resource allocations, and scenarios where automatic reset would be inappropriate.
		/// The usage counter continues to accumulate without bound unless manually reset via
		/// <c>ResetUsageAsync</c> or <c>ResetQuotaAsync</c> methods.
		/// </remarks>
		Total = 3
	}
}