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

using SaasSuite.Billing.Interfaces;

namespace SaasSuite.Billing.Webhooks
{
	/// <summary>
	/// A no-operation implementation of <see cref="IWebhookSignatureVerifier"/> that always rejects signature verification.
	/// </summary>
	/// <remarks>
	/// This is the default implementation registered when no specific payment provider signature verifier is provided.
	/// For security, this implementation always returns <see langword="false"/> to prevent unauthorized webhooks.
	/// </remarks>
	public class NoOpWebhookSignatureVerifier
		: IWebhookSignatureVerifier
	{
		#region ' Methods '

		/// <summary>
		/// Verifies a webhook signature by always returning <see langword="false"/> for security.
		/// </summary>
		/// <param name="payload">The webhook payload (ignored by this implementation).</param>
		/// <param name="signature">The signature to verify (ignored by this implementation).</param>
		/// <param name="secret">The secret key for verification (ignored by this implementation).</param>
		/// <returns>
		/// Always returns <see langword="false"/> to reject all webhook signatures, ensuring that webhooks
		/// are not processed without a proper verification implementation.
		/// </returns>
		/// <remarks>
		/// This implementation does not perform any actual signature verification and rejects all webhook requests
		/// until a provider-specific verifier is registered. This is a safety measure to prevent unauthorized webhooks.
		/// </remarks>
		public bool VerifySignature(string payload, string signature, string secret)
		{
			// Default implementation returns false for safety - no signature can be verified without a provider-specific implementation
			return false;
		}

		#endregion
	}
}