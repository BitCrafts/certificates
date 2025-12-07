# BitCrafts.Certificates.Linux

Linux-specific implementations for certificate management using OpenSSL, GnuPG, OpenSSH, and standard Linux filesystem semantics.

## Overview

This package provides Linux-native implementations of the BitCrafts.Certificates.Abstractions interfaces. It leverages system tools (openssl, gpg, ssh) for certificate operations, encryption, and deployment.

## Installation

```bash
dotnet add package BitCrafts.Certificates.Linux
```

From GitHub Packages:
```bash
dotnet nuget add source https://nuget.pkg.github.com/BitCrafts/index.json \
  --name github \
  --username YOUR_GITHUB_USERNAME \
  --password YOUR_GITHUB_TOKEN
  
dotnet add package BitCrafts.Certificates.Linux
```

## Prerequisites

The following tools must be installed on your Linux system:

- **OpenSSL**: For certificate generation and management
- **GnuPG (gpg)**: For encryption and decryption
- **OpenSSH client**: For SSH-based deployments (optional)
- **.NET 8.0 Runtime**: For running the application

Install on RHEL/AlmaLinux/Rocky:
```bash
sudo dnf install openssl gnupg2 openssh-clients dotnet-runtime-8.0
```

Install on Ubuntu/Debian:
```bash
sudo apt install openssl gnupg openssh-client dotnet-runtime-8.0
```

## Key Components

### Services

- **CertificateServiceLinux**: Creates certificates using OpenSSL
- **EncryptionServiceGpg**: Encrypts/decrypts using GPG
- **SshClientFactoryOpenSsh**: Creates SSH clients for remote deployments
- **FileSystemDeployerLinux**: Deploys to local/NFS/Gluster/Ceph filesystems
- **DeploymentWorkflowServiceLinux**: Orchestrates deployment workflows
- **TargetResolverLinux**: Resolves hostnames and validates connectivity

### Process Wrappers

- **OpenSslWrapper**: Secure wrapper around openssl command
- **GpgWrapper**: Secure wrapper around gpg command
- **SshWrapper**: Secure wrapper around ssh/scp commands
- **ProcessWrapperBase**: Base class with security validation

All process wrappers include:
- Input validation and sanitization
- Path escaping to prevent injection
- Proper error handling
- Secure temporary file management

## Usage

### Basic Certificate Creation

```csharp
using BitCrafts.Certificates.Abstractions.Models;
using BitCrafts.Certificates.Linux.Services;

var certificateService = new CertificateServiceLinux();

var metadata = new CertificateMetadata
{
    CommonName = "server.example.com",
    Type = CertificateType.Server,
    ValidityDays = 365,
    SubjectAlternativeNames = new List<string> { "*.example.com" },
    IpAddresses = new List<string> { "192.168.1.100" }
};

var result = await certificateService.CreateCertificateAsync(metadata);

if (result.Success)
{
    Console.WriteLine("Certificate created successfully!");
}
```

### GPG Encryption

```csharp
using BitCrafts.Certificates.Linux.Services;

var encryptionService = new EncryptionServiceGpg();

var gpgKey = new UserKeyReference 
{ 
    KeyId = "user@example.com" 
};

// Encrypt certificate data
var encryptedData = await encryptionService.EncryptAsync(
    certificateData,
    gpgKey);

// Decrypt when needed
var decryptedData = await encryptionService.DecryptAsync(
    encryptedData,
    gpgKey);
```

### SSH Deployment

```csharp
using BitCrafts.Certificates.Linux.Services;

var sshFactory = new SshClientFactoryOpenSsh();

var target = new DeploymentTarget
{
    HostnameOrIp = "server.example.com",
    DestinationPath = "/etc/ssl/certs",
    Username = "deploy",
    Port = 22,
    PrivateKeyPath = "/home/admin/.ssh/id_rsa",
    Owner = "nginx",
    Group = "nginx",
    Permissions = "0600"
};

using var sshClient = sshFactory.CreateClient(target);
await sshClient.ConnectAsync();
await sshClient.UploadFileAsync(certificateData, "/etc/ssl/certs/server.pem");
await sshClient.SetPermissionsAsync("/etc/ssl/certs/server.pem", "0600");
```

### Filesystem Deployment

```csharp
using BitCrafts.Certificates.Linux.Services;

var deployer = new FileSystemDeployerLinux();

var target = new DeploymentTarget
{
    HostnameOrIp = "local",
    DestinationPath = "/mnt/nfs/certs",
    Owner = "www-data",
    Group = "www-data",
    Permissions = "0644"
};

var result = await deployer.DeployAsync(certificate, target);

if (result.Success)
{
    Console.WriteLine($"Deployed: {result.Message}");
}
```

### Complete Workflow

```csharp
using BitCrafts.Certificates.Linux.Services;

// Set up services
var certificateService = new CertificateServiceLinux();
var encryptionService = new EncryptionServiceGpg();
var repository = /* your ICertificateRepository implementation */;
var sshFactory = new SshClientFactoryOpenSsh();
var fileSystemDeployer = new FileSystemDeployerLinux();
var targetResolver = new TargetResolverLinux();

var workflowService = new DeploymentWorkflowServiceLinux(
    repository,
    encryptionService,
    sshFactory,
    fileSystemDeployer,
    targetResolver);

// Create workflow
var workflow = new DeploymentWorkflow
{
    Name = "Production Servers",
    Type = DeploymentWorkflowType.SSH,
    Targets = new List<DeploymentTarget>
    {
        new()
        {
            HostnameOrIp = "web1.example.com",
            DestinationPath = "/etc/nginx/ssl",
            Username = "deploy",
            PrivateKeyPath = "/home/admin/.ssh/deploy_rsa",
            Permissions = "0600"
        }
    }
};

// Test connectivity
var connectivityResult = await workflowService.TestConnectivityAsync(workflow);

// Execute deployment
if (connectivityResult.Success)
{
    var deploymentResult = await workflowService.ExecuteWorkflowAsync(
        workflow,
        certificate);
}
```

## Security Considerations

### File Permissions

- Private keys: 0600 (owner read/write only)
- Certificates: 0644 (owner read/write, others read)
- Directories: 0700 (owner access only)

### GPG

- Store private keys securely
- Use strong passphrases
- Regularly rotate keys
- Never commit keys to version control

### SSH

- Use key-based authentication only
- Protect SSH private keys (0600 permissions)
- Regularly rotate SSH keys
- Consider using SSH certificates for better management

### Command Injection Prevention

All process wrappers include:
- Parameter validation
- Path normalization
- No shell execution (ArgumentList used instead of shell strings)
- Proper escaping of all inputs

## Supported Network Filesystems

- **Local Filesystem**: Standard Linux filesystems (ext4, xfs, etc.)
- **NFS**: Network File System with optional Kerberos security
- **GlusterFS**: Distributed filesystem with SSL/TLS support
- **CephFS**: Distributed filesystem with CephX authentication

See [DEPLOYMENT_WORKFLOWS.md](https://github.com/BitCrafts/certificates/blob/main/docs/DEPLOYMENT_WORKFLOWS.md) for detailed configuration instructions.

## Testing

Run unit tests:
```bash
dotnet test BitCrafts.Certificates.Linux.Tests
```

## License

AGPL-3.0-only

## Links

- [GitHub Repository](https://github.com/BitCrafts/certificates)
- [Documentation](https://github.com/BitCrafts/certificates/tree/main/docs)
- [Abstractions Package](https://github.com/BitCrafts/certificates/tree/main/BitCrafts.Certificates.Abstractions)
