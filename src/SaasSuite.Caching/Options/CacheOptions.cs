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

using SaasSuite.Caching.Helpers;
using SaasSuite.Caching.Interfaces;

namespace SaasSuite.Caching.Options
{
	/// <summary>
	/// Configuration options for controlling caching behavior in the SaasSuite framework.
	/// </summary>
	/// <remarks>
	/// These options control global caching behavior including default expiration times,
	/// tenant isolation settings, and cache key naming conventions. Options can be configured
	/// through the ASP.NET Core options pattern using <c>services.Configure&lt;CacheOptions&gt;</c>
	/// or via configuration files like appsettings.json.
	/// </remarks>
	public class CacheOptions
	{
		#region ' Properties '

		/// <summary>
		/// Gets or sets a value indicating whether tenant isolation is enabled for cache keys.
		/// </summary>
		/// <value>
		/// <see langword="true"/> to automatically enforce tenant isolation in cache keys; otherwise, <see langword="false"/>.
		/// Defaults to <see langword="true"/>.
		/// </value>
		/// <remarks>
		/// When enabled, cache keys should include tenant identifiers to prevent data leakage between tenants.
		/// Use <see cref="TenantCacheKeyHelper"/> to generate properly isolated keys.
		/// Disable this only for single-tenant deployments where isolation is not required.
		/// Disabling in multi-tenant scenarios can lead to serious security vulnerabilities.
		/// </remarks>
		public bool EnableTenantIsolation { get; set; } = true;

		/// <summary>
		/// Gets or sets the prefix prepended to all cache keys for namespace isolation.
		/// </summary>
		/// <value>
		/// A string prefix used in cache key construction. Defaults to "saas:".
		/// Cannot be <see langword="null"/>.
		/// </value>
		/// <remarks>
		/// The key prefix helps prevent collisions when multiple applications share the same cache store.
		/// It creates a namespace for your application's cache entries, making it easier to:
		/// <list type="bullet">
		/// <item><description>Identify cache entries belonging to your application</description></item>
		/// <item><description>Bulk delete application cache entries (e.g., during deployment)</description></item>
		/// <item><description>Monitor cache usage per application</description></item>
		/// </list>
		/// Use different prefixes for different environments (e.g., "saas:dev:", "saas:prod:") when sharing cache infrastructure.
		/// </remarks>
		public string KeyPrefix { get; set; } = "saas:";

		/// <summary>
		/// Gets or sets the default expiration time for cached items when no explicit expiration is specified.
		/// </summary>
		/// <value>
		/// A <see cref="TimeSpan"/> representing the default cache lifetime. Defaults to 30 minutes.
		/// </value>
		/// <remarks>
		/// This value is used by <see cref="ICacheService.SetAsync{T}(string, T, TimeSpan?, CancellationToken)"/>
		/// and <see cref="ICacheService.SetAsync{T}(string, T, CacheEntryOptions, CancellationToken)"/>
		/// when no explicit expiration is provided. Individual cache operations can override this default
		/// by providing an explicit expiration time. Consider adjusting this based on:
		/// <list type="bullet">
		/// <item><description>Data volatility (more volatile data = shorter expiration)</description></item>
		/// <item><description>Memory constraints (shorter expiration = less memory usage)</description></item>
		/// <item><description>Performance requirements (longer expiration = better performance)</description></item>
		/// </list>
		/// </remarks>
		public TimeSpan DefaultExpiration { get; set; } = TimeSpan.FromMinutes(30);

		#endregion
	}
}