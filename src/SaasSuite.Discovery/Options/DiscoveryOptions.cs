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

using System.Reflection;

namespace SaasSuite.Discovery.Options
{
	/// <summary>
	/// Configuration options for controlling tenant service discovery behavior.
	/// </summary>
	/// <remarks>
	/// These options determine which assemblies are searched, which types are discovered,
	/// and how discovered services are registered. Options can be configured programmatically
	/// or loaded from configuration sources.
	/// </remarks>
	public class DiscoveryOptions
	{
		#region ' Properties '

		/// <summary>
		/// Gets or sets a value indicating whether to register services as their concrete types.
		/// </summary>
		/// <value>
		/// <see langword="true"/> to register services as themselves (concrete types);
		/// otherwise, <see langword="false"/>. Defaults to <see langword="true"/>.
		/// </value>
		/// <remarks>
		/// Concrete type registration allows direct resolution of implementation types,
		/// which is useful when:
		/// <list type="bullet">
		/// <item><description>Services don't implement specific interfaces</description></item>
		/// <item><description>You need to resolve specific implementations</description></item>
		/// <item><description>Decorators or factories need access to concrete types</description></item>
		/// </list>
		/// When both <see cref="RegisterInterfaces"/> and this property are <see langword="true"/>,
		/// services are registered both by their interfaces and concrete types, allowing resolution
		/// through either mechanism. If both are <see langword="false"/>, no services are registered.
		/// </remarks>
		public bool RegisterConcreteTypes { get; set; } = true;

		/// <summary>
		/// Gets or sets a value indicating whether to register services with their implemented interfaces.
		/// </summary>
		/// <value>
		/// <see langword="true"/> to register services for all their public interfaces (excluding generic definitions);
		/// otherwise, <see langword="false"/>. Defaults to <see langword="true"/>.
		/// </value>
		/// <remarks>
		/// When enabled, a service implementing multiple interfaces is registered for each interface,
		/// allowing resolution through any of its contracts. For example, a class implementing
		/// <c>IRepository</c>, <c>IQueryable</c>, and <c>IDisposable</c> would be registered three times.
		/// <para>
		/// Disable this if you only want concrete type registration or have custom interface
		/// selection logic. This can be combined with <see cref="RegisterConcreteTypes"/> to control
		/// registration strategy precisely.
		/// </para>
		/// </remarks>
		public bool RegisterInterfaces { get; set; } = true;

		/// <summary>
		/// Gets or sets namespace filters for limiting type discovery to specific namespaces.
		/// </summary>
		/// <value>
		/// A collection of namespace prefixes. Only types in these namespaces (or sub-namespaces)
		/// are discovered. If empty, all namespaces are included. Defaults to an empty collection.
		/// Cannot be <see langword="null"/>.
		/// </value>
		/// <remarks>
		/// Namespace filtering uses prefix matching, so "MyApp.Services" matches both
		/// "MyApp.Services" and "MyApp.Services.Implementation". This is useful for:
		/// <list type="bullet">
		/// <item><description>Limiting discovery to specific application layers (e.g., "MyApp.Application")</description></item>
		/// <item><description>Excluding test or example code from production builds</description></item>
		/// <item><description>Organizing services by feature or module</description></item>
		/// </list>
		/// When combined with assembly filters, both constraints must be satisfied for a type to be discovered.
		/// </remarks>
		public ICollection<string> NamespaceFilters { get; set; } = new List<string>();

		/// <summary>
		/// Gets or sets the assemblies to search for tenant services.
		/// </summary>
		/// <value>
		/// A collection of <see cref="Assembly"/> instances to search. If empty, all loaded assemblies
		/// (excluding system assemblies) are searched. Defaults to an empty collection.
		/// Cannot be <see langword="null"/>.
		/// </value>
		/// <remarks>
		/// Explicitly specifying assemblies improves performance by avoiding unnecessary searching
		/// of unrelated assemblies. Add assemblies that contain services to be discovered:
		/// <list type="bullet">
		/// <item><description>Application domain/business logic assemblies</description></item>
		/// <item><description>Infrastructure layer assemblies</description></item>
		/// <item><description>Integration/adapter assemblies</description></item>
		/// </list>
		/// System assemblies (System.*, Microsoft.*, mscorlib, netstandard) are automatically
		/// excluded even when not explicitly filtering.
		/// </remarks>
		public ICollection<Assembly> Assemblies { get; set; } = new List<Assembly>();

		#endregion
	}
}