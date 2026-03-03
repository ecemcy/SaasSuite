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

using SaasSuite.Quotas.Middleware;

namespace SaasSuite.Quotas.Options
{
	/// <summary>
	/// Provides configuration options for controlling quota enforcement behavior throughout the application.
	/// </summary>
	/// <remarks>
	/// These options control how the quota system behaves when quotas are undefined, exceeded,
	/// or checked during HTTP request processing. Options can be configured during service registration
	/// using the options pattern or bound from configuration sources like appsettings.json.
	/// Changes to these options require application restart to take effect as they are captured
	/// at service registration time and injected into middleware and services.
	/// </remarks>
	public class QuotaOptions
	{
		#region ' Properties '

		/// <summary>
		/// Gets or sets a value indicating whether requests should be allowed when no quota definition exists for a checked quota.
		/// </summary>
		/// <value>
		/// <see langword="true"/> to allow requests when quotas are undefined (fail-open);
		/// <see langword="false"/> to deny requests for undefined quotas (fail-closed).
		/// Defaults to <see langword="true"/>.
		/// </value>
		/// <remarks>
		/// This setting determines the security posture for undefined quotas:
		/// <list type="bullet">
		/// <item><description>When <see langword="true"/> (fail-open): Missing quota definitions are treated as unlimited access,
		/// allowing operations to proceed. This is more permissive and suitable for gradual quota rollout scenarios.</description></item>
		/// <item><description>When <see langword="false"/> (fail-closed): Any quota check for a non-existent quota will fail,
		/// blocking the operation. This is more restrictive and ensures explicit quota configuration before allowing access.</description></item>
		/// </list>
		/// Choose fail-open for flexibility during development or when adding quotas incrementally.
		/// Choose fail-closed for maximum security when all quotas should be explicitly defined.
		/// </remarks>
		public bool AllowIfQuotaNotDefined { get; set; } = true;

		/// <summary>
		/// Gets or sets a value indicating whether quota enforcement is globally active.
		/// </summary>
		/// <value>
		/// <see langword="true"/> to enforce quota limits (normal operation);
		/// <see langword="false"/> to bypass all quota checks (enforcement disabled).
		/// Defaults to <see langword="true"/>.
		/// </value>
		/// <remarks>
		/// Setting this to <see langword="false"/> disables all quota enforcement globally, acting as a master
		/// kill switch for the quota system. When disabled:
		/// <list type="bullet">
		/// <item><description>All quota checks immediately return success without querying the store</description></item>
		/// <item><description>Usage tracking may still occur (implementation-dependent)</description></item>
		/// <item><description>No HTTP 429 responses will be returned for quota violations</description></item>
		/// <item><description>Quota headers may still be included if <see cref="IncludeQuotaHeaders"/> is enabled</description></item>
		/// </list>
		/// Useful for testing, debugging, troubleshooting production issues, or emergency overrides
		/// when quota enforcement needs to be temporarily disabled without code changes.
		/// </remarks>
		public bool EnableEnforcement { get; set; } = true;

		/// <summary>
		/// Gets or sets a value indicating whether quota information should be included in HTTP response headers.
		/// </summary>
		/// <value>
		/// <see langword="true"/> to add X-RateLimit-* headers to responses;
		/// <see langword="false"/> to omit quota headers.
		/// Defaults to <see langword="true"/>.
		/// </value>
		/// <remarks>
		/// When enabled, responses include standard rate limiting headers following industry conventions:
		/// <list type="bullet">
		/// <item><description><c>X-RateLimit-Limit</c>: Maximum quota limit</description></item>
		/// <item><description><c>X-RateLimit-Remaining</c>: Units remaining before quota is exceeded</description></item>
		/// <item><description><c>X-RateLimit-Reset</c>: Unix timestamp when quota resets</description></item>
		/// </list>
		/// These headers allow API clients to implement intelligent retry logic, display usage information
		/// to users, and avoid hitting quota limits through proactive throttling.
		/// Disable this setting if you prefer not to expose quota information to clients, though this may
		/// reduce the client's ability to handle quota limits gracefully.
		/// </remarks>
		public bool IncludeQuotaHeaders { get; set; } = true;

		/// <summary>
		/// Gets or sets the error message returned to clients when a quota is exceeded.
		/// </summary>
		/// <value>
		/// A string containing the message to display when quota limits are violated.
		/// Defaults to <c>"Quota exceeded"</c>.
		/// </value>
		/// <remarks>
		/// This message is returned in HTTP 429 (Too Many Requests) response bodies and should
		/// provide clear, actionable guidance to the client about the quota violation.
		/// Consider including information such as:
		/// <list type="bullet">
		/// <item><description>What quota was exceeded (though this may be in headers)</description></item>
		/// <item><description>When the quota will reset (if not in headers)</description></item>
		/// <item><description>How to request quota increases or upgrade plans</description></item>
		/// <item><description>Support contact information for assistance</description></item>
		/// </list>
		/// For APIs returning JSON responses, consider setting this to a JSON string with structured
		/// error information rather than plain text. The message should be user-friendly while
		/// maintaining consistency with your API's error response format.
		/// </remarks>
		public string QuotaExceededMessage { get; set; } = "Quota exceeded";

		/// <summary>
		/// Gets or sets the collection of quota names that should be monitored and enforced by the middleware.
		/// </summary>
		/// <value>
		/// A list of quota names (e.g., "api-calls", "storage-gb") to actively track,
		/// or <see langword="null"/> / empty list to disable automatic middleware enforcement.
		/// Defaults to <see langword="null"/>.
		/// </value>
		/// <remarks>
		/// This property allows selective enforcement of specific quotas through the middleware.
		/// If populated, only the listed quota names will be automatically checked on each HTTP request
		/// by <see cref="QuotaEnforcementMiddleware"/>. If <see langword="null"/> or empty, the middleware
		/// will not enforce any quotas automatically (though quotas can still be checked manually via <c>QuotaService</c>).
		/// Each quota name in the list will consume one unit per request that passes through the middleware.
		/// For quotas measured differently (e.g., by bytes uploaded, not by request count), either:
		/// <list type="bullet">
		/// <item><description>Don't include them in this list and enforce them manually in specific endpoints</description></item>
		/// <item><description>Create custom middleware for those quota types</description></item>
		/// </list>
		/// Quota names should match those defined in <see cref="QuotaDefinition.Name"/> for the tenant.
		/// </remarks>
		public List<string>? TrackedQuotas { get; set; }

		#endregion
	}
}