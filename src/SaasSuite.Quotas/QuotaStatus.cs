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

using SaasSuite.Quotas.Enumerations;

namespace SaasSuite.Quotas
{
	/// <summary>
	/// Represents the current usage status of a quota, including consumption metrics and calculated availability.
	/// </summary>
	/// <remarks>
	/// This class provides a snapshot of quota usage at a point in time, with computed properties
	/// that indicate whether the quota is exceeded and how much capacity remains.
	/// The calculated properties (<see cref="IsExceeded"/>, <see cref="PercentageUsed"/>, <see cref="Remaining"/>)
	/// are derived from the <see cref="CurrentUsage"/> and <see cref="Limit"/> values, making this class
	/// ideal for real-time quota monitoring, API responses, and user interface displays.
	/// This class supports multi-targeting for .NET 6-10 with conditional compilation for the C# 11 required modifier.
	/// </remarks>
	public class QuotaStatus
	{
		#region ' Constructors '

		/// <summary>
		/// Initializes a new instance of the <see cref="QuotaStatus"/> class with default values.
		/// </summary>
		/// <remarks>
		/// This parameterless constructor is required by JSON deserializers, ORMs, and other reflection-based tools.
		/// The <see cref="QuotaName"/> property is initialized with <c>default!</c> to satisfy the compiler's null-safety analysis.
		/// Serialization frameworks are expected to populate the <see cref="QuotaName"/> property after instantiation.
		/// For .NET 7+, the required modifier on <see cref="QuotaName"/> ensures deserializers must set this property.
		/// </remarks>
		public QuotaStatus()
		{
			// Initialize QuotaName with default! to satisfy nullable reference type requirements
			// Deserializers will populate this property from the serialized data
			this.QuotaName = default!;
		}

		// Conditional constructor for .NET 6 and earlier versions that don't support the 'required' modifier
#if !NET7_0_OR_GREATER
		/// <summary>
		/// Initializes a new instance of the <see cref="QuotaStatus"/> class with the specified quota name.
		/// </summary>
		/// <param name="quotaName">The unique identifier of the quota being tracked. Cannot be <see langword="null"/>, empty, or whitespace.</param>
		/// <exception cref="ArgumentException">
		/// Thrown when <paramref name="quotaName"/> is <see langword="null"/>, empty, or contains only whitespace characters.
		/// </exception>
		/// <remarks>
		/// This constructor is only compiled for targets prior to .NET 7 where the C# 11 required modifier is not available.
		/// It provides explicit validation and initialization to ensure the <see cref="QuotaName"/> property is always set to a valid value.
		/// For .NET 7+, the required modifier on <see cref="QuotaName"/> enforces this constraint at compile-time via object initializers.
		/// This constructor is particularly useful when creating quota status objects programmatically in code targeting .NET 6.
		/// </remarks>
		public QuotaStatus(string quotaName)
		{
			// Validate that quotaName is not null, empty, or whitespace
			// This ensures data integrity for frameworks that don't support the required modifier
			this.QuotaName = string.IsNullOrWhiteSpace(quotaName)
				? throw new ArgumentException("QuotaName cannot be null or empty.", nameof(quotaName))
				: quotaName;
		}
#endif

		#endregion

		#region ' Properties '

		/// <summary>
		/// Gets a value indicating whether the current usage has met or exceeded the quota limit.
		/// </summary>
		/// <value>
		/// <see langword="true"/> if <see cref="CurrentUsage"/> is greater than or equal to <see cref="Limit"/>; otherwise, <see langword="false"/>.
		/// </value>
		/// <remarks>
		/// This computed property provides a quick boolean check for quota enforcement decisions.
		/// When <see langword="true"/>, further consumption attempts should typically be denied (HTTP 429),
		/// though enforcement behavior can be configured via <c>QuotaOptions.EnableEnforcement</c>.
		/// The comparison uses greater-than-or-equal to handle edge cases where usage exactly equals the limit.
		/// </remarks>
		public bool IsExceeded => this.CurrentUsage >= this.Limit;

		/// <summary>
		/// Gets the percentage of the quota that has been consumed.
		/// </summary>
		/// <value>
		/// A double between 0.0 and 100.0 (or higher if exceeded) representing usage as a percentage.
		/// Returns <c>0.0</c> if <see cref="Limit"/> is zero to avoid division by zero.
		/// </value>
		/// <remarks>
		/// Calculated as (CurrentUsage / Limit) * 100. Values over 100 indicate the quota is exceeded.
		/// This property is useful for:
		/// <list type="bullet">
		/// <item><description>Displaying progress bars in user interfaces</description></item>
		/// <item><description>Triggering usage warnings at threshold percentages (e.g., 80%, 90%)</description></item>
		/// <item><description>Generating usage reports and analytics</description></item>
		/// </list>
		/// The calculation handles edge cases gracefully, returning 0 when the limit is zero.
		/// </remarks>
		public double PercentageUsed => this.Limit > 0 ? (double)this.CurrentUsage / this.Limit * 100 : 0;

		/// <summary>
		/// Gets or sets the current accumulated usage count for this quota.
		/// </summary>
		/// <value>
		/// A non-negative integer representing how many units have been consumed in the current period.
		/// </value>
		/// <remarks>
		/// The usage counter increments with each consumption operation (via <c>ConsumeAsync</c> or <c>IncrementUsageAsync</c>)
		/// and automatically resets to zero based on the quota's <see cref="Period"/>.
		/// This value is compared against <see cref="Limit"/> to determine if the quota is exceeded.
		/// </remarks>
		public int CurrentUsage { get; set; }

		/// <summary>
		/// Gets or sets the maximum allowed usage value for this quota.
		/// </summary>
		/// <value>
		/// A positive integer representing the quota ceiling.
		/// </value>
		/// <remarks>
		/// This value is copied from the corresponding <see cref="QuotaDefinition.Limit"/> for convenience,
		/// allowing status objects to be self-contained without requiring additional lookups.
		/// When <see cref="CurrentUsage"/> reaches or exceeds this limit, the quota is considered exceeded.
		/// </remarks>
		public int Limit { get; set; }

		/// <summary>
		/// Gets the number of units remaining before the quota limit is reached.
		/// </summary>
		/// <value>
		/// A non-negative integer representing available quota capacity.
		/// Returns <c>0</c> if the quota is already exceeded.
		/// </value>
		/// <remarks>
		/// Calculated as <c>max(0, Limit - CurrentUsage)</c>. Once the quota is exceeded, this property returns zero
		/// rather than a negative value, making it safe to use in capacity checks without additional validation.
		/// This property is particularly useful for:
		/// <list type="bullet">
		/// <item><description>Pre-checking if a batch operation can proceed (e.g., "Can I consume 10 units?")</description></item>
		/// <item><description>Displaying remaining capacity to users ("You have 42 API calls remaining")</description></item>
		/// <item><description>Implementing throttling logic based on remaining capacity</description></item>
		/// </list>
		/// The Math.Max ensures the value is always non-negative, even when CurrentUsage exceeds Limit.
		/// </remarks>
		public int Remaining => Math.Max(0, this.Limit - this.CurrentUsage);

		/// <summary>
		/// Gets or sets the unique identifier of the quota being tracked.
		/// </summary>
		/// <value>
		/// A string matching the <see cref="QuotaDefinition.Name"/> this status represents.
		/// Cannot be <see langword="null"/> or empty.
		/// </value>
		/// <remarks>
		/// This identifier correlates the status with its corresponding definition.
		/// For .NET 7+, the required modifier ensures this property must be initialized via object initializers or constructors.
		/// For earlier .NET versions, use the constructor overload that accepts a quotaName parameter to ensure proper initialization.
		/// </remarks>
#if NET7_0_OR_GREATER
		public required string QuotaName { get; set; }
#else
		public string QuotaName { get; set; }
#endif

		/// <summary>
		/// Gets or sets the UTC timestamp when the usage counter will next reset to zero.
		/// </summary>
		/// <value>
		/// A <see cref="DateTime"/> in UTC indicating the next reset time, or <see langword="null"/> for <see cref="QuotaPeriod.Total"/> quotas.
		/// </value>
		/// <remarks>
		/// The reset time is calculated based on the quota's <see cref="Period"/>:
		/// <list type="bullet">
		/// <item><description><see cref="QuotaPeriod.Hourly"/>: Top of the next hour (e.g., 15:00:00)</description></item>
		/// <item><description><see cref="QuotaPeriod.Daily"/>: Midnight UTC of the next day (00:00:00)</description></item>
		/// <item><description><see cref="QuotaPeriod.Monthly"/>: Midnight UTC on the first day of the next month</description></item>
		/// <item><description><see cref="QuotaPeriod.Total"/>: <see langword="null"/> (never resets automatically)</description></item>
		/// </list>
		/// This information allows API clients to implement intelligent retry logic and display countdown timers to users.
		/// All times are in UTC to ensure consistency across different time zones.
		/// </remarks>
		public DateTime? ResetTime { get; set; }

		/// <summary>
		/// Gets or sets the time period over which this quota is measured.
		/// </summary>
		/// <value>
		/// A <see cref="QuotaPeriod"/> value indicating the reset schedule.
		/// </value>
		/// <remarks>
		/// Copied from the quota definition for reference when displaying status information to users.
		/// This helps API clients and UIs understand when the quota will reset without additional queries.
		/// The actual reset behavior is managed by the quota store implementation based on <see cref="ResetTime"/>.
		/// </remarks>
		public QuotaPeriod Period { get; set; }

		#endregion
	}
}