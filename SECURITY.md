# Security Policy

## Supported Versions

This section indicates which versions of BitCrafts Certificates are currently supported with security updates.

| Version | Supported          |
| ------- | ------------------ |
| 1.0.x   | :white_check_mark: |
| < 1.0   | :x:                |

## Known Security Issues

**⚠️ CRITICAL WARNING**: This application currently has several critical security issues that make it unsuitable for production use. See [SECURITY_REVIEW.md](SECURITY_REVIEW.md) for details.

**Do not deploy this application in any environment (including home/intranet) until the critical security issues have been addressed.**

### Critical Issues (Must Fix Before Deployment):
1. **No Authentication/Authorization** - Anyone with network access can issue/revoke certificates and download private keys
2. **Unencrypted Root CA Key** - Root CA private key stored as plaintext on filesystem
3. **Unprotected Private Key Downloads** - Private keys can be downloaded without password protection

### High-Priority Issues:
4. Missing rate limiting (DoS vulnerability)
5. Weak Content Security Policy allowing unsafe-inline scripts
6. AllowedHosts set to wildcard (Host header injection)
7. No CRL/OCSP implementation (revoked certs not checked)

See the complete [Security Review Report](SECURITY_REVIEW.md) for all findings and remediation recommendations.

## Reporting a Vulnerability

We take security vulnerabilities seriously. If you discover a security issue in BitCrafts Certificates, please report it responsibly.

### How to Report

**Please do NOT report security vulnerabilities through public GitHub issues.**

Instead, please report security vulnerabilities by email to:

**benzsoftware@pm.me**

### What to Include

Please include the following information in your report:

- Type of vulnerability (e.g., authentication bypass, XSS, SQL injection, etc.)
- Full paths of source file(s) related to the vulnerability
- Location of the affected source code (tag/branch/commit or direct URL)
- Step-by-step instructions to reproduce the issue
- Proof-of-concept or exploit code (if possible)
- Impact of the issue, including how an attacker might exploit it

### What to Expect

After you submit a vulnerability report:

1. **Acknowledgment**: We will acknowledge receipt within 48 hours
2. **Assessment**: We will assess the vulnerability and determine its severity within 5 business days
3. **Communication**: We will keep you informed of our progress
4. **Fix Timeline**: 
   - Critical vulnerabilities: Fix within 7 days
   - High severity: Fix within 30 days
   - Medium severity: Fix within 90 days
   - Low severity: Fix in next planned release
5. **Disclosure**: We will coordinate with you on public disclosure timing
6. **Credit**: We will acknowledge your contribution in the release notes (unless you prefer to remain anonymous)

### Security Update Process

When a security vulnerability is fixed:

1. A security advisory will be published in GitHub Security Advisories
2. A new version will be released with the fix
3. The CHANGELOG will document the security fix (with CVE if applicable)
4. Users will be notified through GitHub release notes

## Security Best Practices for Deployment

Even after the critical issues are fixed, follow these best practices when deploying BitCrafts Certificates:

### Network Security
- Deploy behind a reverse proxy with TLS termination (Apache, Nginx, etc.)
- Use a valid TLS certificate for the web interface
- Restrict network access using firewall rules
- Only allow access from trusted networks
- Consider using VPN for remote access

### Authentication & Authorization
- Enable strong authentication (once implemented)
- Use multi-factor authentication for administrative operations
- Regularly review access logs
- Implement least-privilege access controls

### Key Management
- Store Root CA private key encrypted or in HSM (once implemented)
- Regularly backup the Root CA key (encrypted)
- Store backups in a secure, off-site location
- Implement key ceremony procedures for CA operations
- Rotate certificates according to your security policy

### System Security
- Run the application as a dedicated service account with minimal privileges
- Keep the operating system and all dependencies up to date
- Enable SELinux or AppArmor for additional isolation
- Implement file integrity monitoring
- Use separate partitions for data directories

### Monitoring & Logging
- Enable and monitor audit logs
- Send logs to a secure, centralized logging system
- Set up alerts for suspicious activities
- Regularly review certificate issuance and revocation logs
- Implement log retention policy

### Operational Security
- Document your security procedures
- Train administrators on secure operations
- Perform regular security assessments
- Have an incident response plan
- Test backup and recovery procedures

### Development Security
- Keep dependencies updated (enable Dependabot)
- Run security scans regularly
- Follow secure coding practices
- Perform code reviews for security
- Test security controls before deployment

## Threat Model

### Assets
- Root CA private key (CRITICAL)
- Issued certificate private keys (HIGH)
- Certificate metadata database (MEDIUM)
- Audit logs (MEDIUM)

### Threat Actors
- External attackers (network access)
- Malicious insiders (system access)
- Automated attacks (bots, scripts)

### Attack Vectors
- Network attacks via web interface
- Privilege escalation on the host system
- Supply chain attacks (dependencies)
- Physical access to the server
- Social engineering

### Security Boundaries
- Network perimeter (firewall, reverse proxy)
- Application authentication/authorization (TO BE IMPLEMENTED)
- Operating system permissions
- File system encryption
- Physical security

## Security Roadmap

Planned security improvements:

### Phase 1: Critical Fixes (v1.1 - Q1 2025)
- [ ] Implement authentication and authorization
- [ ] Encrypt Root CA private key
- [ ] Password-protect private key downloads
- [ ] Add rate limiting

### Phase 2: High-Priority Fixes (v1.2 - Q2 2025)
- [ ] Fix Content Security Policy
- [ ] Configure AllowedHosts properly
- [ ] Implement CRL generation and distribution
- [ ] Add OCSP responder

### Phase 3: Medium-Priority Fixes (v1.3 - Q3 2025)
- [ ] Add session security configuration
- [ ] Implement path traversal validation
- [ ] Strengthen input validation
- [ ] Add audit log integrity protection

### Phase 4: Advanced Security Features (v2.0 - Q4 2025)
- [ ] HSM/KMS integration for Root CA key
- [ ] Multi-factor authentication
- [ ] API with OAuth2 authentication
- [ ] Certificate templates
- [ ] Automated certificate renewal
- [ ] Integration with SIEM systems

## Dependencies Security

This project uses the following dependencies:

| Dependency | Version | Security Notes |
|------------|---------|----------------|
| .NET | 8.0 | Keep updated with latest patches |
| Microsoft.Data.Sqlite | 8.0.10 | Monitor for security updates |

### Dependency Update Policy

- Security updates are applied within 7 days of availability
- Regular dependency updates are performed monthly
- Breaking changes are evaluated and documented
- Dependabot is enabled for automated PRs

## Compliance

This application is designed for personal/home use and is not currently compliant with:

- SOC 2
- ISO 27001
- PCI DSS
- HIPAA
- FedRAMP

If you need compliance with these standards, significant additional work would be required.

## Security Contacts

**Project Maintainer**: Younes Benmoussa
**Security Email**: benzsoftware@pm.me
**Project Repository**: https://github.com/BitCrafts/certificates

## License

This security policy is part of the BitCrafts Certificates project, licensed under AGPL-3.0-only.

---

**Last Updated**: December 4, 2025
**Next Review**: March 2025
