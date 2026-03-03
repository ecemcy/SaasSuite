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
	/// Defines the configuration and constraints for a quota, including the limit, period, and scope.
	/// </summary>
	/// <remarks>
	/// Quota definitions are templates that specify how quota enforcement should behave.
	/// They are typically configured per tenant or resource and used to track and limit usage.
	/// This class supports multi-targeting for .NET 6-10, with conditional compilation to handle
	/// the C# 11 required modifier for .NET 7+ while maintaining compatibility with earlier versions.
	/// </remarks>
	public class QuotaDefinition
	{
		#region ' Constructors '

		/// <summary>
		/// Initializes a new instance of the <see cref="QuotaDefinition"/> class with default values.
		/// </summary>
		/// <remarks>
		/// This parameterless constructor is required by JSON deserializers, ORMs, and other reflection-based tools.
		/// The <see cref="Name"/> property is initialized with <c>default!</c> to satisfy the compiler's null-safety analysis.
		/// Serialization frameworks are expected to populate the <see cref="Name"/> property after instantiation.
		/// For .NET 7+, the required modifier on <see cref="Name"/> ensures deserializers must set this property.
		/// </remarks>
		public QuotaDefinition()
		{
			// Initialize Name with default! to satisfy nullable reference type requirements
			// Deserializers will populate this property from the serialized data
			this.Name = default!;
		}

		// Conditional constructor for .NET 6 and earlier versions that don't support the 'required' modifier
#if !NET7_0_OR_GREATER
		/// <summary>
		/// Initializes a new instance of the <see cref="QuotaDefinition"/> class with the specified name.
		/// </summary>
		/// <param name="name">The unique identifier for this quota. Cannot be <see langword="null"/>, empty, or whitespace.</param>
		/// <exception cref="ArgumentException">
		/// Thrown when <paramref name="name"/> is <see langword="null"/>, empty, or contains only whitespace characters.
		/// </exception>
		/// <remarks>
		/// This constructor is only compiled for targets prior to .NET 7 where the C# 11 required modifier is not available.
		/// It provides explicit validation and initialization to ensure the <see cref="Name"/> property is always set to a valid value.
		/// For .NET 7+, the required modifier on <see cref="Name"/> enforces this constraint at compile-time via object initializers.
		/// </remarks>
		public QuotaDefinition(string name)
		{
			// Validate that name is not null, empty, or whitespace
			// This ensures data integrity for frameworks that don't support the required modifier
			this.Name = string.IsNullOrWhiteSpace(name)
				? throw new ArgumentException("Name cannot be null or empty.", nameof(name))
				: name;
		}
#endif

		#endregion

		#region ' Properties '

		/// <summary>
		/// Gets or sets the maximum allowed usage value for this quota.
		/// </summary>
		/// <value>
		/// A positive integer representing the quota limit. Setting to <c>0</c> effectively blocks all usage.
		/// </value>
		/// <remarks>
		/// The limit represents the ceiling value before the quota is considered exceeded.
		/// What constitutes one unit depends on the quota being tracked (e.g., API calls, storage bytes, active users).
		/// When <see cref="QuotaStatus.CurrentUsage"/> reaches or exceeds this value,
		/// the quota is considered exceeded and <see cref="QuotaStatus.IsExceeded"/> returns <see langword="true"/>.
		/// </remarks>
		public int Limit { get; set; }

		/// <summary>
		/// Gets or sets the unique identifier for this quota.
		/// </summary>
		/// <value>
		/// A string that uniquely identifies the quota type (e.g., "api-calls", "storage-gb", "active-users").
		/// Cannot be <see langword="null"/> or empty.
		/// </value>
		/// <remarks>
		/// The name is used as a key to identify and retrieve quota status across the quota management system.
		/// It should be consistent across the application and typically follows a kebab-case or snake_case naming convention.
		/// For .NET 7+, the required modifier ensures this property must be initialized via object initializers or constructors.
		/// For earlier .NET versions, use the constructor overload that accepts a name parameter to ensure proper initialization.
		/// </remarks>
#if NET7_0_OR_GREATER
		public required string Name { get; set; }
#else
		public string Name { get; set; }
#endif

		/// <summary>
		/// Gets or sets optional metadata associated with this quota definition.
		/// </summary>
		/// <value>
		/// A dictionary of key-value pairs containing additional contextual information, or <see langword="null"/> if no metadata exists.
		/// </value>
		/// <remarks>
		/// Metadata can store descriptive information such as:
		/// <list type="bullet">
		/// <item><description>Display names for user interfaces (e.g., "display_name" -> "API Request Limit")</description></item>
		/// <item><description>Units of measurement (e.g., "unit" -> "requests", "unit" -> "GB")</description></item>
		/// <item><description>Category or grouping information (e.g., "category" -> "billing", "tier" -> "premium")</description></item>
		/// <item><description>Localization keys or any custom data needed for reporting and presentation purposes.</description></item>
		/// </list>
		/// This extensibility allows quota definitions to carry application-specific information without schema changes.
		/// </remarks>
		public Dictionary<string, string>? Metadata { get; set; }

		/// <summary>
		/// Gets or sets the time period over which this quota is measured and reset.
		/// </summary>
		/// <value>
		/// A <see cref="QuotaPeriod"/> value indicating when usage counters reset.
		/// Defaults to <see cref="QuotaPeriod.Monthly"/>.
		/// </value>
		/// <remarks>
		/// The period determines the quota's temporal behavior:
		/// <list type="bullet">
		/// <item><description><see cref="QuotaPeriod.Hourly"/>: Resets at the top of each hour</description></item>
		/// <item><description><see cref="QuotaPeriod.Daily"/>: Resets at midnight UTC each day</description></item>
		/// <item><description><see cref="QuotaPeriod.Monthly"/>: Resets at midnight UTC on the first day of each month</description></item>
		/// <item><description><see cref="QuotaPeriod.Total"/>: Never resets automatically (lifetime quota)</description></item>
		/// </list>
		/// The reset behavior is automatically handled by the quota store implementation based on this setting.
		/// </remarks>
		public QuotaPeriod Period { get; set; } = QuotaPeriod.Monthly;

		/// <summary>
		/// Gets or sets the granularity level at which this quota is enforced.
		/// </summary>
		/// <value>
		/// A <see cref="QuotaScope"/> value indicating whether the quota applies per tenant, resource, or user.
		/// Defaults to <see cref="QuotaScope.Tenant"/>.
		/// </value>
		/// <remarks>
		/// The scope determines how usage is tracked and aggregated:
		/// <list type="bullet">
		/// <item><description><see cref="QuotaScope.Tenant"/>: Quota is shared across all users and resources within a tenant</description></item>
		/// <item><description><see cref="QuotaScope.Resource"/>: Quota is tracked per individual resource type or entity</description></item>
		/// <item><description><see cref="QuotaScope.User"/>: Quota is isolated per individual user within a tenant</description></item>
		/// </list>
		/// Tenant-scoped quotas are the most common and prevent a single tenant from exceeding allocated limits.
		/// User-scoped quotas provide fairness by preventing individual users from consuming all tenant resources.
		/// </remarks>
		public QuotaScope Scope { get; set; } = QuotaScope.Tenant;

		#endregion
	}
}