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

namespace SaasSuite.Samples.SampleWebApp.Infrastructure.Enumerations
{
	/// <summary>
	/// Defines tenant-scoped roles for hierarchical authorization within a multi-tenant application.
	/// </summary>
	/// <remarks>
	/// <para>Roles follow a hierarchical permission model where higher roles inherit lower role permissions:</para>
	/// <list type="bullet">
	/// <item><description><see cref="TenantOwner"/> has all permissions including billing management</description></item>
	/// <item><description><see cref="TenantAdmin"/> can manage users and settings but not billing</description></item>
	/// <item><description><see cref="TenantMember"/> has basic read/write access to tenant resources</description></item>
	/// </list>
	/// </remarks>
	public enum TenantRole
	{
		/// <summary>
		/// Tenant owner with full administrative access including billing, subscription management, and user administration.
		/// </summary>
		/// <remarks>
		/// This is the highest privilege level within a tenant. Typically assigned to the account creator or primary decision maker.
		/// </remarks>
		TenantOwner = 0,

		/// <summary>
		/// Tenant administrator with user management and settings access, but no billing or subscription modification permissions.
		/// </summary>
		/// <remarks>
		/// Admins can invite users, modify tenant settings, and manage day-to-day operations without financial control.
		/// </remarks>
		TenantAdmin = 1,

		/// <summary>
		/// Regular tenant member with standard read and write access to tenant resources.
		/// </summary>
		/// <remarks>
		/// This is the default role for most users. Members have access to core features but cannot manage other users or settings.
		/// </remarks>
		TenantMember = 2
	}
}