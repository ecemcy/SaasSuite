# SaasSuite.Caching

[![NuGet](https://img.shields.io/nuget/v/SaasSuite.Caching.svg)](https://www.nuget.org/packages/SaasSuite.Caching)
[![License: Apache-2.0](https://img.shields.io/badge/License-Apache%202.0-blue.svg)](LICENSE)
[![Platform](https://img.shields.io/badge/.NET-6%2B-purple.svg)](https://dotnet.microsoft.com/)

Tenant-aware caching abstractions and implementations for multi-tenant SaaS applications.

## Overview

`SaasSuite.Caching` provides caching capabilities with automatic tenant isolation. It ensures that cached data is properly scoped to prevent data leakage between tenants while maintaining high performance.

## Features

- **Tenant Isolation**: Automatic tenant-scoped cache keys prevent data leakage
- **Global Caching**: Support for tenant-agnostic global cache entries
- **Configurable Expiration**: Per-item and default TTL settings
- **Per-Entry Expiration Policy**: Support both absolute and sliding expiration via `CacheEntryOptions`
- **Event-Driven Invalidation**: Namespace versioning via `ICacheInvalidationPublisher` for scalable, per-tenant cache invalidation
- **Key Prefixing**: Customizable key prefixes for cache namespacing
- **Async Operations**: All operations support async/await with cancellation tokens

## Installation

```bash
dotnet add package SaasSuite.Caching
dotnet add package SaasSuite.Core
```

## Quick Start

### 1. Register Services

```csharp
// Use in-memory cache (development/single-server)
builder.Services.AddSaasCaching(options =>
{
    options.DefaultExpiration = TimeSpan.FromMinutes(30);
    options.EnableTenantIsolation = true;
    options.KeyPrefix = "myapp:";
});
```

This registers:
- `ICacheService` → `InMemoryCacheService`
- `INamespaceVersionStore` → `InMemoryNamespaceVersionStore`
- `ICacheInvalidationPublisher` → `CacheInvalidationPublisher`

### 2. Use the Cache Service

```csharp
using SaasSuite.Caching.Interfaces;

public class UserService
{
    private readonly ICacheService _cacheService;
    private readonly IUserRepository _userRepository;
    
    public UserService(ICacheService cacheService, IUserRepository userRepository)
    {
        _cacheService = cacheService;
        _userRepository = userRepository;
    }
    
    public async Task<User> GetUserAsync(string userId)
    {
        var cacheKey = $"users:{userId}";
        
        // Try to get from cache
        var cachedUser = await _cacheService.GetAsync<User>(cacheKey);
        if (cachedUser != null)
            return cachedUser;
        
        // Load from database
        var user = await _userRepository.GetByIdAsync(userId);
        
        // Cache for 15 minutes (absolute expiration)
        await _cacheService.SetAsync(cacheKey, user, TimeSpan.FromMinutes(15));
        
        return user;
    }
}
```

## Per-Entry Expiration Policy

Use `CacheEntryOptions` to control expiration on a per-call basis:

```csharp
// Absolute expiration only
await _cacheService.SetAsync(key, value, new CacheEntryOptions
{
    AbsoluteExpiration = TimeSpan.FromHours(1)
});

// Sliding expiration only (refreshed on each access)
await _cacheService.SetAsync(key, value, new CacheEntryOptions
{
    SlidingExpiration = TimeSpan.FromMinutes(10)
});

// Both: absolute caps the total lifetime; sliding resets within that cap
await _cacheService.SetAsync(key, value, new CacheEntryOptions
{
    AbsoluteExpiration = TimeSpan.FromHours(2),
    SlidingExpiration = TimeSpan.FromMinutes(15)
});
```

When neither `AbsoluteExpiration` nor `SlidingExpiration` is set in `CacheEntryOptions`, the
`CacheOptions.DefaultExpiration` is used as the absolute expiration.

## Event-Driven Cache Invalidation

`SaasSuite.Caching` uses **namespace versioning** for scalable invalidation. Instead of scanning or
deleting individual keys, each tenant/area namespace has an associated version number. Incrementing
the version orphans all cache entries stored under the previous version (they expire naturally at
their original TTL).

### How It Works

1. **Build a versioned key** using `TenantCacheKeyHelper.GetVersionedTenantKey`, passing the current
   version from `INamespaceVersionStore`.
2. **Store and retrieve** cache entries using that versioned key.
3. **Publish an invalidation event** via `ICacheInvalidationPublisher` to bump the version.
4. The next call to `GetVersionedTenantKey` will use the new version, yielding a different key and
   thus a cache miss, triggering a fresh load.

### Example

```csharp
using SaasSuite.Caching.Interfaces;
using SaasSuite.Caching.Invalidation;

public class ProductService
{
    private readonly ICacheService _cache;
    private readonly INamespaceVersionStore _versionStore;
    private readonly ICacheInvalidationPublisher _invalidationPublisher;
    private readonly IProductRepository _repo;

    public ProductService(
        ICacheService cache,
        INamespaceVersionStore versionStore,
        ICacheInvalidationPublisher invalidationPublisher,
        IProductRepository repo)
    {
        _cache = cache;
        _versionStore = versionStore;
        _invalidationPublisher = invalidationPublisher;
        _repo = repo;
    }

    public async Task<Product?> GetProductAsync(string tenantId, string productId)
    {
        var version = _versionStore.GetVersion(tenantId, area: "products");
        var key = TenantCacheKeyHelper.GetVersionedTenantKey(tenantId, version, productId, area: "products");

        var product = await _cache.GetAsync<Product>(key);
        if (product != null)
            return product;

        product = await _repo.GetByIdAsync(productId);
        if (product != null)
            await _cache.SetAsync(key, product, TimeSpan.FromMinutes(30));

        return product;
    }

    public async Task UpdateProductAsync(Product product)
    {
        await _repo.UpdateAsync(product);

        // Invalidate all product cache entries for this tenant
        await _invalidationPublisher.PublishAsync(new TenantCacheInvalidatedEvent(product.TenantId, "products"));
    }
}
```

### Invalidating the Entire Tenant Namespace

Omit `Area` to invalidate all versioned entries for a tenant regardless of area:

```csharp
await _invalidationPublisher.PublishAsync(new TenantCacheInvalidatedEvent("acme"));
// Area = null → invalidates the root tenant namespace version
```

## Tenant Isolation

### Automatic Tenant Scoping

When tenant isolation is enabled, cache keys are automatically prefixed with the current tenant ID:

```csharp
// Your code
await _cacheService.SetAsync("user-profile", userProfile);

// Actual cache key (automatic)
// "myapp:tenant:tenant-123:user-profile"
```

### Manual Tenant Keys

Use `TenantCacheKeyHelper` for manual key generation:

```csharp
using SaasSuite.Caching.Helpers;
using SaasSuite.Core;

// Simple tenant key (not versioned)
var tenantId = new TenantId("tenant-123");
var key = TenantCacheKeyHelper.GetTenantKey(tenantId, "user-profile");
// Result: "saas:tenant:tenant-123:user-profile"

// Versioned tenant key (for event-driven invalidation)
var version = _versionStore.GetVersion(tenantId, "users");
var versionedKey = TenantCacheKeyHelper.GetVersionedTenantKey(tenantId, version, "user-profile", "users");
// Result: "saas:tenant:tenant-123:v1:users:user-profile"
```

### Global Cache Keys

For tenant-agnostic data, use global keys:

```csharp
var globalKey = TenantCacheKeyHelper.GetGlobalKey("app-settings");
// Result: "saas:global:app-settings"

await _cacheService.SetAsync(globalKey, settings);
```

## Core Interface

### ICacheService

```csharp
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);

    // Absolute expiration (or default from CacheOptions)
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null,
        CancellationToken cancellationToken = default);

    // Per-entry policy (absolute and/or sliding)
    Task SetAsync<T>(string key, T value, CacheEntryOptions options,
        CancellationToken cancellationToken = default);

    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
}
```

## Usage Patterns

### Cache-Aside Pattern

```csharp
public async Task<Product> GetProductAsync(string productId)
{
    var key = $"products:{productId}";
    
    // Try cache first
    var product = await _cacheService.GetAsync<Product>(key);
    if (product != null)
        return product;
    
    // Load from source
    product = await _productRepository.GetByIdAsync(productId);
    
    // Update cache
    await _cacheService.SetAsync(key, product, TimeSpan.FromHours(1));
    
    return product;
}
```

### Invalidation on Update

```csharp
public async Task UpdateProductAsync(Product product)
{
    // Update in database
    await _productRepository.UpdateAsync(product);
    
    // Invalidate cache
    var key = $"products:{product.Id}";
    await _cacheService.RemoveAsync(key);
}
```

### List Caching

```csharp
public async Task<List<Product>> GetProductsByCategoryAsync(string categoryId)
{
    var key = $"products:category:{categoryId}";
    
    var products = await _cacheService.GetAsync<List<Product>>(key);
    if (products != null)
        return products;
    
    products = await _productRepository.GetByCategoryAsync(categoryId);
    
    await _cacheService.SetAsync(
        key, 
        products, 
        TimeSpan.FromMinutes(10));
    
    return products;
}
```

### Session Data Caching

```csharp
public async Task<UserSession> GetUserSessionAsync(string sessionId)
{
    var key = $"sessions:{sessionId}";
    
    return await _cacheService.GetAsync<UserSession>(key);
}

public async Task SaveUserSessionAsync(UserSession session)
{
    var key = $"sessions:{session.SessionId}";
    
    // Cache until session expiration
    await _cacheService.SetAsync(
        key,
        session,
        TimeSpan.FromMinutes(30));
}
```

## Configuration Options

```csharp
public class CacheOptions
{
    // Default TTL used when no per-entry expiration is specified
    public TimeSpan DefaultExpiration { get; set; } = TimeSpan.FromMinutes(30);
    
    // Enable automatic tenant key isolation
    public bool EnableTenantIsolation { get; set; } = true;
    
    // Custom prefix for all cache keys
    public string KeyPrefix { get; set; } = "saas:";
}
```

## Distributed Caching

For multi-server scenarios, implement `ICacheService` with a distributed cache backend:

```csharp
// Redis example (custom implementation)
services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = "localhost:6379";
});

services.AddSingleton<ICacheService, RedisCacheService>();
```

## Implementing a Custom Cache Backend

Implement `ICacheService` to use any distributed backend (e.g., Redis via a separate
`SaasSuite.Caching.Redis` package), then register your implementation:

```csharp
services.AddSingleton<ICacheService, MyRedisCacheService>();

// INamespaceVersionStore and ICacheInvalidationPublisher are also replaceable
services.AddSingleton<INamespaceVersionStore, MyRedisNamespaceVersionStore>();
```

## Cache Key Strategies

### Hierarchical Keys

```csharp
// Tenant-level aggregates
"tenant:{tenantId}:stats"

// User-specific data
"tenant:{tenantId}:users:{userId}:profile"

// Feature data
"tenant:{tenantId}:features:{featureId}"
```

### Wildcard Invalidation

When using Redis or similar, you can invalidate key patterns:

```csharp
// Invalidate all user-related keys for a tenant
// "tenant:123:users:*"
```

## Performance Considerations

1. **Cache What's Expensive**: Focus on database queries, API calls, complex calculations
2. **Set Appropriate TTLs**: Balance freshness vs. performance
3. **Use Sliding Expiration for Sessions**: Extends lifetime on each access
4. **Namespace Versioning**: Prefer over key scanning for bulk invalidation
5. **Monitor Hit Rates**: Track cache effectiveness
6. **Size Awareness**: Be mindful of cached object sizes
7. **Serialization**: Use efficient serialization for complex objects

## Testing

Use the in-memory implementation for unit tests:

```csharp
var services = new ServiceCollection();
services.AddSaasCaching();
var provider = services.BuildServiceProvider();

var cacheService = provider.GetRequiredService<ICacheService>();
var publisher = provider.GetRequiredService<ICacheInvalidationPublisher>();
var versionStore = provider.GetRequiredService<INamespaceVersionStore>();
```

## Related Packages

- **[SaasSuite.Core](../SaasSuite.Core/README.md)**: Tenant context integration
- **[SaasSuite.Caching.Redis](../SaasSuite.Caching.Redis/README.md)**: Official Redis (distributed) cache provider for this package

## Dependencies

- **Microsoft.Extensions.Caching.Memory**: In-memory implementation

## License

This package is licensed under the [Apache License 2.0](https://www.apache.org/licenses/LICENSE-2.0).
See the [LICENSE](../../LICENSE) file in the repository root for details.