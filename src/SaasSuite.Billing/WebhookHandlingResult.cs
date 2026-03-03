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

namespace SaasSuite.Billing
{
	/// <summary>
	/// Represents the result of processing a webhook notification from a payment provider.
	/// </summary>
	/// <remarks>
	/// This class encapsulates the outcome of webhook handling, including success status, error information, or skip reasons.
	/// </remarks>
	public class WebhookHandlingResult
	{
		#region ' Properties '

		/// <summary>
		/// Gets or sets a value indicating whether the webhook was intentionally skipped because the handler doesn't support it.
		/// </summary>
		/// <value>
		/// <see langword="true"/> if the webhook was skipped due to being unsupported or not applicable;
		/// otherwise, <see langword="false"/>. Defaults to <see langword="false"/>.
		/// </value>
		/// <remarks>
		/// This is used when the webhook event type is not relevant to the current handler implementation.
		/// </remarks>
		public bool Skipped { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether the webhook was successfully handled and processed.
		/// </summary>
		/// <value>
		/// <see langword="true"/> if the webhook was successfully processed and the system was updated;
		/// otherwise, <see langword="false"/>. Defaults to <see langword="false"/>.
		/// </value>
		public bool Success { get; set; }

		/// <summary>
		/// Gets or sets an error message if the webhook handling failed.
		/// </summary>
		/// <value>
		/// A descriptive error message string, or <see langword="null"/> if the operation was successful.
		/// </value>
		/// <remarks>
		/// This provides details about what went wrong during webhook processing or why the webhook was skipped.
		/// </remarks>
		public string? ErrorMessage { get; set; }

		#endregion

		#region ' Static Methods '

		/// <summary>
		/// Creates a failed webhook handling result indicating the webhook processing encountered an error.
		/// </summary>
		/// <param name="errorMessage">A description of the error that occurred during webhook handling.</param>
		/// <returns>
		/// A <see cref="WebhookHandlingResult"/> with <see cref="Success"/> set to <see langword="false"/>
		/// and the error stored in <see cref="ErrorMessage"/>.
		/// </returns>
		/// <remarks>
		/// Use this when webhook processing fails due to validation errors, system errors, or data issues.
		/// </remarks>
		public static WebhookHandlingResult FailureResult(string errorMessage)
		{
			return new WebhookHandlingResult() { Success = false, ErrorMessage = errorMessage };
		}

		/// <summary>
		/// Creates a skipped webhook handling result indicating the webhook was not processed because it's unsupported.
		/// </summary>
		/// <param name="reason">A description of why the webhook was skipped.</param>
		/// <returns>
		/// A <see cref="WebhookHandlingResult"/> with <see cref="Skipped"/> set to <see langword="true"/>
		/// and the reason stored in <see cref="ErrorMessage"/>.
		/// </returns>
		/// <remarks>
		/// Use this when the webhook event type is not handled by the current implementation.
		/// </remarks>
		public static WebhookHandlingResult SkippedResult(string reason)
		{
			return new WebhookHandlingResult() { Skipped = true, ErrorMessage = reason };
		}

		/// <summary>
		/// Creates a successful webhook handling result indicating the webhook was processed correctly.
		/// </summary>
		/// <returns>A <see cref="WebhookHandlingResult"/> with <see cref="Success"/> set to <see langword="true"/>.</returns>
		public static WebhookHandlingResult SuccessResult()
		{
			return new WebhookHandlingResult() { Success = true };
		}

		#endregion
	}
}