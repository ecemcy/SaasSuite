# SaasSuite.EfCore

[![NuGet](https://img.shields.io/nuget/v/SaasSuite.EfCore.svg)](https://www.nuget.org/packages/SaasSuite.EfCore)
[![License: Apache-2.0](https://img.shields.io/badge/License-Apache%202.0-blue.svg)](LICENSE)
[![Platform](https://img.shields.io/badge/.NET-6%2B-purple.svg)](https://dotnet.microsoft.com/)

Entity Framework Core integration for multi-tenant SaaS applications with automatic tenant isolation and per-tenant database support.

## Overview

`SaasSuite.EfCore` provides seamless integration between Entity Framework Core and SaasSuite's multi-tenancy features. It automatically isolates tenant data through query filters, manages per-tenant databases, and prevents accidental cross-tenant data access.

## Features

- **Automatic Query Filtering**: Global filters automatically scope queries to the current tenant
- **Tenant ID Auto-Injection**: Automatically sets tenant ID on new entities
- **Per-Tenant Databases**: Support for separate databases per tenant
- **Per-Tenant Model Caching**: Different schemas for different tenants
- **Change Tracking Protection**: Prevents unauthorized tenant ID modifications
- **Flexible Configuration**: Enable/disable features based on your needs

## Installation

```bash
dotnet add package SaasSuite.EfCore
dotnet add package SaasSuite.Core
```

## Quick Start

### 1. Mark Entities as Tenant-Scoped

```csharp
using SaasSuite.Core;
using SaasSuite.EfCore.Interfaces;

public class Product : ITenantEntity
{
    public int Id { get; set; }
    public TenantId TenantId { get; set; } // Required by ITenantEntity
    public string Name { get; set; }
    public decimal Price { get; set; }
}

public class Order : ITenantEntity
{
    public int Id { get; set; }
    public TenantId TenantId { get; set; }
    public DateTime OrderDate { get; set; }
    public List<OrderItem> Items { get; set; }
}
```

### 2. Configure DbContext

```csharp
using Microsoft.EntityFrameworkCore;
using SaasSuite.Core.Interfaces;

public class AppDbContext : DbContext
{
    private readonly ITenantAccessor _tenantAccessor;
    
    public DbSet<Product> Products { get; set; }
    public DbSet<Order> Orders { get; set; }
    
    public AppDbContext(
        DbContextOptions<AppDbContext> options,
        ITenantAccessor tenantAccessor) : base(options)
    {
        _tenantAccessor = tenantAccessor;
    }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Apply automatic tenant query filters
        modelBuilder.ApplyTenantQueryFilter(_tenantAccessor);
    }
}
```

### 3. Register Services

```csharp
var builder = WebApplication.CreateBuilder(args);

// Register SaasSuite.Core
builder.Services.AddSaasCore();

// Register tenant-aware DbContext
builder.Services.AddSaasTenantDbContext<AppDbContext>(options =>
{
    options.AutoSetTenantId = true;
    options.UseGlobalQueryFilter = true;
    options.UsePerTenantModelCache = false; // Enable if using per-tenant schemas
});
```

## Core Features

### Automatic Query Filtering

All queries on `ITenantEntity` types are automatically filtered to the current tenant:

```csharp
public class ProductService
{
    private readonly AppDbContext _context;
    
    public ProductService(AppDbContext context)
    {
        _context = context;
    }
    
    public async Task<List<Product>> GetProductsAsync()
    {
        // Automatically filtered to current tenant
        // No need to manually filter by TenantId
        return await _context.Products.ToListAsync();
    }
    
    public async Task<Product?> GetProductByIdAsync(int id)
    {
        // Also automatically filtered
        return await _context.Products
            .FirstOrDefaultAsync(p => p.Id == id);
    }
}
```

Generated SQL automatically includes tenant filter:
```sql
SELECT * FROM Products 
WHERE TenantId = 'tenant-123' AND Id = @p0
```

### Automatic Tenant ID Injection

When saving new entities, the tenant ID is automatically set:

```csharp
public async Task<Product> CreateProductAsync(Product product)
{
    // No need to manually set TenantId
    _context.Products.Add(product);
    await _context.SaveChangesAsync();
    
    // product.TenantId is now set automatically
    return product;
}
```

### Change Tracking Protection

Prevents accidental or malicious tenant ID changes:

```csharp
var product = await _context.Products.FindAsync(id);
product.TenantId = new TenantId("different-tenant"); // This will be ignored!

await _context.SaveChangesAsync(); // TenantId remains unchanged
```

## Per-Tenant Databases

### Using Tenant-Specific Connection Strings

```csharp
using SaasSuite.EfCore.Implementations;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // Register tenant store with connection strings
        services.AddSingleton<ITenantStore, MyTenantStore>();
        
        // Register DbContext factory
        services.AddSingleton<ITenantDbContextFactory<AppDbContext>, 
            TenantDbContextFactory<AppDbContext>>();
        
        services.AddSaasTenantDbContext<AppDbContext>(options =>
        {
            options.UsePerTenantModelCache = true; // Enable for per-tenant schemas
        });
    }
}
```

### Accessing Tenant-Specific Context

```csharp
public class MultiTenantDataService
{
    private readonly ITenantDbContextFactory<AppDbContext> _contextFactory;
    
    public MultiTenantDataService(
        ITenantDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }
    
    public async Task<List<Product>> GetProductsForTenantAsync(TenantId tenantId)
    {
        await using var context = await _contextFactory
            .CreateDbContextAsync(tenantId);
        
        return await context.Products.ToListAsync();
    }
}
```

## Per-Tenant Schema Support

For scenarios where each tenant has a different database schema:

```csharp
public class TenantModelCacheKeyFactory : IModelCacheKeyFactory
{
    private readonly ITenantAccessor _tenantAccessor;
    
    public TenantModelCacheKeyFactory(ITenantAccessor tenantAccessor)
    {
        _tenantAccessor = tenantAccessor;
    }
    
    public object Create(DbContext context, bool designTime)
    {
        if (designTime)
            return new object();
        
        var tenantId = _tenantAccessor.TenantContext?.TenantId.Value ?? "default";
        return (context.GetType(), tenantId);
    }
}

// Register the custom model cache key factory
services.AddSingleton<IModelCacheKeyFactory, TenantModelCacheKeyFactory>();
```

## Configuration Options

```csharp
public class EfCoreOptions
{
    // Automatically set TenantId on new entities
    public bool AutoSetTenantId { get; set; } = true;
    
    // Apply global query filters for tenant isolation
    public bool UseGlobalQueryFilter { get; set; } = true;
    
    // Enable per-tenant model caching (for different schemas)
    public bool UsePerTenantModelCache { get; set; } = false;
    
    // Default connection string (when tenant doesn't specify one)
    public string? DefaultConnectionString { get; set; }
}
```

## Advanced Usage

### Bypassing Tenant Filters

For admin operations that need to access all tenant data:

```csharp
public async Task<List<Product>> GetAllProductsAcrossTenantsAsync()
{
    // Disable query filters
    return await _context.Products
        .IgnoreQueryFilters()
        .ToListAsync();
}
```

### Manual Tenant Filtering

When working with non-tenant entities or complex queries:

```csharp
public async Task<Order> GetOrderWithDetailsAsync(int orderId)
{
    var tenantId = _tenantAccessor.TenantContext.TenantId;
    
    return await _context.Orders
        .Where(o => o.TenantId == tenantId && o.Id == orderId)
        .Include(o => o.Items)
        .FirstOrDefaultAsync();
}
```

### Migrations with Multi-Tenancy

```csharp
// Run migrations for a specific tenant
public async Task MigrateTenantDatabaseAsync(TenantId tenantId)
{
    await using var context = await _contextFactory
        .CreateDbContextAsync(tenantId);
    
    await context.Database.MigrateAsync();
}

// Run migrations for all tenants
public async Task MigrateAllTenantsAsync()
{
    var tenants = await _tenantStore.GetAllAsync();
    
    foreach (var tenant in tenants)
    {
        await MigrateTenantDatabaseAsync(tenant.TenantId);
    }
}
```

## Best Practices

1. **Always Use ITenantEntity**: Mark all tenant-scoped entities with the interface
2. **Trust the Filters**: Let automatic filtering handle tenant isolation
3. **Avoid Manual TenantId Checks**: The query filters handle this for you
4. **Test Isolation**: Verify queries don't return cross-tenant data
5. **Use IgnoreQueryFilters Sparingly**: Only for admin operations
6. **Separate Shared Data**: Use different entities for tenant-agnostic data

## Testing

Create a test DbContext with in-memory database:

```csharp
public class TestAppDbContext : AppDbContext
{
    public TestAppDbContext(
        DbContextOptions<AppDbContext> options,
        ITenantAccessor tenantAccessor) 
        : base(options, tenantAccessor)
    {
    }
    
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseInMemoryDatabase("TestDb");
        }
    }
}

// In tests
var options = new DbContextOptionsBuilder<AppDbContext>()
    .UseInMemoryDatabase(databaseName: "TestDatabase")
    .Options;

var mockTenantAccessor = Mock.Of<ITenantAccessor>(
    x => x.TenantContext == new TenantContext 
    { 
        TenantId = new TenantId("test-tenant") 
    });

var context = new AppDbContext(options, mockTenantAccessor);
```

## Related Packages

- **[SaasSuite.Core](../SaasSuite.Core/README.md)**: Core multi-tenancy abstractions
- **[SaasSuite.Migration](../SaasSuite.Migration/README.md)**: Tenant data migration tools
- **[SaasSuite.Dapper](../SaasSuite.Dapper/README.md)**: Dapper integration (alternative)
- **[SaasSuite.Mongo](../SaasSuite.Mongo/README.md)**: MongoDB integration (alternative)

## License

This package is licensed under the [Apache License 2.0](https://www.apache.org/licenses/LICENSE-2.0).
See the [LICENSE](../../LICENSE) file in the repository root for details.