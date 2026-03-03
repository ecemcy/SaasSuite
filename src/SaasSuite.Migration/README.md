# SaasSuite.Migration

[![NuGet](https://img.shields.io/nuget/v/SaasSuite.Migration.svg)](https://www.nuget.org/packages/SaasSuite.Migration)
[![License: Apache-2.0](https://img.shields.io/badge/License-Apache%202.0-blue.svg)](LICENSE)
[![Platform](https://img.shields.io/badge/.NET-6%2B-purple.svg)](https://dotnet.microsoft.com/)

Multi-tenant data migration engine for SaasSuite.

## Features

- **Dry-run mode**: Validate migrations without applying changes
- **Batching support**: Process tenants in configurable batches
- **Checkpointing**: Resume migrations from the last successful checkpoint
- **Parallel execution**: Process multiple tenants simultaneously
- **Rollback hooks**: Define rollback operations for migration steps
- **Progress reporting**: Track migration progress in real-time
- **CLI integration**: Ready for command-line interface integration

## Installation

```bash
dotnet add package SaasSuite.Migration
dotnet add package SaasSuite.Core
```

## Quick Start

### 1. Register Services

```csharp
services.AddSaasMigration(options =>
{
    options.BatchSize = 10;
    options.EnableParallelExecution = true;
    options.MaxDegreeOfParallelism = 4;
    options.EnableCheckpointing = true;
});

// Register your tenant provider
services.AddScoped<ITenantProvider, YourTenantProvider>();

// Register migration steps
services.AddMigrationStep<Step1>();
services.AddMigrationStep<Step2>();
```

### 2. Implement a Tenant Provider

```csharp
using SaasSuite.Core.Interfaces;
using SaasSuite.Migration.Interfaces;

public class YourTenantProvider : ITenantProvider
{
    private readonly ITenantStore _tenantStore;

    public YourTenantProvider(ITenantStore tenantStore)
    {
        _tenantStore = tenantStore;
    }

    public async Task<IEnumerable<TenantInfo>> GetAllTenantsAsync(
        CancellationToken cancellationToken = default)
    {
        // Implement your logic to retrieve all tenants
        // Example: return await _tenantStore.GetAllAsync(cancellationToken);
    }
}
```

### 3. Create Migration Steps

```csharp
using SaasSuite.Migration.Base;

public class AddNewColumnMigration : MigrationStepBase
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public override string Name => "AddNewColumn";
    public override string Description => "Adds a new column to the users table";

    public AddNewColumnMigration(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public override async Task ExecuteAsync(string tenantId, CancellationToken cancellationToken)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        // Set tenant context
        // Execute migration logic
    }

    public override async Task<bool> ValidateAsync(string tenantId, CancellationToken cancellationToken)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        // Validate migration can be applied
        return true;
    }

    public override async Task RollbackAsync(string tenantId, CancellationToken cancellationToken)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        // Rollback logic (optional)
    }
}
```

### 4. Execute Migration

```csharp
using SaasSuite.Migration;
using SaasSuite.Migration.Interfaces;

public class MigrationService
{
    private readonly IMigrationEngine _migrationEngine;
    private readonly IEnumerable<IMigrationStep> _migrationSteps;

    public MigrationService(
        IMigrationEngine migrationEngine,
        IEnumerable<IMigrationStep> migrationSteps)
    {
        _migrationEngine = migrationEngine;
        _migrationSteps = migrationSteps;
    }

    public async Task RunMigrationAsync()
    {
        // Perform dry run first
        var dryRunResult = await _migrationEngine.DryRunAsync(_migrationSteps);
        
        if (!dryRunResult.IsSuccess)
        {
            Console.WriteLine($"Dry run failed: {dryRunResult.Errors.Count} errors");
            return;
        }

        // Execute actual migration with progress reporting
        var progress = new Progress<MigrationProgress>(p =>
        {
            Console.WriteLine($"Progress: {p.PercentComplete:F2}% - {p.Message}");
        });

        var result = await _migrationEngine.ExecuteAsync(
            _migrationSteps,
            progress: progress);

        Console.WriteLine($"Migration completed: {result.SuccessfulTenants} successful, " +
                         $"{result.FailedTenants} failed");
    }
}
```

## Advanced Usage

### Resume from Checkpoint

```csharp
// Load previous checkpoint
var checkpoint = await LoadCheckpointAsync();

var result = await _migrationEngine.ExecuteAsync(
    _migrationSteps,
    checkpoint: checkpoint);
```

### Migrate Specific Tenants

```csharp
var tenantIds = new[] { "tenant-1", "tenant-2", "tenant-3" };

var result = await _migrationEngine.ExecuteAsync(
    _migrationSteps,
    tenantIds: tenantIds);
```

### Custom Migration Options

```csharp
var options = new MigrationOptions
{
    BatchSize = 5,
    EnableParallelExecution = true,
    MaxDegreeOfParallelism = 2,
    ContinueOnFailure = true,
    TenantTimeout = TimeSpan.FromMinutes(10)
};

var result = await _migrationEngine.ExecuteAsync(
    _migrationSteps,
    options: options);
```

### Rollback Migration

```csharp
var affectedTenantIds = new[] { "tenant-1", "tenant-2" };

var result = await _migrationEngine.RollbackAsync(
    _migrationSteps,
    affectedTenantIds);
```

## Configuration Options

Configure the migration engine behavior through `MigrationOptions`:

**BatchSize** (default: `10`)  
Number of tenants to process in each batch. Adjust based on your system resources and tenant data size.

**EnableParallelExecution** (default: `false`)  
Enable parallel processing of tenants within batches. Improves throughput for independent migrations.

**MaxDegreeOfParallelism** (default: `4`)  
Maximum number of parallel operations when parallel execution is enabled. Higher values increase throughput but also resource usage.

**EnableCheckpointing** (default: `true`)  
Enable automatic checkpointing for resumability. Allows migrations to resume from the last successful checkpoint after interruption.

**CheckpointInterval** (default: `1`)  
Number of batches processed between checkpoint saves. Lower values provide more frequent resume points but slightly impact performance.

**ContinueOnFailure** (default: `true`)  
Continue processing remaining tenants when individual tenant migrations fail. Failed tenants are tracked in the result.

**TenantTimeout** (default: `5 minutes`)  
Maximum time allowed for each tenant's migration operation. Prevents hung operations from blocking the entire migration.

**EnableProgressReporting** (default: `true`)  
Enable progress callbacks for real-time monitoring. Disable to reduce overhead in high-performance scenarios.

## Related Packages

- **[SaasSuite.Core](../SaasSuite.Core/README.md)**: Tenant resolution and context management
- **[SaasSuite.SaasSuite.EfCore](../SaasSuite.SaasSuite.EfCore/README.md)**: EF Core integration for multi-tenant data access
- **[SaasSuite.Quotas](../SaasSuite.Quotas/README.md)**: Resource quota enforcement

## License

This package is licensed under the [Apache License 2.0](https://www.apache.org/licenses/LICENSE-2.0).
See the [LICENSE](../../LICENSE) file in the repository root for details.