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

namespace SaasSuite.Compliance.Enumerations
{
	/// <summary>
	/// Enumerates the supported serialization formats for data exports.
	/// </summary>
	/// <remarks>
	/// These formats represent common, machine-readable data formats that satisfy data portability
	/// requirements under regulations like GDPR. Each format has different characteristics
	/// suited to different use cases and user preferences.
	/// </remarks>
	public enum DataExportFormat
	{
		/// <summary>
		/// JavaScript Object Notation format with nested structure support.
		/// </summary>
		/// <remarks>
		/// JSON is the default format, providing a good balance between human readability and machine parseability.
		/// It preserves nested object structures, making it ideal for complex data models and programmatic processing.
		/// JSON is widely supported by modern programming languages and APIs.
		/// </remarks>
		Json = 0,

		/// <summary>
		/// Comma-Separated Values format with flat, tabular structure.
		/// </summary>
		/// <remarks>
		/// CSV is highly human-readable and easily imported into spreadsheet applications like Excel and Google Sheets.
		/// It uses a flat, tabular structure with headers, making it suitable for simple data without deep nesting.
		/// Complex nested objects must be flattened or serialized as strings within CSV cells.
		/// </remarks>
		Csv = 1,

		/// <summary>
		/// Extensible Markup Language format with hierarchical structure.
		/// </summary>
		/// <remarks>
		/// XML provides a verbose but widely compatible format suitable for interoperability with legacy systems.
		/// It supports hierarchical data structures and schema validation through XSD.
		/// XML is more verbose than JSON but offers strong tooling support and backward compatibility.
		/// </remarks>
		Xml = 2
	}
}