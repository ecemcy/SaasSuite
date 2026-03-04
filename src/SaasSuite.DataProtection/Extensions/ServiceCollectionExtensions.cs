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

using SaasSuite.DataProtection.Interfaces;
using SaasSuite.DataProtection.Services;

namespace Microsoft.Extensions.DependencyInjection
{
	/// <summary>
	/// Provides extension methods for registering SaasSuite data protection services in the dependency injection container.
	/// </summary>
	/// <remarks>
	/// These extensions simplify the registration of encryption and key management services required
	/// for protecting tenant data at rest. The methods support both default in-memory implementations
	/// for testing and custom implementations for production scenarios.
	/// </remarks>
	public static class ServiceCollectionExtensions
	{
		#region ' Static Methods '

		/// <summary>
		/// Registers SaasSuite data protection services using the default in-memory implementations.
		/// </summary>
		/// <param name="services">The <see cref="IServiceCollection"/> to add services to. Cannot be <see langword="null"/>.</param>
		/// <returns>The <see cref="IServiceCollection"/> for method chaining.</returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="services"/> is <see langword="null"/>.
		/// </exception>
		/// <remarks>
		/// <para>
		/// This method registers the following services as singletons:
		/// <list type="bullet">
		/// <item><description><see cref="InMemoryKeyEncryptionKeyProvider"/> as <see cref="IKeyEncryptionKeyProvider"/></description></item>
		/// <item><description><see cref="InMemoryTenantKeyProvider"/> as <see cref="ITenantKeyProvider"/></description></item>
		/// </list>
		/// </para>
		/// <para>
		/// <strong>WARNING:</strong> The in-memory implementations are NOT suitable for production use.
		/// All encryption keys are stored in memory and will be lost when the application restarts,
		/// making previously encrypted data unrecoverable. Use this only for:
		/// <list type="bullet">
		/// <item><description>Development and testing environments</description></item>
		/// <item><description>Demonstrations and prototypes</description></item>
		/// <item><description>Integration tests that verify encryption workflow</description></item>
		/// </list>
		/// </para>
		/// <para>
		/// For production environments, use <see cref="AddSaasDataProtection{TKeyEncryptionKeyProvider, TTenantKeyProvider}"/>
		/// to register custom implementations backed by secure key storage solutions like Azure Key Vault,
		/// AWS KMS, HashiCorp Vault, or HSM devices.
		/// </para>
		/// </remarks>
		public static IServiceCollection AddSaasDataProtection(this IServiceCollection services)
		{
			// Validate that services collection is not null
			ArgumentNullException.ThrowIfNull(services);

			// Register default in-memory key encryption key provider as singleton
			_ = services.AddSingleton<IKeyEncryptionKeyProvider, InMemoryKeyEncryptionKeyProvider>();

			// Register default in-memory tenant key provider as singleton
			_ = services.AddSingleton<ITenantKeyProvider, InMemoryTenantKeyProvider>();

			// Return the service collection for fluent chaining
			return services;
		}

		/// <summary>
		/// Registers SaasSuite data protection services with custom implementations for production scenarios.
		/// </summary>
		/// <typeparam name="TKeyEncryptionKeyProvider">
		/// The concrete type implementing <see cref="IKeyEncryptionKeyProvider"/> for master key management.
		/// Must be a class with a public constructor compatible with dependency injection.
		/// </typeparam>
		/// <typeparam name="TTenantKeyProvider">
		/// The concrete type implementing <see cref="ITenantKeyProvider"/> for tenant-specific key management.
		/// Must be a class with a public constructor compatible with dependency injection.
		/// </typeparam>
		/// <param name="services">The <see cref="IServiceCollection"/> to add services to. Cannot be <see langword="null"/>.</param>
		/// <returns>The <see cref="IServiceCollection"/> for method chaining.</returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="services"/> is <see langword="null"/>.
		/// </exception>
		/// <remarks>
		/// Use this overload to provide production-ready implementations backed by secure key management solutions.
		/// Both implementations are registered as singletons, which is typically appropriate for stateless
		/// services that manage external key storage resources.
		/// <para>
		/// Common production implementations include:
		/// <list type="bullet">
		/// <item><description>Azure Key Vault for cloud-based key storage with HSM backing</description></item>
		/// <item><description>AWS Key Management Service (KMS) for AWS-hosted applications</description></item>
		/// <item><description>HashiCorp Vault for on-premises or hybrid deployments</description></item>
		/// <item><description>Hardware Security Modules (HSMs) for the highest security requirements</description></item>
		/// <item><description>Database-backed storage with encrypted key material</description></item>
		/// </list>
		/// </para>
		/// <para>
		/// Your custom implementations should:
		/// <list type="bullet">
		/// <item><description>Protect keys at rest using encryption or HSM backing</description></item>
		/// <item><description>Implement key rotation capabilities</description></item>
		/// <item><description>Enforce access controls and audit logging</description></item>
		/// <item><description>Handle key versioning for backward compatibility</description></item>
		/// <item><description>Support multi-region deployments if required</description></item>
		/// </list>
		/// </para>
		/// </remarks>
		public static IServiceCollection AddSaasDataProtection<TKeyEncryptionKeyProvider, TTenantKeyProvider>(this IServiceCollection services)
			where TKeyEncryptionKeyProvider : class, IKeyEncryptionKeyProvider
			where TTenantKeyProvider : class, ITenantKeyProvider
		{
			// Validate that services collection is not null
			ArgumentNullException.ThrowIfNull(services);

			// Register custom key encryption key provider as singleton
			_ = services.AddSingleton<IKeyEncryptionKeyProvider, TKeyEncryptionKeyProvider>();

			// Register custom tenant key provider as singleton
			_ = services.AddSingleton<ITenantKeyProvider, TTenantKeyProvider>();

			// Return the service collection for fluent chaining
			return services;
		}

		#endregion
	}
}