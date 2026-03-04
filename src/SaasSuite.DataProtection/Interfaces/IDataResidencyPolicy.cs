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

namespace SaasSuite.DataProtection.Interfaces
{
	/// <summary>
	/// Defines the contract for enforcing data residency policies in multi-tenant environments.
	/// </summary>
	/// <remarks>
	/// Data residency policies ensure that tenant data is stored and processed in specific
	/// geographic regions to comply with regulations like GDPR, CCPA, data sovereignty laws,
	/// and contractual requirements. Implementations should enforce these policies at the
	/// data access layer to prevent unauthorized cross-region data transfer.
	/// </remarks>
	public interface IDataResidencyPolicy
	{
		#region ' Methods '

		/// <summary>
		/// Asynchronously validates whether an operation is permitted in a given region for a tenant.
		/// </summary>
		/// <param name="tenantId">The identifier of the tenant. Cannot be <see langword="null"/>.</param>
		/// <param name="region">The region code where the operation would be performed. Cannot be <see langword="null"/> or whitespace.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
		/// <returns>
		/// A task that represents the asynchronous operation. The task result is <see langword="true"/> if
		/// the operation is allowed in the specified region; otherwise, <see langword="false"/>.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="tenantId"/>, its value, or <paramref name="region"/> is <see langword="null"/> or whitespace.
		/// </exception>
		/// <remarks>
		/// Use this method before performing operations that access or modify tenant data to ensure
		/// compliance with data residency policies. If this method returns <see langword="false"/>,
		/// the operation should be rejected or redirected to an appropriate region.
		/// <para>
		/// Common validation scenarios include:
		/// <list type="bullet">
		/// <item><description>Checking if a database operation can execute in the current region</description></item>
		/// <item><description>Validating if a file can be stored in a specific storage location</description></item>
		/// <item><description>Ensuring API requests are processed in compliant regions</description></item>
		/// <item><description>Verifying backup and disaster recovery locations</description></item>
		/// </list>
		/// </para>
		/// Some policies may allow multiple regions (e.g., EU countries for GDPR compliance),
		/// while others may require a single specific region.
		/// </remarks>
		Task<bool> IsRegionAllowedAsync(TenantId tenantId, string region, CancellationToken cancellationToken = default);

		/// <summary>
		/// Asynchronously retrieves the required geographic region for storing a tenant's data.
		/// </summary>
		/// <param name="tenantId">The identifier of the tenant. Cannot be <see langword="null"/>.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
		/// <returns>
		/// A task that represents the asynchronous operation. The task result contains the region code
		/// (e.g., "us-east-1", "eu-west-1", "ap-southeast-1") where the tenant's data must reside.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="tenantId"/> or its value is <see langword="null"/>.
		/// </exception>
		/// <remarks>
		/// Region codes should follow a consistent naming convention across your infrastructure.
		/// Common patterns include:
		/// <list type="bullet">
		/// <item><description>AWS regions: "us-east-1", "eu-west-1", "ap-southeast-2"</description></item>
		/// <item><description>Azure regions: "eastus", "westeurope", "southeastasia"</description></item>
		/// <item><description>Custom regions: "north-america", "europe", "asia-pacific"</description></item>
		/// </list>
		/// The region is typically determined by tenant configuration, contractual agreements,
		/// or regulatory requirements specific to the tenant's jurisdiction.
		/// </remarks>
		Task<string> GetRequiredRegionAsync(TenantId tenantId, CancellationToken cancellationToken = default);

		#endregion
	}
}