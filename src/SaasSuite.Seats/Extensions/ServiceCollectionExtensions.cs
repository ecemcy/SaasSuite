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

using SaasSuite.Seats.Interfaces;
using SaasSuite.Seats.Options;
using SaasSuite.Seats.Services;

namespace Microsoft.Extensions.DependencyInjection
{
	/// <summary>
	/// Provides extension methods for registering SaasSuite seat management services in the dependency injection container.
	/// These extensions simplify the configuration of seat allocation and enforcement capabilities in multi-tenant ASP.NET Core applications.
	/// </summary>
	/// <remarks>
	/// This static class extends <see cref="IServiceCollection"/> to enable fluent registration of seat management services
	/// using method chaining. The extensions are defined in the <see cref="Microsoft.Extensions.DependencyInjection"/>
	/// namespace to make them automatically available when the DI namespace is imported, following the .NET convention
	/// for extension method discoverability in ASP.NET Core applications.
	/// </remarks>
	public static class ServiceCollectionExtensions
	{
		#region ' Static Methods '

		/// <summary>
		/// Adds SaasSuite seat management services to the dependency injection service collection.
		/// Registers the default in-memory implementation of <see cref="ISeatService"/> and optionally
		/// configures <see cref="SeatEnforcerOptions"/> for middleware behavior customization.
		/// </summary>
		/// <param name="services">The service collection to add the seat management services to. Cannot be <see langword="null"/>.</param>
		/// <param name="configureOptions">
		/// Optional action delegate to configure <see cref="SeatEnforcerOptions"/>. If <see langword="null"/>,
		/// default options are used. This delegate receives a default-initialized options object that can be modified
		/// to customize enforcement behavior, messages, and user identification strategies.
		/// </param>
		/// <returns>The same <paramref name="services"/> instance for method chaining, enabling fluent configuration.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is <see langword="null"/>.</exception>
		/// <remarks>
		/// This method registers the following services:
		/// <list type="bullet">
		/// <item>
		/// <description>
		/// <see cref="ISeatService"/> as <see cref="SeatService"/> with <see cref="ServiceLifetime.Singleton"/> lifetime.
		/// The singleton lifetime is appropriate because the service uses thread-safe in-memory storage and maintains
		/// application-wide seat allocation state. A single instance is shared across all requests for efficiency and consistency.
		/// </description>
		/// </item>
		/// <item>
		/// <description>
		/// <see cref="SeatEnforcerOptions"/> (if <paramref name="configureOptions"/> is provided) configured via the
		/// options pattern to control middleware behavior. Options include enablement flags, error messages, and
		/// user identification configuration.
		/// </description>
		/// </item>
		/// </list>
		/// <para>
		/// The default <see cref="SeatService"/> implementation stores seat allocations in memory using concurrent
		/// collections and per-tenant locking. This is suitable for:
		/// <list type="bullet">
		/// <item><description>Development and testing environments</description></item>
		/// <item><description>Single-instance production deployments where persistence across restarts is not required</description></item>
		/// <item><description>Scenarios where seat allocations can be reconfigured on application startup</description></item>
		/// <item><description>Applications with moderate seat management requirements</description></item>
		/// </list>
		/// </para>
		/// <para>
		/// For production multi-instance deployments or when seat allocations must persist across restarts,
		/// consider registering a custom implementation that uses:
		/// <list type="bullet">
		/// <item><description>Database storage (SQL Server, PostgreSQL) for durability and audit trails</description></item>
		/// <item><description>Distributed cache (Redis, Memcached) for performance and cross-instance consistency</description></item>
		/// <item><description>Distributed locking (Redis locks, database locks) for atomic operations</description></item>
		/// <item><description>Message queues or event buses for seat state synchronization</description></item>
		/// </list>
		/// To replace the default implementation, register your custom implementation after calling this method,
		/// or register it before calling this method to prevent the default registration.
		/// </para>
		/// <para>
		/// Configuration example with options:
		/// <code>
		/// services.AddSaasSeats(options =>
		/// {
		///     options.EnableEnforcement = true;
		///     options.SeatLimitMessage = "Your organization has reached its user limit.";
		///     options.UserIdClaimType = "sub";
		///     options.UserIdHeaderName = "X-User-Id";
		/// });
		/// </code>
		/// </para>
		/// <para>
		/// After registration, the seat service can be injected into controllers, services, and middleware
		/// using standard dependency injection:
		/// <code>
		/// public class UserController : Controller
		/// {
		///     private readonly ISeatService _seatService;
		///
		///     public UserController(ISeatService seatService)
		///     {
		///         _seatService = seatService;
		///     }
		/// }
		/// </code>
		/// </para>
		/// <para>
		/// To enable automatic seat enforcement in the request pipeline, also call:
		/// <code>
		/// app.UseSeatEnforcer();
		/// </code>
		/// in the application's Configure method after authentication and tenant resolution middleware.
		/// </para>
		/// </remarks>
		public static IServiceCollection AddSaasSeats(this IServiceCollection services, Action<SeatEnforcerOptions>? configureOptions = null)
		{
			// Validate that services collection is not null
			ArgumentNullException.ThrowIfNull(services);

			// Register the seat service as a singleton
			// Using singleton lifetime because:
			// 1. The service uses thread-safe concurrent collections
			// 2. Seat allocations are application-wide state that should be shared
			// 3. A single instance is more efficient than creating one per request
			// 4. The in-memory implementation maintains state across requests
			_ = services.AddSingleton<ISeatService, SeatService>();

			// If configuration action is provided, register the options
			if (configureOptions != null)
			{
				// Use the standard .NET options pattern to configure SeatEnforcerOptions
				// This allows the options to be injected as IOptions<SeatEnforcerOptions> in middleware
				_ = services.Configure(configureOptions);
			}

			// Return services for method chaining
			return services;
		}

		#endregion
	}
}