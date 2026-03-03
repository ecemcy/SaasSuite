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

using SaasSuite.Seats.Middleware;

namespace SaasSuite.Seats.Options
{
	/// <summary>
	/// Configuration options for the <see cref="SeatEnforcerMiddleware"/>.
	/// Controls seat enforcement behavior including enablement, error messages, and user identification strategies.
	/// </summary>
	/// <remarks>
	/// These options can be configured during service registration:
	/// <code>
	/// services.AddSaasSeats(options =>
	/// {
	///     options.EnableEnforcement = true;
	///     options.SeatLimitMessage = "Your organization has reached its user limit.";
	///     options.UserIdClaimType = "sub";
	///     options.UserIdHeaderName = "X-User-Id";
	/// });
	/// </code>
	/// </remarks>
	public class SeatEnforcerOptions
	{
		#region ' Properties '

		/// <summary>
		/// Gets or sets a value indicating whether seat enforcement is globally enabled for the application.
		/// When <see langword="false"/>, all seat checks are bypassed and requests proceed without seat validation.
		/// </summary>
		/// <value>
		/// <see langword="true"/> to enable seat enforcement and block requests when seat limits are exceeded;
		/// <see langword="false"/> to disable enforcement and allow all requests regardless of seat availability.
		/// Defaults to <see langword="true"/>.
		/// </value>
		/// <remarks>
		/// This setting provides a global kill switch for seat enforcement, useful for:
		/// <list type="bullet">
		/// <item><description>Development and testing environments where seat limits should not be enforced</description></item>
		/// <item><description>Temporarily disabling enforcement during incidents or maintenance</description></item>
		/// <item><description>Gradual rollout of seat enforcement to production environments</description></item>
		/// <item><description>Emergency scenarios requiring immediate removal of seat restrictions</description></item>
		/// </list>
		/// When disabled, the middleware still executes but skips all seat-related checks, minimizing performance impact.
		/// </remarks>
		public bool EnableEnforcement { get; set; } = true;

		/// <summary>
		/// Gets or sets the error message returned to clients when seat limits are exceeded.
		/// This message is included in the JSON error response sent with HTTP 429 status codes.
		/// </summary>
		/// <value>
		/// A user-friendly error message explaining why the request was rejected. Defaults to
		/// "Tenant has reached maximum seat limit". Should be clear and actionable for end users.
		/// </value>
		/// <remarks>
		/// Best practices for seat limit messages:
		/// <list type="bullet">
		/// <item><description>Be clear and specific about why access is denied</description></item>
		/// <item><description>Provide guidance on how to resolve the issue (contact admin, upgrade plan)</description></item>
		/// <item><description>Avoid technical jargon that end users may not understand</description></item>
		/// <item><description>Consider internationalization for multi-language applications</description></item>
		/// <item><description>Include contact information or support links if appropriate</description></item>
		/// </list>
		/// Example messages:
		/// <list type="bullet">
		/// <item><description>"Your organization has reached its maximum user limit. Please contact your administrator."</description></item>
		/// <item><description>"All available seats are currently in use. Please try again later or upgrade your plan."</description></item>
		/// <item><description>"Maximum concurrent users exceeded. Please wait for another user to log out."</description></item>
		/// </list>
		/// </remarks>
		public string SeatLimitMessage { get; set; } = "Tenant has reached maximum seat limit";

		/// <summary>
		/// Gets or sets the claim type used to extract user identifiers from authentication tokens.
		/// This claim is checked first when attempting to identify the current user for seat enforcement.
		/// </summary>
		/// <value>
		/// The name of the claim containing the user identifier in JWT tokens or authentication cookies.
		/// Defaults to "sub" (subject), which is the standard claim type for user IDs in OpenID Connect and OAuth 2.0.
		/// </value>
		/// <remarks>
		/// Common claim types by authentication provider:
		/// <list type="bullet">
		/// <item><description>"sub" - Standard OpenID Connect subject claim (default)</description></item>
		/// <item><description>"http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier" - ASP.NET Identity</description></item>
		/// <item><description>"oid" - Azure AD object identifier</description></item>
		/// <item><description>"email" - Email address (if used as unique identifier)</description></item>
		/// <item><description>"preferred_username" - Username from identity provider</description></item>
		/// </list>
		/// Ensure this claim type matches the claim issued by your authentication provider to avoid
		/// users being treated as anonymous and bypassing seat enforcement.
		/// </remarks>
		public string UserIdClaimType { get; set; } = "sub";

		/// <summary>
		/// Gets or sets the HTTP header name used as a fallback to extract user identifiers when claims are not available.
		/// This header is checked if the user is not authenticated or the configured claim type is not present.
		/// </summary>
		/// <value>
		/// The name of the HTTP header containing the user identifier. Defaults to "X-User-Id".
		/// Header names are case-insensitive per HTTP specifications.
		/// </value>
		/// <remarks>
		/// This fallback mechanism supports scenarios such as:
		/// <list type="bullet">
		/// <item><description>API key authentication where users are not represented by claims</description></item>
		/// <item><description>Service-to-service communication with custom authentication</description></item>
		/// <item><description>Legacy systems that use header-based authentication</description></item>
		/// <item><description>Proxy or gateway servers that inject user context via headers</description></item>
		/// <item><description>Development and testing scenarios with simplified authentication</description></item>
		/// </list>
		/// <para>
		/// Security considerations:
		/// <list type="bullet">
		/// <item><description>Ensure the header cannot be spoofed by end users (validate at edge/gateway)</description></item>
		/// <item><description>Use HTTPS to prevent header tampering in transit</description></item>
		/// <item><description>Consider using signed headers or tokens for additional security</description></item>
		/// <item><description>Prefer claim-based authentication for production environments</description></item>
		/// </list>
		/// </para>
		/// </remarks>
		public string UserIdHeaderName { get; set; } = "X-User-Id";

		#endregion
	}
}