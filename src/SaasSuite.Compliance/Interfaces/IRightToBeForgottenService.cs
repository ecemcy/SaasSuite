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

namespace SaasSuite.Compliance.Interfaces
{
	/// <summary>
	/// Defines the contract for implementing the "Right to be Forgotten" (Right to Erasure) as required by data protection regulations.
	/// </summary>
	/// <remarks>
	/// <para>This interface supports compliance with GDPR Article 17 (Right to Erasure) and similar provisions
	/// in other regulations like CCPA. It provides two approaches to fulfilling erasure requests:</para>
	/// <list type="bullet">
	/// <item><description>Anonymization: Replaces personally identifiable information with anonymized values while preserving data structure</description></item>
	/// <item><description>Deletion: Permanently removes all tenant data from the system</description></item>
	/// </list>
	/// <para>Implementations must ensure complete removal of personal data across all storage systems including
	/// databases, file storage, caches, backups, and third-party services.</para>
	/// </remarks>
	public interface IRightToBeForgottenService
	{
		#region ' Methods '

		/// <summary>
		/// Asynchronously anonymizes all personally identifiable information for a tenant while preserving data structure.
		/// </summary>
		/// <param name="tenantId">The identifier of the tenant whose data should be anonymized. Cannot be <see langword="null"/>.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
		/// <returns>A task that represents the asynchronous operation.</returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="tenantId"/> or its value is <see langword="null"/>.
		/// </exception>
		/// <remarks>
		/// <para>Anonymization replaces personal data with generic or randomized values while maintaining referential
		/// integrity and data structure. This approach is preferable when:</para>
		/// <list type="bullet">
		/// <item><description>Analytics and reporting require historical data structure</description></item>
		/// <item><description>Deletion would violate other legal retention requirements (e.g., financial records)</description></item>
		/// <item><description>System functionality depends on maintaining relational integrity</description></item>
		/// </list>
		/// <para>The anonymization process should replace:</para>
		/// <list type="bullet">
		/// <item><description>Names with generic identifiers (e.g., "User_12345")</description></item>
		/// <item><description>Email addresses with anonymized versions (e.g., "anonymized_12345@example.com")</description></item>
		/// <item><description>Phone numbers, addresses, and other PII with placeholder values</description></item>
		/// <item><description>Profile images and documents with removal or generic placeholders</description></item>
		/// </list>
		/// <para>The operation should be idempotent, allowing safe re-execution if needed.
		/// Log all anonymization actions for compliance auditing.
		/// Consider providing a grace period before executing to allow users to undo accidental requests.</para>
		/// </remarks>
		Task AnonymizeAsync(TenantId tenantId, CancellationToken cancellationToken = default);

		/// <summary>
		/// Asynchronously and permanently deletes all data for a tenant from all storage systems.
		/// </summary>
		/// <param name="tenantId">The identifier of the tenant whose data should be permanently deleted. Cannot be <see langword="null"/>.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
		/// <returns>A task that represents the asynchronous operation.</returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="tenantId"/> or its value is <see langword="null"/>.
		/// </exception>
		/// <remarks>
		/// <para>This method performs irreversible data deletion and should be used with extreme caution.
		/// Deletion is appropriate when:</para>
		/// <list type="bullet">
		/// <item><description>The tenant explicitly requests complete data removal</description></item>
		/// <item><description>No legal retention requirements apply</description></item>
		/// <item><description>Anonymization is not sufficient for the use case</description></item>
		/// </list>
		/// <para>The deletion process must remove data from:</para>
		/// <list type="bullet">
		/// <item><description>Primary databases (all tables and records)</description></item>
		/// <item><description>File storage (documents, images, attachments)</description></item>
		/// <item><description>Caches (in-memory, distributed, CDN)</description></item>
		/// <item><description>Search indexes (Elasticsearch, Azure Search, etc.)</description></item>
		/// <item><description>Message queues and event streams</description></item>
		/// <item><description>Backup systems (consider backup retention policies)</description></item>
		/// <item><description>Third-party integrated services</description></item>
		/// </list>
		/// <para>Implementations should:</para>
		/// <list type="bullet">
		/// <item><description>Use database transactions where possible to ensure consistency</description></item>
		/// <item><description>Implement cascade deletion to handle relational data</description></item>
		/// <item><description>Log the deletion action with timestamp and initiator for audit trail</description></item>
		/// <item><description>Verify deletion success across all systems</description></item>
		/// <item><description>Consider implementing a soft-delete grace period before permanent deletion</description></item>
		/// <item><description>Queue the deletion for background processing if it's a long-running operation</description></item>
		/// </list>
		/// <para>Be aware that some backup systems may retain data beyond this operation for disaster recovery purposes.</para>
		/// </remarks>
		Task DeleteAsync(TenantId tenantId, CancellationToken cancellationToken = default);

		#endregion
	}
}