# SaasSuite.Discovery

[![NuGet](https://img.shields.io/nuget/v/SaasSuite.Discovery.svg)](https://www.nuget.org/packages/SaasSuite.Discovery)
[![License: Apache-2.0](https://img.shields.io/badge/License-Apache%202.0-blue.svg)](LICENSE)
[![Platform](https://img.shields.io/badge/.NET-6%2B-purple.svg)](https://dotnet.microsoft.com/)

Service discovery and manifest generation for SaasSuite tenant services.

## Overview

This library provides automatic service discovery and registration for multi-tenant SaaS applications. It uses reflection-based discovery to find types marked with `TenantServiceAttribute` and provides both attribute-based and fluent API registration patterns.

## Features

- **Automatic Service Discovery**: Discovers tenant services from assemblies marked with `TenantServiceAttribute`
- **Fluent Discovery API**: Configure complex service registration scenarios with a discovery-focused API
- **Tenant Scopes**: Support for Global, Request, and SingletonPerTenant scopes
- **Tenant Predicates**: Conditional service registration based on tenant context
- **Decorator Support**: Tenant-conditional decorators for scoped/transient services
- **Lifetime Management**: Respects service lifetime (Singleton, Scoped, Transient)
- **Assembly Filtering**: Discover from specific assemblies or all loaded assemblies
- **Namespace Filtering**: Filter types by namespace patterns
- **Interface Registration**: Automatically registers services with their implemented interfaces
- **Concrete Type Registration**: Optionally registers services as themselves
- **Discovery Manifests**: Generate JSON manifests of discovered services via CLI

## Installation

```bash
dotnet add package SaasSuite.Discovery
```

## Usage

### Attribute-Based Discovery (Simple)

Mark services with `TenantServiceAttribute` for automatic discovery:

```csharp
using SaasSuite.Core;
using SaasSuite.Core.Enumerations;
using Microsoft.Extensions.DependencyInjection;

[TenantService(ServiceLifetime.Scoped, TenantScope.Request)]
public class MyTenantService : IMyTenantService
{
    // Service implementation
}

[TenantService(ServiceLifetime.Singleton, TenantScope.SingletonPerTenant)]
public class CachedTenantService : ICachedTenantService
{
    // Cached per tenant across requests
}
```

Register services using attribute-based discovery:

```csharp
using SaasSuite.Discovery;

// In your Startup.cs or Program.cs
services.AddSaasDiscovery();
```

### Fluent API (Advanced Scenarios)

Use the fluent API for more control:

```csharp
services.AddSaasDiscoveryBuilder(builder => builder
    .FromAssemblyOf<MyService>()
    .IncludeTypes(type => type.Name.EndsWith("Service"))
    .AsInterfaces()
    .WithScopedLifetime()
    .WithRequestScope()
    .Activate());
```

### Configuration Options

Configure discovery behavior with options:

```csharp
services.AddSaasDiscovery(options =>
{
    // Discover from specific assemblies
    options.Assemblies.Add(typeof(MyService).Assembly);
    
    // Filter by namespace
    options.NamespaceFilters.Add("MyApp.Services");
    options.NamespaceFilters.Add("MyApp.Repositories");
    
    // Control registration behavior
    options.RegisterInterfaces = true;      // Register with interfaces (default: true)
    options.RegisterConcreteTypes = true;   // Register concrete types (default: true)
});
```

### Tenant Scopes

Three tenant scope options are supported:

- **Global**: Service is not tenant-specific (standard DI behavior)
- **Request**: Service is resolved per request and tenant-aware
- **SingletonPerTenant**: Service is cached as a singleton per tenant across requests

```csharp
// Request-scoped, tenant-aware service
[TenantService(ServiceLifetime.Scoped, TenantScope.Request)]
public class RequestService : IRequestService { }

// Cached per tenant (singleton per tenant)
[TenantService(ServiceLifetime.Scoped, TenantScope.SingletonPerTenant)]
public class TenantCacheService : ITenantCacheService { }
```

### Tenant Predicates

Conditionally register services based on tenant context:

```csharp
services.AddSaasDiscoveryBuilder(builder => builder
    .FromAssemblyOf<PremiumService>()
    .IncludeTypes(type => type.Name.EndsWith("PremiumService"))
    .AsInterfaces()
    .WithScopedLifetime()
    .WhenTenant(tenant => tenant.TenantInfo?.Tier == "Premium")
    .Activate());
```

### Decorators (v1 Limitation)

Tenant-conditional decorators are supported for **scoped and transient services only**. Decorators for singleton services are not supported in v1.

```csharp
// Supported: Decorator for scoped/transient service
services.AddSaasDiscoveryBuilder(builder => builder
    .FromAssemblyOf<MyService>()
    .IncludeTypes(type => type == typeof(MyService))
    .AsInterfaces()
    .WithScopedLifetime()  // Scoped - decorators supported
    .Activate());

// NOT supported in v1: Decorator for singleton service
// Singleton decorators will be added in a future version
```

## Discovery Manifests

Generate a JSON manifest of discovered services using the CLI:

```bash
# Write manifest to stdout
dotnet run --project SaasSuite.Cli discovery-manifest

# Write manifest to file
dotnet run --project SaasSuite.Cli discovery-manifest --output manifest.json

# Compact JSON output
dotnet run --project SaasSuite.Cli discovery-manifest --pretty false
```

Example manifest output:

```json
{
  "generatedAt": "2026-02-14T10:30:00Z",
  "totalRegistrations": 3,
  "registrations": [
    {
      "implementationType": "MyApp.Services.TenantService",
      "serviceTypes": ["MyApp.Services.ITenantService"],
      "lifetime": "Scoped",
      "tenantScope": "Request",
      "hasTenantPredicate": false,
      "source": "Attribute: MyApp.MyApp.Services.TenantService"
    }
  ],
  "summary": {
    "byLifetime": { "Scoped": 2, "Singleton": 1 },
    "byTenantScope": { "Request": 2, "Global": 1 },
    "withTenantPredicates": 0,
    "withDecorators": 0
  }
}
```

## Architecture

### Discovery → Manifest → Activation

The library follows a clear separation:

1. **Discovery**: Reflection-based discovery finds services in assemblies
2. **Manifest**: A manifest/list of discovered registrations is built
3. **Activation**: The manifest is applied to `IServiceCollection`

The convenience method `AddSaasDiscovery()` runs all three phases automatically. For advanced scenarios, use `DiscoveryBuilder` to customize each phase.

### Discovery vs. Core

Discovery is an **optional module** for consumer applications. It's not part of the core SaasSuite framework. Applications can:

- Use discovery for automatic registration (recommended for most apps)
- Manually register services without discovery
- Mix both approaches

## How It Works

1. **Assembly Discovery**: The library searches specified assemblies (or all loaded assemblies)
2. **Type Filtering**: Applies namespace filters and attribute checks
3. **Service Type Resolution**: Determines which service types to register (interfaces, concrete types, etc.)
4. **Registration**: Creates `ServiceDescriptor` entries and adds them to `IServiceCollection`
5. **Manifest Generation**: Tracks all registrations for reporting and debugging

## API Reference

### Extension Methods

- `AddSaasDiscovery()` - Simple attribute-based discovery
- `AddSaasDiscoveryBuilder()` - Fluent API for advanced scenarios

### Builder Methods

- `FromAssembly()` / `FromAssemblyOf<T>()` - Specify assemblies
- `IncludeTypes()` - Filter types to discover
- `WithAttribute<T>()` - Filter by attribute
- `InNamespaces()` - Filter by namespace
- `AsInterfaces()` / `AsConcreteTypes()` / `AsMatchingInterface()` - Service type selection
- `WithScopedLifetime()` / `WithSingletonLifetime()` / `WithTransientLifetime()` - Lifetime
- `WithRequestScope()` / `WithGlobalScope()` / `WithSingletonPerTenantScope()` - Tenant scope
- `WhenTenant()` - Conditional registration
- `Activate()` - Terminal operation that applies discovery
- `BuildManifest()` - Generate manifest without activation

## Related Packages

- **[SaasSuite.Core](../SaasSuite.Core/README.md)**: Core multi-tenancy abstractions

## License

This package is licensed under the [Apache License 2.0](https://www.apache.org/licenses/LICENSE-2.0).
See the [LICENSE](../../LICENSE) file in the repository root for details.