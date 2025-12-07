# BitCrafts Certificates

Personal small Certificate Authority web app to run at home for issuing and managing server and client certificates.

## Purpose

- Provide a lightweight CA for home/intranet use.
- Issue client and server certificates (ECDSA P-256) and record metadata.
- Simple web UI to issue, list, and revoke certificates.
- **NEW**: REST API for programmatic certificate management and deployment.
- **NEW**: Automated certificate deployment via SSH or network filesystem.
- Designed for personal/home deployment and experimentation; not a production-grade CA for public certificates.

## Key Features

- Root CA generation and storage under a configurable data directory.
- Per-certificate key and PEM artifact generation (server and client certs).
- Audit logging of issuance/revocation actions.
- Simple SQLite-backed metadata store for issued certificates.
- **NEW**: Clean Architecture design with separated domain, application, and infrastructure layers.
- **NEW**: REST API for certificate management (Ansible-friendly).
- **NEW**: Certificate deployment to remote systems via SSH or network filesystem.
- **NEW**: Swagger/OpenAPI documentation for API.
- Deployment helpers: systemd unit, self-contained publish support, optional Apache reverse proxy template, SELinux helpers.

## Architecture

The application now follows **Clean Architecture** principles:

- **Domain Layer**: Business entities and interfaces (Certificate, RootCA, deployment targets)
- **Application Layer**: Use cases and application services
- **Infrastructure Layer**: Implementations for database, storage, PKI, and deployment
- **API Layer**: REST API controllers
- **Presentation Layer**: MVC controllers and views

See [docs/CLEAN_ARCHITECTURE.md](docs/CLEAN_ARCHITECTURE.md) for details.

## REST API

The application exposes a REST API for:
- Creating, listing, revoking, and deleting certificates
- Downloading certificate archives
- Deploying certificates to infrastructure
- Testing deployment connections

See [docs/API_USAGE.md](docs/API_USAGE.md) for API documentation and examples.

### API Endpoints

- `POST /api/CertificatesApi/server` - Create server certificate
- `POST /api/CertificatesApi/client` - Create client certificate
- `GET /api/CertificatesApi` - List all certificates
- `POST /api/CertificatesApi/{id}/revoke` - Revoke certificate
- `POST /api/DeploymentApi/deploy` - Deploy certificate
- `POST /api/DeploymentApi/test` - Test deployment connection

API documentation available at `/swagger` in development mode.

## Certificate Deployment

The application can deploy certificates to infrastructure using:

### SSH Deployment
Deploy certificates to remote servers via SSH/SCP:
```bash
POST /api/DeploymentApi/deploy
{
  "certificateId": 123,
  "deploymentTarget": {
    "type": "SSH",
    "target": "192.168.1.100",
    "username": "deploy",
    "privateKeyPath": "/path/to/key",
    "destinationPath": "/etc/ssl/certs"
  }
}
```

### Network Filesystem Deployment
Deploy certificates to mounted network shares:
```bash
POST /api/DeploymentApi/deploy
{
  "certificateId": 123,
  "deploymentTarget": {
    "type": "NetworkFileSystem",
    "target": "/mnt/network-share",
    "destinationPath": "/mnt/network-share/certs"
  }
}
```

## Ansible Integration

The API is designed to work seamlessly with Ansible playbooks. Example:

```yaml
- name: Deploy certificate
  uri:
    url: "http://localhost:5000/api/CertificatesApi/server"
    method: POST
    body_format: json
    body:
      fqdn: "{{ inventory_hostname }}"
      ipAddresses: ["{{ ansible_default_ipv4.address }}"]
```

See [docs/API_USAGE.md](docs/API_USAGE.md) for complete Ansible examples.

## Security hardening applied

- Private keys and certs are written atomically to temporary files then moved into place to reduce exposure during write operations.
- File permissions are tightened to owner-only (0600 for private keys; 0700 for directories where appropriate).
- Audit log uses the application data logs dir and file permissions are set to 0600.
- Minimal server-side validation for FQDN/user inputs.
- CSP, HSTS, and several security headers are set by middleware.
- **NEW**: Deployment operations use secure SSH connections with key-based authentication.
- **NEW**: File permissions automatically set on deployed certificates.

## Deployment

- The `deploy/` folder contains an AlmaLinux-focused deployment script:
  - `deploy/deploy_almalinux.sh` — builds and installs the app to `/opt/bitcrafts/certificates` and data under `/srv/bitcrafts/certificates`.
  - `deploy/bitcrafts.service` — systemd unit (uses `/opt/bitcrafts/certificates/run.sh` wrapper).
  - `deploy/run.sh` — wrapper that runs the self-contained binary when present or falls back to `dotnet <dll>`.
  - `deploy/apache/bitcrafts.conf` — Apache httpd reverse-proxy sample (TLS termination config placeholder).

- The deploy script supports:
  - `--no-self-contained` (framework-dependent publish)
  - `--rid <rid>` override for self-contained publish
  - `--install-apache` to install a reverse proxy configuration
  - `--selinux` to apply SELinux file contexts and booleans (best-effort)

## Building and Running

```bash
# Build
dotnet build

# Run in development mode
dotnet run --project BitCrafts.Certificates

# Access the application
# Web UI: http://localhost:5000
# API: http://localhost:5000/api
# Swagger: http://localhost:5000/swagger
```

## Technology Stack

- **ASP.NET Core 8.0** - Web framework
- **SQLite** - Database (easily replaceable with PostgreSQL, MySQL, etc.)
- **Swashbuckle** - OpenAPI/Swagger documentation
- **Clean Architecture** - Design pattern
- **ECDSA P-256** - Cryptography

## Future Enhancements

- Additional database support (PostgreSQL, MySQL, SQL Server)
- Cloud storage adapters (S3, Azure Blob)
- Additional deployment methods (Kubernetes, Docker)
- API authentication (API keys, JWT, mutual TLS)
- Web UI for deployment management

# License

This project is licensed under AGPLv3 (Affero GPL v3). See `LICENSE` for the full text.

