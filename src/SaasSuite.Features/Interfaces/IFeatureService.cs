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

using SaasSuite.Core;

namespace SaasSuite.Features.Interfaces
{
	/// <summary>
	/// Defines operations for managing tenant-specific feature flags in a multi-tenant environment.
	/// Provides functionality to check, enable, disable, and retrieve feature flag states for individual tenants,
	/// enabling fine-grained control over feature availability and gradual feature rollouts.
	/// </summary>
	/// <remarks>
	/// This service supports multi-tenant feature flag management with the following capabilities:
	/// <list type="bullet">
	/// <item><description>Tenant-specific feature overrides that take precedence over global settings</description></item>
	/// <item><description>Fallback to global feature definitions when tenant-specific settings are not defined</description></item>
	/// <item><description>Runtime feature toggling without application restarts</description></item>
	/// <item><description>Bulk feature retrieval for efficient feature state queries</description></item>
	/// </list>
	/// <para>
	/// Implementations should consider:
	/// <list type="bullet">
	/// <item><description>Caching strategies to minimize database lookups for frequently checked features</description></item>
	/// <item><description>Thread-safety for concurrent access across multiple requests</description></item>
	/// <item><description>Audit logging for feature state changes for compliance and debugging</description></item>
	/// <item><description>Feature flag inheritance hierarchies (global, tier-level, tenant-level)</description></item>
	/// </list>
	/// </para>
	/// <para>
	/// Common use cases include:
	/// <list type="bullet">
	/// <item><description>A/B testing of new features with specific tenant groups</description></item>
	/// <item><description>Gradual feature rollouts starting with pilot tenants</description></item>
	/// <item><description>Emergency feature kill switches for problematic functionality</description></item>
	/// <item><description>Tiered feature access based on subscription levels</description></item>
	/// <item><description>Beta feature access for early adopter tenants</description></item>
	/// </list>
	/// </para>
	/// </remarks>
	public interface IFeatureService
	{
		#region ' Methods '

		/// <summary>
		/// Checks whether a specific feature is enabled for the given tenant.
		/// This method evaluates tenant-specific feature settings first, then falls back to global feature settings if no tenant-specific setting exists.
		/// </summary>
		/// <param name="tenantId">The unique identifier of the tenant for which to check the feature status. Cannot be <see langword="null"/>.</param>
		/// <param name="featureName">The name of the feature to check. Must be a non-empty, non-whitespace string. Feature names are case-sensitive.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests during the asynchronous operation.</param>
		/// <returns>
		/// A task that represents the asynchronous operation. The task result is <see langword="true"/> if the feature is enabled
		/// for the specified tenant; otherwise <see langword="false"/>. Returns <see langword="false"/> if the feature is not defined
		/// for the tenant and no global default exists.
		/// </returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="tenantId"/> is <see langword="null"/>.</exception>
		/// <exception cref="ArgumentException">Thrown when <paramref name="featureName"/> is <see langword="null"/>, empty, or contains only whitespace.</exception>
		/// <remarks>
		/// The feature resolution follows this precedence order:
		/// <list type="number">
		/// <item><description>Tenant-specific feature setting (if exists) - highest priority</description></item>
		/// <item><description>Global feature default (if exists) - fallback priority</description></item>
		/// <item><description>Default to <see langword="false"/> if feature is not defined anywhere</description></item>
		/// </list>
		/// <para>
		/// This method should be called before executing feature-gated code to determine whether the feature
		/// is available for the current tenant. It is safe to call frequently as implementations typically
		/// include caching mechanisms to optimize performance.
		/// </para>
		/// <para>
		/// For bulk feature checks, consider using <see cref="GetAllFeaturesAsync"/> to reduce the number
		/// of method calls and improve performance when multiple feature states are needed.
		/// </para>
		/// </remarks>
		Task<bool> IsEnabledAsync(TenantId tenantId, string featureName, CancellationToken cancellationToken = default);

		/// <summary>
		/// Disables a specific feature for the given tenant, creating a tenant-specific override that takes precedence over global settings.
		/// If the feature was previously enabled or not set for this tenant, it will be explicitly disabled.
		/// </summary>
		/// <param name="tenantId">The unique identifier of the tenant for which to disable the feature. Cannot be <see langword="null"/>.</param>
		/// <param name="featureName">The name of the feature to disable. Must be a non-empty, non-whitespace string. Feature names are case-sensitive.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests during the asynchronous operation.</param>
		/// <returns>A task that represents the asynchronous operation. The task completes when the feature has been disabled.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="tenantId"/> is <see langword="null"/>.</exception>
		/// <exception cref="ArgumentException">Thrown when <paramref name="featureName"/> is <see langword="null"/>, empty, or contains only whitespace.</exception>
		/// <remarks>
		/// This method creates or updates a tenant-specific feature flag setting to disabled. The change takes effect immediately
		/// for subsequent calls to <see cref="IsEnabledAsync"/> for this tenant and feature combination.
		/// <para>
		/// Disabling a feature for a tenant creates a tenant-specific override that persists until explicitly changed
		/// via <see cref="EnableFeatureAsync"/> or removed from the underlying storage. This override takes
		/// precedence over any global feature settings, meaning even if a feature is globally enabled, it will
		/// be disabled for this specific tenant.
		/// </para>
		/// <para>
		/// Use this method to:
		/// <list type="bullet">
		/// <item><description>Exclude specific tenants from a globally enabled feature</description></item>
		/// <item><description>Implement emergency kill switches for problematic features</description></item>
		/// <item><description>Control feature access based on subscription tier or tenant configuration</description></item>
		/// <item><description>Gradually roll back features that are causing issues</description></item>
		/// </list>
		/// </para>
		/// <para>
		/// Implementations should consider the same caching and notification concerns as <see cref="EnableFeatureAsync"/>.
		/// </para>
		/// </remarks>
		Task DisableFeatureAsync(TenantId tenantId, string featureName, CancellationToken cancellationToken = default);

		/// <summary>
		/// Enables a specific feature for the given tenant, creating a tenant-specific override that takes precedence over global settings.
		/// If the feature was previously disabled or not set for this tenant, it will be explicitly enabled.
		/// </summary>
		/// <param name="tenantId">The unique identifier of the tenant for which to enable the feature. Cannot be <see langword="null"/>.</param>
		/// <param name="featureName">The name of the feature to enable. Must be a non-empty, non-whitespace string. Feature names are case-sensitive.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests during the asynchronous operation.</param>
		/// <returns>A task that represents the asynchronous operation. The task completes when the feature has been enabled.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="tenantId"/> is <see langword="null"/>.</exception>
		/// <exception cref="ArgumentException">Thrown when <paramref name="featureName"/> is <see langword="null"/>, empty, or contains only whitespace.</exception>
		/// <remarks>
		/// This method creates or updates a tenant-specific feature flag setting. The change takes effect immediately
		/// for subsequent calls to <see cref="IsEnabledAsync"/> for this tenant and feature combination.
		/// <para>
		/// Enabling a feature for a tenant creates a tenant-specific override that persists until explicitly changed
		/// via <see cref="DisableFeatureAsync"/> or removed from the underlying storage. This override takes
		/// precedence over any global feature settings.
		/// </para>
		/// <para>
		/// Implementations should consider:
		/// <list type="bullet">
		/// <item><description>Invalidating any cached feature states for the tenant</description></item>
		/// <item><description>Logging the feature state change for audit purposes</description></item>
		/// <item><description>Notifying other application instances in multi-instance deployments</description></item>
		/// <item><description>Persisting the change to durable storage for consistency across restarts</description></item>
		/// </list>
		/// </para>
		/// </remarks>
		Task EnableFeatureAsync(TenantId tenantId, string featureName, CancellationToken cancellationToken = default);

		/// <summary>
		/// Retrieves all feature flags and their current enabled/disabled status for the specified tenant.
		/// The result includes both global features and tenant-specific overrides, with tenant-specific settings taking precedence.
		/// </summary>
		/// <param name="tenantId">The unique identifier of the tenant for which to retrieve all feature statuses. Cannot be <see langword="null"/>.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests during the asynchronous operation.</param>
		/// <returns>
		/// A task that represents the asynchronous operation. The task result is a dictionary where keys are feature names (strings)
		/// and values are boolean flags indicating whether each feature is enabled (<see langword="true"/>) or disabled (<see langword="false"/>)
		/// for the specified tenant. Returns an empty dictionary if no features are defined.
		/// </returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="tenantId"/> is <see langword="null"/>.</exception>
		/// <remarks>
		/// This method returns a consolidated view of all features available to the tenant, combining:
		/// <list type="bullet">
		/// <item><description>Global feature defaults that apply to all tenants</description></item>
		/// <item><description>Tenant-specific feature overrides that supersede global defaults</description></item>
		/// </list>
		/// <para>
		/// The returned dictionary reflects the effective feature state that would be returned by calling
		/// <see cref="IsEnabledAsync"/> for each individual feature. This method is more efficient than
		/// making multiple individual feature checks when you need to evaluate multiple features.
		/// </para>
		/// <para>
		/// Use this method to:
		/// <list type="bullet">
		/// <item><description>Display a feature management UI showing all available features</description></item>
		/// <item><description>Initialize client-side feature flag caches</description></item>
		/// <item><description>Generate feature availability reports</description></item>
		/// <item><description>Perform bulk feature checks at application startup</description></item>
		/// </list>
		/// </para>
		/// <para>
		/// The returned dictionary is a snapshot of feature states at the time of the call. Feature states
		/// may change after the dictionary is returned if features are enabled or disabled concurrently.
		/// </para>
		/// </remarks>
		Task<IDictionary<string, bool>> GetAllFeaturesAsync(TenantId tenantId, CancellationToken cancellationToken = default);

		#endregion
	}
}