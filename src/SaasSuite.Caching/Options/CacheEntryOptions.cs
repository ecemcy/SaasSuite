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

namespace SaasSuite.Caching.Options
{
	/// <summary>
	/// Defines expiration policies for individual cache entries, allowing fine-grained control over cache lifetime.
	/// </summary>
	/// <remarks>
	/// This class provides two expiration strategies that can be used independently or together:
	/// <list type="bullet">
	/// <item><description><see cref="AbsoluteExpiration"/>: Fixed-time expiration regardless of access patterns</description></item>
	/// <item><description><see cref="SlidingExpiration"/>: Activity-based expiration that extends on each access</description></item>
	/// </list>
	/// When both strategies are configured, the cache entry expires at whichever condition is met first.
	/// If neither property is set, the implementation may fall back to a default expiration policy.
	/// </remarks>
	public class CacheEntryOptions
	{
		#region ' Properties '

		/// <summary>
		/// Gets or sets the absolute expiration time relative to the current time.
		/// </summary>
		/// <value>
		/// A <see cref="TimeSpan"/> representing the fixed duration after which the cache entry expires,
		/// or <see langword="null"/> if no absolute expiration is configured.
		/// </value>
		/// <remarks>
		/// When set, the cache entry will be removed after this duration elapses from the time it was stored,
		/// regardless of how frequently it is accessed. This is useful for data that becomes stale after
		/// a known period, such as time-sensitive reports or temporary authorization tokens.
		/// Absolute expiration takes precedence over sliding expiration if the absolute time is reached first.
		/// </remarks>
		public TimeSpan? AbsoluteExpiration { get; set; }

		/// <summary>
		/// Gets or sets the sliding expiration window that resets on each access.
		/// </summary>
		/// <value>
		/// A <see cref="TimeSpan"/> representing the inactivity period after which the cache entry expires,
		/// or <see langword="null"/> if no sliding expiration is configured.
		/// </value>
		/// <remarks>
		/// When set, the cache entry's lifetime is extended by this duration each time it is accessed.
		/// The entry expires only if it remains untouched for the full sliding window duration.
		/// This is ideal for frequently accessed data that should remain cached while active,
		/// such as user session data or frequently queried configuration settings.
		/// If both <see cref="AbsoluteExpiration"/> and <see cref="SlidingExpiration"/> are configured,
		/// the entry expires at whichever condition occurs first.
		/// </remarks>
		public TimeSpan? SlidingExpiration { get; set; }

		#endregion
	}
}