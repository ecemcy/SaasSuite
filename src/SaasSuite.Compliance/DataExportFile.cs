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

namespace SaasSuite.Compliance
{
	/// <summary>
	/// Represents metadata about a single file within a data export package.
	/// </summary>
	/// <remarks>
	/// Each export file typically contains data from a specific category or entity type,
	/// serialized in the format specified in the parent <see cref="DataExportManifest"/>.
	/// This class provides detailed information about the file's contents and characteristics.
	/// </remarks>
	public class DataExportFile
	{
		#region ' Properties '

		/// <summary>
		/// Gets or sets the number of records or entities contained in the file.
		/// </summary>
		/// <value>
		/// An integer representing the count of data records in the file.
		/// </value>
		/// <remarks>
		/// The record count provides insight into the volume of data in each category.
		/// For JSON files, this might be the number of objects in the root array.
		/// For CSV files, this might be the number of data rows (excluding headers).
		/// For XML files, this might be the number of root-level elements or specific entity nodes.
		/// </remarks>
		public int RecordCount { get; set; }

		/// <summary>
		/// Gets or sets the size of the file in bytes.
		/// </summary>
		/// <value>
		/// A long integer representing the file size in bytes (uncompressed).
		/// </value>
		/// <remarks>
		/// File sizes help users estimate download times and storage requirements.
		/// For compressed exports, this represents the uncompressed size of the individual file,
		/// not the compressed size of the entire package.
		/// </remarks>
		public long SizeBytes { get; set; }

		/// <summary>
		/// Gets or sets the category or type of data contained in the file.
		/// </summary>
		/// <value>
		/// A string describing the data category, such as "Users", "Transactions", "AuditLogs", or "Documents".
		/// Defaults to an empty string. Should not be <see langword="null"/>.
		/// </value>
		/// <remarks>
		/// Categories help users understand the contents of each file without examining the data.
		/// Use consistent, human-readable names that align with domain entities in your application.
		/// Categories can be used for selective exports where users choose which data to include.
		/// </remarks>
		public string Category { get; set; } = string.Empty;

		/// <summary>
		/// Gets or sets the relative path of the file within the export package.
		/// </summary>
		/// <value>
		/// A string representing the file path relative to the export root, such as "users.json" or "data/transactions.csv".
		/// Defaults to an empty string. Should not be <see langword="null"/>.
		/// </value>
		/// <remarks>
		/// The path indicates where the file is located within the export structure, especially important
		/// for compressed exports where multiple files are packaged together.
		/// Use consistent naming conventions for easier navigation (e.g., category names, plural forms).
		/// </remarks>
		public string Path { get; set; } = string.Empty;

		#endregion
	}
}