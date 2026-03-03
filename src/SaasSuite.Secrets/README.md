# SaasSuite.Secrets

[![NuGet](https://img.shields.io/nuget/v/SaasSuite.Secrets.svg)](https://www.nuget.org/packages/SaasSuite.Secrets)
[![License: Apache-2.0](https://img.shields.io/badge/License-Apache%202.0-blue.svg)](LICENSE)
[![Platform](https://img.shields.io/badge/.NET-6%2B-purple.svg)](https://dotnet.microsoft.com/)

Abstractions and contracts for tenant-aware secret management in multi-tenant SaaS applications.

## Overview

`SaasSuite.Secrets` provides the foundational interfaces and models for implementing secure, tenant-scoped secret management. This package contains no provider-specific dependencies, making it ideal as a shared contract between your application and various secret store implementations.

## Features

- **ISecretStore**: Core interface for retrieving, setting, and managing secrets
- **Tenant Scoping**: Built-in support for tenant-specific secret namespaces
- **Secret Rotation**: Interfaces and events for handling secret rotation workflows
- **Provider Agnostic**: No dependencies on specific cloud providers

## Installation

```bash
dotnet add package SaasSuite.Secrets
```

## Usage

### Define Secret Operations

```csharp
using SaasSuite.Secrets.Interfaces;

public class MyService
{
    private readonly ISecretStore _secretStore;
    
    public MyService(ISecretStore secretStore)
    {
        _secretStore = secretStore;
    }
    
    public async Task<string?> GetApiKeyAsync()
    {
        // Automatically scoped to current tenant
        return await _secretStore.GetSecretAsync("api-key");
    }
}
```

### Tenant-Scoped Secret Names

The package includes `SecretNameHelper` for generating tenant-scoped names:

```csharp
using SaasSuite.Secrets.Helpers;

var tenantId = new TenantId("tenant-123");
var secretName = SecretNameHelper.GetTenantScopedName(
    tenantId, 
    "api-key", 
    "tenants/{tenantId}/"
);
// Result: "tenants/tenant-123/api-key"
```

## Implementing a Secret Store

To implement a custom secret store, implement the `ISecretStore` interface:

```csharp
public class MySecretStore : ISecretStore
{
    public async Task<string?> GetSecretAsync(string secretName, CancellationToken cancellationToken)
    {
        // Your implementation
    }
    
    // Implement other methods...
}
```

## Related Packages

- **[SaasSuite.Secrets.AzureKeyVault](../SaasSuite.Secrets.AzureKeyVault/README.md)**: Azure Key Vault implementation
- **[SaasSuite.Secrets.AWS](../SaasSuite.Secrets.AWS/README.md)**: AWS Secrets Manager implementation

## License

Apache-2.0
