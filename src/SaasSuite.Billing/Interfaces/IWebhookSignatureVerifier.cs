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

namespace SaasSuite.Billing.Interfaces
{
	/// <summary>
	/// Defines a contract for verifying cryptographic signatures of webhook payloads from payment providers.
	/// </summary>
	/// <remarks>
	/// Implementations validate that webhook requests are authentic and originated from the configured payment provider,
	/// preventing unauthorized or malicious webhook submissions.
	/// </remarks>
	public interface IWebhookSignatureVerifier
	{
		#region ' Methods '

		/// <summary>
		/// Verifies the cryptographic signature of a webhook payload to ensure authenticity.
		/// </summary>
		/// <param name="payload">The raw webhook payload string received from the payment provider, exactly as transmitted.</param>
		/// <param name="signature">The signature value provided by the payment provider in the webhook headers.</param>
		/// <param name="secret">The shared secret key configured for webhook verification, provided by the payment gateway.</param>
		/// <returns>
		/// <see langword="true"/> if the signature is valid and the webhook is authentic;
		/// otherwise, <see langword="false"/> if the signature is invalid or verification fails.
		/// </returns>
		/// <remarks>
		/// This method should implement the signature verification algorithm specific to the payment provider
		/// (e.g., HMAC-SHA256 for Stripe, or provider-specific signature schemes).
		/// </remarks>
		bool VerifySignature(string payload, string signature, string secret);

		#endregion
	}
}