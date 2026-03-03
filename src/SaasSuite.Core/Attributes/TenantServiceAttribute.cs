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

using SaasSuite.Core.Enumerations;

namespace SaasSuite.Core.Attributes
{
	/// <summary>
	/// Declares that a service is tenant-aware and provides its desired dependency injection lifetime
	/// and tenant scoping behavior. This attribute serves as metadata for registration and discovery logic.
	/// Can be applied to classes or interfaces representing tenant-scoped services.
	/// </summary>
	/// <remarks>
	/// This attribute is metadata only and requires corresponding registration or assembly discovery logic to have any effect.
	/// The attribute should be used in conjunction with service registration extensions that honor these settings.
	/// It supports inheritance via <see cref="AttributeUsageAttribute.Inherited"/> being <see langword="true"/>,
	/// allowing base classes or interfaces to declare tenant-awareness that derived types automatically inherit.
	/// Multiple instances of this attribute cannot be applied to the same target due to <see cref="AttributeUsageAttribute.AllowMultiple"/> being <see langword="false"/>.
	/// </remarks>
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = false, Inherited = true)]
	public sealed class TenantServiceAttribute
		: Attribute
	{
		#region ' Constructors '

		/// <summary>
		/// Initializes a new instance of the <see cref="TenantServiceAttribute"/> class with the specified lifetime and tenant scope.
		/// If no parameters are provided, defaults to <see cref="ServiceLifetime.Scoped">scoped</see> lifetime with
		/// <see cref="TenantScope.Request">request</see>-level tenant scoping.
		/// </summary>
		/// <param name="lifetime">
		/// The desired <see cref="ServiceLifetime"/> for the service in the DI container.
		/// Defaults to <see cref="ServiceLifetime.Scoped"/> for per-request service instances.
		/// </param>
		/// <param name="tenantScope">
		/// How the service should be scoped with respect to tenants.
		/// Defaults to <see cref="TenantScope.Request"/> for per-request tenant isolation.
		/// </param>
		public TenantServiceAttribute(ServiceLifetime lifetime = ServiceLifetime.Scoped, TenantScope tenantScope = TenantScope.Request)
		{
			this.Lifetime = lifetime;
			this.TenantScope = tenantScope;
		}

		#endregion

		#region ' Properties '

		/// <summary>
		/// Gets the requested dependency injection lifetime for this service.
		/// Determines whether the service is created once per request (<see cref="ServiceLifetime.Scoped"/>),
		/// once per application (<see cref="ServiceLifetime.Singleton"/>),
		/// or per each dependency resolution (<see cref="ServiceLifetime.Transient"/>).
		/// </summary>
		/// <value>
		/// A <see cref="ServiceLifetime"/> enumeration value indicating how the service instance lifecycle should be managed.
		/// This value is set during attribute construction and cannot be changed afterward.
		/// </value>
		public ServiceLifetime Lifetime { get; }

		/// <summary>
		/// Gets the desired tenant scoping behavior for this service.
		/// Determines how service instances are associated with and isolated across tenants.
		/// Options include <see cref="TenantScope.Global"/> (shared across tenants),
		/// <see cref="TenantScope.Request"/> (per-request isolation),
		/// or <see cref="TenantScope.SingletonPerTenant"/> (one instance per tenant).
		/// </summary>
		/// <value>
		/// A <see cref="Enumerations.TenantScope"/> enumeration value specifying the tenant isolation strategy for this service.
		/// This value is set during attribute construction and cannot be changed afterward.
		/// </value>
		public TenantScope TenantScope { get; }

		#endregion
	}
}