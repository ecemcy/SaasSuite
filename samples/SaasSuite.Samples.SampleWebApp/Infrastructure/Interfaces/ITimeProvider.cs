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

namespace SaasSuite.Samples.SampleWebApp.Infrastructure.Interfaces
{
	/// <summary>
	/// Provides an abstraction for retrieving the current date and time to support testability and time-based operations.
	/// </summary>
	/// <remarks>
	/// This abstraction enables unit testing of time-dependent logic by allowing tests to inject custom
	/// time providers that return deterministic values rather than relying on system time.
	/// </remarks>
	public interface ITimeProvider
	{
		#region ' Properties '

		/// <summary>
		/// Gets the current Coordinated Universal Time (UTC) date and time.
		/// </summary>
		/// <value>
		/// A <see cref="DateTimeOffset"/> representing the current UTC date and time.
		/// </value>
		DateTimeOffset UtcNow { get; }

		#endregion
	}
}