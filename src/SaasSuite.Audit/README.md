# SaasSuite.Audit

[![NuGet](https://img.shields.io/nuget/v/SaasSuite.Audit.svg)](https://www.nuget.org/packages/SaasSuite.Audit)
[![License: Apache-2.0](https://img.shields.io/badge/License-Apache%202.0-blue.svg)](LICENSE)
[![Platform](https://img.shields.io/badge/.NET-6%2B-purple.svg)](https://dotnet.microsoft.com/)

Audit logging for multi-tenant SaaS applications with comprehensive event tracking and querying capabilities.

## Overview

`SaasSuite.Audit` provides a complete audit logging solution for tracking user actions, system events, and resource changes in multi-tenant applications. It captures who did what, when, and provides powerful querying capabilities for compliance and security monitoring.

## Features

- **Event Logging**: Record user actions and system events
- **Multi-Tenant**: Full tenant isolation for audit events
- **Rich Metadata**: Capture additional context and metadata
- **Flexible Querying**: Filter by date range, action, resource, and tenant
- **Admin View**: Cross-tenant audit trail for administrators
- **Thread-Safe**: Concurrent event recording
- **Extensible**: Easy to implement custom storage backends

## Installation

```bash
dotnet add package SaasSuite.Audit
dotnet add package SaasSuite.Core
```

## Quick Start

### 1. Register Services

```csharp
builder.Services.AddSaasAudit();
```

### 2. Log Audit Events

```csharp
using SaasSuite.Audit.Interfaces;

public class InvoiceController : ControllerBase
{
    private readonly IAuditService _auditService;
    
    public InvoiceController(IAuditService auditService)
    {
        _auditService = auditService;
    }
    
    [HttpPost]
    public async Task<IActionResult> CreateInvoice(InvoiceDto dto)
    {
        // Create invoice logic...
        
        // Log the audit event
        await _auditService.LogAsync(
            tenantId: new TenantId("tenant-123"),
            userId: User.Identity.Name,
            action: "Create",
            resource: "Invoice",
            details: $"Created invoice {invoice.Id} for amount ${invoice.Total}",
            metadata: new Dictionary<string, string>
            {
                { "InvoiceId", invoice.Id },
                { "Amount", invoice.Total.ToString() }
            });
        
        return Ok(invoice);
    }
}
```

## Core Concepts

### Audit Event Structure

```csharp
public class AuditEvent
{
    public string Id { get; set; }              // Unique event ID
    public TenantId TenantId { get; set; }      // Tenant who owns this event
    public string UserId { get; set; }          // User who performed the action
    public string Action { get; set; }          // Action type (Create, Update, Delete)
    public string Resource { get; set; }        // Resource affected (Invoice, User, etc.)
    public string? Details { get; set; }        // Additional details
    public DateTime Timestamp { get; set; }     // When it happened
    public string? IpAddress { get; set; }      // IP address (optional)
    public IDictionary<string, string> Metadata { get; set; }  // Additional key-value data
}
```

## Usage Examples

### Log User Actions

```csharp
// Login event
await _auditService.LogAsync(
    tenantId,
    userId: user.Email,
    action: "Login",
    resource: "Authentication",
    details: "User logged in successfully",
    metadata: new Dictionary<string, string>
    {
        { "LoginMethod", "OAuth" },
        { "Provider", "Google" }
    });

// Update event
await _auditService.LogAsync(
    tenantId,
    userId: currentUser.Id,
    action: "Update",
    resource: "Subscription",
    details: $"Changed plan from {oldPlan} to {newPlan}",
    metadata: new Dictionary<string, string>
    {
        { "OldPlan", oldPlan },
        { "NewPlan", newPlan },
        { "EffectiveDate", effectiveDate.ToString() }
    });

// Delete event
await _auditService.LogAsync(
    tenantId,
    userId: currentUser.Id,
    action: "Delete",
    resource: "Document",
    details: $"Deleted document: {document.Name}",
    metadata: new Dictionary<string, string>
    {
        { "DocumentId", document.Id },
        { "DocumentName", document.Name }
    });
```

### Query Audit Events

```csharp
// Get all events for a tenant
var events = await _auditService.GetEventsAsync(
    tenantId,
    startDate: DateTime.UtcNow.AddDays(-30),
    endDate: DateTime.UtcNow);

// Filter by action
var deleteEvents = await _auditService.GetEventsAsync(
    tenantId,
    action: "Delete",
    startDate: DateTime.UtcNow.AddDays(-7));

// Filter by resource
var invoiceEvents = await _auditService.GetEventsAsync(
    tenantId,
    resource: "Invoice",
    startDate: DateTime.UtcNow.AddMonths(-1));

// Combined filters
var recentUpdates = await _auditService.GetEventsAsync(
    tenantId,
    startDate: DateTime.UtcNow.AddHours(-24),
    action: "Update",
    resource: "Subscription");
```

### Admin View (Cross-Tenant)

```csharp
// Get all events across all tenants
var allEvents = await _auditService.GetAllEventsAsync(
    startDate: DateTime.UtcNow.AddDays(-7),
    endDate: DateTime.UtcNow);

// Find all delete operations across tenants
var allDeletes = await _auditService.GetAllEventsAsync(
    action: "Delete",
    startDate: DateTime.UtcNow.AddDays(-30));
```

## Common Actions

Standard action names for consistency:

- **Authentication**: `Login`, `Logout`, `PasswordChange`, `MfaEnabled`
- **CRUD**: `Create`, `Read`, `Update`, `Delete`
- **Subscription**: `Subscribe`, `Upgrade`, `Downgrade`, `Cancel`, `Renew`
- **Billing**: `InvoiceGenerated`, `PaymentReceived`, `RefundIssued`
- **User Management**: `UserInvited`, `UserRemoved`, `RoleChanged`, `PermissionGranted`
- **Configuration**: `SettingChanged`, `FeatureEnabled`, `FeatureDisabled`

## Common Resources

Standard resource names:

- **User Management**: `User`, `Role`, `Permission`, `ApiKey`
- **Billing**: `Invoice`, `Payment`, `Subscription`, `Refund`
- **Content**: `Document`, `File`, `Folder`, `Attachment`
- **Configuration**: `Setting`, `Feature`, `Integration`
- **System**: `Authentication`, `Authorization`, `Configuration`

## Custom Storage Implementation

The default implementation uses in-memory storage. For production, implement a custom backend:

```csharp
using SaasSuite.Audit.Interfaces;

public class DatabaseAuditService : IAuditService
{
    private readonly IDbContext _dbContext;
    
    public DatabaseAuditService(IDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    
    public async Task<AuditEvent> LogAsync(
        TenantId tenantId,
        string userId,
        string action,
        string resource,
        string? details = null,
        IDictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        var auditEvent = new AuditEvent
        {
            TenantId = tenantId,
            UserId = userId,
            Action = action,
            Resource = resource,
            Details = details,
            Metadata = metadata ?? new Dictionary<string, string>()
        };
        
        await _dbContext.AuditEvents.AddAsync(auditEvent, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        
        return auditEvent;
    }
    
    // Implement other methods...
}

// Register custom implementation
builder.Services.AddSaasAudit<DatabaseAuditService>();
```

## Integration with Other Packages

- **SaasSuite.Core**: Tenant-aware audit logging
- **SaasSuite.Compliance**: GDPR audit trail for data access
- **SaasSuite.DataProtection**: Audit sensitive data operations

## Best Practices

1. **Log Important Actions**: Focus on security-relevant and compliance-required events
2. **Include Context**: Add metadata to make events searchable and meaningful
3. **Consistent Naming**: Use standard action and resource names across your application
4. **User Identification**: Always include the user ID performing the action
5. **Timestamp Accuracy**: Use UTC timestamps for consistency
6. **Storage Considerations**: Implement database storage for production use
7. **Retention Policies**: Archive or purge old events based on compliance requirements
8. **Query Performance**: Index tenant ID, timestamp, action, and resource fields

## Storage

The default in-memory implementation is suitable for development and testing. For production use:

- Implement `IAuditService` with your database backend
- Consider using time-series databases for high-volume scenarios
- Implement data retention and archival policies
- Index frequently queried fields

## Related Packages

- **[SaasSuite.Core](../SaasSuite.Core/README.md)**: Core multi-tenancy features
- **[SaasSuite.Compliance](../SaasSuite.Compliance/README.md)**: Compliance and GDPR support
- **[SaasSuite.DataProtection](../SaasSuite.DataProtection/README.md)**: Data protection features

## License

This package is licensed under the [Apache License 2.0](https://www.apache.org/licenses/LICENSE-2.0).
See the [LICENSE](../../LICENSE) file in the repository root for details.