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
using SaasSuite.Secrets.Interfaces;

namespace SaasSuite.Secrets
{
	/// <summary>
	/// Represents an event that occurs when a secret is rotated to a new version.
	/// </summary>
	/// <remarks>
	/// <para>
	/// This event is typically raised by implementations of <see cref="ISecretStore"/> when a secret
	/// value is updated, allowing downstream systems to react to secret changes.
	/// </para>
	/// <para>
	/// Common use cases include:
	/// <list type="bullet">
	/// <item><description>Invalidating cached secret values</description></item>
	/// <item><description>Updating connection strings in active services</description></item>
	/// <item><description>Auditing secret rotation activities</description></item>
	/// <item><description>Notifying administrators of security-critical changes</description></item>
	/// </list>
	/// </para>
	/// </remarks>
	/// <example>
	/// <code>
	/// var rotationEvent = new SecretRotationEvent
	/// {
	///     SecretName = "DatabasePassword",
	///     TenantId = new TenantId("tenant-123"),
	///     OldVersion = "v1",
	///     NewVersion = "v2",
	///     RotatedAt = DateTimeOffset.UtcNow
	/// };
	///
	/// await rotationHandler.HandleRotationAsync(rotationEvent);
	/// </code>
	/// </example>
	public class SecretRotationEvent
	{
		#region ' Properties '

		/// <summary>
		/// Gets or sets the version identifier of the newly rotated secret.
		/// </summary>
		/// <value>
		/// A string representing the version identifier of the new secret value.
		/// The format of this identifier is implementation-specific.
		/// </value>
		/// <remarks>
		/// Version identifiers are typically assigned by the underlying secret store
		/// (e.g., Azure Key Vault uses GUIDs, AWS Secrets Manager uses version stages).
		/// </remarks>
		public string NewVersion { get; set; } = string.Empty;

		/// <summary>
		/// Gets or sets the name of the secret that was rotated.
		/// </summary>
		/// <value>
		/// The base name of the secret, without tenant-specific prefixes.
		/// </value>
		/// <remarks>
		/// This should be the logical secret name used by the application,
		/// not the physical storage name that may include tenant scoping prefixes.
		/// </remarks>
		public string SecretName { get; set; } = string.Empty;

		/// <summary>
		/// Gets or sets the version identifier of the secret before rotation.
		/// </summary>
		/// <value>
		/// The previous version identifier, or <see langword="null"/> if this is the first version
		/// or if the old version is unknown.
		/// </value>
		/// <remarks>
		/// A <see langword="null"/> value typically indicates either the initial creation of the secret
		/// or a scenario where version tracking was not available.
		/// </remarks>
		public string? OldVersion { get; set; }

		/// <summary>
		/// Gets or sets the UTC timestamp when the secret rotation occurred.
		/// </summary>
		/// <value>
		/// A <see cref="DateTimeOffset"/> representing the moment the secret was rotated,
		/// always expressed in UTC.
		/// </value>
		/// <remarks>
		/// This timestamp should reflect when the rotation was completed in the secret store,
		/// not when the event was processed or created.
		/// </remarks>
		public DateTimeOffset RotatedAt { get; set; }

		/// <summary>
		/// Gets or sets the identifier of the tenant whose secret was rotated.
		/// </summary>
		/// <value>
		/// The <see cref="TenantId"/> identifying which tenant's secret was affected by this rotation.
		/// </value>
		/// <remarks>
		/// This ensures that rotation handlers can properly scope their actions to the correct tenant,
		/// maintaining isolation in multi-tenant environments.
		/// </remarks>
		public TenantId TenantId { get; set; }

		#endregion
	}
}