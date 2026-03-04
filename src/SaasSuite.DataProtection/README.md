# SaasSuite.DataProtection

[![NuGet](https://img.shields.io/nuget/v/SaasSuite.DataProtection.svg)](https://www.nuget.org/packages/SaasSuite.DataProtection)
[![License: Apache-2.0](https://img.shields.io/badge/License-Apache%202.0-blue.svg)](LICENSE)
[![Platform](https://img.shields.io/badge/.NET-6%2B-purple.svg)](https://dotnet.microsoft.com/)

Encryption, key management, and data residency helpers for multi-tenant SaaS applications.

## Overview

`SaasSuite.DataProtection` provides tenant-aware encryption and key management capabilities. It implements envelope encryption patterns, tenant-specific key isolation, and data residency policy enforcement.

## Features

- **Envelope Encryption**: Secure data encryption using key encryption keys (KEK) and data encryption keys (DEK)
- **Tenant Key Isolation**: Each tenant gets isolated encryption keys
- **Key Rotation Support**: Interfaces for implementing key rotation workflows
- **Data Residency**: Policy enforcement for data location requirements
- **Integration with Secret Stores**: Uses `SaasSuite.Secrets` for secure key storage

## Installation

```bash
dotnet add package SaasSuite.DataProtection
dotnet add package SaasSuite.Core
dotnet add package SaasSuite.Secrets
```

## Usage

### Register Services

```csharp
// Use in-memory implementations (for testing/demo)
// WARNING: Not suitable for production
services.AddSaasDataProtection();

// Or provide custom implementations
services.AddSaasDataProtection<
    MyKeyEncryptionKeyProvider,
    MyTenantKeyProvider>();
```

### Envelope Encryption

```csharp
using SaasSuite.Core;
using SaasSuite.DataProtection.Helpers;
using SaasSuite.DataProtection.Interfaces;

public class DataEncryptionService
{
    private readonly ITenantKeyProvider _keyProvider;
    
    public DataEncryptionService(ITenantKeyProvider keyProvider)
    {
        _keyProvider = keyProvider;
    }
    
    public async Task<byte[]> EncryptSensitiveDataAsync(
        string tenantId, 
        byte[] plaintext)
    {
        // Get tenant-specific encryption key
        var key = await _keyProvider.GetKeyAsync(new TenantId(tenantId));
        
        // Encrypt using envelope encryption
        return EnvelopeEncryptionHelper.Encrypt(plaintext, key);
    }
    
    public async Task<byte[]> DecryptSensitiveDataAsync(
        string tenantId, 
        byte[] ciphertext)
    {
        // Get tenant-specific encryption key
        var key = await _keyProvider.GetKeyAsync(new TenantId(tenantId));
        
        // Decrypt
        return EnvelopeEncryptionHelper.Decrypt(ciphertext, key);
    }
}
```

### Key Rotation

```csharp
using SaasSuite.Core;
using SaasSuite.DataProtection.Helpers;
using SaasSuite.DataProtection.Interfaces;

public class KeyRotationService
{
    private readonly IKeyEncryptionKeyProvider _kekProvider;
    private readonly ITenantKeyProvider _tenantKeyProvider;
    
    public async Task RotateTenantKeyAsync(string tenantId)
    {
        // Get current key ID
        var currentKeyId = await _tenantKeyProvider.GetKeyIdAsync(
            new TenantId(tenantId));
        
        // Generate new data encryption key
        var newDek = EnvelopeEncryptionHelper.GenerateDataEncryptionKey();
        
        // Encrypt new DEK with master key
        var encryptedDek = await _kekProvider.EncryptKeyAsync(
            newDek, 
            "master-key-001");
        
        // Store encrypted DEK (implementation specific)
        // Re-encrypt data with new key (implementation specific)
    }
}
```

### Integration with Secret Stores

```csharp
using SaasSuite.Core;
using SaasSuite.Secrets.Interfaces;

public class SecretStoreKeyProvider : ITenantKeyProvider
{
    private readonly ISecretStore _secretStore;
    
    public SecretStoreKeyProvider(ISecretStore secretStore)
    {
        _secretStore = secretStore;
    }
    
    public async Task<byte[]> GetKeyAsync(
        TenantId tenantId, 
        CancellationToken cancellationToken)
    {
        // Retrieve encrypted key from secret store
        var encryptedKeyBase64 = await _secretStore.GetSecretAsync(
            "encryption-key", 
            cancellationToken);
        
        if (encryptedKeyBase64 == null)
        {
            throw new InvalidOperationException(
                $"Encryption key not found for tenant {tenantId}");
        }
        
        return Convert.FromBase64String(encryptedKeyBase64);
    }
    
    public async Task<string> GetKeyIdAsync(
        TenantId tenantId, 
        CancellationToken cancellationToken)
    {
        return $"tenant-{tenantId.Value}-key";
    }
}
```

### Data Residency Policy

```csharp
using SaasSuite.Core;
using SaasSuite.DataProtection.Interfaces;

public class ConfigurationDataResidencyPolicy : IDataResidencyPolicy
{
    public Task<string> GetRequiredRegionAsync(
        TenantId tenantId, 
        CancellationToken cancellationToken)
    {
        // Look up tenant's required region from configuration/database
        // For EU tenants: "eu-west-1"
        // For US tenants: "us-east-1"
        return Task.FromResult("us-east-1");
    }
    
    public Task<bool> IsRegionAllowedAsync(
        TenantId tenantId, 
        string region, 
        CancellationToken cancellationToken)
    {
        // Validate if operation is allowed in the specified region
        var requiredRegion = await GetRequiredRegionAsync(
            tenantId, 
            cancellationToken);
        
        return region == requiredRegion;
    }
}
```

## Security Best Practices

1. **Key Storage**: Never store encryption keys in code or configuration files
2. **Key Rotation**: Implement regular key rotation policies
3. **Access Control**: Limit access to key management operations
4. **Audit Logging**: Log all key access and rotation events
5. **Production Keys**: Use hardware security modules (HSM) or cloud KMS for production
6. **Separation**: Keep master keys separate from data encryption keys

## Production Implementations

For production use, implement the interfaces with:

- **Azure Key Vault**: For KEK storage and key operations
- **AWS KMS**: For key management and envelope encryption
- **HashiCorp Vault**: For multi-cloud key management
- **Hardware Security Modules (HSM)**: For maximum security

Example with Azure Key Vault:

```csharp
public class AzureKeyVaultKekProvider : IKeyEncryptionKeyProvider
{
    private readonly KeyClient _keyClient;
    
    public async Task<byte[]> EncryptKeyAsync(
        byte[] plainKey, 
        string keyId, 
        CancellationToken cancellationToken)
    {
        var cryptoClient = _keyClient.GetCryptographyClient(keyId);
        var result = await cryptoClient.EncryptAsync(
            EncryptionAlgorithm.RsaOaep256, 
            plainKey, 
            cancellationToken);
        
        return result.Ciphertext;
    }
    
    // Implement other methods...
}
```

## Related Packages

- **[SaasSuite.Secrets](../SaasSuite.Secrets/README.md)**: For secure key storage
- **[SaasSuite.Compliance](../SaasSuite.Compliance/README.md)**: For data protection compliance
- **[SaasSuite.Audit](../SaasSuite.Audit/README.md)**: For key access auditing

## License

This package is licensed under the [Apache License 2.0](https://www.apache.org/licenses/LICENSE-2.0).
See the [LICENSE](../../LICENSE) file in the repository root for details.