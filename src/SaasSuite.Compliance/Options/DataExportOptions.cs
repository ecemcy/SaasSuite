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

using SaasSuite.Compliance.Enumerations;

namespace SaasSuite.Compliance.Options
{
	/// <summary>
	/// Configuration options for controlling data export operations and output format.
	/// </summary>
	/// <remarks>
	/// These options allow customization of data export behavior to meet specific requirements
	/// for data portability requests under GDPR Article 20 and similar regulations.
	/// Options control the export format, included data categories, compression, and audit log inclusion.
	/// </remarks>
	public class DataExportOptions
	{
		#region ' Properties '

		/// <summary>
		/// Gets or sets a value indicating whether to compress the export into an archive.
		/// </summary>
		/// <value>
		/// <see langword="true"/> to compress the export (typically as a ZIP file); otherwise, <see langword="false"/>.
		/// Defaults to <see langword="true"/>.
		/// </value>
		/// <remarks>
		/// Compression significantly reduces bandwidth and storage requirements for large exports.
		/// When enabled, all export files are packaged into a single compressed archive (typically ZIP format).
		/// Disable compression only for small exports or when the client cannot handle compressed files.
		/// </remarks>
		public bool Compress { get; set; } = true;

		/// <summary>
		/// Gets or sets a value indicating whether to include audit logs in the export.
		/// </summary>
		/// <value>
		/// <see langword="true"/> to include audit logs in the export; otherwise, <see langword="false"/>.
		/// Defaults to <see langword="true"/>.
		/// </value>
		/// <remarks>
		/// Audit logs provide a complete history of actions performed by and on behalf of the tenant.
		/// Including audit logs helps tenants understand how their data was accessed and modified.
		/// Set to <see langword="false"/> to exclude audit logs and reduce export size, though this
		/// may not fully satisfy transparency requirements under some regulations.
		/// </remarks>
		public bool IncludeAuditLogs { get; set; } = true;

		/// <summary>
		/// Gets or sets the serialization format for exported data files.
		/// </summary>
		/// <value>
		/// A <see cref="DataExportFormat"/> enumeration value specifying the output format.
		/// Defaults to <see cref="DataExportFormat.Json"/>.
		/// </value>
		/// <remarks>
		/// <para>The format determines how data is serialized in export files:</para>
		/// <list type="bullet">
		/// <item><description>JSON: Machine-readable, preserves nested structures, ideal for APIs and programmatic processing</description></item>
		/// <item><description>CSV: Human-readable, flat structure, easy to import into spreadsheets and databases</description></item>
		/// <item><description>XML: Verbose but widely compatible across different systems and platforms</description></item>
		/// </list>
		/// <para>Choose the format based on the intended use of the exported data and user preferences.</para>
		/// </remarks>
		public DataExportFormat Format { get; set; } = DataExportFormat.Json;

		/// <summary>
		/// Gets or sets specific data categories to include in the export.
		/// </summary>
		/// <value>
		/// An optional collection of category names to include in the export.
		/// If <see langword="null"/> or empty, all available data categories are included.
		/// </value>
		/// <remarks>
		/// <para>Use this property to implement selective data exports, allowing tenants to choose which
		/// types of data to export. Common categories might include:</para>
		/// <list type="bullet">
		/// <item><description>"Users": User profiles and account information</description></item>
		/// <item><description>"Transactions": Financial and business transactions</description></item>
		/// <item><description>"Documents": Uploaded files and attachments</description></item>
		/// <item><description>"Messages": Communications and correspondence</description></item>
		/// <item><description>"AuditLogs": Activity and audit history</description></item>
		/// </list>
		/// <para>When <see langword="null"/>, the exporter includes all available data categories.
		/// Category names should match those defined in your domain model for consistency.</para>
		/// </remarks>
		public IEnumerable<string>? Categories { get; set; }

		#endregion
	}
}