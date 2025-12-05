# BitCrafts Certificates

Personal small Certificate Authority web app to run at home for issuing and managing server and client certificates.

Purpose

- Provide a lightweight CA for home/intranet use.
- Issue client and server certificates (ECDSA P-256) and record metadata.
- Simple web UI to issue, list, and revoke certificates.
- Designed for personal/home deployment and experimentation; not a production-grade CA for public certificates.

Key features

- Root CA generation and storage under a configurable data directory.
- Per-certificate key and PEM artifact generation (server and client certs).
- Audit logging of issuance/revocation actions.
- Simple SQLite-backed metadata store for issued certificates.
- Deployment helpers: systemd unit, self-contained publish support, optional Apache reverse proxy template, SELinux helpers.

Security hardening applied

- Private keys and certs are written atomically to temporary files then moved into place to reduce exposure during write operations.
- File permissions are tightened to owner-only (0600 for private keys; 0700 for directories where appropriate).
- Audit log uses the application data logs dir and file permissions are set to 0600.
- Minimal server-side validation for FQDN/user inputs.
- CSP, HSTS, and several security headers are set by middleware.

Deployment

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

# License

This project is licensed under AGPLv3 (Affero GPL v3). See `LICENSE` for the full text.

