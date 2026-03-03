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

using SaasSuite.Features.Interfaces;

namespace SaasSuite.Features
{
	/// <summary>
	/// Represents a feature flag definition with metadata describing the feature's purpose, default state, and categorization.
	/// This model defines the structure and attributes of a feature flag that can be toggled on or off for tenants
	/// in a multi-tenant environment.
	/// </summary>
	/// <remarks>
	/// Feature flags (also known as feature toggles) are a software development technique that allows teams to
	/// modify system behavior without changing code, enabling:
	/// <list type="bullet">
	/// <item><description>Gradual feature rollouts to specific tenants or user groups</description></item>
	/// <item><description>A/B testing of new functionality</description></item>
	/// <item><description>Emergency kill switches for problematic features</description></item>
	/// <item><description>Trunk-based development with features hidden until ready</description></item>
	/// <item><description>Tiered feature access based on subscription levels</description></item>
	/// </list>
	/// <para>
	/// This class defines the metadata for a feature flag but does not contain its current state for any specific tenant.
	/// The actual enabled/disabled state is managed by implementations of <see cref="IFeatureService"/>
	/// and can vary per tenant based on tenant-specific overrides and global defaults.
	/// </para>
	/// <para>
	/// Feature flag definitions are typically loaded from configuration files, databases, or feature management
	/// services and used to initialize the feature flag system at application startup.
	/// </para>
	/// </remarks>
	public class FeatureFlag
	{
		#region ' Properties '

		/// <summary>
		/// Gets or sets a value indicating whether the feature is enabled by default for all tenants when no tenant-specific override exists.
		/// This serves as the global default state for the feature across the entire application.
		/// </summary>
		/// <value>
		/// <see langword="true"/> if the feature should be enabled by default for all tenants;
		/// <see langword="false"/> if the feature should be disabled by default.
		/// Defaults to <see langword="false"/> for conservative feature rollout.
		/// </value>
		/// <remarks>
		/// The default enabled state is used when:
		/// <list type="bullet">
		/// <item><description>A tenant has no specific override setting for this feature</description></item>
		/// <item><description>The feature is newly added and tenants haven't been configured yet</description></item>
		/// <item><description>Tenant-specific settings are being cleared or reset</description></item>
		/// </list>
		/// <para>
		/// Setting this to <see langword="true"/> means the feature is "opt-out" (enabled for everyone unless explicitly disabled).
		/// Setting this to <see langword="false"/> means the feature is "opt-in" (disabled for everyone unless explicitly enabled).
		/// </para>
		/// <para>
		/// Tenant-specific overrides configured via <see cref="IFeatureService.EnableFeatureAsync"/>
		/// or <see cref="IFeatureService.DisableFeatureAsync"/> always take precedence over this default value.
		/// </para>
		/// <para>
		/// Best practices:
		/// <list type="bullet">
		/// <item><description>Use <see langword="false"/> for new, experimental, or high-risk features</description></item>
		/// <item><description>Use <see langword="true"/> for stable features that should be widely available</description></item>
		/// <item><description>Consider the impact on existing tenants when changing this value</description></item>
		/// </list>
		/// </para>
		/// </remarks>
		public bool EnabledByDefault { get; set; }

		/// <summary>
		/// Gets or sets the unique name identifier of the feature.
		/// This name is used throughout the application to reference and check the feature's status.
		/// </summary>
		/// <value>
		/// A string containing the feature's unique identifier. Must be unique across all features.
		/// Defaults to an empty string. Feature names are case-sensitive and should follow a consistent
		/// naming convention (e.g., "AdvancedReporting", "BetaUI", "ExperimentalApi").
		/// </value>
		/// <remarks>
		/// The feature name serves as the primary key for feature lookups in <see cref="IFeatureService"/>.
		/// Best practices for feature names:
		/// <list type="bullet">
		/// <item><description>Use PascalCase or camelCase for consistency</description></item>
		/// <item><description>Make names descriptive and self-documenting</description></item>
		/// <item><description>Consider namespacing for large applications (e.g., "Reporting.Advanced", "UI.Beta")</description></item>
		/// <item><description>Define feature names as constants to prevent typos</description></item>
		/// <item><description>Avoid special characters that may cause issues in configuration systems</description></item>
		/// </list>
		/// </remarks>
		public string Name { get; set; } = string.Empty;

		/// <summary>
		/// Gets or sets a human-readable description explaining the feature's purpose, impact, and usage.
		/// This description helps developers and administrators understand what the feature does and when to enable it.
		/// </summary>
		/// <value>
		/// A string containing the feature description, or <see langword="null"/> if no description is provided.
		/// Should clearly explain the feature's functionality, potential impact, and any prerequisites or dependencies.
		/// </value>
		/// <remarks>
		/// A well-written description should include:
		/// <list type="bullet">
		/// <item><description>What the feature does and which functionality it controls</description></item>
		/// <item><description>Who the intended users are (all users, administrators, specific roles)</description></item>
		/// <item><description>Any prerequisites or dependencies required for the feature to work</description></item>
		/// <item><description>Potential performance or resource implications</description></item>
		/// <item><description>Whether the feature is experimental, beta, or production-ready</description></item>
		/// </list>
		/// This description is typically displayed in feature management UIs to help administrators make
		/// informed decisions about which features to enable for specific tenants.
		/// </remarks>
		public string? Description { get; set; }

		/// <summary>
		/// Gets or sets the collection of tags used to categorize and group related features.
		/// Tags enable filtering, searching, and organizing features in management interfaces and reports.
		/// </summary>
		/// <value>
		/// A read-only list of string tags associated with this feature. Defaults to an empty array.
		/// Tags are typically lowercase and may include categories like "beta", "premium", "experimental",
		/// "reporting", "ui", "api", or version identifiers like "v2", "2024-q1".
		/// </value>
		/// <remarks>
		/// Tags provide flexible metadata for features beyond their name and description. Common uses include:
		/// <list type="bullet">
		/// <item><description>Categorization by feature area (e.g., "reporting", "analytics", "security")</description></item>
		/// <item><description>Maturity indicators (e.g., "beta", "experimental", "stable", "deprecated")</description></item>
		/// <item><description>Subscription tier associations (e.g., "premium", "enterprise", "free")</description></item>
		/// <item><description>Version or release tracking (e.g., "v2", "2024-q1", "spring-release")</description></item>
		/// <item><description>Platform or environment indicators (e.g., "web", "mobile", "api")</description></item>
		/// <item><description>Team or ownership identification (e.g., "team-alpha", "platform-team")</description></item>
		/// </list>
		/// <para>
		/// Tags enable bulk operations and queries such as:
		/// <list type="bullet">
		/// <item><description>Enabling all "beta" features for a pilot tenant</description></item>
		/// <item><description>Disabling all "experimental" features in production</description></item>
		/// <item><description>Generating reports of all "premium" features</description></item>
		/// <item><description>Finding all features related to "reporting"</description></item>
		/// </list>
		/// </para>
		/// <para>
		/// The collection is read-only to prevent accidental modification after initialization.
		/// To change tags, assign a new list or array to this property. An empty array indicates
		/// the feature has no tags, which is a valid state.
		/// </para>
		/// </remarks>
		public IReadOnlyList<string> Tags { get; set; } = Array.Empty<string>();

		#endregion
	}
}