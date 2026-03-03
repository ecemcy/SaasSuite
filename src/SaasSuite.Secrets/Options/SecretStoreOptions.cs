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

namespace SaasSuite.Secrets.Options
{
	/// <summary>
	/// Provides configuration options for secret store behavior including tenant scoping and caching.
	/// </summary>
	/// <remarks>
	/// <para>
	/// These options control how secrets are stored, retrieved, and cached in a multi-tenant environment.
	/// Proper configuration balances performance, security, and consistency requirements.
	/// </para>
	/// <para>
	/// <strong>Security Considerations:</strong>
	/// <list type="bullet">
	/// <item><description>Caching reduces secret store calls but delays propagation of rotated secrets</description></item>
	/// <item><description>The prefix template ensures tenant isolation; changing it requires secret migration</description></item>
	/// <item><description>Shorter cache durations improve security posture at the cost of performance</description></item>
	/// </list>
	/// </para>
	/// </remarks>
	/// <example>
	/// Configuration via appsettings.json:
	/// <code>
	/// {
	///   "SecretStore": {
	///     "EnableCaching": true,
	///     "CacheDurationSeconds": 600,
	///     "PrefixTemplate": "tenants/{tenantId}/"
	///   }
	/// }
	/// </code>
	///
	/// Configuration via code:
	/// <code>
	/// services.Configure&lt;SecretStoreOptions&gt;(options =>
	/// {
	///     options.EnableCaching = true;
	///     options.CacheDurationSeconds = 300;
	///     options.PrefixTemplate = "app/tenants/{tenantId}/";
	/// });
	/// </code>
	/// </example>
	public class SecretStoreOptions
	{
		#region ' Properties '

		/// <summary>
		/// Gets or sets a value indicating whether secret caching is enabled.
		/// </summary>
		/// <value>
		/// <see langword="true"/> to enable caching; otherwise, <see langword="false"/>.
		/// Default value is <see langword="false"/>.
		/// </value>
		/// <remarks>
		/// <para>
		/// Enabling caching can significantly improve performance by reducing calls to the underlying
		/// secret store, but it introduces a delay in the propagation of secret updates.
		/// </para>
		/// <para>
		/// <strong>Performance Impact:</strong> Caching can reduce secret retrieval latency from hundreds
		/// of milliseconds to microseconds for cached values.
		/// </para>
		/// <para>
		/// <strong>Security Impact:</strong> When secrets are rotated, cached values remain active until
		/// they expire, which may be acceptable for some scenarios but critical for others.
		/// </para>
		/// </remarks>
		public bool EnableCaching { get; set; } = false;

		/// <summary>
		/// Gets or sets the cache duration in seconds for cached secrets.
		/// </summary>
		/// <value>
		/// The number of seconds to cache secret values. Default value is <c>300</c> (5 minutes).
		/// Must be a positive value when caching is enabled.
		/// </value>
		/// <remarks>
		/// <para>
		/// This setting only applies when <see cref="EnableCaching"/> is <see langword="true"/>.
		/// </para>
		/// <para>
		/// <strong>Recommended Values:</strong>
		/// <list type="table">
		/// <listheader>
		/// <term>Scenario</term>
		/// <description>Duration</description>
		/// </listheader>
		/// <item>
		/// <term>High-security environments</term>
		/// <description>60-300 seconds (1-5 minutes)</description>
		/// </item>
		/// <item>
		/// <term>Standard applications</term>
		/// <description>300-900 seconds (5-15 minutes)</description>
		/// </item>
		/// <item>
		/// <term>Low-frequency changes</term>
		/// <description>900-3600 seconds (15-60 minutes)</description>
		/// </item>
		/// </list>
		/// </para>
		/// </remarks>
		public int CacheDurationSeconds { get; set; } = 300;

		/// <summary>
		/// Gets or sets the template string used to prefix secret names with tenant information.
		/// </summary>
		/// <value>
		/// A string template where <c>{tenantId}</c> will be replaced with the actual tenant identifier.
		/// Default value is <c>"tenants/{tenantId}/"</c>.
		/// </value>
		/// <remarks>
		/// <para>
		/// This prefix ensures tenant isolation by namespacing secrets per tenant. All secret operations
		/// automatically prepend this prefix to maintain multi-tenant separation.
		/// </para>
		/// <para>
		/// <strong>Template Rules:</strong>
		/// <list type="bullet">
		/// <item><description>Must contain the exact placeholder <c>{tenantId}</c></description></item>
		/// <item><description>Should end with a separator character (typically <c>/</c> or <c>-</c>)</description></item>
		/// <item><description>Must comply with the naming rules of the underlying secret store</description></item>
		/// </list>
		/// </para>
		/// <para>
		/// <strong>Migration Warning:</strong> Changing this template in production requires migrating
		/// all existing secrets to the new naming scheme.
		/// </para>
		/// </remarks>
		/// <example>
		/// Common template patterns:
		/// <code>
		/// // Hierarchical path style (default)
		/// "tenants/{tenantId}/"
		///
		/// // Flat with separator
		/// "{tenantId}-"
		///
		/// // Application-scoped
		/// "myapp/tenants/{tenantId}/"
		///
		/// // Environment-aware
		/// "prod/tenants/{tenantId}/"
		/// </code>
		/// </example>
		public string PrefixTemplate { get; set; } = "tenants/{tenantId}/";

		#endregion
	}
}