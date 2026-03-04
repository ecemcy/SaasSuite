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

using SaasSuite.Compliance.Interfaces;
using SaasSuite.Compliance.Services;
using SaasSuite.Compliance.Stores;

namespace Microsoft.Extensions.DependencyInjection
{
	/// <summary>
	/// Provides extension methods for registering SaasSuite compliance services in the dependency injection container.
	/// </summary>
	/// <remarks>
	/// These extensions simplify the registration of compliance-related services required for GDPR, CCPA,
	/// and other data protection regulations. Services include data export, right-to-be-forgotten,
	/// and user consent management capabilities.
	/// </remarks>
	public static class ServiceCollectionExtensions
	{
		#region ' Static Methods '

		/// <summary>
		/// Registers SaasSuite compliance services using default in-memory implementations.
		/// </summary>
		/// <param name="services">The <see cref="IServiceCollection"/> to add services to. Cannot be <see langword="null"/>.</param>
		/// <returns>The <see cref="IServiceCollection"/> for method chaining.</returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="services"/> is <see langword="null"/>.
		/// </exception>
		/// <remarks>
		/// <para>This method registers the following services as singletons:</para>
		/// <list type="bullet">
		/// <item><description><see cref="InMemoryComplianceExporter"/> as <see cref="IComplianceExporter"/> for data export operations</description></item>
		/// <item><description><see cref="InMemoryRightToBeForgottenService"/> as <see cref="IRightToBeForgottenService"/> for data deletion/anonymization</description></item>
		/// <item><description><see cref="InMemoryConsentStore"/> as <see cref="IConsentStore"/> for user consent management</description></item>
		/// </list>
		/// <para>The in-memory implementations are suitable for development, testing, and demonstration purposes.
		/// For production use, register custom implementations backed by persistent storage using the generic overload.
		/// All data stored by these services is lost when the application restarts.</para>
		/// </remarks>
		public static IServiceCollection AddSaasCompliance(this IServiceCollection services)
		{
			// Validate that services collection is not null
			ArgumentNullException.ThrowIfNull(services);

			// Register default compliance exporter for data export operations
			_ = services.AddSingleton<IComplianceExporter, InMemoryComplianceExporter>();

			// Register default right-to-be-forgotten service for data deletion and anonymization
			_ = services.AddSingleton<IRightToBeForgottenService, InMemoryRightToBeForgottenService>();

			// Register default consent store for managing user consent records
			_ = services.AddSingleton<IConsentStore, InMemoryConsentStore>();

			// Return the service collection for fluent chaining
			return services;
		}

		/// <summary>
		/// Registers SaasSuite compliance services with custom implementations for production scenarios.
		/// </summary>
		/// <typeparam name="TExporter">
		/// The concrete type implementing <see cref="IComplianceExporter"/> for data export operations.
		/// Must be a class with a public constructor compatible with dependency injection.
		/// </typeparam>
		/// <typeparam name="TRightToBeForgotten">
		/// The concrete type implementing <see cref="IRightToBeForgottenService"/> for data deletion and anonymization.
		/// Must be a class with a public constructor compatible with dependency injection.
		/// </typeparam>
		/// <typeparam name="TConsentStore">
		/// The concrete type implementing <see cref="IConsentStore"/> for consent management.
		/// Must be a class with a public constructor compatible with dependency injection.
		/// </typeparam>
		/// <param name="services">The <see cref="IServiceCollection"/> to add services to. Cannot be <see langword="null"/>.</param>
		/// <returns>The <see cref="IServiceCollection"/> for method chaining.</returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="services"/> is <see langword="null"/>.
		/// </exception>
		/// <remarks>
		/// <para>Use this overload to provide custom implementations backed by databases, distributed storage,
		/// or external compliance management systems. All implementations are registered as singletons,
		/// which is typically appropriate for stateless services that manage external resources.
		/// If your implementations require scoped or transient lifetimes, register them manually instead.</para>
		/// <para>Common custom implementations might include:</para>
		/// <list type="bullet">
		/// <item><description>Database-backed stores using Entity Framework Core</description></item>
		/// <item><description>Azure Blob Storage for data exports</description></item>
		/// <item><description>Integration with third-party compliance platforms</description></item>
		/// </list>
		/// <para>Ensure your custom implementations handle multi-tenancy correctly to maintain data isolation.</para>
		/// </remarks>
		public static IServiceCollection AddSaasCompliance<TExporter, TRightToBeForgotten, TConsentStore>(this IServiceCollection services)
			where TExporter : class, IComplianceExporter
			where TRightToBeForgotten : class, IRightToBeForgottenService
			where TConsentStore : class, IConsentStore
		{
			// Validate that services collection is not null
			ArgumentNullException.ThrowIfNull(services);

			// Register custom compliance exporter implementation
			_ = services.AddSingleton<IComplianceExporter, TExporter>();

			// Register custom right-to-be-forgotten service implementation
			_ = services.AddSingleton<IRightToBeForgottenService, TRightToBeForgotten>();

			// Register custom consent store implementation
			_ = services.AddSingleton<IConsentStore, TConsentStore>();

			// Return the service collection for fluent chaining
			return services;
		}

		#endregion
	}
}