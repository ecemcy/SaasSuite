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
using SaasSuite.Compliance.Options;

namespace SaasSuite.Compliance
{
	/// <summary>
	/// Represents a manifest describing the contents and metadata of an exported tenant data package.
	/// </summary>
	/// <remarks>
	/// The manifest provides a comprehensive index of all files included in a data export,
	/// enabling tenants to understand and navigate the exported data. This supports compliance
	/// with GDPR Article 20 (Right to Data Portability) and similar regulations requiring
	/// data exports in structured, machine-readable formats.
	/// </remarks>
	public class DataExportManifest
	{
		#region ' Properties '

		/// <summary>
		/// Gets or sets a value indicating whether the export files are compressed.
		/// </summary>
		/// <value>
		/// <see langword="true"/> if the export is compressed (typically as a ZIP archive); otherwise, <see langword="false"/>.
		/// </value>
		/// <remarks>
		/// Compression reduces bandwidth and storage requirements for large exports.
		/// This is set based on <see cref="DataExportOptions.Compress"/> provided during export.
		/// If <see langword="true"/>, the export is typically packaged as a single ZIP file containing all listed files.
		/// </remarks>
		public bool IsCompressed { get; set; }

		/// <summary>
		/// Gets or sets the total size of all exported files in bytes.
		/// </summary>
		/// <value>
		/// A long integer representing the aggregate size in bytes of all files in the export.
		/// </value>
		/// <remarks>
		/// This is typically calculated as the sum of <see cref="DataExportFile.SizeBytes"/> for all files.
		/// It helps users understand storage requirements for downloading and storing the export.
		/// If <see cref="IsCompressed"/> is <see langword="true"/>, this represents the compressed size.
		/// </remarks>
		public long TotalSizeBytes { get; set; }

		/// <summary>
		/// Gets or sets the unique identifier for this data export.
		/// </summary>
		/// <value>
		/// A globally unique string identifier generated at the time of export creation.
		/// Defaults to a new GUID string representation.
		/// </value>
		/// <remarks>
		/// This identifier is used to track, retrieve, and manage the export package.
		/// It can be provided to users for referencing their export in support requests.
		/// The default value is automatically generated using <see cref="Guid.NewGuid"/>.
		/// </remarks>
		public string ExportId { get; set; } = Guid.NewGuid().ToString();

		/// <summary>
		/// Gets or sets the identifier of the tenant whose data was exported.
		/// </summary>
		/// <value>
		/// A string representing the tenant identifier. Defaults to an empty string.
		/// Should not be <see langword="null"/>.
		/// </value>
		/// <remarks>
		/// This field links the export to a specific tenant, ensuring proper data isolation
		/// and access control. It is set by the export service during the export process.
		/// </remarks>
		public string TenantId { get; set; } = string.Empty;

		/// <summary>
		/// Gets or sets the format of the exported data files.
		/// </summary>
		/// <value>
		/// A <see cref="DataExportFormat"/> enumeration value indicating the serialization format.
		/// </value>
		/// <remarks>
		/// <para>The format determines how data is serialized in the export files (JSON, CSV, or XML).
		/// This is set based on the <see cref="DataExportOptions.Format"/> provided during export.
		/// Different formats suit different use cases:</para>
		/// <list type="bullet">
		/// <item><description>JSON: Machine-readable, preserves nested structures, ideal for programmatic processing</description></item>
		/// <item><description>CSV: Human-readable, flat structure, easy to import into spreadsheets</description></item>
		/// <item><description>XML: Verbose but widely compatible, good for interoperability</description></item>
		/// </list>
		/// </remarks>
		public DataExportFormat Format { get; set; }

		/// <summary>
		/// Gets or sets the UTC timestamp when the export was created.
		/// </summary>
		/// <value>
		/// A <see cref="DateTimeOffset"/> value in UTC representing when the export was generated.
		/// Defaults to the current UTC time.
		/// </value>
		/// <remarks>
		/// This timestamp indicates the point-in-time snapshot of data included in the export.
		/// Data changes after this time are not included. The default value is automatically
		/// set to <see cref="DateTimeOffset.UtcNow"/> when the manifest is created.
		/// </remarks>
		public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

		/// <summary>
		/// Gets or sets the list of files included in the data export.
		/// </summary>
		/// <value>
		/// A list of <see cref="DataExportFile"/> instances describing each file in the export.
		/// Defaults to an empty list. Cannot be <see langword="null"/>.
		/// </value>
		/// <remarks>
		/// Each file represents a category or collection of related data (e.g., users, transactions, logs).
		/// The list provides a complete inventory of the export contents, including file paths, sizes,
		/// and record counts. The list is initialized as empty to prevent null reference exceptions.
		/// </remarks>
		public List<DataExportFile> Files { get; set; } = new List<DataExportFile>();

		#endregion
	}
}