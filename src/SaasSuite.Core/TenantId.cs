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

namespace SaasSuite.Core
{
	/// <summary>
	/// Strongly-typed tenant identifier that wraps a string value.
	/// Provides type safety to prevent accidental mixing of tenant identifiers with other string IDs in the application.
	/// This struct implements value equality semantics and supports implicit conversions to and from string.
	/// </summary>
	/// <remarks>
	/// Using a dedicated type helps prevent accidental mixing of tenant identifiers with other string IDs
	/// and provides compile-time type safety. The struct is immutable and lightweight (single string field),
	/// making it suitable for frequent use throughout the application without performance concerns.
	/// The implicit conversion operators allow seamless interoperability with string-based APIs while
	/// maintaining type safety at the application boundaries.
	/// </remarks>
	public readonly struct TenantId
		: IEquatable<TenantId>
	{
		#region ' Constructors '

		/// <summary>
		/// Initializes a new instance of the <see cref="TenantId"/> struct with the specified value.
		/// Validates that the value is not <see langword="null"/> or whitespace.
		/// </summary>
		/// <param name="value">The raw tenant identifier value. Must not be <see langword="null"/>, empty, or whitespace.</param>
		/// <exception cref="ArgumentException">Thrown when <paramref name="value"/> is <see langword="null"/>, empty, or contains only whitespace characters.</exception>
		public TenantId(string value)
		{
			if (string.IsNullOrWhiteSpace(value))
			{
				throw new ArgumentException("Tenant identifier cannot be null or whitespace.", nameof(value));
			}

			this.Value = value;
		}

		#endregion

		#region ' Properties '

		/// <summary>
		/// Gets the raw string identifier value.
		/// This is the actual tenant identifier as stored in the system.
		/// </summary>
		/// <value>
		/// The string representation of the tenant identifier. This value is never <see langword="null"/> or whitespace
		/// due to validation in the constructor. The value is immutable after construction.
		/// </value>
		public string Value { get; }

		#endregion

		#region ' Conversions '

		/// <summary>
		/// Enables implicit conversion from <see cref="TenantId"/> to <see cref="string"/>.
		/// Allows <see cref="TenantId"/> instances to be automatically converted to their string representation.
		/// </summary>
		/// <param name="tenantId">The <see cref="TenantId"/> to convert.</param>
		/// <returns>The string value of the tenant identifier, which is never <see langword="null"/> or whitespace.</returns>
		public static implicit operator string(TenantId tenantId)
		{
			return tenantId.Value;
		}

		/// <summary>
		/// Enables implicit conversion from <see cref="string"/> to <see cref="TenantId"/>.
		/// Allows string values to be automatically converted to <see cref="TenantId"/> instances where expected.
		/// </summary>
		/// <param name="value">The string value to convert. Must not be <see langword="null"/>, empty, or whitespace.</param>
		/// <returns>A new <see cref="TenantId"/> instance wrapping the provided string value.</returns>
		/// <exception cref="ArgumentException">Thrown when <paramref name="value"/> is <see langword="null"/>, empty, or contains only whitespace characters.</exception>
		public static implicit operator TenantId(string value)
		{
			return new TenantId(value);
		}

		#endregion

		#region ' Operators '

		/// <summary>
		/// Compares two <see cref="TenantId"/> values for equality using value semantics.
		/// </summary>
		/// <param name="left">The first <see cref="TenantId"/> to compare.</param>
		/// <param name="right">The second <see cref="TenantId"/> to compare.</param>
		/// <returns><see langword="true"/> if both <see cref="TenantId"/> instances have the same string value; otherwise <see langword="false"/>.</returns>
		public static bool operator ==(TenantId left, TenantId right)
		{
			return left.Equals(right);
		}

		/// <summary>
		/// Compares two <see cref="TenantId"/> values for inequality using value semantics.
		/// </summary>
		/// <param name="left">The first <see cref="TenantId"/> to compare.</param>
		/// <param name="right">The second <see cref="TenantId"/> to compare.</param>
		/// <returns><see langword="true"/> if the <see cref="TenantId"/> instances have different string values; otherwise <see langword="false"/>.</returns>
		public static bool operator !=(TenantId left, TenantId right)
		{
			return !left.Equals(right);
		}

		#endregion

		#region ' Methods '

		/// <summary>
		/// Determines whether the current <see cref="TenantId"/> equals another <see cref="TenantId"/> instance.
		/// Implements value-based equality comparison using the underlying string value.
		/// </summary>
		/// <param name="other">The <see cref="TenantId"/> to compare with the current instance.</param>
		/// <returns><see langword="true"/> if the values are equal; otherwise <see langword="false"/>.</returns>
		public bool Equals(TenantId other)
		{
			return this.Value == other.Value;
		}

		#endregion

		#region ' Override Methods '

		/// <summary>
		/// Determines whether the specified object is equal to the current <see cref="TenantId"/>.
		/// Supports comparison with both <see cref="TenantId"/> instances and boxed <see cref="TenantId"/> values.
		/// </summary>
		/// <param name="obj">The object to compare with the current instance. Can be <see langword="null"/>.</param>
		/// <returns><see langword="true"/> if the specified object is a <see cref="TenantId"/> with the same string value; otherwise <see langword="false"/>.</returns>
		public override bool Equals(object? obj)
		{
			return obj is TenantId other && this.Equals(other);
		}

		/// <summary>
		/// Returns the hash code for this <see cref="TenantId"/> instance.
		/// The hash code is based on the underlying string value to ensure consistency with equality comparisons.
		/// </summary>
		/// <returns>A hash code for the current <see cref="TenantId"/>, or 0 if the value is <see langword="null"/> (which should never occur due to constructor validation).</returns>
		public override int GetHashCode()
		{
			return this.Value?.GetHashCode() ?? 0;
		}

		/// <summary>
		/// Returns the string representation of this <see cref="TenantId"/>.
		/// </summary>
		/// <returns>The raw tenant identifier string value, which is never <see langword="null"/> or whitespace.</returns>
		public override string ToString()
		{
			return this.Value;
		}

		#endregion

		#region ' Static Methods '

		/// <summary>
		/// Creates a <see cref="TenantId"/> instance from a raw string value.
		/// This factory method provides an alternative to using the constructor directly.
		/// </summary>
		/// <param name="value">The raw tenant identifier string. Must not be <see langword="null"/>, empty, or whitespace.</param>
		/// <returns>A new <see cref="TenantId"/> instance wrapping the provided value.</returns>
		/// <exception cref="ArgumentException">Thrown when <paramref name="value"/> is <see langword="null"/>, empty, or contains only whitespace characters.</exception>
		public static TenantId From(string value)
		{
			return new TenantId(value);
		}

		#endregion
	}
}