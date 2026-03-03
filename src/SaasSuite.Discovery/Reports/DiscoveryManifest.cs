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

using System.Text.Json.Serialization;

namespace SaasSuite.Discovery.Reports
{
	/// <summary>
	/// Represents a comprehensive manifest of all services discovered during service discovery.
	/// </summary>
	/// <remarks>
	/// The discovery manifest provides a complete inventory of discovered services with metadata
	/// including registration details, lifetimes, tenant scopes, and summary statistics.
	/// This manifest can be serialized to JSON for documentation, diagnostics, or CI/CD validation.
	/// </remarks>
	public class DiscoveryManifest
	{
		#region ' Properties '

		/// <summary>
		/// Gets the total count of service registrations in the manifest.
		/// </summary>
		/// <value>
		/// An integer representing the number of service registrations discovered.
		/// </value>
		/// <remarks>
		/// This is a computed property that returns <see cref="Registrations"/>.Count.
		/// It provides a convenient top-level statistic for manifest summaries.
		/// </remarks>
		[JsonPropertyName("totalRegistrations")]
		public int TotalRegistrations => this.Registrations.Count;

		/// <summary>
		/// Gets the UTC timestamp when the manifest was generated.
		/// </summary>
		/// <value>
		/// A <see cref="DateTimeOffset"/> in UTC indicating when the discovery process completed.
		/// Defaults to the current UTC time.
		/// </value>
		/// <remarks>
		/// The timestamp allows tracking when the manifest was created and can be used to
		/// detect configuration drift or verify manifest freshness in automated processes.
		/// </remarks>
		[JsonPropertyName("generatedAt")]
		public DateTimeOffset GeneratedAt { get; init; } = DateTimeOffset.UtcNow;

		/// <summary>
		/// Gets the collection of all discovered service registrations.
		/// </summary>
		/// <value>
		/// A list of <see cref="ServiceRegistrationEntry"/> objects representing each discovered service.
		/// Defaults to an empty list. Cannot be <see langword="null"/>.
		/// </value>
		/// <remarks>
		/// Each entry contains detailed information about a service registration including
		/// implementation type, service types, lifetime, tenant scope, decorators, and predicates.
		/// The list can be filtered, sorted, or analyzed for various diagnostic purposes.
		/// </remarks>
		[JsonPropertyName("registrations")]
		public List<ServiceRegistrationEntry> Registrations { get; init; } = new List<ServiceRegistrationEntry>();

		/// <summary>
		/// Gets summary statistics about the discovered services.
		/// </summary>
		/// <value>
		/// A <see cref="ManifestSummary"/> object containing aggregated statistics.
		/// Defaults to a new empty summary. Cannot be <see langword="null"/>.
		/// </value>
		/// <remarks>
		/// The summary provides quick insights including:
		/// <list type="bullet">
		/// <item><description>Distribution of services by lifetime (Scoped, Singleton, Transient)</description></item>
		/// <item><description>Distribution by tenant scope (Global, Request, SingletonPerTenant)</description></item>
		/// <item><description>Count of services with tenant predicates</description></item>
		/// <item><description>Count of services with decorators applied</description></item>
		/// <item><description>Services grouped by source (assembly, attribute, etc.)</description></item>
		/// </list>
		/// </remarks>
		[JsonPropertyName("summary")]
		public ManifestSummary Summary { get; init; } = new ManifestSummary();

		#endregion
	}
}