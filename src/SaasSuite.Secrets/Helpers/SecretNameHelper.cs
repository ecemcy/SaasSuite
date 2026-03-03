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

namespace SaasSuite.Secrets.Helpers
{
	/// <summary>
	/// Provides utility methods for generating and parsing tenant-scoped secret names.
	/// </summary>
	/// <remarks>
	/// <para>
	/// This helper class manages the transformation between logical secret names (as used by applications)
	/// and physical secret names (as stored in secret stores) by applying tenant-specific prefixes.
	/// </para>
	/// <para>
	/// <strong>Thread Safety:</strong> All methods in this class are thread-safe as they are stateless
	/// and operate only on the provided parameters.
	/// </para>
	/// </remarks>
	/// <example>
	/// Basic usage:
	/// <code>
	/// var tenantId = new TenantId("tenant-123");
	/// var prefixTemplate = "tenants/{tenantId}/";
	///
	/// // Generate a scoped name: "tenants/tenant-123/ApiKey"
	/// var scopedName = SecretNameHelper.GetTenantScopedName(tenantId, "ApiKey", prefixTemplate);
	///
	/// // Extract the base name back: "ApiKey"
	/// var baseName = SecretNameHelper.GetBaseSecretName(tenantId, scopedName, prefixTemplate);
	/// </code>
	/// </example>
	public static class SecretNameHelper
	{
		#region ' Static Methods '

		/// <summary>
		/// Extracts the base secret name from a fully-qualified tenant-scoped secret name.
		/// </summary>
		/// <param name="tenantId">
		/// The tenant identifier used to determine the prefix. This must match the tenant ID
		/// that was used when the scoped name was originally created.
		/// </param>
		/// <param name="fullSecretName">
		/// The complete secret name including tenant prefix, as stored in the secret store.
		/// </param>
		/// <param name="prefixTemplate">
		/// The template string used to generate the prefix. Must match the template used when creating
		/// the scoped name. The template should contain <c>{tenantId}</c> as a placeholder.
		/// </param>
		/// <returns>
		/// The base secret name without tenant scoping if the prefix matches; otherwise, returns
		/// the original name unchanged (useful for secrets that may not follow the scoping convention).
		/// </returns>
		/// <remarks>
		/// <para>
		/// This method performs a case-insensitive comparison when checking for the prefix,
		/// ensuring compatibility with secret stores that may normalize casing.
		/// </para>
		/// <para>
		/// If the full secret name doesn't start with the expected prefix, the method returns
		/// the name unchanged. This allows graceful handling of unscoped or differently-scoped secrets.
		/// </para>
		/// </remarks>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="fullSecretName"/> or <paramref name="prefixTemplate"/> is <see langword="null"/>.
		/// </exception>
		/// <example>
		/// <code>
		/// var tenantId = new TenantId("tenant-456");
		/// var prefixTemplate = "tenants/{tenantId}/";
		///
		/// // Extract base name from scoped name
		/// var scopedName = "tenants/tenant-456/DatabasePassword";
		/// var baseName = SecretNameHelper.GetBaseSecretName(tenantId, scopedName, prefixTemplate);
		/// // Result: "DatabasePassword"
		///
		/// // Handle name without matching prefix
		/// var unscopedName = "GlobalSecret";
		/// var result = SecretNameHelper.GetBaseSecretName(tenantId, unscopedName, prefixTemplate);
		/// // Result: "GlobalSecret" (unchanged)
		/// </code>
		/// </example>
		public static string GetBaseSecretName(TenantId tenantId, string fullSecretName, string prefixTemplate)
		{
			ArgumentNullException.ThrowIfNull(fullSecretName);
			ArgumentNullException.ThrowIfNull(prefixTemplate);

			// Generate the expected prefix by replacing the placeholder with the actual tenant ID
			string prefix = prefixTemplate.Replace("{tenantId}", tenantId.Value);

			// Check if the full name starts with the expected prefix (case-insensitive comparison)
			if (fullSecretName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
			{
				// Remove the prefix to get the base secret name
				return fullSecretName.Substring(prefix.Length);
			}

			// Return unchanged if prefix doesn't match (handles edge cases gracefully)
			return fullSecretName;
		}

		/// <summary>
		/// Generates a fully-qualified secret name by combining a tenant-specific prefix with the base secret name.
		/// </summary>
		/// <param name="tenantId">
		/// The tenant identifier used to scope the secret. This ensures tenant isolation by making
		/// the secret name unique to this tenant.
		/// </param>
		/// <param name="secretName">
		/// The base secret name without tenant scoping. This is the logical name used by the application.
		/// </param>
		/// <param name="prefixTemplate">
		/// A template string for the tenant prefix. Use <c>{tenantId}</c> as a placeholder that will
		/// be replaced with the actual tenant ID value.
		/// </param>
		/// <returns>
		/// The complete secret name with tenant scoping applied, ready to be used with the underlying secret store.
		/// </returns>
		/// <remarks>
		/// <para>
		/// The resulting scoped name ensures that secrets are isolated per tenant, preventing
		/// accidental cross-tenant access and simplifying secret management.
		/// </para>
		/// <para>
		/// <strong>Naming Conventions:</strong> The prefix template should be designed to comply
		/// with the naming rules of your secret store (e.g., Azure Key Vault allows alphanumeric
		/// characters and hyphens, AWS Secrets Manager allows slashes for hierarchical naming).
		/// </para>
		/// </remarks>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="secretName"/> or <paramref name="prefixTemplate"/> is <see langword="null"/>.
		/// </exception>
		/// <example>
		/// <code>
		/// var tenantId = new TenantId("contoso");
		/// var prefixTemplate = "tenants/{tenantId}/";
		///
		/// // Generate scoped name for an API key
		/// var scopedName = SecretNameHelper.GetTenantScopedName(tenantId, "StripeApiKey", prefixTemplate);
		/// // Result: "tenants/contoso/StripeApiKey"
		///
		/// // Using a different prefix template
		/// var altTemplate = "{tenantId}-";
		/// var altScopedName = SecretNameHelper.GetTenantScopedName(tenantId, "StripeApiKey", altTemplate);
		/// // Result: "contoso-StripeApiKey"
		///
		/// // Environment-specific template
		/// var envTemplate = "prod/tenants/{tenantId}/";
		/// var envScopedName = SecretNameHelper.GetTenantScopedName(tenantId, "StripeApiKey", envTemplate);
		/// // Result: "prod/tenants/contoso/StripeApiKey"
		/// </code>
		/// </example>
		public static string GetTenantScopedName(TenantId tenantId, string secretName, string prefixTemplate)
		{
			ArgumentNullException.ThrowIfNull(secretName);
			ArgumentNullException.ThrowIfNull(prefixTemplate);

			// Replace the {tenantId} placeholder with the actual tenant identifier
			string prefix = prefixTemplate.Replace("{tenantId}", tenantId.Value);

			// Combine prefix and base name to create the fully-qualified secret name
			return $"{prefix}{secretName}";
		}

		#endregion
	}
}