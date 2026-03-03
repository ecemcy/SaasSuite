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

using System.Collections.Concurrent;

using SaasSuite.Core;
using SaasSuite.Features.Interfaces;

namespace SaasSuite.Features.Services
{
	/// <summary>
	/// In-memory implementation of the <see cref="IFeatureService"/> interface using thread-safe collections.
	/// Provides feature flag management with tenant-specific overrides and global defaults, suitable for
	/// development, testing, and single-instance production deployments.
	/// </summary>
	/// <remarks>
	/// This implementation stores all feature flag data in memory using concurrent collections for thread safety.
	/// Key characteristics:
	/// <list type="bullet">
	/// <item><description>All data is lost when the application restarts - not persistent</description></item>
	/// <item><description>Thread-safe for concurrent access within a single process</description></item>
	/// <item><description>Not synchronized across multiple application instances</description></item>
	/// <item><description>Suitable for development, testing, and single-instance deployments</description></item>
	/// <item><description>Zero external dependencies or configuration required</description></item>
	/// </list>
	/// <para>
	/// For production multi-instance deployments, consider replacing this implementation with a persistent
	/// version that uses:
	/// <list type="bullet">
	/// <item><description>Database storage (SQL Server, PostgreSQL, etc.) for durability</description></item>
	/// <item><description>Distributed cache (Redis, Memcached) for performance and consistency</description></item>
	/// <item><description>External feature flag service (LaunchDarkly, Azure App Configuration, etc.)</description></item>
	/// </list>
	/// </para>
	/// <para>
	/// The implementation supports a two-tier feature flag system:
	/// <list type="number">
	/// <item><description>Global features - Apply to all tenants unless overridden</description></item>
	/// <item><description>Tenant-specific features - Override global settings for individual tenants</description></item>
	/// </list>
	/// </para>
	/// </remarks>
	public class FeatureService
		: IFeatureService
	{
		#region ' Fields '

		/// <summary>
		/// Thread-safe dictionary storing global feature flag defaults that apply to all tenants.
		/// Key is the feature name, value is a boolean indicating whether the feature is globally enabled or disabled.
		/// </summary>
		/// <remarks>
		/// Global features serve as defaults when a tenant has no specific override defined. This allows:
		/// <list type="bullet">
		/// <item><description>Setting application-wide feature defaults</description></item>
		/// <item><description>Quickly enabling/disabling features for all tenants</description></item>
		/// <item><description>Reducing storage by only persisting tenant-specific overrides</description></item>
		/// </list>
		/// Tenant-specific settings in <see cref="_tenantFeatures"/> always take precedence over global settings.
		/// </remarks>
		private readonly ConcurrentDictionary<string, bool> _globalFeatures = new ConcurrentDictionary<string, bool>();

		/// <summary>
		/// Thread-safe dictionary storing tenant-specific feature flag overrides.
		/// Key is the tenant ID string, value is a nested dictionary mapping feature names to their enabled/disabled state.
		/// </summary>
		/// <remarks>
		/// This nested structure allows efficient lookup of tenant-specific feature settings:
		/// <code>
		/// _tenantFeatures[tenantId][featureName] = enabled
		/// </code>
		/// Each tenant has its own independent dictionary of feature flags, enabling tenant-specific overrides
		/// that don't affect other tenants. The outer dictionary uses tenant IDs as keys, while the inner
		/// dictionaries use feature names as keys with boolean values indicating enabled/disabled state.
		/// </remarks>
		private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, bool>> _tenantFeatures = new ConcurrentDictionary<string, ConcurrentDictionary<string, bool>>();

		#endregion

		#region ' Methods '

		/// <summary>
		/// Sets a global feature flag that applies to all tenants unless overridden by tenant-specific settings.
		/// This method allows configuring application-wide feature defaults.
		/// </summary>
		/// <param name="featureName">The name of the feature to configure. Must be a non-empty, non-whitespace string. Feature names are case-sensitive.</param>
		/// <param name="enabled">
		/// <see langword="true"/> to enable the feature globally for all tenants without specific overrides;
		/// <see langword="false"/> to disable the feature globally.
		/// </param>
		/// <exception cref="ArgumentException">Thrown when <paramref name="featureName"/> is <see langword="null"/>, empty, or contains only whitespace.</exception>
		/// <remarks>
		/// Global features serve as defaults when tenants don't have specific overrides configured.
		/// This is useful for:
		/// <list type="bullet">
		/// <item><description>Setting initial feature states for all tenants</description></item>
		/// <item><description>Quickly enabling or disabling features application-wide</description></item>
		/// <item><description>Establishing baseline feature availability</description></item>
		/// </list>
		/// <para>
		/// Tenant-specific overrides configured via <see cref="EnableFeatureAsync"/> or <see cref="DisableFeatureAsync"/>
		/// always take precedence over global settings. This allows fine-grained control while maintaining
		/// reasonable defaults.
		/// </para>
		/// <para>
		/// This is a public method that is not part of the <see cref="IFeatureService"/> interface.
		/// It's specific to this in-memory implementation and is typically called during application
		/// initialization or by administrative endpoints.
		/// </para>
		/// </remarks>
		public void SetGlobalFeature(string featureName, bool enabled)
		{
			// Validate that feature name is not null, empty, or whitespace
			if (string.IsNullOrWhiteSpace(featureName))
			{
				throw new ArgumentException("Feature name cannot be null or whitespace", nameof(featureName));
			}

			// Set or update the global feature flag value
			// This affects all tenants that don't have a specific override
			this._globalFeatures[featureName] = enabled;
		}

		/// <summary>
		/// Disables a specific feature for the given tenant, creating a tenant-specific override.
		/// If the feature was previously enabled or not set, it will be explicitly disabled for this tenant.
		/// </summary>
		/// <param name="tenantId">The unique identifier of the tenant for which to disable the feature. Cannot be <see langword="null"/>.</param>
		/// <param name="featureName">The name of the feature to disable. Must be a non-empty, non-whitespace string. Feature names are case-sensitive.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests. Not used in this implementation but included for interface compliance.</param>
		/// <returns>A completed task indicating the operation has finished successfully.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="tenantId"/> is <see langword="null"/>.</exception>
		/// <exception cref="ArgumentException">Thrown when <paramref name="featureName"/> is <see langword="null"/>, empty, or contains only whitespace.</exception>
		/// <remarks>
		/// This method creates or updates a tenant-specific feature flag setting to disabled. The change is immediately
		/// visible to subsequent calls to <see cref="IsEnabledAsync"/> for this tenant and feature.
		/// This override takes precedence over global settings, meaning even if a feature is globally enabled,
		/// it will be disabled for this specific tenant.
		/// </remarks>
		public Task DisableFeatureAsync(TenantId tenantId, string featureName, CancellationToken cancellationToken = default)
		{
			// Validate that tenant ID is not null
			ArgumentNullException.ThrowIfNull(tenantId.Value, nameof(tenantId));

			// Validate that feature name is not null, empty, or whitespace
			if (string.IsNullOrWhiteSpace(featureName))
			{
				throw new ArgumentException("Feature name cannot be null or whitespace", nameof(featureName));
			}

			// Convert tenant ID to string for dictionary lookup
			string key = tenantId.Value;

			// Get or create the tenant's feature dictionary if it doesn't exist
			ConcurrentDictionary<string, bool> tenantFeatureDict = this._tenantFeatures.GetOrAdd(key, _ => new ConcurrentDictionary<string, bool>());

			// Set the feature to disabled (false) for this tenant
			tenantFeatureDict[featureName] = false;

			return Task.CompletedTask;
		}

		/// <summary>
		/// Enables a specific feature for the given tenant, creating a tenant-specific override.
		/// If the feature was previously disabled or not set, it will be explicitly enabled for this tenant.
		/// </summary>
		/// <param name="tenantId">The unique identifier of the tenant for which to enable the feature. Cannot be <see langword="null"/>.</param>
		/// <param name="featureName">The name of the feature to enable. Must be a non-empty, non-whitespace string. Feature names are case-sensitive.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests. Not used in this implementation but included for interface compliance.</param>
		/// <returns>A completed task indicating the operation has finished successfully.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="tenantId"/> is <see langword="null"/>.</exception>
		/// <exception cref="ArgumentException">Thrown when <paramref name="featureName"/> is <see langword="null"/>, empty, or contains only whitespace.</exception>
		/// <remarks>
		/// This method creates or updates a tenant-specific feature flag setting. The change is immediately
		/// visible to subsequent calls to <see cref="IsEnabledAsync"/> for this tenant and feature.
		/// The setting persists in memory until the application restarts or the feature is disabled.
		/// </remarks>
		public Task EnableFeatureAsync(TenantId tenantId, string featureName, CancellationToken cancellationToken = default)
		{
			// Validate that tenant ID is not null
			ArgumentNullException.ThrowIfNull(tenantId.Value, nameof(tenantId));

			// Validate that feature name is not null, empty, or whitespace
			if (string.IsNullOrWhiteSpace(featureName))
			{
				throw new ArgumentException("Feature name cannot be null or whitespace", nameof(featureName));
			}

			// Convert tenant ID to string for dictionary lookup
			string key = tenantId.Value;

			// Get or create the tenant's feature dictionary if it doesn't exist
			ConcurrentDictionary<string, bool> tenantFeatureDict = this._tenantFeatures.GetOrAdd(key, _ => new ConcurrentDictionary<string, bool>());

			// Set the feature to enabled (true) for this tenant
			tenantFeatureDict[featureName] = true;

			return Task.CompletedTask;
		}

		/// <summary>
		/// Checks whether a specific feature is enabled for the given tenant.
		/// Evaluates tenant-specific settings first, then falls back to global settings, and finally defaults to <see langword="false"/>.
		/// </summary>
		/// <param name="tenantId">The unique identifier of the tenant for which to check the feature status. Cannot be <see langword="null"/>.</param>
		/// <param name="featureName">The name of the feature to check. Must be a non-empty, non-whitespace string. Feature names are case-sensitive.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests. Not used in this implementation but included for interface compliance.</param>
		/// <returns>
		/// A completed task with result <see langword="true"/> if the feature is enabled for the specified tenant;
		/// otherwise <see langword="false"/>.
		/// </returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="tenantId"/> is <see langword="null"/>.</exception>
		/// <exception cref="ArgumentException">Thrown when <paramref name="featureName"/> is <see langword="null"/>, empty, or contains only whitespace.</exception>
		/// <remarks>
		/// The feature resolution follows this precedence order:
		/// <list type="number">
		/// <item><description>Check for tenant-specific override - if found, return that value</description></item>
		/// <item><description>Check for global feature default - if found, return that value</description></item>
		/// <item><description>Return <see langword="false"/> if feature is not defined anywhere</description></item>
		/// </list>
		/// This method is synchronous despite returning a <see cref="Task"/> to maintain interface compatibility
		/// with asynchronous implementations that may perform I/O operations.
		/// </remarks>
		public Task<bool> IsEnabledAsync(TenantId tenantId, string featureName, CancellationToken cancellationToken = default)
		{
			// Validate that tenant ID is not null
			ArgumentNullException.ThrowIfNull(tenantId.Value, nameof(tenantId));

			// Validate that feature name is not null, empty, or whitespace
			if (string.IsNullOrWhiteSpace(featureName))
			{
				throw new ArgumentException("Feature name cannot be null or whitespace", nameof(featureName));
			}

			// Convert tenant ID to string for dictionary lookup
			string key = tenantId.Value;

			// First, check for tenant-specific feature override
			if (this._tenantFeatures.TryGetValue(key, out ConcurrentDictionary<string, bool>? tenantFeatureDict))
			{
				// If tenant has feature overrides, check if this specific feature is configured
				if (tenantFeatureDict.TryGetValue(featureName, out bool enabled))
				{
					// Return the tenant-specific setting (highest priority)
					return Task.FromResult(enabled);
				}
			}

			// Second, fall back to global feature default if no tenant override exists
			if (this._globalFeatures.TryGetValue(featureName, out bool globalEnabled))
			{
				// Return the global default setting
				return Task.FromResult(globalEnabled);
			}

			// Feature not defined anywhere, default to disabled (false)
			return Task.FromResult(false);
		}

		/// <summary>
		/// Retrieves all feature flags and their current enabled/disabled status for the specified tenant.
		/// Returns a consolidated view combining global defaults with tenant-specific overrides.
		/// </summary>
		/// <param name="tenantId">The unique identifier of the tenant for which to retrieve all feature statuses. Cannot be <see langword="null"/>.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests. Not used in this implementation but included for interface compliance.</param>
		/// <returns>
		/// A completed task containing a dictionary where keys are feature names and values are boolean flags
		/// indicating enabled/disabled status. Returns an empty dictionary if no features are defined.
		/// </returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="tenantId"/> is <see langword="null"/>.</exception>
		/// <remarks>
		/// The returned dictionary is built by:
		/// <list type="number">
		/// <item><description>First adding all global features with their default values</description></item>
		/// <item><description>Then overriding with tenant-specific settings where they exist</description></item>
		/// </list>
		/// This ensures tenant-specific overrides always take precedence over global defaults in the final result.
		/// The returned dictionary is a snapshot and may not reflect concurrent changes made after the call.
		/// </remarks>
		public Task<IDictionary<string, bool>> GetAllFeaturesAsync(TenantId tenantId, CancellationToken cancellationToken = default)
		{
			// Validate that tenant ID is not null
			ArgumentNullException.ThrowIfNull(tenantId.Value, nameof(tenantId));

			// Convert tenant ID to string for dictionary lookup
			string key = tenantId.Value;

			// Create a new dictionary to hold the consolidated feature states
			Dictionary<string, bool> result = new Dictionary<string, bool>();

			// First, add all global features with their default values
			// These serve as the baseline for all tenants
			foreach (KeyValuePair<string, bool> kvp in this._globalFeatures)
			{
				result[kvp.Key] = kvp.Value;
			}

			// Then, override with tenant-specific features if they exist
			// Tenant-specific settings take precedence over global defaults
			if (this._tenantFeatures.TryGetValue(key, out ConcurrentDictionary<string, bool>? tenantFeatureDict))
			{
				foreach (KeyValuePair<string, bool> kvp in tenantFeatureDict)
				{
					// This will add new tenant-specific features or override global defaults
					result[kvp.Key] = kvp.Value;
				}
			}

			// Return the consolidated dictionary as a task
			return Task.FromResult<IDictionary<string, bool>>(result);
		}

		#endregion
	}
}