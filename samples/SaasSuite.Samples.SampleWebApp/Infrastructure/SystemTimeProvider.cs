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

using SaasSuite.Samples.SampleWebApp.Infrastructure.Interfaces;

namespace SaasSuite.Samples.SampleWebApp.Infrastructure
{
	/// <summary>
	/// Production implementation of <see cref="ITimeProvider"/> that returns the actual system time.
	/// </summary>
	/// <remarks>
	/// This implementation uses <see cref="DateTimeOffset.UtcNow"/> to provide real system time.
	/// For testing scenarios, inject a mock implementation that returns deterministic values.
	/// </remarks>
	public class SystemTimeProvider
		: ITimeProvider
	{
		#region ' Properties '

		/// <inheritdoc/>
		public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;

		#endregion
	}
}