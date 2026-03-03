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

using SaasSuite.Core.Enumerations;
using SaasSuite.Core.Interfaces;

namespace SaasSuite.Core
{
	/// <summary>
	/// Represents comprehensive tenant metadata and configuration information.
	/// Contains all essential data about a tenant including identity, settings, isolation configuration, and lifecycle timestamps.
	/// </summary>
	/// <remarks>
	/// This class serves as the primary data model for tenant information throughout the application.
	/// It is loaded by <see cref="ITenantStore"/> during tenant resolution and included
	/// in the <see cref="TenantContext"/> for access by application components. The model supports
	/// various multi-tenancy patterns through the <see cref="IsolationLevel"/> property and provides
	/// extensibility through metadata and settings collections.
	/// </remarks>
	public class TenantInfo
	{
		#region ' Properties '

		/// <summary>
		/// Gets or sets a value indicating whether the tenant is currently active and operational.
		/// Inactive tenants may be suspended, pending activation, or soft-deleted.
		/// </summary>
		/// <value>
		/// <see langword="true"/> if the tenant is active and users can access the system;
		/// <see langword="false"/> if the tenant is suspended, disabled, or pending activation.
		/// Defaults to <see langword="true"/>. Used to prevent access to deactivated tenants.
		/// </value>
		public bool IsActive { get; set; } = true;

		/// <summary>
		/// Gets or sets the display name of the tenant.
		/// Provides a human-readable name for the tenant organization or account.
		/// </summary>
		/// <value>
		/// The tenant's display name such as "Acme Corporation" or "Enterprise Client A".
		/// Defaults to an empty string. Used in UI displays, logs, and reports.
		/// </value>
		public string Name { get; set; } = string.Empty;

		/// <summary>
		/// Gets or sets the database connection string for tenant data storage.
		/// Contains the connection information for accessing tenant-specific databases or schemas.
		/// </summary>
		/// <value>
		/// A full database connection string, or <see langword="null"/> if the tenant uses a shared
		/// database with logical separation. Required for <see cref="IsolationLevel.Dedicated"/> tenants.
		/// Should be encrypted when stored and secured when accessed.
		/// </value>
		public string? ConnectionString { get; set; }

		/// <summary>
		/// Gets or sets an alternative identifier key for tenant resolution.
		/// Used for resolving tenants via subdomain, hostname, or other URL-based strategies.
		/// </summary>
		/// <value>
		/// An identifier such as a subdomain ("acme"), custom domain ("acme.example.com"),
		/// or slug ("acme-corp"). May be <see langword="null"/> if not using identifier-based resolution.
		/// Must be unique across all tenants if set.
		/// </value>
		public string? Identifier { get; set; }

		/// <summary>
		/// Gets or sets the date and time when the tenant was created.
		/// Provides an audit trail for tenant provisioning.
		/// </summary>
		/// <value>
		/// The date and time when this tenant was first created in the system.
		/// Defaults to the current UTC time when a new instance is created.
		/// Used for reporting, billing, and compliance purposes.
		/// </value>
		public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

		/// <summary>
		/// Gets or sets the date and time when the tenant information was last modified.
		/// Provides an audit trail for tenant configuration changes.
		/// </summary>
		/// <value>
		/// The date and time when this tenant's information was last updated, or <see langword="null"/>
		/// if the tenant has never been modified since creation. Should be updated whenever
		/// tenant properties, settings, or metadata are changed.
		/// </value>
		public DateTimeOffset? UpdatedAt { get; set; }

		/// <summary>
		/// Gets or sets additional custom metadata for extensibility.
		/// Provides a flexible key-value store for tenant-specific data not covered by standard properties.
		/// </summary>
		/// <value>
		/// A dictionary of custom metadata key-value pairs for storing additional information
		/// such as billing identifiers, CRM references, or integration tokens.
		/// May be <see langword="null"/> if no additional metadata is needed.
		/// All values are stored as strings for simplicity.
		/// </value>
		public IDictionary<string, string>? Metadata { get; set; }

		/// <summary>
		/// Gets or sets the isolation level that defines how tenant data and resources are separated.
		/// Determines the multi-tenancy strategy applied to this tenant.
		/// </summary>
		/// <value>
		/// The <see cref="IsolationLevel"/> specifying whether the tenant uses shared, dedicated,
		/// hybrid, or no isolation. This value guides data access strategies, security policies,
		/// and resource allocation throughout the application.
		/// </value>
		public IsolationLevel IsolationLevel { get; set; }

		/// <summary>
		/// Gets or sets the unique identifier for the tenant.
		/// This is the primary key used to reference the tenant throughout the system.
		/// </summary>
		/// <value>
		/// The <see cref="TenantId"/> that uniquely identifies this tenant. Used in tenant resolution,
		/// context storage, and all tenant-specific operations.
		/// </value>
		public TenantId Id { get; set; }

		/// <summary>
		/// Gets or sets tenant-specific configuration settings and feature flags.
		/// Contains business logic configuration including quotas, features, and custom settings.
		/// </summary>
		/// <value>
		/// A <see cref="TenantSettings"/> object containing all configurable aspects of the tenant
		/// such as user limits, storage quotas, enabled features, and tier information.
		/// May be <see langword="null"/> if default settings should be applied.
		/// </value>
		public TenantSettings? Settings { get; set; }

		#endregion
	}
}