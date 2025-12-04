BitCrafts Certificates - AlmaLinux deployment

This folder contains a basic deployment script and a systemd unit template for AlmaLinux / RHEL-like systems.

Files
- deploy_almalinux.sh - Build and install the app, create service user, install systemd unit, enable firewall rules. Run as root (sudo).
- bitcrafts.service - systemd unit template for manual installation.

Usage
1. On an AlmaLinux host with dotnet installed, copy the repo and run:

```bash
sudo ./deploy_almalinux.sh
```

2. To use apache (httpd) as TLS terminator and reverse proxy, run with --install-apache and configure TLS certs as described in `deploy/apache/bitcrafts.conf`.

New options
- --no-self-contained: build framework-dependent (requires dotnet runtime on target)
- --rid RID: specify runtime identifier for self-contained publish (auto-detected by default)
- --install-apache: install and enable httpd and the bundled reverse-proxy config
- --selinux: attempt SELinux fcontext adjustments and enable httpd_can_network_connect

Security notes
- Do NOT store secrets in the systemd unit. Use systemd drop-in or an environment file with restricted permissions (chmod 640, owned by root:bitcrafts).
- Ensure the service user (default: bitcrafts) has no login shell.
- Place data directories (CA keys, DB) under `/srv/bitcrafts/certificates` and restrict to the service user.
- Consider using a dedicated secrets manager (HashiCorp Vault, AWS Secrets Manager) or an HSM for the Root CA private key in production.

After deployment
- Visit the app and complete the Setup flow to create the root CA and set the domain.
- Audit logs are written to `/srv/bitcrafts/certificates/logs/audit.jsonl` by default.
