# SaasSuite.Compliance

[![NuGet](https://img.shields.io/nuget/v/SaasSuite.Compliance.svg)](https://www.nuget.org/packages/SaasSuite.Compliance)
[![License: Apache-2.0](https://img.shields.io/badge/License-Apache%202.0-blue.svg)](LICENSE)
[![Platform](https://img.shields.io/badge/.NET-6%2B-purple.svg)](https://dotnet.microsoft.com/)

GDPR and CCPA compliance workflows for multi-tenant SaaS applications.

## Overview

`SaasSuite.Compliance` provides essential compliance features for SaaS applications including data export, right-to-be-forgotten workflows, and consent management. This package helps you meet GDPR, CCPA, and other privacy regulation requirements.

## Features

- **Data Export**: Export tenant data in JSON, CSV, or XML formats
- **Right to be Forgotten**: Anonymize or delete tenant data on request
- **Consent Management**: Track and manage user consent for data processing
- **Compliance Events**: Audit trail of all compliance-related actions
- **CLI Integration**: Command-line tools for compliance operations

## Installation

```bash
dotnet add package SaasSuite.Compliance
dotnet add package SaasSuite.Core
dotnet add package SaasSuite.Audit
```

## Usage

### Register Services

```csharp
// Use in-memory implementations (for testing/demo)
services.AddSaasCompliance();

// Or provide custom implementations
services.AddSaasCompliance<
    MyComplianceExporter,
    MyRightToBeForgottenService,
    MyConsentStore>();
```

### Export Tenant Data

```csharp
using SaasSuite.Compliance.Interfaces;
using SaasSuite.Compliance.Options;

public class ComplianceController
{
    private readonly IComplianceExporter _exporter;
    
    public ComplianceController(IComplianceExporter exporter)
    {
        _exporter = exporter;
    }
    
    public async Task<DataExportManifest> ExportDataAsync(string tenantId)
    {
        var options = new DataExportOptions
        {
            Format = DataExportFormat.Json,
            IncludeAuditLogs = true,
            Compress = true
        };
        
        var manifest = await _exporter.ExportTenantAsync(
            new TenantId(tenantId), 
            options);
        
        return manifest;
    }
}
```

### Right to be Forgotten

```csharp
using SaasSuite.Compliance.Interfaces;

public class DataDeletionService
{
    private readonly IRightToBeForgottenService _forgottenService;
    
    public DataDeletionService(IRightToBeForgottenService forgottenService)
    {
        _forgottenService = forgottenService;
    }
    
    public async Task AnonymizeUserDataAsync(string tenantId)
    {
        // Anonymize personal data while maintaining referential integrity
        await _forgottenService.AnonymizeAsync(new TenantId(tenantId));
    }
    
    public async Task DeleteAllDataAsync(string tenantId)
    {
        // Permanently delete all tenant data
        await _forgottenService.DeleteAsync(new TenantId(tenantId));
    }
}
```

### Consent Management

```csharp
using SaasSuite.Compliance;
using SaasSuite.Compliance.Interfaces;

public class ConsentService
{
    private readonly IConsentStore _consentStore;
    
    public ConsentService(IConsentStore consentStore)
    {
        _consentStore = consentStore;
    }
    
    public async Task RecordUserConsentAsync(string tenantId, string userId)
    {
        var consent = new ConsentRecord
        {
            ConsentType = "marketing",
            IsGranted = true,
            IpAddress = "192.168.1.1",
            Metadata = new Dictionary<string, string>
            {
                ["source"] = "web-app",
                ["version"] = "1.0"
            }
        };
        
        await _consentStore.RecordConsentAsync(
            new TenantId(tenantId),
            userId,
            consent);
    }
    
    public async Task<bool> HasMarketingConsentAsync(string tenantId, string userId)
    {
        return await _consentStore.HasConsentAsync(
            new TenantId(tenantId),
            userId,
            "marketing");
    }
}
```

## CLI Commands

The package integrates with the SaasSuite CLI:

```bash
# Export tenant data
saas compliance export --tenant tenant-123 --format json --output ./export

# Execute right-to-be-forgotten
saas compliance forget --tenant tenant-123 --mode anonymize

# View consent history
saas compliance consent --tenant tenant-123 --user user-456
```

## Custom Implementations

The package provides interfaces for implementing custom storage backends:

### Custom Exporter

```csharp
public class DatabaseComplianceExporter : IComplianceExporter
{
    public async Task<DataExportManifest> ExportTenantAsync(
        TenantId tenantId, 
        DataExportOptions options,
        CancellationToken cancellationToken)
    {
        // Export data from your database
        // Generate files
        // Return manifest
    }
}
```

### Custom Consent Store

```csharp
public class DatabaseConsentStore : IConsentStore
{
    public async Task RecordConsentAsync(
        TenantId tenantId, 
        string userId, 
        ConsentRecord consent,
        CancellationToken cancellationToken)
    {
        // Store consent in your database
    }
    
    // Implement other methods...
}
```

## Compliance Events

All compliance operations emit events that integrate with `SaasSuite.Audit`:

- `DataExportRequested` / `DataExportCompleted`
- `RightToBeForgottenRequested` 
- `DataAnonymized` / `DataDeleted`
- `ConsentGranted` / `ConsentRevoked`

These events provide a complete audit trail for compliance purposes.

## Best Practices

1. **Regular Data Exports**: Schedule periodic data exports for backup and compliance
2. **Consent Expiration**: Set appropriate expiration dates on consent records
3. **Audit Trails**: Maintain detailed logs of all compliance operations
4. **Testing**: Test anonymization and deletion thoroughly in non-production environments
5. **Documentation**: Document your data retention and deletion policies

## Related Packages

- **[SaasSuite.Audit](../SaasSuite.Audit/README.md)**: For compliance event logging
- **[SaasSuite.Core](../SaasSuite.Core/README.md)**: For tenant context

## License

This package is licensed under the [Apache License 2.0](https://www.apache.org/licenses/LICENSE-2.0).
See the [LICENSE](../../LICENSE) file in the repository root for details.