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

namespace SaasSuite.Core
{
	/// <summary>
	/// Represents tenant-specific configuration settings, quotas, and feature flags.
	/// Defines configurable aspects of tenant behavior including resource limits, enabled features, and security settings.
	/// </summary>
	/// <remarks>
	/// This class is used within <see cref="TenantInfo"/> to store business logic configuration that varies
	/// per tenant. Settings can be used to implement tiered service plans, enforce usage limits, enable/disable
	/// features, and customize tenant behavior. The <see cref="CustomSettings"/> dictionary provides extensibility
	/// for application-specific configuration without modifying the core model.
	/// </remarks>
	public class TenantSettings
	{
		#region ' Properties '

		/// <summary>
		/// Gets or sets a value indicating whether data encryption at rest is enabled for this tenant.
		/// Determines if tenant data should be encrypted when stored in databases or file systems.
		/// </summary>
		/// <value>
		/// <see langword="true"/> to enable encryption at rest for tenant data; <see langword="false"/>
		/// to use standard storage without encryption. Defaults to <see langword="true"/> for security.
		/// Required for tenants with compliance requirements such as HIPAA, PCI-DSS, or GDPR.
		/// </value>
		public bool EncryptionEnabled { get; set; } = true;

		/// <summary>
		/// Gets or sets the data retention period in days for this tenant.
		/// Defines how long tenant data should be retained before automatic deletion or archival.
		/// </summary>
		/// <value>
		/// The number of days to retain data, or <see langword="null"/> for indefinite retention.
		/// Used by cleanup jobs and archival processes to enforce compliance and data lifecycle policies.
		/// Common values range from 30 days for logs to 7 years for financial records.
		/// </value>
		public int? DataRetentionDays { get; set; }

		/// <summary>
		/// Gets or sets the maximum number of users allowed for this tenant.
		/// Enforces user account limits based on subscription tier or plan.
		/// </summary>
		/// <value>
		/// The maximum number of user accounts that can be created for this tenant,
		/// or <see langword="null"/> for unlimited users. Used to enforce subscription
		/// limits and prevent tenant over-provisioning.
		/// </value>
		public int? MaxUsers { get; set; }

		/// <summary>
		/// Gets or sets the maximum storage quota for this tenant in bytes.
		/// Limits the total amount of data the tenant can store in the system.
		/// </summary>
		/// <value>
		/// The maximum storage capacity in bytes, or <see langword="null"/> for unlimited storage.
		/// Used to enforce storage quotas for uploads, attachments, and tenant-specific data.
		/// Should be checked before allowing file uploads or data creation operations.
		/// </value>
		public long? MaxStorageBytes { get; set; }

		/// <summary>
		/// Gets or sets the service tier or plan level for this tenant.
		/// Identifies the tenant's subscription level for billing and feature access.
		/// </summary>
		/// <value>
		/// A string identifier for the tenant's service tier such as "Free", "Professional", "Enterprise",
		/// or <see langword="null"/> if not using tiered plans. Used for billing, feature gating,
		/// and displaying plan information in the UI.
		/// </value>
		public string? Tier { get; set; }

		/// <summary>
		/// Gets or sets the collection of feature flags enabled for this tenant.
		/// Controls which application features are available based on subscription tier or configuration.
		/// </summary>
		/// <value>
		/// A collection of feature flag names (strings) that are enabled for this tenant.
		/// Defaults to an empty list. Feature names are typically constants defined in the application.
		/// Use <see cref="TenantContextExtensions.IsFeatureEnabled"/> for easy feature checking.
		/// </value>
		/// <remarks>
		/// Common feature flags might include:
		/// <list type="bullet">
		/// <item><description>"AdvancedReporting" - Access to premium reporting features</description></item>
		/// <item><description>"APIAccess" - Ability to use REST APIs</description></item>
		/// <item><description>"CustomBranding" - UI customization capabilities</description></item>
		/// <item><description>"SSOIntegration" - Single sign-on support</description></item>
		/// </list>
		/// </remarks>
		public ICollection<string> EnabledFeatures { get; set; } = new List<string>();

		/// <summary>
		/// Gets or sets the collection of email domain names that are allowed for user accounts.
		/// Restricts user registration to specific email domains for enhanced security.
		/// </summary>
		/// <value>
		/// A collection of allowed email domain names (e.g., "example.com", "acme.org"),
		/// or <see langword="null"/> to allow any email domain. Used during user registration
		/// and invitation processes to enforce domain restrictions for enterprise tenants.
		/// Domains should be validated and normalized (lowercase, no @ symbol).
		/// </value>
		public ICollection<string>? AllowedDomains { get; set; }

		/// <summary>
		/// Gets or sets custom configuration values specific to the application.
		/// Provides an extensibility mechanism for tenant-specific settings without modifying the core model.
		/// </summary>
		/// <value>
		/// A dictionary of custom setting key-value pairs. Keys are setting names (strings) and values
		/// can be any object type. Defaults to an empty dictionary. Use type-safe access via
		/// <see cref="TenantContextExtensions.GetSetting{T}"/> to retrieve values with casting.
		/// </value>
		/// <remarks>
		/// This dictionary enables applications to store tenant-specific configuration without extending
		/// the <see cref="TenantSettings"/> class. Values should be serializable for persistence.
		/// Consider using well-known constant keys to avoid naming collisions.
		/// </remarks>
		public IDictionary<string, object> CustomSettings { get; set; } = new Dictionary<string, object>();

		#endregion
	}
}