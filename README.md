# BitCrafts Certificates

[![NuGet - Abstractions](https://img.shields.io/badge/NuGet-Abstractions-blue)](https://github.com/BitCrafts/certificates/packages)
[![NuGet - Linux](https://img.shields.io/badge/NuGet-Linux-blue)](https://github.com/BitCrafts/certificates/packages)
[![.NET 8.0](https://img.shields.io/badge/.NET-8.0-purple)](https://dotnet.microsoft.com/)
[![License: AGPL-3.0](https://img.shields.io/badge/License-AGPL%203.0-blue.svg)](LICENSE)

Certificate Authority management suite for intranet and home lab environments with Linux-first design and RHEL administrator GUI workflow.

## Purpose

- Provide a lightweight CA for intranet/home lab use
- Issue and manage server and client certificates (ECDSA P-256)
- Support RHEL administrator workflows via GUI application
- Deploy certificates securely to infrastructure via SSH or network filesystems
- Store certificates encrypted in database using GPG
- Designed for personal/home/intranet deployment; not for public-facing CAs

## Key Features

- **Modular Architecture**: Separate library packages for abstractions and Linux implementations
- **GUI Application**: Avalonia-based cross-platform GUI for RHEL administrators
- **Encrypted Storage**: Certificates stored encrypted in database using user's GPG key
- **Flexible Deployment**: Deploy via SSH or to mounted network filesystems (NFS, GlusterFS, CephFS)
- **Linux-Native**: Uses OpenSSL, GnuPG, and OpenSSH for certificate operations
- **Security-Focused**: Least-privilege file operations, encrypted storage, audit logging
- **NuGet Packages**: Published to GitHub Packages for easy consumption

## Architecture

The application is structured into separate library packages:

### BitCrafts.Certificates.Abstractions
- **Purpose**: Platform-agnostic domain models, interfaces, and contracts
- **Contains**: 
  - Interfaces: `ICertificateService`, `ICertificateRepository`, `IEncryptionService`, `IDeploymentWorkflowService`, `ITargetResolver`, `ISshClientFactory`, `IFileSystemDeployer`
  - Domain models: `Certificate`, `CertificateMetadata`, `DeploymentWorkflow`, `DeploymentTarget`, `UserKeyReference`
  - Results, exceptions, and DTOs
- **Package**: Available on GitHub Packages

### BitCrafts.Certificates.Linux
- **Purpose**: Linux-specific implementations using native tools
- **Contains**:
  - `CertificateServiceLinux`: Certificate creation with OpenSSL
  - `EncryptionServiceGpg`: GPG-based encryption/decryption
  - `SshClientFactoryOpenSsh`: SSH deployment using system ssh/scp
  - `FileSystemDeployerLinux`: Deploy to local/NFS/Gluster/Ceph with proper permissions
  - `DeploymentWorkflowServiceLinux`: Orchestrate deployment workflows
  - `TargetResolverLinux`: Resolve and validate deployment targets
  - Process wrappers for openssl, gpg, ssh with security validation
- **Package**: Available on GitHub Packages

### BitCrafts.Certificates.Avalonia
- **Purpose**: Cross-platform GUI application for certificate management
- **Target**: RHEL administrators running GUI workstation
- **Features**:
  - Create and manage certificates
  - Configure and execute deployment workflows
  - Encrypted certificate storage with GPG
  - SSH and FileSystem deployment configuration

See [docs/DEPLOYMENT_WORKFLOWS.md](docs/DEPLOYMENT_WORKFLOWS.md) for deployment details.

## Certificate Deployment Workflows

The application supports two deployment workflows:

### SSH Deployment
Deploy certificates to remote servers via SSH/SCP:
- Uses system OpenSSH with public key authentication
- Supports setting file ownership (chown) and permissions (chmod)
- Automatically transfers and configures certificates on target servers

Example target configuration:
```csharp
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
```

### Network Filesystem Deployment
Deploy certificates to mounted network shares:
- Supports local filesystems, NFS, GlusterFS, and CephFS
- Applies proper file ownership and permissions
- Ideal for clustered environments with shared storage

Example target configuration:
```csharp
var target = new DeploymentTarget
{
    HostnameOrIp = "local",
    DestinationPath = "/mnt/nfs/certs",
    Owner = "www-data",
    Group = "www-data",
    Permissions = "0644"
};
```

See [docs/DEPLOYMENT_WORKFLOWS.md](docs/DEPLOYMENT_WORKFLOWS.md) for complete documentation.

## Encrypted Certificate Storage

Certificates are stored encrypted in the database:
- Uses user's GPG key for encryption (public key) and decryption (private key)
- Certificate data encrypted before database persistence
- Decryption occurs transiently during deployment only
- No plaintext certificate files written to disk during creation

## Using the Packages

### Installation

Add the GitHub Packages source to your NuGet configuration:

```bash
dotnet nuget add source https://nuget.pkg.github.com/BitCrafts/index.json \
  --name github \
  --username YOUR_GITHUB_USERNAME \
  --password YOUR_GITHUB_TOKEN \
  --store-password-in-clear-text
```

Install the packages:

```bash
# Install abstractions
dotnet add package BitCrafts.Certificates.Abstractions

# Install Linux implementations
dotnet add package BitCrafts.Certificates.Linux
```

### Example Usage

```csharp
using BitCrafts.Certificates.Abstractions.Interfaces;
using BitCrafts.Certificates.Abstractions.Models;
using BitCrafts.Certificates.Linux.Services;

// Create services
var certificateService = new CertificateServiceLinux();
var encryptionService = new EncryptionServiceGpg();

// Create a certificate
var metadata = new CertificateMetadata
{
    CommonName = "server.example.com",
    Type = CertificateType.Server,
    ValidityDays = 365,
    SubjectAlternativeNames = new List<string> { "*.example.com" }
};

var result = await certificateService.CreateCertificateAsync(metadata);
if (result.Success && result.Certificate != null)
{
    // Encrypt certificate data
    var gpgKey = new UserKeyReference { KeyId = "user@example.com" };
    var encryptedData = await encryptionService.EncryptAsync(
        result.Certificate.EncryptedData!,
        gpgKey
    );
    
    // Store encrypted data in database
    result.Certificate.EncryptedData = encryptedData;
    await repository.SaveAsync(result.Certificate);
}
```

## Running the GUI Application on RHEL

### Prerequisites

- Red Hat Enterprise Linux 8 or later (or compatible: AlmaLinux, Rocky Linux)
- .NET 8.0 Runtime
- GnuPG (gpg) installed and configured with user's key
- OpenSSL for certificate operations
- OpenSSH client for SSH deployments (optional)
- Access to mounted network filesystems for filesystem deployments (optional)

### Setup

1. **Install .NET 8.0 Runtime:**
```bash
sudo dnf install dotnet-runtime-8.0
```

2. **Configure GPG Key:**
```bash
# Generate a GPG key if you don't have one
gpg --gen-key

# List your keys to get the key ID
gpg --list-keys
```

3. **Configure Database Connection:**
The application uses SQLite by default. Configure the data directory:
```bash
export BITCRAFTS_DATA_DIR="/var/lib/bitcrafts/certificates"
export BITCRAFTS_DB_PATH="/var/lib/bitcrafts/certificates/certificates.db"
```

4. **Run the GUI Application:**
```bash
dotnet BitCrafts.Certificates.Avalonia.dll
```

### Configuring Deployment Workflows

See [docs/DEPLOYMENT_WORKFLOWS.md](docs/DEPLOYMENT_WORKFLOWS.md) for detailed instructions on:
- Setting up SSH key-based authentication
- Mounting network filesystems (NFS, GlusterFS, CephFS)
- Configuring deployment targets
- Security best practices

## Building and Testing

```bash
# Build all projects
dotnet build

# Run unit tests
dotnet test

# Build specific package
dotnet build BitCrafts.Certificates.Abstractions/BitCrafts.Certificates.Abstractions.csproj
dotnet build BitCrafts.Certificates.Linux/BitCrafts.Certificates.Linux.csproj
```

## Security

- **Encrypted Storage**: Certificates encrypted with GPG before database storage
- **Least Privilege**: File operations use explicit chmod/chown with minimal permissions
- **Input Validation**: All external command parameters validated and escaped
- **No Plaintext Files**: Certificate data never written unencrypted to disk during creation
- **Audit Logging**: All operations logged without exposing sensitive key material
- **Secure Deployment**: SSH uses key-based authentication; filesystem deployments apply proper permissions

## Technology Stack

- **.NET 8.0** - Runtime platform
- **Avalonia 11.x** - Cross-platform UI framework
- **SQLite** - Default database (replaceable with PostgreSQL, MySQL, etc.)
- **OpenSSL** - Certificate generation (via process wrapper)
- **GnuPG** - Certificate encryption/decryption
- **OpenSSH** - Remote deployment
- **xUnit + FluentAssertions** - Testing

## Project Structure

```
BitCrafts.Certificates/
├── BitCrafts.Certificates.Abstractions/     # Platform-agnostic interfaces and models
├── BitCrafts.Certificates.Linux/            # Linux-specific implementations
├── BitCrafts.Certificates.Avalonia/         # GUI application
├── BitCrafts.Certificates.Abstractions.Tests/  # Abstractions unit tests
├── BitCrafts.Certificates.Linux.Tests/      # Linux implementation tests
├── docs/                                     # Documentation
│   └── DEPLOYMENT_WORKFLOWS.md              # Deployment workflow guide
├── .github/workflows/                        # CI/CD workflows
│   ├── publish-abstractions.yml
│   └── publish-linux.yml
└── nuget.config                             # NuGet package sources
```

## Publishing Packages

Packages are automatically published to GitHub Packages when tags are pushed:

```bash
# Publish Abstractions package
git tag abstractions-v1.0.0
git push origin abstractions-v1.0.0

# Publish Linux package
git tag linux-v1.0.0
git push origin linux-v1.0.0
```

Or manually trigger via GitHub Actions workflow dispatch.

## Contributing

This project is designed for intranet/home lab use. Contributions welcome for:
- Additional deployment methods
- Platform-specific implementations (Windows, macOS)
- Database provider implementations
- Enhanced security features
- Bug fixes and documentation improvements

## Future Enhancements

- Windows and macOS implementations
- Additional database providers (PostgreSQL, MySQL, SQL Server)
- Kubernetes Secret deployment support
- Automated certificate rotation
- Web-based management interface (optional)
- Integration with external CAs (Let's Encrypt, etc.)

## License

This project is licensed under AGPLv3 (Affero GPL v3). See `LICENSE` for the full text.


