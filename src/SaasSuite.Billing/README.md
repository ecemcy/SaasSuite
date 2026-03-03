# SaasSuite.Billing

[![NuGet](https://img.shields.io/nuget/v/SaasSuite.Billing.svg)](https://www.nuget.org/packages/SaasSuite.Billing)
[![License: Apache-2.0](https://img.shields.io/badge/License-Apache%202.0-blue.svg)](LICENSE)
[![Platform](https://img.shields.io/badge/.NET-6%2B-purple.svg)](https://dotnet.microsoft.com/)

Comprehensive billing orchestration for multi-tenant SaaS applications with support for subscriptions, usage-based pricing, invoicing, and payment processing.

## Overview

`SaasSuite.Billing` provides a complete billing engine that orchestrates subscription charges, usage-based fees, invoice generation, payment processing, and reconciliation. It integrates seamlessly with `SaasSuite.Subscriptions` and `SaasSuite.Metering` to provide flexible pricing models.

## Features

- **Invoice Generation**: Create invoices combining subscription and usage charges
- **Usage-Based Billing**: Integrate metered usage (API calls, storage, bandwidth, compute)
- **Prorated Charges**: Automatic proration for mid-cycle subscription changes
- **Tax & Discounts**: Apply tax rates and discount codes to invoices
- **Payment Processing**: Track payments and update invoice status
- **Reconciliation**: Automatic overdue detection and status updates
- **Refund Management**: Full and partial refunds for paid invoices
- **Webhook Handling**: Process payment provider webhook notifications
- **Financial Reporting**: Generate tenant billing summaries

## Installation

```bash
dotnet add package SaasSuite.Billing
dotnet add package SaasSuite.Metering
dotnet add package SaasSuite.Subscriptions
```

## Quick Start

### 1. Register Services

```csharp
builder.Services.AddSaasBilling(options =>
{
    options.DefaultCurrency = "USD";
    options.DefaultPaymentTermsDays = 30;
    options.DefaultTaxRate = 0.08m; // 8% tax
    options.EnableAutoReconciliation = true;
});
```

### 2. Generate an Invoice

```csharp
using SaasSuite.Billing.Interfaces;
using SaasSuite.Core;

public class BillingController : ControllerBase
{
    private readonly IBillingOrchestrator _billingOrchestrator;
    
    public BillingController(IBillingOrchestrator billingOrchestrator)
    {
        _billingOrchestrator = billingOrchestrator;
    }
    
    [HttpPost("generate-invoice")]
    public async Task<IActionResult> GenerateInvoice(string tenantId)
    {
        var invoice = await _billingOrchestrator.GenerateInvoiceAsync(
            new TenantId(tenantId),
            new BillingCycle
            {
                StartDate = DateTime.UtcNow.AddMonths(-1),
                EndDate = DateTime.UtcNow
            });
        
        return Ok(invoice);
    }
}
```

## Core Concepts

### Invoice Lifecycle

```csharp
Draft → Pending → Paid
             ↓
          Overdue → Refunded
             ↓
         Cancelled
```

### Invoice Structure

```csharp
public class Invoice
{
    public string InvoiceId { get; set; }
    public TenantId TenantId { get; set; }
    public InvoiceStatus Status { get; set; }
    public List<InvoiceLineItem> LineItems { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal AmountDue { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime? PaidDate { get; set; }
}
```

### Line Items

```csharp
public class InvoiceLineItem
{
    public string Description { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Amount { get; set; }
    public string MetricName { get; set; } // For usage-based items
}
```

## Usage Examples

### Usage-Based Billing

```csharp
// Billing automatically includes metered usage
var invoice = await _billingOrchestrator.GenerateInvoiceAsync(
    tenantId,
    billingCycle);

// Invoice contains:
// - Base subscription charge
// - Usage charges (API calls @ $0.001 each)
// - Storage charges (GB-months @ $0.10/GB)
// - Bandwidth charges (GB transferred @ $0.12/GB)
```

### Apply Discount

```csharp
await _billingOrchestrator.ApplyDiscountAsync(
    invoiceId,
    amount: 50.00m,
    reason: "New customer promotion");
```

### Apply Tax

```csharp
await _billingOrchestrator.ApplyTaxAsync(
    invoiceId,
    taxAmount: 24.50m,
    taxRate: 0.08m);
```

### Process Payment

```csharp
await _billingOrchestrator.ProcessPaymentAsync(
    invoiceId,
    amount: 274.50m,
    paymentMethod: "credit_card",
    transactionId: "ch_1234567890");
```

### Finalize Invoice

```csharp
// Move invoice from Draft to Pending
await _billingOrchestrator.FinalizeInvoiceAsync(invoiceId);
```

### Issue Refund

```csharp
// Full refund
await _billingOrchestrator.RefundInvoiceAsync(
    invoiceId,
    amount: null, // null = full refund
    reason: "Duplicate charge");

// Partial refund
await _billingOrchestrator.RefundInvoiceAsync(
    invoiceId,
    amount: 50.00m,
    reason: "Service credit");
```

## Reconciliation

Automatic reconciliation runs periodically to mark overdue invoices:

```csharp
// Manual reconciliation trigger
await _billingOrchestrator.ReconcileAsync(
    tenantId: null, // null = all tenants
    asOfDate: DateTime.UtcNow);
```

## Financial Reporting

```csharp
using SaasSuite.Billing.Interfaces;
using SaasSuite.Core;

public class ReportingService
{
    private readonly IInvoiceService _invoiceService;
    
    public async Task<TenantBillingSummary> GetBillingSummaryAsync(
        TenantId tenantId)
    {
        var invoices = await _invoiceService.GetByTenantAsync(tenantId);
        
        return new TenantBillingSummary
        {
            TotalAmount = invoices.Sum(i => i.TotalAmount),
            PaidAmount = invoices
                .Where(i => i.Status == InvoiceStatus.Paid)
                .Sum(i => i.AmountDue),
            OutstandingAmount = invoices
                .Where(i => i.Status == InvoiceStatus.Pending)
                .Sum(i => i.AmountDue),
            OverdueAmount = invoices
                .Where(i => i.Status == InvoiceStatus.Overdue)
                .Sum(i => i.AmountDue)
        };
    }
}
```

## Webhook Integration

Process payment provider webhooks:

```csharp
using SaasSuite.Billing.Interfaces;

[HttpPost("webhooks/stripe")]
public async Task<IActionResult> StripeWebhook(
    [FromHeader(Name = "Stripe-Signature")] string signature)
{
    var payload = await new StreamReader(Request.Body).ReadToEndAsync();
    
    // Verify signature
    var verifier = HttpContext.RequestServices
        .GetRequiredService<IWebhookSignatureVerifier>();
    
    if (!await verifier.VerifyAsync(payload, signature, "stripe"))
        return Unauthorized();
    
    // Handle webhook
    var handler = HttpContext.RequestServices
        .GetRequiredService<IPaymentWebhookHandler>();
    
    var result = await handler.HandleWebhookAsync(
        provider: "stripe",
        eventType: "payment.succeeded",
        payload: payload);
    
    return result.Success ? Ok() : BadRequest(result.ErrorMessage);
}
```

## Configuration Options

```csharp
public class BillingOptions
{
    public string DefaultCurrency { get; set; } = "USD";
    public int DefaultPaymentTermsDays { get; set; } = 30;
    public decimal DefaultTaxRate { get; set; } = 0.0m;
    public bool EnableAutoReconciliation { get; set; } = true;
    public int ReconciliationIntervalHours { get; set; } = 24;
    public int OverdueReminderDays { get; set; } = 7;
}
```

## Integration with Other Packages

- **SaasSuite.Subscriptions**: Automatically bills subscription charges
- **SaasSuite.Metering**: Includes metered usage in invoices
- **SaasSuite.Core**: Tenant-aware billing operations

## Storage

The package uses `IInvoiceService` for persistence. The default in-memory implementation is suitable for development. For production, implement a custom invoice store backed by your database.

## Related Packages

- **[SaasSuite.Subscriptions](../SaasSuite.Subscriptions/README.md)**: Subscription management
- **[SaasSuite.Metering](../SaasSuite.Metering/README.md)**: Usage tracking
- **[SaasSuite.Payments.Stripe](../SaasSuite.Payments.Stripe/README.md)**: Stripe integration
- **[SaasSuite.Payments.PayPal](../SaasSuite.Payments.PayPal/README.md)**: PayPal integration

## License

This package is licensed under the [Apache License 2.0](https://www.apache.org/licenses/LICENSE-2.0).
See the [LICENSE](../../LICENSE) file in the repository root for details.