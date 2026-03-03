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

using Microsoft.Extensions.DependencyInjection;

using SaasSuite.Core.Attributes;
using SaasSuite.Discovery.Options;

namespace SaasSuite.Discovery.Interfaces
{
	/// <summary>
	/// Defines the contract for discovering and registering tenant-aware services from assemblies.
	/// </summary>
	/// <remarks>
	/// Service discoverers search assemblies for types marked with service attributes
	/// (such as <see cref="TenantServiceAttribute"/>) and automatically
	/// register them with the dependency injection container using the appropriate
	/// lifetime and tenant scope settings.
	/// </remarks>
	public interface ITenantServiceDiscoverer
	{
		#region ' Methods '

		/// <summary>
		/// Discovers tenant services from assemblies and registers them in the service collection.
		/// </summary>
		/// <param name="services">The <see cref="IServiceCollection"/> to register discovered services into. Cannot be <see langword="null"/>.</param>
		/// <param name="options">
		/// Configuration options that control discovery behavior including assembly selection,
		/// namespace filtering, and registration strategies. Cannot be <see langword="null"/>.
		/// </param>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="services"/> or <paramref name="options"/> is <see langword="null"/>.
		/// </exception>
		/// <remarks>
		/// This method performs the following steps:
		/// <list type="number">
		/// <item><description>Identifies assemblies to search based on <see cref="DiscoveryOptions.Assemblies"/></description></item>
		/// <item><description>Enumerates all public, concrete types in each assembly</description></item>
		/// <item><description>Filters types based on attributes and namespace filters</description></item>
		/// <item><description>Determines service types (interfaces, concrete types) based on options</description></item>
		/// <item><description>Registers services with their specified lifetime and tenant scope</description></item>
		/// </list>
		/// If no assemblies are specified in options, all loaded non-system assemblies are searched.
		/// The discovery process handles reflection exceptions gracefully, logging warnings for
		/// types that cannot be loaded.
		/// </remarks>
		void DiscoverAndRegister(IServiceCollection services, DiscoveryOptions options);

		#endregion
	}
}