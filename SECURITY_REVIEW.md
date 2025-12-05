# Security Review Report - BitCrafts Certificates

**Review Date:** December 4, 2025  
**Reviewer:** GitHub Copilot Security Review  
**Project:** BitCrafts Certificates - Personal Certificate Authority Web Application

## Executive Summary

This security review identified **7 critical and high-priority security issues** that should be addressed before deploying this application in any environment, even for personal/home use. While the application demonstrates good security practices in some areas (CSRF protection, security headers, parameterized queries), it has significant gaps in authentication, authorization, cryptographic key protection, and operational security.

**Risk Level:** HIGH - The application manages cryptographic keys and certificates without authentication, allowing anyone with network access to issue or revoke certificates and download private keys.

---

## Critical Security Issues

### 1. No Authentication or Authorization Mechanism

**Severity:** CRITICAL  
**CWE:** CWE-306 (Missing Authentication for Critical Function)  
**CVSS 3.1 Score:** 9.8 (Critical)

**Description:**
The application has no authentication or authorization mechanism. Anyone who can access the web interface can:
- Issue server and client certificates
- Revoke any certificate
- Download private keys
- View all certificates and audit logs
- Reconfigure the Certificate Authority

**Location:**
- `Program.cs` - No authentication middleware configured
- All controllers lack `[Authorize]` attributes
- `app.UseAuthorization()` is called but no authorization is configured

**Impact:**
- Unauthorized certificate issuance
- Theft of private keys
- Denial of service through certificate revocation
- Complete compromise of the CA infrastructure

**Recommendation:**
1. Implement authentication (consider ASP.NET Core Identity, OAuth2, or at minimum HTTP Basic Auth behind HTTPS)
2. Add `[Authorize]` attributes to all controllers except Setup
3. Implement role-based access control (Admin, Operator, Viewer roles)
4. Add multi-factor authentication for CA operations
5. Consider client certificate authentication for additional security

**Priority:** Must fix before any deployment

---

### 2. Root CA Private Key Stored Unencrypted on Filesystem

**Severity:** CRITICAL  
**CWE:** CWE-311 (Missing Encryption of Sensitive Data)  
**CVSS 3.1 Score:** 9.1 (Critical)

**Description:**
The Root CA private key is stored as plaintext PEM file on the filesystem with only filesystem permissions (0600) protecting it. If the system is compromised, the attacker gains the Root CA key and can issue trusted certificates indefinitely.

**Location:**
- `Pki/CaService.cs` lines 34, 68-72
- Root key stored at `{DataDir}/pki/ca/root_ca.key`

**Impact:**
- Complete compromise of the CA if system is breached
- Attacker can issue certificates for any domain/user
- No way to detect unauthorized key usage
- Difficult to rotate/revoke the Root CA

**Recommendation:**
1. Encrypt the Root CA private key with a passphrase (use PKCS#8 with encryption)
2. Store the passphrase in a secure secrets manager (Azure Key Vault, HashiCorp Vault, etc.)
3. Consider using Hardware Security Module (HSM) or cloud KMS for production
4. Implement key ceremony procedures for Root CA operations
5. Add audit logging for all Root CA key access
6. Document key backup and recovery procedures

**Priority:** Must fix before any deployment

---

### 3. Private Keys Downloadable Without Password Protection

**Severity:** CRITICAL  
**CWE:** CWE-522 (Insufficiently Protected Credentials)  
**CVSS 3.1 Score:** 8.8 (High)

**Description:**
The `/Servers/Download/{id}` endpoint allows downloading private keys and certificates in a tar.gz archive without any password protection. Combined with the lack of authentication, this means anyone can download all private keys.

**Location:**
- `Controllers/ServersController.cs` lines 108-135
- `Helpers/TarGzHelper.cs` - Creates unprotected archives

**Impact:**
- Private keys exposed in transit and at rest on user's computer
- Keys could be intercepted or stolen
- No protection if download link is shared or leaked

**Recommendation:**
1. Password-protect downloaded archives (use ZIP with AES-256 encryption or GPG)
2. Generate one-time download tokens with expiration
3. Require re-authentication before download
4. Consider encrypting the private key with a user-provided passphrase
5. Add audit logging for all private key downloads
6. Display prominent security warning during download

**Priority:** Must fix before any deployment

---

## High-Priority Security Issues

### 4. Missing Rate Limiting and DoS Protection

**Severity:** HIGH  
**CWE:** CWE-770 (Allocation of Resources Without Limits or Throttling)  
**CVSS 3.1 Score:** 7.5 (High)

**Description:**
The application has no rate limiting on any endpoints, allowing:
- Unlimited certificate issuance (disk space exhaustion)
- Brute force attacks (once auth is added)
- DoS through repeated expensive crypto operations

**Location:**
- `Program.cs` - No rate limiting middleware
- All controllers lack rate limiting

**Impact:**
- Disk space exhaustion from certificate spam
- CPU exhaustion from crypto operations
- Service unavailability
- Resource exhaustion attacks

**Recommendation:**
1. Add rate limiting middleware (AspNetCoreRateLimit or built-in .NET 8 rate limiting)
2. Limit certificate issuance per time period (e.g., 10 certs/hour)
3. Limit revocation operations
4. Add CAPTCHA for Setup page
5. Implement request throttling per IP address
6. Monitor and alert on unusual activity

**Priority:** High

---

### 5. Weak Content Security Policy Allowing unsafe-inline

**Severity:** HIGH  
**CWE:** CWE-1275 (Sensitive Cookie with Improper SameSite Attribute)  
**CVSS 3.1 Score:** 7.3 (High)

**Description:**
The Content Security Policy allows `unsafe-inline` for both scripts and styles, which defeats much of the XSS protection CSP provides.

**Location:**
- `Program.cs` line 92: `"script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'"`

**Impact:**
- XSS attacks can execute inline scripts
- Injected styles can be used for data exfiltration
- Reduced effectiveness of CSP as a defense layer

**Recommendation:**
1. Remove `unsafe-inline` from script-src
2. Use nonce-based or hash-based CSP for inline scripts
3. Move inline styles to external CSS files
4. Use ASP.NET Core Tag Helpers for CSP nonces
5. Add `strict-dynamic` for better security
6. Test thoroughly to ensure UI still works

**Example improved CSP:**
```csharp
"default-src 'self'; script-src 'self' 'nonce-{random}'; style-src 'self'; object-src 'none'; base-uri 'self'; form-action 'self'"
```

**Priority:** High

---

### 6. AllowedHosts Set to Wildcard (*)

**Severity:** HIGH  
**CWE:** CWE-644 (Improper Neutralization of HTTP Headers for Scripting Syntax)  
**CVSS 3.1 Score:** 7.1 (High)

**Description:**
The `AllowedHosts` configuration is set to `*` (all hosts), making the application vulnerable to Host header injection attacks.

**Location:**
- `appsettings.json` line 8: `"AllowedHosts": "*"`

**Impact:**
- Host header injection attacks
- Cache poisoning
- Password reset poisoning (if email features added)
- Potential for SSRF in some scenarios

**Recommendation:**
1. Set `AllowedHosts` to specific domain(s): `["certificates.home.lan", "localhost"]`
2. Use environment-specific configuration
3. Add Host header validation middleware if needed
4. Document the expected hostname in deployment guide

**Priority:** High

---

### 7. No Certificate Revocation List (CRL) or OCSP Implementation

**Severity:** HIGH  
**CWE:** CWE-299 (Improper Check for Certificate Revocation)  
**CVSS 3.1 Score:** 6.5 (Medium)

**Description:**
While certificates can be marked as "revoked" in the database, there's no proper CRL generation or OCSP responder. Clients have no way to check if a certificate has been revoked.

**Location:**
- `Services/RevocationStore.cs` - Only appends to JSON file
- No CRL generation code
- No OCSP responder implementation
- Root CA certificate lacks CRL Distribution Points

**Impact:**
- Revoked certificates remain trusted by clients
- Compromised private keys cannot be effectively revoked
- No standard way to communicate revocation status

**Recommendation:**
1. Implement proper X.509 CRL generation
2. Add CRL Distribution Points to all issued certificates
3. Serve CRL file via HTTP endpoint
4. Consider implementing OCSP responder
5. Add CRL to root CA certificate
6. Document revocation checking procedures for clients
7. Automate CRL updates (e.g., daily regeneration)

**Priority:** High

---

## Medium-Priority Security Issues

### 8. No Session Security Configuration

**Severity:** MEDIUM  
**CWE:** CWE-614 (Sensitive Cookie in HTTPS Session Without 'Secure' Attribute)  
**CVSS 3.1 Score:** 6.5 (Medium)

**Description:**
Once authentication is added, session cookies will need proper security configuration. Currently, there's no explicit session configuration for:
- Secure flag
- HttpOnly flag  
- SameSite attribute
- Cookie expiration
- Session timeout

**Location:**
- `Program.cs` - No session or cookie configuration

**Impact:**
- Session hijacking via XSS (without HttpOnly)
- Session hijacking via network interception (without Secure)
- CSRF attacks (without proper SameSite)
- Long-lived sessions increase attack window

**Recommendation:**
1. Add session configuration:
```csharp
builder.Services.AddSession(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.IsEssential = true;
});
```

2. Configure antiforgery cookies similarly
3. Implement session timeout warnings
4. Add "Remember Me" feature with separate token management

**Priority:** Medium (Must fix when adding authentication)

---

### 9. Potential Path Traversal in Certificate Download

**Severity:** MEDIUM  
**CWE:** CWE-22 (Improper Limitation of a Pathname to a Restricted Directory)  
**CVSS 3.1 Score:** 6.5 (Medium)

**Description:**
While the application retrieves file paths from the database rather than user input, there's no validation that the paths are within the expected directory structure. If an attacker can manipulate the database, they could potentially read arbitrary files.

**Location:**
- `Controllers/ServersController.cs` lines 114-120
- `Controllers/ClientsController.cs` (similar pattern)

**Impact:**
- Potential to read arbitrary files if database is compromised
- Information disclosure
- Could be chained with SQL injection (though none found)

**Recommendation:**
1. Validate that resolved paths are within DataDirectory
2. Add path canonicalization and validation:
```csharp
var certPath = Path.GetFullPath(rec.CertPath);
if (!certPath.StartsWith(_data.CertsServersDir, StringComparison.OrdinalIgnoreCase))
{
    return NotFound();
}
```

3. Use Path.Combine for all path operations
4. Add unit tests for path traversal attempts

**Priority:** Medium

---

### 10. Insufficient Input Validation on Domain and Username Fields

**Severity:** MEDIUM  
**CWE:** CWE-20 (Improper Input Validation)  
**CVSS 3.1 Score:** 5.3 (Medium)

**Description:**
While basic regex validation exists, the domain and username validation could be more robust:
- Domain validation allows invalid DNS characters in some edge cases
- Username validation is very permissive
- No length limits enforced in code (only in UI)
- No validation against reserved names

**Location:**
- `Controllers/SetupController.cs` line 66: `"^[a-zA-Z0-9.-]+$"`
- `Controllers/ServersController.cs` line 148: `"^[A-Za-z0-9.-]+$"`
- `Controllers/ClientsController.cs` line 112: `"^[A-Za-z0-9._-]+$"`
- `Pki/LeafCertificateService.cs` lines 206-247

**Impact:**
- Potential for injecting special characters into certificates
- File system issues with special characters in paths
- X.509 name constraint violations

**Recommendation:**
1. Strengthen domain validation to match RFC 1035/1123
2. Add explicit length limits (max 253 for FQDN, max 64 for username)
3. Validate against reserved names (e.g., "root", "admin", "..", etc.)
4. Reject leading/trailing dots and hyphens more strictly
5. Add comprehensive validation unit tests
6. Consider using a validation library (FluentValidation)

**Priority:** Medium

---

### 11. No Audit Log Integrity Protection

**Severity:** MEDIUM  
**CWE:** CWE-778 (Insufficient Logging)  
**CVSS 3.1 Score:** 5.3 (Medium)

**Description:**
Audit logs are written to a plain text file without integrity protection. An attacker with write access could modify or delete audit logs to hide their activities.

**Location:**
- `Services/AuditLogger.cs` - Writes to `audit.jsonl` with no protection
- `Services/RevocationStore.cs` - Similar issue with `revoked.jsonl`

**Impact:**
- Attackers can cover their tracks
- Cannot prove log integrity for forensics
- Compliance issues (SOC2, ISO27001, etc.)

**Recommendation:**
1. Implement log signing (HMAC or digital signatures)
2. Use append-only logging mechanism
3. Send logs to remote syslog/SIEM for tamper protection
4. Add log integrity verification tool
5. Consider using structured logging framework (Serilog with sinks)
6. Implement log rotation with integrity checks
7. Sign each log entry with timestamp from trusted time source

**Priority:** Medium

---

## Low-Priority Security Issues

### 12. Deployment Script Runs Commands as Root Unnecessarily

**Severity:** LOW  
**CWE:** CWE-250 (Execution with Unnecessary Privileges)  
**CVSS 3.1 Score:** 4.2 (Medium)

**Description:**
The deployment script (`deploy/deploy_almalinux.sh`) requires running as root for the entire script, including build operations that don't need elevated privileges.

**Location:**
- `deploy/deploy_almalinux.sh` lines 48-51

**Impact:**
- Increased attack surface during deployment
- Build artifacts owned by root
- Potential for privilege escalation if script is modified

**Recommendation:**
1. Split the script into user-level and root-level operations
2. Use `sudo` only for operations that require it
3. Build as regular user, then use sudo for installation
4. Document minimum required privileges
5. Add privilege drop after initial setup

**Priority:** Low

---

### 13. No Documented Security Update Policy

**Severity:** LOW  
**CWE:** CWE-1104 (Use of Unmaintained Third Party Components)

**Description:**
The project has no documented security update policy for:
- Dependency updates (.NET, Microsoft.Data.Sqlite)
- Security vulnerability disclosure
- Patch release process
- End-of-life support

**Impact:**
- Unknown vulnerability exposure
- No clear process for security patches
- Users don't know how to report vulnerabilities

**Recommendation:**
1. Create SECURITY.md file with:
   - Supported versions
   - How to report vulnerabilities
   - Security update policy
   - Expected response time
2. Set up Dependabot for automated dependency updates
3. Subscribe to .NET security announcements
4. Document the vulnerability disclosure process
5. Add security contact email

**Priority:** Low

---

## Positive Security Practices Found

The following security practices are already implemented well:

1. ✅ **CSRF Protection** - Proper use of `[ValidateAntiForgeryToken]` on all POST actions
2. ✅ **Parameterized SQL Queries** - All database operations use parameterized queries, preventing SQL injection
3. ✅ **Security Headers** - Good set of security headers (X-Content-Type-Options, X-Frame-Options, etc.)
4. ✅ **HSTS** - Properly configured for production
5. ✅ **File Permissions** - Attempts to set restrictive permissions (0600/0700) on sensitive files
6. ✅ **Atomic File Writes** - Uses temporary files with atomic moves for sensitive data
7. ✅ **Secure Defaults** - HTTPS redirection in production, secure cookie defaults
8. ✅ **Input Validation** - Basic regex validation on user inputs
9. ✅ **Audit Logging** - Actions are logged (though integrity protection needed)
10. ✅ **Least Privilege** - Service runs as dedicated user account

---

## Summary of Recommendations by Priority

### Must Fix Before ANY Deployment (Critical)
1. Implement authentication and authorization
2. Encrypt Root CA private key or use HSM
3. Password-protect or encrypt downloaded private keys

### Must Fix Before Production Use (High)
4. Implement rate limiting
5. Fix Content Security Policy (remove unsafe-inline)
6. Configure AllowedHosts properly
7. Implement CRL/OCSP

### Should Fix (Medium)
8. Configure session security (when auth is added)
9. Add path traversal validation
10. Strengthen input validation
11. Add audit log integrity protection

### Nice to Have (Low)
12. Improve deployment script privileges
13. Document security update policy

---

## Testing Recommendations

To verify security fixes:

1. **Authentication Testing**
   - Test unauthorized access attempts
   - Test role-based access control
   - Test session management

2. **Rate Limiting Testing**
   - Test with rapid certificate issuance
   - Test DoS scenarios

3. **Input Validation Testing**
   - Fuzz testing on all input fields
   - Test special characters and path traversal attempts
   - Test maximum length inputs

4. **Key Security Testing**
   - Verify Root CA key encryption
   - Test private key download protection
   - Verify file permissions

5. **Penetration Testing**
   - Consider hiring professional penetration testers
   - Use OWASP ZAP or Burp Suite for web app testing

---

## Compliance Considerations

If this application needs to meet compliance standards:

- **SOC2**: Requires authentication, authorization, audit logging with integrity
- **ISO 27001**: Requires documented security policies, access controls, key management
- **PCI DSS**: Requires strong authentication, encryption, logging, and network security
- **GDPR**: If handling EU user data, requires privacy controls and data protection

---

## Conclusion

The BitCrafts Certificates application demonstrates good security awareness in some areas but has critical gaps that must be addressed before deployment. The most urgent issues are:

1. Complete lack of authentication/authorization
2. Unencrypted Root CA private key storage
3. Unprotected private key downloads

These issues make the application **unsuitable for deployment even in a home/intranet environment** until fixed. An attacker with network access could completely compromise the CA infrastructure.

After addressing the critical and high-priority issues, this application could be suitable for personal/home use, but would still require additional hardening for any production or internet-facing deployment.

**Estimated effort to reach production-ready security:**
- Critical fixes: 2-4 weeks
- High-priority fixes: 2-3 weeks  
- Medium-priority fixes: 1-2 weeks
- Total: 5-9 weeks of security-focused development

---

## References

- OWASP Top 10 2021: https://owasp.org/Top10/
- OWASP Authentication Cheat Sheet: https://cheatsheetseries.owasp.org/cheatsheets/Authentication_Cheat_Sheet.html
- CWE Top 25: https://cwe.mitre.org/top25/
- .NET Security Best Practices: https://learn.microsoft.com/en-us/aspnet/core/security/
- X.509 Certificate and CRL Profile: RFC 5280
