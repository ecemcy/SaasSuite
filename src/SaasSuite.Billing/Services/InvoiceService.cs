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

using SaasSuite.Billing.Enumerations;
using SaasSuite.Core;

namespace SaasSuite.Billing.Services
{
	/// <summary>
	/// Service for managing invoice persistence and retrieval in a multi-tenant SaaS application.
	/// </summary>
	/// <remarks>
	/// This service provides CRUD operations for invoices and query methods for filtering and aggregation.
	/// This is an in-memory implementation suitable for development and testing; production systems should use a persistent data store.
	/// </remarks>
	public class InvoiceService
	{
		#region ' Fields '

		/// <summary>
		/// Synchronization lock for thread-safe access to the invoice dictionary.
		/// </summary>
		private readonly object _lock = new object();

		/// <summary>
		/// In-memory dictionary storing invoices by their unique identifier.
		/// </summary>
		private readonly Dictionary<string, Invoice> _invoices = new Dictionary<string, Invoice>();

		#endregion

		#region ' Methods '

		/// <summary>
		/// Creates a new invoice in the system.
		/// </summary>
		/// <param name="invoice">The invoice to create. Must not be <see langword="null"/> and must have a unique identifier.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests during the asynchronous operation.</param>
		/// <returns>A task that represents the asynchronous operation.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="invoice"/> is <see langword="null"/>.</exception>
		/// <exception cref="InvalidOperationException">Thrown when an invoice with the same identifier already exists.</exception>
		/// <remarks>
		/// The invoice must have a unique identifier that doesn't already exist.
		/// </remarks>
		public Task CreateAsync(Invoice invoice, CancellationToken cancellationToken = default)
		{
			ArgumentNullException.ThrowIfNull(invoice);

			lock (this._lock)
			{
				if (this._invoices.ContainsKey(invoice.InvoiceId))
				{
					throw new InvalidOperationException($"Invoice {invoice.InvoiceId} already exists");
				}

				this._invoices[invoice.InvoiceId] = invoice;
			}

			return Task.CompletedTask;
		}

		/// <summary>
		/// Updates an existing invoice in the system.
		/// </summary>
		/// <param name="invoice">The invoice to update with modified values. Must not be <see langword="null"/>.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests during the asynchronous operation.</param>
		/// <returns>A task that represents the asynchronous operation.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="invoice"/> is <see langword="null"/>.</exception>
		/// <exception cref="InvalidOperationException">Thrown when the invoice does not exist in the system.</exception>
		/// <remarks>
		/// The invoice must already exist, and the <see cref="Invoice.UpdatedAt"/> timestamp is automatically set to the current UTC time.
		/// </remarks>
		public Task UpdateAsync(Invoice invoice, CancellationToken cancellationToken = default)
		{
			ArgumentNullException.ThrowIfNull(invoice);

			lock (this._lock)
			{
				if (!this._invoices.ContainsKey(invoice.InvoiceId))
				{
					throw new InvalidOperationException($"Invoice {invoice.InvoiceId} not found");
				}

				// Automatically update the timestamp
				invoice.UpdatedAt = DateTimeOffset.UtcNow;
				this._invoices[invoice.InvoiceId] = invoice;
			}

			return Task.CompletedTask;
		}

		/// <summary>
		/// Deletes an invoice from the system by its unique identifier.
		/// </summary>
		/// <param name="invoiceId">The unique identifier of the invoice to delete. Cannot be <see langword="null"/> or whitespace.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests during the asynchronous operation.</param>
		/// <returns>
		/// A task that represents the asynchronous operation.
		/// The task result contains <see langword="true"/> if the invoice was found and deleted;
		/// otherwise, <see langword="false"/> if the invoice did not exist.
		/// </returns>
		/// <exception cref="ArgumentException">Thrown when <paramref name="invoiceId"/> is <see langword="null"/> or whitespace.</exception>
		public Task<bool> DeleteAsync(string invoiceId, CancellationToken cancellationToken = default)
		{
			if (string.IsNullOrWhiteSpace(invoiceId))
			{
				throw new ArgumentException("Invoice ID cannot be null or empty", nameof(invoiceId));
			}

			lock (this._lock)
			{
				return Task.FromResult(this._invoices.Remove(invoiceId));
			}
		}

		/// <summary>
		/// Calculates financial totals across all invoices for a specific tenant.
		/// </summary>
		/// <param name="tenantId">The unique identifier of the tenant to calculate totals for.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests during the asynchronous operation.</param>
		/// <returns>
		/// A task that represents the asynchronous operation.
		/// The task result contains a tuple with:
		/// <list type="bullet">
		/// <item><description><c>TotalAmount</c>: Sum of all invoice amounts</description></item>
		/// <item><description><c>AmountPaid</c>: Sum of all payments received</description></item>
		/// <item><description><c>AmountDue</c>: Sum of all outstanding balances</description></item>
		/// </list>
		/// </returns>
		/// <remarks>
		/// This aggregates the total invoice amounts, amounts paid, and outstanding balances.
		/// </remarks>
		public Task<(decimal TotalAmount, decimal AmountPaid, decimal AmountDue)> GetTenantTotalsAsync(TenantId tenantId, CancellationToken cancellationToken = default)
		{
			lock (this._lock)
			{
				List<Invoice> tenantInvoices = this._invoices.Values
					.Where(i => i.TenantId == tenantId)
					.ToList();

				decimal totalAmount = tenantInvoices.Sum(i => i.Amount);
				decimal amountPaid = tenantInvoices.Sum(i => i.AmountPaid);
				decimal amountDue = tenantInvoices.Sum(i => i.AmountDue);

				return Task.FromResult((totalAmount, amountPaid, amountDue));
			}
		}

		/// <summary>
		/// Retrieves an invoice by its unique identifier.
		/// </summary>
		/// <param name="invoiceId">The unique identifier of the invoice to retrieve. Cannot be <see langword="null"/> or whitespace.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests during the asynchronous operation.</param>
		/// <returns>
		/// A task that represents the asynchronous operation.
		/// The task result contains the <see cref="Invoice"/> if found; otherwise, <see langword="null"/>.
		/// </returns>
		/// <exception cref="ArgumentException">Thrown when <paramref name="invoiceId"/> is <see langword="null"/> or whitespace.</exception>
		public Task<Invoice?> GetByIdAsync(string invoiceId, CancellationToken cancellationToken = default)
		{
			if (string.IsNullOrWhiteSpace(invoiceId))
			{
				throw new ArgumentException("Invoice ID cannot be null or empty", nameof(invoiceId));
			}

			lock (this._lock)
			{
				_ = this._invoices.TryGetValue(invoiceId, out Invoice? invoice);
				return Task.FromResult(invoice);
			}
		}

		/// <summary>
		/// Retrieves all invoices in the system.
		/// </summary>
		/// <param name="cancellationToken">A token to monitor for cancellation requests during the asynchronous operation.</param>
		/// <returns>
		/// A task that represents the asynchronous operation.
		/// The task result contains a list of all invoices, ordered by newest first.
		/// </returns>
		/// <remarks>
		/// Results are ordered by creation date descending (newest first).
		/// </remarks>
		public Task<List<Invoice>> GetAllAsync(CancellationToken cancellationToken = default)
		{
			lock (this._lock)
			{
				List<Invoice> invoices = this._invoices.Values
					.OrderByDescending(i => i.CreatedAt)
					.ToList();

				return Task.FromResult(invoices);
			}
		}

		/// <summary>
		/// Retrieves invoices filtered by a specific status.
		/// </summary>
		/// <param name="status">The invoice status to filter by.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests during the asynchronous operation.</param>
		/// <returns>
		/// A task that represents the asynchronous operation.
		/// The task result contains a list of invoices with the specified status, ordered by newest first.
		/// </returns>
		/// <remarks>
		/// Results are ordered by creation date descending (newest first).
		/// </remarks>
		public Task<List<Invoice>> GetByStatusAsync(InvoiceStatus status, CancellationToken cancellationToken = default)
		{
			lock (this._lock)
			{
				List<Invoice> invoices = this._invoices.Values
					.Where(i => i.Status == status)
					.OrderByDescending(i => i.CreatedAt)
					.ToList();

				return Task.FromResult(invoices);
			}
		}

		/// <summary>
		/// Retrieves all invoices for a specific tenant.
		/// </summary>
		/// <param name="tenantId">The unique identifier of the tenant whose invoices to retrieve.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests during the asynchronous operation.</param>
		/// <returns>
		/// A task that represents the asynchronous operation.
		/// The task result contains a list of invoices belonging to the specified tenant, ordered by newest first.
		/// </returns>
		/// <remarks>
		/// Results are ordered by creation date descending (newest first).
		/// </remarks>
		public Task<List<Invoice>> GetByTenantIdAsync(TenantId tenantId, CancellationToken cancellationToken = default)
		{
			lock (this._lock)
			{
				List<Invoice> invoices = this._invoices.Values
					.Where(i => i.TenantId == tenantId)
					.OrderByDescending(i => i.CreatedAt)
					.ToList();

				return Task.FromResult(invoices);
			}
		}

		/// <summary>
		/// Retrieves all overdue invoices.
		/// </summary>
		/// <param name="cancellationToken">A token to monitor for cancellation requests during the asynchronous operation.</param>
		/// <returns>
		/// A task that represents the asynchronous operation.
		/// The task result contains a list of overdue invoices, ordered by due date with oldest first.
		/// </returns>
		/// <remarks>
		/// Overdue invoices are pending invoices with due dates in the past.
		/// Results are ordered by due date ascending (oldest overdue first).
		/// </remarks>
		public Task<List<Invoice>> GetOverdueInvoicesAsync(CancellationToken cancellationToken = default)
		{
			DateTimeOffset now = DateTimeOffset.UtcNow;

			lock (this._lock)
			{
				List<Invoice> invoices = this._invoices.Values
					.Where(i => i.Status == InvoiceStatus.Pending && i.DueDate < now)
					.OrderBy(i => i.DueDate) // Oldest overdue invoices first
					.ToList();

				return Task.FromResult(invoices);
			}
		}

		#endregion
	}
}