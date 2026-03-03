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

using SaasSuite.Core.Interfaces;

namespace SaasSuite.Core
{
	/// <summary>
	/// Holds the tenant identity and optionally resolved tenant metadata for the current execution flow.
	/// This type encapsulates all tenant-specific information required for request processing and is
	/// intended to be created by tenant resolution middleware and accessed downstream through <see cref="ITenantAccessor"/>.
	/// </summary>
	/// <remarks>
	/// This type is intended to be created by tenant resolution middleware and then accessed through
	/// <see cref="ITenantAccessor"/> for downstream components such as services, filters, and controllers.
	/// The <see cref="Properties"/> collection provides an extensible mechanism for attaching additional per-request
	/// tenant-scoped data without modifying the core model, enabling middleware and services to store custom data
	/// that flows throughout the request lifetime.
	/// </remarks>
	public class TenantContext
	{
		#region ' Constructors '

		/// <summary>
		/// Initializes a new instance of the <see cref="TenantContext"/> class with the specified tenant identifier and optional metadata.
		/// Creates an empty property bag for extensibility.
		/// </summary>
		/// <param name="tenantId">The resolved tenant identifier for the current request or execution flow. Must not be <see langword="null"/> or empty.</param>
		/// <param name="tenantInfo">Optional tenant metadata loaded from a store. May be <see langword="null"/> if tenant details are not yet loaded or not required.</param>
		public TenantContext(TenantId tenantId, TenantInfo? tenantInfo = null)
		{
			this.TenantId = tenantId;
			this.TenantInfo = tenantInfo;

			// Initialize the property bag for middleware/services to attach request-scoped tenant data without extending the model.
			this.Properties = new Dictionary<string, object>();
		}

		#endregion

		#region ' Properties '

		/// <summary>
		/// Gets an extensible property bag for attaching additional per-tenant/per-request values.
		/// This collection allows middleware and services to store custom data without modifying the core model.
		/// Values stored here are scoped to the current request and the associated tenant context.
		/// </summary>
		/// <value>
		/// A mutable dictionary of string keys to object values. This dictionary is never <see langword="null"/>
		/// and is initialized as an empty collection during context construction. The dictionary is not thread-safe
		/// for modifications, but is safe within the async-local context of a single request flow.
		/// </value>
		/// <remarks>
		/// Prefer using well-known constant keys to avoid key collisions across different components.
		/// This collection is scoped to the current async context and will not leak between concurrent requests.
		/// Example usage: storing request-specific tenant configuration, feature flags resolved at runtime,
		/// or temporary computation results that need to be shared across middleware and services.
		/// </remarks>
		public IDictionary<string, object> Properties { get; }

		/// <summary>
		/// Gets the resolved tenant identifier for this context.
		/// This value uniquely identifies the tenant associated with the current request or execution flow.
		/// </summary>
		/// <value>
		/// A <see cref="Core.TenantId"/> that uniquely identifies the tenant for this context.
		/// This property is never <see langword="null"/> and is set during context construction.
		/// </value>
		public TenantId TenantId { get; }

		/// <summary>
		/// Gets resolved tenant metadata if loaded from the tenant store.
		/// May be <see langword="null"/> if the tenant information has not been loaded or is not available.
		/// Contains detailed tenant configuration including isolation level, settings, and connection information.
		/// </summary>
		/// <value>
		/// A <see cref="Core.TenantInfo"/> object containing full tenant metadata, or <see langword="null"/> if the tenant
		/// information was not loaded from the store. A <see langword="null"/> value may indicate that only the tenant
		/// ID was resolved without loading full metadata, or that the tenant was not found in the store.
		/// </value>
		public TenantInfo? TenantInfo { get; }

		#endregion

		#region ' Methods '

		/// <summary>
		/// Stores or updates a value in the <see cref="Properties"/> collection under the provided key.
		/// If the key already exists, the value is replaced. If the key is new, it is added to the collection.
		/// </summary>
		/// <param name="key">The property key. Use well-known constant keys to avoid collisions across different components.</param>
		/// <param name="value">The property value to store. Can be any object type and will be stored as-is without cloning.</param>
		/// <remarks>
		/// This method adds or updates entries in the property bag. The operation is not atomic and should only
		/// be called from within the async context of a single request flow. Multiple calls with the same key
		/// will overwrite previous values. Consider using namespaced keys (e.g., "MyFeature.SettingName") to
		/// avoid collisions between different middleware or service components.
		/// </remarks>
		public void SetProperty(string key, object value)
		{
			this.Properties[key] = value;
		}

		/// <summary>
		/// Retrieves a property from the <see cref="Properties"/> collection and safely casts it to the specified type.
		/// Returns the default value for the type if the key does not exist or the value cannot be cast to the target type.
		/// </summary>
		/// <typeparam name="T">The expected value type. The property value will be cast to this type if it exists and is compatible.</typeparam>
		/// <param name="key">The property key to retrieve. Should match a previously set key value.</param>
		/// <returns>
		/// The value if the key exists and the value is of type <typeparamref name="T"/>; otherwise returns <c>default(T)</c>.
		/// For reference types, this will be <see langword="null"/>. For value types, this will be the default value for that type (e.g., 0 for integers, <see langword="false"/> for booleans).
		/// </returns>
		/// <remarks>
		/// This method safely retrieves values without throwing exceptions for unknown keys or type mismatches.
		/// Callers can detect whether a property exists and is of the correct type by comparing the result to <c>default(T)</c>,
		/// though this may be ambiguous if the stored value is actually the default value for the type.
		/// For better clarity in such cases, use <see cref="IDictionary{TKey, TValue}.TryGetValue"/> on the <see cref="Properties"/> collection directly.
		/// </remarks>
		public T? GetProperty<T>(string key)
		{
			// Safely retrieve the value without throwing exceptions for unknown/mismatched types
			// Callers can detect default/null returns to determine if the property exists
			if (this.Properties.TryGetValue(key, out object? value) && value is T typedValue)
			{
				return typedValue;
			}

			return default;
		}

		#endregion
	}
}