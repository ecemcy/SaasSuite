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

namespace SaasSuite.Core
{
	/// <summary>
	/// Provides convenience extension methods for querying common tenant metadata from a <see cref="TenantContext"/> instance.
	/// </summary>
	/// <remarks>
	/// These helpers simplify access to frequently-used tenant information and provide null-safe operations,
	/// eliminating the need for repetitive null-checking in application code.
	/// </remarks>
	public static class TenantContextExtensions
	{
		#region ' Static Methods '

		/// <summary>
		/// Checks if the tenant is currently active and operational.
		/// Inactive tenants may be suspended, disabled, or pending activation.
		/// </summary>
		/// <param name="context">The tenant context to check. May be <see langword="null"/>.</param>
		/// <returns>
		/// <see langword="true"/> if the tenant is active; otherwise <see langword="false"/>.
		/// </returns>
		/// <remarks>
		/// This method safely checks the tenant's active status without throwing exceptions.
		/// Returns <see langword="false"/> if:
		/// <list type="bullet">
		/// <item><description>The context is <see langword="null"/></description></item>
		/// <item><description>The context's <see cref="TenantContext.TenantInfo"/> is <see langword="null"/></description></item>
		/// <item><description>The tenant's <see cref="TenantInfo.IsActive"/> property is <see langword="false"/></description></item>
		/// </list>
		/// <para>
		/// Inactive tenants typically should not be able to access the system. Use this method in authorization
		/// handlers, middleware, or business logic to enforce tenant activation status:
		/// <code>
		/// if (!tenantContext.IsActive())
		/// {
		///     return Forbid("Tenant account is not active");
		/// }
		/// </code>
		/// </para>
		/// </remarks>
		public static bool IsActive(this TenantContext? context)
		{
			return context?.TenantInfo?.IsActive ?? false;
		}

		/// <summary>
		/// Checks if a specific feature is enabled for the tenant.
		/// Features are managed through the tenant's <see cref="TenantSettings.EnabledFeatures"/> collection in settings.
		/// </summary>
		/// <param name="context">The tenant context to check. May be <see langword="null"/>.</param>
		/// <param name="featureName">The name of the feature to check. Comparison is case-sensitive.</param>
		/// <returns>
		/// <see langword="true"/> if the feature is explicitly enabled for this tenant;
		/// otherwise <see langword="false"/>.
		/// </returns>
		/// <remarks>
		/// This method safely checks if a feature flag is enabled for the tenant without throwing exceptions.
		/// Returns <see langword="false"/> if:
		/// <list type="bullet">
		/// <item><description>The context is <see langword="null"/></description></item>
		/// <item><description>The context's <see cref="TenantContext.TenantInfo"/> is <see langword="null"/></description></item>
		/// <item><description>The tenant info's <see cref="TenantInfo.Settings"/> is <see langword="null"/></description></item>
		/// <item><description>The settings' <see cref="TenantSettings.EnabledFeatures"/> collection is <see langword="null"/></description></item>
		/// <item><description>The feature name is not present in the collection</description></item>
		/// </list>
		/// <para>
		/// Feature flags are case-sensitive and should be defined as constants to avoid typos:
		/// <code>
		/// public static class Features
		/// {
		///     public const string AdvancedReporting = "AdvancedReporting";
		///     public const string BetaUI = "BetaUI";
		/// }
		///
		/// if (tenantContext.IsFeatureEnabled(Features.AdvancedReporting))
		/// {
		///     // Show advanced reporting options
		/// }
		/// </code>
		/// </para>
		/// <para>
		/// Use this method in controllers, services, and views to conditionally enable functionality
		/// based on tenant subscription tier or configuration.
		/// </para>
		/// </remarks>
		public static bool IsFeatureEnabled(this TenantContext? context, string featureName)
		{
			return context?.TenantInfo?.Settings?.EnabledFeatures?.Contains(featureName) ?? false;
		}

		/// <summary>
		/// Checks if the tenant context is valid and contains a usable tenant identifier.
		/// A valid context is non-<see langword="null"/> and has a non-empty tenant ID value.
		/// </summary>
		/// <param name="context">The tenant context to validate. May be <see langword="null"/>.</param>
		/// <returns>
		/// <see langword="true"/> if the context is non-<see langword="null"/> and has a valid tenant ID;
		/// otherwise <see langword="false"/>.
		/// </returns>
		/// <remarks>
		/// This method is useful for validating tenant context before performing tenant-specific operations.
		/// A context is considered invalid if it is <see langword="null"/> or if the tenant ID value is
		/// <see langword="null"/>, empty, or contains only whitespace characters.
		/// Use this method as a guard clause in services and controllers that require tenant context:
		/// <code>
		/// if (!tenantContext.IsValid())
		/// {
		///     throw new InvalidOperationException("Valid tenant context is required");
		/// }
		/// </code>
		/// </remarks>
		public static bool IsValid(this TenantContext? context)
		{
			return context != null && !string.IsNullOrWhiteSpace(context.TenantId.Value);
		}

		/// <summary>
		/// Gets the tenant name from the context's tenant information.
		/// Provides null-safe access to the tenant's display name.
		/// </summary>
		/// <param name="context">The tenant context to query. May be <see langword="null"/>.</param>
		/// <returns>
		/// The tenant name if available; otherwise <see langword="null"/>.
		/// </returns>
		/// <remarks>
		/// This method safely navigates the context hierarchy to retrieve the tenant name without
		/// throwing <see cref="NullReferenceException"/>. Returns <see langword="null"/> if:
		/// <list type="bullet">
		/// <item><description>The context is <see langword="null"/></description></item>
		/// <item><description>The context's <see cref="TenantContext.TenantInfo"/> is <see langword="null"/></description></item>
		/// <item><description>The tenant info's <see cref="TenantInfo.Name"/> is <see langword="null"/></description></item>
		/// </list>
		/// The tenant name is typically used for display purposes in UI, logging, and reports.
		/// </remarks>
		public static string? GetTenantName(this TenantContext? context)
		{
			return context?.TenantInfo?.Name;
		}

		/// <summary>
		/// Gets the isolation level configured for the tenant.
		/// Returns the tenant's configured isolation strategy or <see cref="IsolationLevel.None"/> if not available.
		/// </summary>
		/// <param name="context">The tenant context to query. May be <see langword="null"/>.</param>
		/// <returns>
		/// The configured <see cref="IsolationLevel"/>, or <see cref="IsolationLevel.None"/> if the context,
		/// tenant info, or isolation level is not available.
		/// </returns>
		/// <remarks>
		/// This method safely retrieves the tenant's isolation level without throwing exceptions.
		/// Returns <see cref="IsolationLevel.None"/> as a fallback if:
		/// <list type="bullet">
		/// <item><description>The context is <see langword="null"/></description></item>
		/// <item><description>The context's <see cref="TenantContext.TenantInfo"/> is <see langword="null"/></description></item>
		/// <item><description>The isolation level is not explicitly set</description></item>
		/// </list>
		/// The isolation level determines how tenant data is separated and is used by data access layers
		/// and infrastructure components to enforce proper tenant boundaries. Applications should check
		/// the isolation level before making assumptions about data storage architecture.
		/// </remarks>
		public static IsolationLevel GetIsolationLevel(this TenantContext? context)
		{
			return context?.TenantInfo?.IsolationLevel ?? IsolationLevel.None;
		}

		/// <summary>
		/// Retrieves a strongly-typed custom setting value from the tenant's configuration.
		/// Provides type-safe access to tenant-specific settings stored in the <see cref="TenantSettings.CustomSettings"/> dictionary.
		/// </summary>
		/// <typeparam name="T">The expected type of the setting value. The value will be cast to this type if compatible.</typeparam>
		/// <param name="context">The tenant context containing settings. May be <see langword="null"/>.</param>
		/// <param name="key">The setting key to retrieve. Must match a key in the <see cref="TenantSettings.CustomSettings"/> dictionary.</param>
		/// <returns>
		/// The setting value cast to type <typeparamref name="T"/> if found and compatible;
		/// otherwise <c>default(T)</c>, which is <see langword="null"/> for reference types and the default value for value types.
		/// </returns>
		/// <remarks>
		/// This method safely navigates the context hierarchy and attempts to retrieve and cast the setting value.
		/// Returns <c>default(T)</c> if:
		/// <list type="bullet">
		/// <item><description>The context is <see langword="null"/></description></item>
		/// <item><description>The context's <see cref="TenantContext.TenantInfo"/> is <see langword="null"/></description></item>
		/// <item><description>The tenant info's <see cref="TenantInfo.Settings"/> is <see langword="null"/></description></item>
		/// <item><description>The settings' <see cref="TenantSettings.CustomSettings"/> dictionary is <see langword="null"/></description></item>
		/// <item><description>The specified key does not exist in the dictionary</description></item>
		/// <item><description>The value exists but cannot be cast to type <typeparamref name="T"/></description></item>
		/// </list>
		/// <para>
		/// Example usage:
		/// <code>
		/// var maxRetries = tenantContext.GetSetting&lt;int&gt;("MaxRetries");
		/// var apiKey = tenantContext.GetSetting&lt;string&gt;("ExternalApiKey");
		/// var features = tenantContext.GetSetting&lt;List&lt;string&gt;&gt;("BetaFeatures");
		/// </code>
		/// </para>
		/// </remarks>
		public static T? GetSetting<T>(this TenantContext? context, string key)
		{
			// Check if custom settings exist
			if (context?.TenantInfo?.Settings?.CustomSettings == null)
			{
				return default;
			}

			// Attempt to retrieve and cast the setting value
			if (context.TenantInfo.Settings.CustomSettings.TryGetValue(key, out object? value) && value is T typedValue)
			{
				return typedValue;
			}

			return default;
		}

		#endregion
	}
}