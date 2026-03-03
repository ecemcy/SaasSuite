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

using System.Text.Json;

using Microsoft.Extensions.DependencyInjection;

using SaasSuite.Core.Enumerations;

namespace SaasSuite.Discovery.Reports
{
	/// <summary>
	/// Provides utility methods for generating discovery manifests from service registrations.
	/// </summary>
	/// <remarks>
	/// The manifest generator creates structured reports of discovered services that can be
	/// serialized to JSON for documentation, diagnostics, or automated validation in CI/CD pipelines.
	/// </remarks>
	public class ManifestGenerator
	{
		#region ' Static Fields '

		/// <summary>
		/// JSON serialization options configured for human-readable output with indentation.
		/// Uses camelCase property naming to align with JavaScript and JSON conventions.
		/// </summary>
		/// <remarks>
		/// This configuration is ideal for documentation, debugging, and manual inspection scenarios
		/// where readability is prioritized over file size.
		/// </remarks>
		private static readonly JsonSerializerOptions DefaultOptions = new JsonSerializerOptions
		{
			WriteIndented = true,
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase
		};

		/// <summary>
		/// JSON serialization options configured for compact output without indentation.
		/// Uses camelCase property naming to align with JavaScript and JSON conventions.
		/// </summary>
		/// <remarks>
		/// This configuration is optimized for storage efficiency and network transmission
		/// where minimizing payload size is more important than human readability.
		/// </remarks>
		private static readonly JsonSerializerOptions CompactOptions = new JsonSerializerOptions
		{
			WriteIndented = false,
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase
		};

		#endregion

		#region ' Static Methods '

		/// <summary>
		/// Asynchronously writes a discovery manifest to a file in JSON format.
		/// </summary>
		/// <param name="manifest">The manifest to write. Cannot be <see langword="null"/>.</param>
		/// <param name="filePath">The file path where the manifest should be written. Cannot be <see langword="null"/> or whitespace.</param>
		/// <param name="indented">
		/// <see langword="true"/> to format the JSON with indentation for readability;
		/// <see langword="false"/> for compact output. Defaults to <see langword="true"/>.
		/// </param>
		/// <returns>A task that represents the asynchronous write operation.</returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="manifest"/> or <paramref name="filePath"/> is <see langword="null"/>.
		/// </exception>
		/// <exception cref="IOException">
		/// Thrown when the file cannot be written due to permissions or path issues.
		/// </exception>
		/// <remarks>
		/// This method creates or overwrites the specified file with the JSON-serialized manifest.
		/// Common use cases include:
		/// <list type="bullet">
		/// <item><description>Generating documentation during build processes</description></item>
		/// <item><description>Creating audit trails of service configurations</description></item>
		/// <item><description>Validating service discovery in CI/CD pipelines</description></item>
		/// <item><description>Debugging service registration issues</description></item>
		/// </list>
		/// The file is written using UTF-8 encoding without BOM. If the directory does not exist,
		/// it must be created before calling this method.
		/// </remarks>
		public static async Task WriteToFileAsync(DiscoveryManifest manifest, string filePath, bool indented = true)
		{
			// Serialize the manifest to a JSON string using the appropriate formatting options
			// This converts the in-memory manifest structure to a JSON representation
			string json = ToJson(manifest, indented);

			// Asynchronously write the JSON content to the specified file path
			// This operation overwrites the file if it exists, or creates a new file if it doesn't
			// Uses UTF-8 encoding by default for broad compatibility
			await File.WriteAllTextAsync(filePath, json);
		}

		/// <summary>
		/// Serializes a discovery manifest to JSON format.
		/// </summary>
		/// <param name="manifest">The manifest to serialize. Cannot be <see langword="null"/>.</param>
		/// <param name="indented">
		/// <see langword="true"/> to format the JSON with indentation for readability;
		/// <see langword="false"/> for compact output. Defaults to <see langword="true"/>.
		/// </param>
		/// <returns>A JSON string representation of the manifest.</returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="manifest"/> is <see langword="null"/>.
		/// </exception>
		/// <exception cref="NotSupportedException">
		/// Thrown when there is no compatible <see cref="System.Text.Json.Serialization.JsonConverter"/>
		/// for the manifest or its properties.
		/// </exception>
		/// <remarks>
		/// The JSON output uses camelCase property naming for consistency with JavaScript conventions.
		/// Indented output is more readable for documentation and manual inspection, while compact
		/// output is more efficient for storage and transmission. The method uses pre-configured
		/// serialization options for performance optimization.
		/// </remarks>
		public static string ToJson(DiscoveryManifest manifest, bool indented = true)
		{
			// Select the appropriate JSON serializer options based on the indented parameter
			// Using pre-configured static options improves performance by avoiding repeated object creation
			JsonSerializerOptions options = indented ? DefaultOptions : CompactOptions;

			// Serialize the manifest object to a JSON string using the selected options
			return JsonSerializer.Serialize(manifest, options);
		}

		/// <summary>
		/// Generates a discovery manifest from a collection of service registrations.
		/// </summary>
		/// <param name="registrations">The service registrations to include in the manifest. Cannot be <see langword="null"/>.</param>
		/// <returns>
		/// A <see cref="DiscoveryManifest"/> containing all registration details and summary statistics.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="registrations"/> is <see langword="null"/>.
		/// </exception>
		/// <remarks>
		/// This method processes all registrations to create a comprehensive manifest including:
		/// <list type="bullet">
		/// <item><description>Individual service registration entries with full metadata</description></item>
		/// <item><description>Aggregated statistics grouped by lifetime, tenant scope, and source</description></item>
		/// <item><description>Counts of services with special configurations (predicates, decorators)</description></item>
		/// <item><description>Timestamp of manifest generation</description></item>
		/// </list>
		/// The manifest can be serialized to JSON using <see cref="ToJson"/> or written to a file
		/// using <see cref="WriteToFileAsync"/>. This method materializes the enumerable to avoid
		/// multiple enumeration issues.
		/// </remarks>
		public static DiscoveryManifest Generate(IEnumerable<ServiceRegistration> registrations)
		{
			// Materialize the enumerable to a list to enable multiple passes over the data
			// without triggering re-enumeration, which could be expensive or cause side effects
			List<ServiceRegistration> regList = registrations.ToList();

			// Build a dictionary of service counts grouped by their lifetime (Singleton, Scoped, Transient)
			// This provides insight into the lifecycle management strategy of the application
			Dictionary<string, int> byLifetime = new Dictionary<string, int>();
			foreach (IGrouping<ServiceLifetime, ServiceRegistration> group in regList.GroupBy(r => r.Lifetime))
			{
				byLifetime[group.Key.ToString()] = group.Count();
			}

			// Build a dictionary of service counts grouped by their tenant scope
			// This helps understand multi-tenant service distribution (Global, Tenant, Isolated)
			Dictionary<string, int> byTenantScope = new Dictionary<string, int>();
			foreach (IGrouping<TenantScope, ServiceRegistration> group in regList.GroupBy(r => r.TenantScope))
			{
				byTenantScope[group.Key.ToString()] = group.Count();
			}

			// Build a dictionary of service counts grouped by their registration source
			// Sources indicate where the service was registered (e.g., "Startup", "Module", "Plugin")
			// Services without a source are categorized as "Unknown" for clarity
			Dictionary<string, int> bySource = new Dictionary<string, int>();
			foreach (IGrouping<string, ServiceRegistration> group in regList.GroupBy(r => r.Source ?? "Unknown"))
			{
				bySource[group.Key] = group.Count();
			}

			// Count services that have tenant-specific predicates configured
			// Predicates allow conditional service resolution based on tenant context
			int withPredicates = regList.Count(r => r.TenantPredicate != null);

			// Count services that have been enhanced with decorator patterns
			// Decorators add cross-cutting concerns like logging, caching, or validation
			int withDecorators = regList.Count(r => r.Decorators.Any());

			// Initialize the manifest with metadata and aggregated statistics
			// The summary provides a high-level overview of the service architecture
			DiscoveryManifest manifest = new DiscoveryManifest
			{
				GeneratedAt = DateTimeOffset.UtcNow,
				Summary = new ManifestSummary
				{
					ByLifetime = byLifetime,
					ByTenantScope = byTenantScope,
					BySource = bySource,
					WithTenantPredicates = withPredicates,
					WithDecorators = withDecorators
				}
			};

			// Transform each service registration into a manifest entry with detailed metadata
			// This creates a complete inventory of all registered services
			foreach (ServiceRegistration registration in regList)
			{
				// Extract the implementation type name, preferring the full name for clarity
				// Falls back to the simple name if the full name is unavailable
				ServiceRegistrationEntry entry = new ServiceRegistrationEntry(
					registration.ImplementationType.FullName ?? registration.ImplementationType.Name,
					// Map all service interface types to their string representations
					// A single implementation can satisfy multiple service contracts
					registration.ServiceTypes
						.Select(t => t.FullName ?? t.Name)
						.ToList(),
					// Convert the lifetime enum to a string for JSON serialization
					registration.Lifetime.ToString(),
					// Convert the tenant scope enum to a string for JSON serialization
					registration.TenantScope.ToString())
				{
					// Flag indicating whether this service has tenant-specific resolution logic
					HasTenantPredicate = registration.TenantPredicate != null,
					// Provide a description of the predicate if one exists
					// Actual predicate logic cannot be serialized, so we provide metadata instead
					TenantPredicateDescription = registration.TenantPredicate != null
						? "Custom tenant predicate configured"
						: null,
					// Include the registration source for traceability
					Source = registration.Source,
					// Map decorator types to their string representations
					// Decorators are listed in the order they were applied
					Decorators = registration.Decorators
						.Select(d => d.FullName ?? d.Name)
						.ToList()
				};

				// Add the completed entry to the manifest's collection
				manifest.Registrations.Add(entry);
			}

			// Return the fully populated manifest ready for serialization or analysis
			return manifest;
		}

		#endregion
	}
}