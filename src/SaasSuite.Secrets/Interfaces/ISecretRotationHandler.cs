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

namespace SaasSuite.Secrets.Interfaces
{
	/// <summary>
	/// Defines the contract for handling secret rotation events in a multi-tenant environment.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Implement this interface to receive notifications when secrets are rotated,
	/// allowing you to update dependent systems or invalidate caches.
	/// </para>
	/// <para>
	/// <strong>Thread Safety:</strong> Implementations should be thread-safe as rotation handlers
	/// may be invoked concurrently for different tenants or secrets.
	/// </para>
	/// <para>
	/// <strong>Error Handling:</strong> Implementations should handle exceptions gracefully and
	/// consider implementing retry logic for transient failures. Throwing exceptions may prevent
	/// the secret rotation from completing successfully.
	/// </para>
	/// <para>
	/// <strong>Performance:</strong> Handlers should execute quickly to avoid blocking secret
	/// rotation operations. For long-running operations, consider queuing work for background processing.
	/// </para>
	/// </remarks>
	/// <example>
	/// Example implementation that invalidates a cache:
	/// <code>
	/// public class CacheInvalidationHandler : ISecretRotationHandler
	/// {
	///     private readonly IMemoryCache _cache;
	///     private readonly ILogger&lt;CacheInvalidationHandler&gt; _logger;
	///
	///     public CacheInvalidationHandler(IMemoryCache cache, ILogger&lt;CacheInvalidationHandler&gt; logger)
	///     {
	///         _cache = cache;
	///         _logger = logger;
	///     }
	///
	///     public async Task HandleRotationAsync(SecretRotationEvent rotationEvent, CancellationToken cancellationToken)
	///     {
	///         try
	///         {
	///             // Build cache key using tenant and secret name
	///             var cacheKey = $"{rotationEvent.TenantId.Value}:{rotationEvent.SecretName}";
	///
	///             // Remove the old cached value
	///             _cache.Remove(cacheKey);
	///
	///             _logger.LogInformation(
	///                 "Invalidated cache for secret {SecretName} of tenant {TenantId}",
	///                 rotationEvent.SecretName,
	///                 rotationEvent.TenantId.Value);
	///
	///             await Task.CompletedTask;
	///         }
	///         catch (Exception ex)
	///         {
	///             _logger.LogError(ex, "Failed to invalidate cache for secret rotation");
	///             // Don't rethrow - allow rotation to complete
	///         }
	///     }
	/// }
	/// </code>
	/// </example>
	public interface ISecretRotationHandler
	{
		#region ' Methods '

		/// <summary>
		/// Handles a secret rotation event asynchronously.
		/// </summary>
		/// <param name="rotationEvent">
		/// The <see cref="SecretRotationEvent"/> containing details about the rotated secret,
		/// including the tenant ID, secret name, old and new versions, and rotation timestamp.
		/// </param>
		/// <param name="cancellationToken">
		/// A token to monitor for cancellation requests. Implementations should respect this token
		/// and cancel long-running operations when cancellation is requested.
		/// </param>
		/// <returns>
		/// A <see cref="Task"/> representing the asynchronous operation. The task completes when
		/// the handler has finished processing the rotation event.
		/// </returns>
		/// <remarks>
		/// <para>
		/// This method is called by the secret store implementation after a secret has been successfully
		/// rotated. Multiple handlers may be registered and will be invoked in registration order.
		/// </para>
		/// <para>
		/// <strong>Best Practices:</strong>
		/// <list type="bullet">
		/// <item><description>Keep processing time minimal to avoid blocking rotation operations</description></item>
		/// <item><description>Implement idempotency to handle duplicate events gracefully</description></item>
		/// <item><description>Log all errors for debugging but avoid throwing exceptions</description></item>
		/// <item><description>Use the cancellation token for any async I/O operations</description></item>
		/// </list>
		/// </para>
		/// </remarks>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="rotationEvent"/> is <see langword="null"/>.
		/// </exception>
		/// <exception cref="OperationCanceledException">
		/// Thrown when the operation is cancelled via the <paramref name="cancellationToken"/>.
		/// </exception>
		Task HandleRotationAsync(SecretRotationEvent rotationEvent, CancellationToken cancellationToken = default);

		#endregion
	}
}