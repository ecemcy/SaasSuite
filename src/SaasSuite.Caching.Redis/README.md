# SaasSuite.Caching.Redis

[![NuGet](https://img.shields.io/nuget/v/SaasSuite.Caching.Redis.svg)](https://www.nuget.org/packages/SaasSuite.Caching.Redis)
[![License: Apache-2.0](https://img.shields.io/badge/License-Apache%202.0-blue.svg)](LICENSE)
[![Platform](https://img.shields.io/badge/.NET-6%2B-purple.svg)](https://dotnet.microsoft.com/)

Redis-backed distributed caching implementation for multi-tenant SaaS applications, built on top of the `SaasSuite.Caching` abstractions.

## Overview

`SaasSuite.Caching.Redis` provides a production-ready distributed cache implementation using [StackExchange.Redis](https://github.com/StackExchange/StackExchange.Redis) and `System.Text.Json`. It implements the `ICacheService` interface defined in `SaasSuite.Caching`, enabling you to switch from the default in-memory cache to Redis with only a DI registration change.

## Installation

```bash
dotnet add package SaasSuite.Caching.Redis
dotnet add package SaasSuite.Caching
```

This package depends on `SaasSuite.Caching` which defines the shared `ICacheService`, `CacheOptions`, and `TenantCacheKeyHelper` types.

## Configuration

### RedisCacheOptions

The `RedisCacheOptions` class provides the following configuration properties:

**ConnectionString** (`string?`, default: `null`)
- Redis connection string (e.g., `"localhost:6379"`)
- Used when `ConnectionMultiplexerFactory` is not provided

**Database** (`int`, default: `-1`)
- Redis database index
- The value `-1` uses the server default database

**ConnectionMultiplexerFactory** (`Func<IConnectionMultiplexer>?`, default: `null`)
- Optional factory for advanced multiplexer configuration
- When set, `ConnectionString` is ignored

## DI Registration

You can register the Redis caching service using one of two approaches:

### Option A: Using a Connection String

```csharp
builder.Services.AddSaasCachingRedis(
    configureRedis: options =>
    {
        options.ConnectionString = "localhost:6379";
        options.Database = 0;
    },
    configureCaching: options =>
    {
        options.DefaultExpiration = TimeSpan.FromMinutes(30);
        options.KeyPrefix = "myapp:";
    });
```

### Option B: Using a Custom Multiplexer

For advanced scenarios where you need more control over the connection:

```csharp
var multiplexer = await ConnectionMultiplexer.ConnectAsync("localhost:6379,abortConnect=false");

builder.Services.AddSaasCachingRedis(
    multiplexer: multiplexer,
    configureCaching: options =>
    {
        options.DefaultExpiration = TimeSpan.FromMinutes(30);
        options.KeyPrefix = "myapp:";
    });
```

Both registration methods configure the following services:
- `IConnectionMultiplexer` as a singleton Redis multiplexer
- `ICacheService` implemented by `RedisCacheService` as a singleton

## Usage

Inject and use `ICacheService` exactly as you would with the in-memory backend. The interface remains consistent across implementations:

```csharp
using SaasSuite.Caching.Helpers;
using SaasSuite.Caching.Interfaces;

public class UserService
{
    private readonly ICacheService _cache;
    private readonly IUserRepository _userRepository;

    public UserService(ICacheService cache, IUserRepository userRepository)
    {
        _cache = cache;
        _userRepository = userRepository;
    }

    public async Task<User?> GetUserAsync(string tenantId, string userId)
    {
        // Use TenantCacheKeyHelper from SaasSuite.Caching for tenant isolation
        var key = TenantCacheKeyHelper.GetTenantKey(tenantId, $"user:{userId}");

        var cached = await _cache.GetAsync<User>(key);
        if (cached is not null)
            return cached;

        var user = await _userRepository.GetByIdAsync(userId);
        if (user is not null)
            await _cache.SetAsync(key, user, TimeSpan.FromMinutes(15));

        return user;
    }
}
```

### Configuring Entry Expiration

You can control cache expiration on a per-entry basis:

**Absolute Expiration**
```csharp
// Entry expires after exactly 1 hour from creation
await _cache.SetAsync(key, value, new CacheEntryOptions
{
    AbsoluteExpiration = TimeSpan.FromHours(1)
});
```

**Sliding Expiration**
```csharp
// Entry expires 10 minutes after last access (stored as a fixed TTL in Redis)
await _cache.SetAsync(key, value, new CacheEntryOptions
{
    SlidingExpiration = TimeSpan.FromMinutes(10)
});
```

### Removing Entries

Remove cached entries when they're no longer needed:

```csharp
await _cache.RemoveAsync(key);
```

## Tenant Isolation

Tenant isolation is a critical feature for multi-tenant applications. The `TenantCacheKeyHelper` from `SaasSuite.Caching` ensures data is properly isolated per tenant:

**Simple Tenant Key**
```csharp
var key = TenantCacheKeyHelper.GetTenantKey("tenant-123", "settings");
// Result: "saas:tenant:tenant-123:settings"
```

**Versioned Tenant Key (for Event-Driven Invalidation)**
```csharp
var version = versionStore.GetVersion("tenant-123", "products");
var versionedKey = TenantCacheKeyHelper.GetVersionedTenantKey("tenant-123", version, "product-42", "products");
// Result: "saas:tenant:tenant-123:v1:products:product-42"
```

The `RedisCacheService` automatically prepends `CacheOptions.KeyPrefix` (default: `"saas:"`) to every key. Ensure your key helpers and `KeyPrefix` setting are properly aligned to avoid conflicts.

## Redis Implementation Details

Understanding how Redis entries are managed:

**Serialization**
- Uses `System.Text.Json` with `JsonSerializerDefaults.Web` for consistent web API behavior
- Automatically serializes/deserializes complex objects

**Time-To-Live (TTL)**
- Determined by the `expiration` parameter or falls back to `CacheOptions.DefaultExpiration`
- Absolute expiration sets a fixed TTL
- Sliding expiration is stored as a fixed TTL in Redis

**Cache Miss Behavior**
- `GetAsync` returns `default` or `null` when a key doesn't exist
- No exceptions are thrown for missing keys

**Deserialization Errors**
- Treated as cache misses
- Corrupted keys are automatically deleted using the Redis `DEL` command
- Ensures cache integrity by removing invalid data

**Remove Operations**
- Executes a Redis `DEL` command to immediately remove the entry
- Returns successfully even if the key doesn't exist

**Connection Multiplexer**
- Single shared `IConnectionMultiplexer` instance per DI container
- Efficiently manages connections and reduces overhead
- Thread-safe and designed for high-concurrency scenarios

## Related Packages

- **[SaasSuite.Caching](../SaasSuite.Caching/README.md)**: Core abstractions including `ICacheService`, `CacheOptions`, and `TenantCacheKeyHelper`

## Dependencies

- **StackExchange.Redis**: Industry-standard Redis client for .NET

## License

This package is licensed under the [Apache License 2.0](https://www.apache.org/licenses/LICENSE-2.0).
See the [LICENSE](../../LICENSE) file in the repository root for details.