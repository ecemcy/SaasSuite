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
	/// Enumerates the types of compliance events that can occur in the system.
	/// </summary>
	/// <remarks>
	/// These event types align with common data protection requirements in regulations like GDPR and CCPA.
	/// The enumeration provides a standardized vocabulary for compliance event tracking and reporting.
	/// </remarks>
	public enum ComplianceEventType
	{
		/// <summary>
		/// Indicates a data export was requested by a tenant or user.
		/// </summary>
		/// <remarks>
		/// This event is logged when a data portability request is received, typically for GDPR Article 20 compliance.
		/// The request may be pending processing.
		/// </remarks>
		DataExportRequested = 0,

		/// <summary>
		/// Indicates a data export was successfully completed and made available.
		/// </summary>
		/// <remarks>
		/// This event is logged when the export process finishes and the data is ready for download or delivery.
		/// Details should include export format, file size, and access information.
		/// </remarks>
		DataExportCompleted = 1,

		/// <summary>
		/// Indicates a right-to-be-forgotten (data erasure) request was received.
		/// </summary>
		/// <remarks>
		/// This event is logged when a user or tenant requests data deletion or anonymization
		/// under GDPR Article 17 or similar regulations. The request may be pending verification and processing.
		/// </remarks>
		RightToBeForgottenRequested = 2,

		/// <summary>
		/// Indicates personally identifiable information was anonymized.
		/// </summary>
		/// <remarks>
		/// This event is logged when PII is replaced with anonymized values while preserving data structure.
		/// Details should include the scope of anonymization and number of records affected.
		/// </remarks>
		DataAnonymized = 3,

		/// <summary>
		/// Indicates data was permanently deleted from the system.
		/// </summary>
		/// <remarks>
		/// This event is logged when tenant or user data is irreversibly removed from all storage systems.
		/// Details should include the scope of deletion and confirmation of removal from all systems.
		/// </remarks>
		DataDeleted = 4,

		/// <summary>
		/// Indicates user consent was granted for a specific type of data processing.
		/// </summary>
		/// <remarks>
		/// This event is logged when a user provides consent for activities like marketing, analytics,
		/// or data processing under GDPR Article 6 and 7. Details should include the consent type and scope.
		/// </remarks>
		ConsentGranted = 5,

		/// <summary>
		/// Indicates user consent was revoked or withdrawn.
		/// </summary>
		/// <remarks>
		/// This event is logged when a user withdraws previously granted consent.
		/// Details should include the consent type and any actions taken in response to the revocation.
		/// </remarks>
		ConsentRevoked = 6
	}
}