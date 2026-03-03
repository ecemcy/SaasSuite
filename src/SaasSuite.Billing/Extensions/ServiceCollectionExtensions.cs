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

using Microsoft.Extensions.DependencyInjection.Extensions;

using SaasSuite.Billing.Interfaces;
using SaasSuite.Billing.Options;
using SaasSuite.Billing.Services;
using SaasSuite.Billing.Webhooks;

namespace Microsoft.Extensions.DependencyInjection
{
	/// <summary>
	/// Extension methods for configuring billing services in an <see cref="IServiceCollection"/>.
	/// </summary>
	/// <remarks>
	/// These extensions provide fluent API methods to register all required billing services, including
	/// invoice management, billing orchestration, reconciliation, and webhook handling.
	/// </remarks>
	public static class ServiceCollectionExtensions
	{
		#region ' Static Methods '

		/// <summary>
		/// Adds SaaS billing services to the service collection with default configuration.
		/// </summary>
		/// <param name="services">The <see cref="IServiceCollection"/> to add services to. Cannot be <see langword="null"/>.</param>
		/// <returns>The <see cref="IServiceCollection"/> for method chaining.</returns>
		/// <remarks>
		/// This registers the invoice service, billing orchestrator, reconciliation job, and no-op webhook handlers.
		/// </remarks>
		public static IServiceCollection AddSaasBilling(this IServiceCollection services)
		{
			return services.AddSaasBilling(_ => { });
		}

		/// <summary>
		/// Adds SaaS billing services to the service collection with custom configuration.
		/// </summary>
		/// <param name="services">The <see cref="IServiceCollection"/> to add services to. Cannot be <see langword="null"/>.</param>
		/// <param name="configureOptions">An action to configure <see cref="BillingOptions"/>. Cannot be <see langword="null"/>.</param>
		/// <returns>The <see cref="IServiceCollection"/> for method chaining.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> or <paramref name="configureOptions"/> is <see langword="null"/>.</exception>
		/// <remarks>
		/// This registers the invoice service, billing orchestrator, reconciliation job, and no-op webhook handlers
		/// with the provided options configuration.
		/// </remarks>
		public static IServiceCollection AddSaasBilling(this IServiceCollection services, Action<BillingOptions> configureOptions)
		{
			ArgumentNullException.ThrowIfNull(services);
			ArgumentNullException.ThrowIfNull(configureOptions);

			// Register billing options
			_ = services.Configure(configureOptions);

			// Register core billing services
			services.TryAddSingleton<InvoiceService>();
			services.TryAddScoped<IBillingOrchestrator, BillingOrchestrator>();
			services.TryAddScoped<ReconciliationJob>();

			// Register default no-op webhook handlers (can be replaced with provider-specific implementations)
			services.TryAddSingleton<IPaymentWebhookHandler, NoOpPaymentWebhookHandler>();
			services.TryAddSingleton<IWebhookSignatureVerifier, NoOpWebhookSignatureVerifier>();

			return services;
		}

		/// <summary>
		/// Adds SaaS billing services with a custom billing orchestrator implementation.
		/// </summary>
		/// <typeparam name="TOrchestrator">The custom type implementing <see cref="IBillingOrchestrator"/>.</typeparam>
		/// <param name="services">The <see cref="IServiceCollection"/> to add services to. Cannot be <see langword="null"/>.</param>
		/// <returns>The <see cref="IServiceCollection"/> for method chaining.</returns>
		/// <remarks>
		/// This allows replacing the default <see cref="BillingOrchestrator"/> with a custom implementation.
		/// </remarks>
		public static IServiceCollection AddSaasBilling<TOrchestrator>(this IServiceCollection services)
			where TOrchestrator : class, IBillingOrchestrator
		{
			return services.AddSaasBilling<TOrchestrator>(_ => { });
		}

		/// <summary>
		/// Adds SaaS billing services with a custom billing orchestrator implementation and configuration.
		/// </summary>
		/// <typeparam name="TOrchestrator">The custom type implementing <see cref="IBillingOrchestrator"/>.</typeparam>
		/// <param name="services">The <see cref="IServiceCollection"/> to add services to. Cannot be <see langword="null"/>.</param>
		/// <param name="configureOptions">An action to configure <see cref="BillingOptions"/>. Cannot be <see langword="null"/>.</param>
		/// <returns>The <see cref="IServiceCollection"/> for method chaining.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> or <paramref name="configureOptions"/> is <see langword="null"/>.</exception>
		/// <remarks>
		/// This allows replacing the default <see cref="BillingOrchestrator"/> with a custom implementation
		/// while also configuring billing options.
		/// </remarks>
		public static IServiceCollection AddSaasBilling<TOrchestrator>(this IServiceCollection services, Action<BillingOptions> configureOptions)
			where TOrchestrator : class, IBillingOrchestrator
		{
			ArgumentNullException.ThrowIfNull(services);
			ArgumentNullException.ThrowIfNull(configureOptions);

			// Register billing options
			_ = services.Configure(configureOptions);

			// Register core billing services with custom orchestrator
			services.TryAddSingleton<InvoiceService>();
			services.TryAddScoped<IBillingOrchestrator, TOrchestrator>();
			services.TryAddScoped<ReconciliationJob>();

			// Register default no-op webhook handlers (can be replaced with provider-specific implementations)
			services.TryAddSingleton<IPaymentWebhookHandler, NoOpPaymentWebhookHandler>();
			services.TryAddSingleton<IWebhookSignatureVerifier, NoOpWebhookSignatureVerifier>();

			return services;
		}

		#endregion
	}
}