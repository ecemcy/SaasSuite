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

namespace SaasSuite.Migration
{
	/// <summary>
	/// Represents an error that occurred during a migration operation.
	/// </summary>
	/// <remarks>
	/// Migration errors capture detailed information about failures, including which tenant
	/// failed, what went wrong, and when it occurred. This information is critical for
	/// diagnostics, debugging, and determining whether to retry or rollback migrations.
	/// </remarks>
	public class MigrationError
	{
		#region ' Properties '

		/// <summary>
		/// Gets or sets a user-friendly error message describing what went wrong.
		/// </summary>
		/// <value>
		/// A string containing the error message. Defaults to an empty string.
		/// Should be set to a meaningful description when creating error records.
		/// </value>
		/// <remarks>
		/// The message should be clear and actionable, suitable for:
		/// <list type="bullet">
		/// <item><description>Displaying in user interfaces or reports</description></item>
		/// <item><description>Including in notification emails or alerts</description></item>
		/// <item><description>Logging for operational monitoring</description></item>
		/// </list>
		/// Avoid including sensitive data like passwords or connection strings in messages.
		/// For technical details, use <see cref="ExceptionDetails"/>.
		/// </remarks>
		public string Message { get; set; } = string.Empty;

		/// <summary>
		/// Gets or sets detailed exception information including stack traces and inner exceptions.
		/// </summary>
		/// <value>
		/// A string containing the full exception details, typically from <see cref="Exception.ToString"/>,
		/// or <see langword="null"/> if no exception information is available.
		/// </value>
		/// <remarks>
		/// Exception details provide technical diagnostic information for troubleshooting, including:
		/// <list type="bullet">
		/// <item><description>Full exception type and message</description></item>
		/// <item><description>Complete stack trace showing call hierarchy</description></item>
		/// <item><description>Inner exception chains for root cause analysis</description></item>
		/// <item><description>Additional exception data and properties</description></item>
		/// </list>
		/// <para>
		/// This field may contain sensitive information and should be:
		/// <list type="bullet">
		/// <item><description>Logged to secure diagnostic systems</description></item>
		/// <item><description>Excluded from user-facing interfaces</description></item>
		/// <item><description>Sanitized before external transmission</description></item>
		/// <item><description>Protected with appropriate access controls</description></item>
		/// </list>
		/// </para>
		/// </remarks>
		public string? ExceptionDetails { get; set; }

		/// <summary>
		/// Gets or sets the tenant identifier where the error occurred.
		/// </summary>
		/// <value>
		/// A string containing the tenant ID, or <see langword="null"/> if the error is not
		/// associated with a specific tenant (e.g., system-level errors).
		/// </value>
		/// <remarks>
		/// The tenant ID allows filtering and analyzing errors by tenant, which is useful for:
		/// <list type="bullet">
		/// <item><description>Identifying tenants that need manual intervention</description></item>
		/// <item><description>Generating tenant-specific error reports</description></item>
		/// <item><description>Implementing targeted retry strategies</description></item>
		/// <item><description>Notifying specific tenant administrators</description></item>
		/// </list>
		/// When <see langword="null"/>, the error typically represents a systemic issue like
		/// connectivity problems, configuration errors, or infrastructure failures.
		/// </remarks>
		public string? TenantId { get; set; }

		/// <summary>
		/// Gets or sets the UTC timestamp when the error occurred.
		/// </summary>
		/// <value>
		/// A <see cref="DateTimeOffset"/> in UTC indicating when the error was captured.
		/// Defaults to the current UTC time.
		/// </value>
		/// <remarks>
		/// The timestamp enables:
		/// <list type="bullet">
		/// <item><description>Chronological ordering of errors in reports</description></item>
		/// <item><description>Correlation with system logs and metrics</description></item>
		/// <item><description>Analysis of error patterns over time</description></item>
		/// <item><description>Identifying temporal factors in failures (e.g., peak hours)</description></item>
		/// </list>
		/// Always stored in UTC to avoid time zone ambiguities and enable accurate
		/// cross-region analysis.
		/// </remarks>
		public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

		#endregion
	}
}