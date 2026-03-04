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

using SaasSuite.Compliance.Options;
using SaasSuite.Core;

namespace SaasSuite.Compliance.Interfaces
{
	/// <summary>
	/// Defines the contract for exporting tenant data to comply with data portability regulations.
	/// </summary>
	/// <remarks>
	/// This interface supports compliance with regulations like GDPR Article 20 (Right to Data Portability)
	/// and CCPA, which require organizations to provide users with copies of their personal data in a
	/// machine-readable format. Implementations should gather all tenant data from various sources,
	/// format it according to specified options, and generate a comprehensive export manifest.
	/// </remarks>
	public interface IComplianceExporter
	{
		#region ' Methods '

		/// <summary>
		/// Asynchronously exports all data for a specific tenant in a structured, portable format.
		/// </summary>
		/// <param name="tenantId">The identifier of the tenant whose data should be exported. Cannot be <see langword="null"/>.</param>
		/// <param name="options">
		/// Optional configuration controlling the export format, included data categories, compression, and audit log inclusion.
		/// If <see langword="null"/>, default options are used (JSON format, compressed, with audit logs).
		/// </param>
		/// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete. Defaults to <see cref="CancellationToken.None"/>.</param>
		/// <returns>
		/// A task that represents the asynchronous operation. The task result contains a <see cref="DataExportManifest"/>
		/// describing the exported data, including file locations, sizes, categories, and metadata.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="tenantId"/> or its value is <see langword="null"/>.
		/// </exception>
		/// <remarks>
		/// <para>This method should collect data from all relevant sources including:</para>
		/// <list type="bullet">
		/// <item><description>User profiles and authentication data</description></item>
		/// <item><description>Business transactions and records</description></item>
		/// <item><description>Audit logs and activity history</description></item>
		/// <item><description>File attachments and media</description></item>
		/// <item><description>Metadata and system-generated information</description></item>
		/// </list>
		/// <para>The export should be performed securely with appropriate access controls to prevent unauthorized data access.
		/// Consider implementing rate limiting and queueing for large exports to prevent system overload.
		/// The manifest should provide sufficient information for the tenant to understand and utilize the exported data.
		/// Implementations should handle data consistency during export, potentially using snapshots or transactions.</para>
		/// </remarks>
		Task<DataExportManifest> ExportTenantAsync(TenantId tenantId, DataExportOptions? options = null, CancellationToken cancellationToken = default);

		#endregion
	}
}