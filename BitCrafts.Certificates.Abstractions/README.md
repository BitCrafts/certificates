# BitCrafts.Certificates.Abstractions

Platform-agnostic domain models, interfaces, and contracts for certificate generation, storage, encryption, and deployment workflows.

## Overview

This package provides the core abstractions for the BitCrafts Certificates system. It contains no platform-specific implementation code, making it suitable for use across different operating systems and deployment scenarios.

## Installation

```bash
dotnet add package BitCrafts.Certificates.Abstractions
```

From GitHub Packages:
```bash
dotnet nuget add source https://nuget.pkg.github.com/BitCrafts/index.json \
  --name github \
  --username YOUR_GITHUB_USERNAME \
  --password YOUR_GITHUB_TOKEN
  
dotnet add package BitCrafts.Certificates.Abstractions
```

## Key Components

### Interfaces

- **ICertificateService**: Create and manage certificates
- **ICertificateRepository**: Persist and retrieve certificates
- **IEncryptionService**: Encrypt/decrypt certificate data
- **IDeploymentWorkflowService**: Manage deployment workflows
- **ITargetResolver**: Resolve and validate deployment targets
- **ISshClientFactory**: Create SSH clients for remote deployments
- **IFileSystemDeployer**: Deploy to local or network filesystems

### Domain Models

- **Certificate**: Certificate entity with metadata and encrypted data
- **CertificateMetadata**: Certificate properties (CN, SANs, validity, etc.)
- **DeploymentWorkflow**: Workflow configuration (SSH or FileSystem)
- **DeploymentTarget**: Target configuration (hostname, path, permissions)
- **UserKeyReference**: Reference to user's GPG key

### Result Types

- **CertificateResult**: Result of certificate operations
- **DeploymentResult**: Result of deployment operations
- **ValidationResult**: Result of validation operations

### Exceptions

- **CertificateException**: Base certificate exception
- **CertificateCreationException**: Certificate creation errors
- **EncryptionException**: Encryption/decryption errors
- **DeploymentException**: Deployment errors
- **ValidationException**: Validation errors

## Usage

This package is typically used in conjunction with a platform-specific implementation package such as `BitCrafts.Certificates.Linux`.

```csharp
using BitCrafts.Certificates.Abstractions.Interfaces;
using BitCrafts.Certificates.Abstractions.Models;

// Use interfaces to remain platform-agnostic
public class MyCertificateManager
{
    private readonly ICertificateService _certificateService;
    private readonly IEncryptionService _encryptionService;
    
    public MyCertificateManager(
        ICertificateService certificateService,
        IEncryptionService encryptionService)
    {
        _certificateService = certificateService;
        _encryptionService = encryptionService;
    }
    
    public async Task<Certificate> CreateSecureCertificateAsync(
        CertificateMetadata metadata,
        UserKeyReference gpgKey)
    {
        // Create certificate
        var result = await _certificateService.CreateCertificateAsync(metadata);
        
        if (!result.Success || result.Certificate == null)
            throw new Exception(result.ErrorMessage);
            
        // Encrypt certificate data
        var encryptedData = await _encryptionService.EncryptAsync(
            result.Certificate.EncryptedData!,
            gpgKey);
            
        result.Certificate.EncryptedData = encryptedData;
        return result.Certificate;
    }
}
```

## License

AGPL-3.0-only

## Links

- [GitHub Repository](https://github.com/BitCrafts/certificates)
- [Documentation](https://github.com/BitCrafts/certificates/tree/main/docs)
- [Linux Implementation](https://github.com/BitCrafts/certificates/tree/main/BitCrafts.Certificates.Linux)
