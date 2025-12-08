# BitCrafts Certificates

[![NuGet - Abstractions](https://img.shields.io/badge/NuGet-Abstractions-blue)](https://github.com/BitCrafts/certificates/packages)
[![NuGet - Linux](https://img.shields.io/badge/NuGet-Linux-blue)](https://github.com/BitCrafts/certificates/packages)
[![.NET 8.0](https://img.shields.io/badge/.NET-8.0-purple)](https://dotnet.microsoft.com/)
[![License: AGPL-3.0](https://img.shields.io/badge/License-AGPL%203.0-blue.svg)](LICENSE)

Certificate Authority management suite for intranet and home lab environments with cross-platform desktop GUI application.

## Purpose

- Provide a lightweight CA for intranet/home lab use
- Issue and manage server and client certificates (ECDSA P-256)
- Cross-platform desktop GUI using Avalonia (Windows, Linux, macOS)
- Deploy certificates securely to infrastructure via SSH or network filesystems
- Audit logging and certificate lifecycle management
- Designed for personal/home/intranet deployment; not for public-facing CAs

## Key Features

- **Cross-Platform GUI**: Avalonia-based desktop application for Windows, Linux, and macOS
- **Certificate Management**: Create, view, revoke, and download server/client certificates
- **Deployment Support**: Deploy certificates via SSH or network filesystems (NFS, GlusterFS, CephFS)
- **Clean Architecture**: Separated business logic, domain, and infrastructure layers
- **Modular Libraries**: Reusable NuGet packages for abstractions and platform-specific implementations
- **Security-Focused**: Least-privilege file operations, secure storage, audit logging
- **SQLite Database**: Lightweight certificate metadata storage

## Architecture

The application is structured into modular components:

### BitCrafts.Certificates (Business Logic Library)
- **Purpose**: Core business logic, domain models, and infrastructure
- **Contains**:
  - **Application Layer**: DTOs, interfaces, and application services
  - **Domain Layer**: Domain entities, value objects, and interfaces
  - **Infrastructure Layer**: Database, storage, PKI, and deployment implementations
  - **Data Layer**: Repository implementations and database access
  - **Services**: Audit logging, revocation management, CA services
- **Used By**: Avalonia GUI application

### BitCrafts.Certificates.Abstractions
- **Purpose**: Platform-agnostic domain models, interfaces, and contracts
- **Contains**: Platform-independent abstractions for certificate management
- **Package**: Available on GitHub Packages

### BitCrafts.Certificates.Linux
- **Purpose**: Linux-specific implementations using native tools
- **Contains**:
  - Certificate operations using OpenSSL
  - GPG-based encryption/decryption
  - SSH deployment using system ssh/scp
  - Network filesystem deployment support
- **Package**: Available on GitHub Packages

### BitCrafts.Certificates.Avalonia
- **Purpose**: Cross-platform desktop GUI application
- **Features**:
  - Certificate list view with filtering (all, server, client)
  - Create server/client certificates with custom parameters
  - View certificate details (subject, validity, status)
  - Revoke certificates with confirmation dialog
  - Download certificates as tar.gz archives
  - Deploy certificates via SSH or network filesystems
  - Setup wizard for initial configuration
- **Platforms**: Windows, Linux, macOS
- **Framework**: Avalonia 11.x with MVVM pattern
- **Package**: Available on GitHub Packages

## Certificate Deployment

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

## Running the Desktop Application

### Download

Download the latest release from the [GitHub Releases](https://github.com/BitCrafts/certificates/releases) page:
- **Linux**: `BitCrafts.Certificates.Avalonia-<version>-linux-x64.tar.gz`
- **Windows**: `BitCrafts.Certificates.Avalonia-<version>-win-x64.zip`
- **macOS**: `BitCrafts.Certificates.Avalonia-<version>-osx-x64.tar.gz`

### Prerequisites

- .NET 8.0 Runtime (self-contained builds include runtime)
- OpenSSL for certificate operations
- For SSH deployment: OpenSSH client configured with key-based authentication
- For filesystem deployment: Access to mounted network filesystems

### Linux

```bash
# Extract the archive
tar -xzf BitCrafts.Certificates.Avalonia-<version>-linux-x64.tar.gz
cd BitCrafts.Certificates.Avalonia

# Set environment variables (optional)
export BITCRAFTS_DATA_DIR="/var/lib/bitcrafts/certificates"
export BITCRAFTS_DB_PATH="/var/lib/bitcrafts/certificates/certificates.db"

# Run the application
./BitCrafts.Certificates.Avalonia
```

### Windows

```powershell
# Extract the ZIP archive
# Navigate to the extracted directory

# Run the application
.\BitCrafts.Certificates.Avalonia.exe
```

### macOS

```bash
# Extract the archive
tar -xzf BitCrafts.Certificates.Avalonia-<version>-osx-x64.tar.gz
cd BitCrafts.Certificates.Avalonia

# Run the application
./BitCrafts.Certificates.Avalonia
```

### Configuration

On first run, the application will prompt you to configure:
- Domain name for the Certificate Authority
- Data directory for certificate storage
- Database path

These settings are stored in `appsettings.json` in the application directory.

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

## Building and Testing

```bash
# Build all projects
dotnet build BitCrafts.Certificates.sln

# Run unit tests
dotnet test BitCrafts.Certificates.sln

# Build specific projects
dotnet build BitCrafts.Certificates/BitCrafts.Certificates.csproj
dotnet build BitCrafts.Certificates.Abstractions/BitCrafts.Certificates.Abstractions.csproj
dotnet build BitCrafts.Certificates.Linux/BitCrafts.Certificates.Linux.csproj
dotnet build BitCrafts.Certificates.Avalonia/BitCrafts.Certificates.Avalonia.csproj
```

## CI/CD

The project includes comprehensive CI/CD workflows:
- **All branches**: Build and test on push
- **Main branch**: Build, test, and create development artifacts
- **Version tags (v*)**: Build, test, publish NuGet packages, create releases with binaries for all platforms

To create a release:
```bash
git tag v1.0.0
git push origin v1.0.0
```

This will automatically:
1. Build and test all projects
2. Publish NuGet packages to GitHub Packages
3. Create binaries for Linux, Windows, and macOS
4. Create a GitHub Release with all artifacts

## Security

- **File Permissions**: Certificate files created with restrictive permissions (0600 for keys, 0644 for certs)
- **Least Privilege**: File operations use explicit chmod/chown with minimal permissions
- **Input Validation**: All external command parameters validated and escaped
- **Audit Logging**: All certificate operations logged for accountability
- **Secure Deployment**: SSH uses key-based authentication; filesystem deployments apply proper permissions

## Technology Stack

- **.NET 8.0** - Runtime platform
- **Avalonia 11.x** - Cross-platform desktop UI framework with MVVM
- **SQLite** - Lightweight certificate metadata database
- **OpenSSL** - Certificate generation (via process wrapper)
- **OpenSSH** - Remote deployment (optional)
- **xUnit + FluentAssertions** - Testing

## Project Structure

```
BitCrafts.Certificates/
├── BitCrafts.Certificates/                    # Business logic library (formerly MVC)
│   ├── Application/                           # Application services and DTOs
│   ├── Domain/                                # Domain entities and interfaces
│   ├── Infrastructure/                        # Infrastructure implementations
│   │   ├── Database/                          # Database adapters
│   │   ├── Storage/                           # File system storage
│   │   ├── Pki/                               # PKI service adapters
│   │   └── Deployment/                        # Deployment services (SSH, NFS)
│   ├── Data/                                  # Data repositories
│   ├── Services/                              # Core services (audit, revocation)
│   └── Pki/                                   # Certificate authority services
├── BitCrafts.Certificates.Avalonia/           # Desktop GUI application
│   ├── ViewModels/                            # MVVM ViewModels
│   ├── Views/                                 # Avalonia views (AXAML)
│   └── Assets/                                # Application resources
├── BitCrafts.Certificates.Abstractions/       # Platform-agnostic abstractions
├── BitCrafts.Certificates.Linux/              # Linux-specific implementations
├── BitCrafts.Certificates.Abstractions.Tests/ # Abstractions unit tests
├── BitCrafts.Certificates.Linux.Tests/        # Linux implementation tests
├── docs/                                      # Documentation
│   └── DEPLOYMENT_WORKFLOWS.md                # Deployment guide
└── .github/workflows/                         # CI/CD workflows
    ├── ci-cd.yml                              # Main build/test/publish pipeline
    ├── publish-abstractions.yml               # Abstractions package publishing
    └── publish-linux.yml                      # Linux package publishing
```

## Contributing

This project is designed for intranet/home lab use. Contributions welcome for:
- UI/UX improvements in the Avalonia application
- Additional deployment methods
- Platform-specific implementations
- Enhanced security features
- Bug fixes and documentation improvements
- Performance optimizations

## Future Enhancements

- Certificate renewal/rotation automation
- Additional database providers (PostgreSQL, MySQL)
- Kubernetes Secret deployment support
- Certificate revocation list (CRL) generation and publishing
- OCSP responder support
- Integration with external CAs (Let's Encrypt, etc.)
- Import/export of CA configuration

## License

This project is licensed under AGPLv3 (Affero GPL v3). See `LICENSE` for the full text.

