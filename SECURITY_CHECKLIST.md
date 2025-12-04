# Security Checklist - BitCrafts Certificates

This checklist helps track the implementation of security fixes. Check off items as they are completed.

## 🔴 CRITICAL - Must Fix Before ANY Deployment

### Issue #1: Authentication & Authorization (CVSS 9.8)
- [ ] Implement authentication system (ASP.NET Identity, OAuth2, or Basic Auth)
- [ ] Add `[Authorize]` attributes to all controllers (except Setup for first-run)
- [ ] Implement role-based access control (Admin, Operator, Viewer roles)
- [ ] Add login/logout functionality
- [ ] Implement session management with proper timeouts
- [ ] Add password policies (complexity, expiration)
- [ ] Test unauthorized access is blocked
- [ ] Test role-based permissions work correctly
- [ ] Document authentication setup in deployment guide
- [ ] Consider MFA for sensitive operations

**Estimated Effort**: 2-3 weeks  
**Status**: ❌ Not Started

---

### Issue #2: Root CA Key Protection (CVSS 9.1)
- [ ] Choose key protection approach (HSM/KMS vs. passphrase encryption)
- [ ] Implement PKCS#8 passphrase encryption for Root CA key
- [ ] Implement secure passphrase storage (secrets manager)
- [ ] Add passphrase prompt during startup or key operations
- [ ] Update key loading code to decrypt with passphrase
- [ ] Migrate existing plaintext key to encrypted format
- [ ] Document key ceremony procedures
- [ ] Implement backup and recovery procedures
- [ ] Add audit logging for all Root CA key access
- [ ] Test certificate issuance with encrypted key
- [ ] Securely delete plaintext key backups

**For HSM/KMS approach**:
- [ ] Choose HSM/KMS provider (Azure Key Vault, AWS KMS, etc.)
- [ ] Integrate signing operations with HSM/KMS API
- [ ] Test certificate generation with HSM-based signing
- [ ] Document HSM setup and configuration
- [ ] Implement key rotation procedures

**Estimated Effort**: 1-2 weeks (passphrase) or 3-4 weeks (HSM)  
**Status**: ❌ Not Started

---

### Issue #3: Protect Private Key Downloads (CVSS 8.8)
- [ ] Implement password-protected ZIP or encryption for downloads
- [ ] Add password strength validation (minimum 12 characters)
- [ ] Add UI for password entry before download
- [ ] Display security warnings to users
- [ ] Implement one-time download tokens with expiration
- [ ] Add rate limiting on download attempts
- [ ] Add audit logging for all download operations
- [ ] Test password protection works correctly
- [ ] Test downloaded files can be extracted with password
- [ ] Document secure download procedures for users

**Estimated Effort**: 1 week  
**Status**: ❌ Not Started

---

## 🟠 HIGH PRIORITY - Required for Production

### Issue #4: Rate Limiting (CVSS 7.5)
- [ ] Choose rate limiting approach (.NET built-in vs. AspNetCoreRateLimit)
- [ ] Implement rate limiting middleware
- [ ] Configure limits for certificate operations (10/hour)
- [ ] Configure limits for downloads (5/5min)
- [ ] Configure limits for API calls (60/min)
- [ ] Add CAPTCHA to Setup page
- [ ] Implement IP-based throttling
- [ ] Add rate limit violation logging
- [ ] Create dashboard for rate limit monitoring
- [ ] Test rate limits are enforced
- [ ] Test legitimate high-volume scenarios
- [ ] Document rate limits in API documentation

**Estimated Effort**: 3-5 days  
**Status**: ❌ Not Started

---

### Issue #5: Fix Content Security Policy (CVSS 7.3)
- [ ] Audit all views for inline scripts and styles
- [ ] Move inline scripts to external JavaScript files
- [ ] Move inline styles to external CSS files
- [ ] Implement nonce generation middleware
- [ ] Add nonce to script and style tags
- [ ] Remove `unsafe-inline` from CSP
- [ ] Implement CSP violation reporting endpoint
- [ ] Test all pages load correctly with new CSP
- [ ] Test all interactive features work
- [ ] Monitor CSP violation reports
- [ ] Gradually tighten CSP based on reports
- [ ] Document CSP configuration

**Estimated Effort**: 1 week  
**Status**: ❌ Not Started

---

### Issue #6: Configure AllowedHosts (CVSS 7.1)
- [ ] Remove wildcard from AllowedHosts configuration
- [ ] Configure specific hostnames in appsettings.json
- [ ] Configure environment-specific values
- [ ] Add Host header validation middleware
- [ ] Update deployment documentation with hostname config
- [ ] Test with configured hostnames
- [ ] Test invalid Host headers are rejected
- [ ] Test subdomain patterns work correctly
- [ ] Test reverse proxy scenarios

**Estimated Effort**: 1 day  
**Status**: ❌ Not Started

---

### Issue #7: Implement CRL/OCSP (CVSS 6.5)
- [ ] Design CRL generation approach
- [ ] Implement X.509 CRL builder
- [ ] Add CRL Distribution Points to issued certificates
- [ ] Add CRL endpoint to serve CRL file
- [ ] Implement automatic CRL regeneration (daily)
- [ ] Update Root CA certificate with CRL DP
- [ ] Test CRL generation and serving
- [ ] Test certificate revocation appears in CRL
- [ ] Document CRL checking procedures for clients
- [ ] Consider implementing OCSP responder (optional)
- [ ] Test with various certificate validation tools

**Estimated Effort**: 2 weeks  
**Status**: ❌ Not Started

---

## 🟡 MEDIUM PRIORITY - Security Hardening

### Issue #8: Session Security Configuration (CVSS 6.5)
- [ ] Configure session cookie security (HttpOnly, Secure, SameSite)
- [ ] Set appropriate session timeout (30 minutes idle)
- [ ] Implement session renewal on important actions
- [ ] Add "Remember Me" functionality with separate tokens
- [ ] Implement session timeout warnings
- [ ] Test session security settings
- [ ] Test session timeout works correctly
- [ ] Document session configuration

**Estimated Effort**: 2 days  
**Status**: ❌ Not Started

---

### Issue #9: Path Traversal Validation (CVSS 6.5)
- [ ] Add path validation to all file operations
- [ ] Implement path canonicalization
- [ ] Validate paths are within expected directories
- [ ] Add unit tests for path traversal attempts
- [ ] Review all Path.Combine usages
- [ ] Test with various path traversal attempts
- [ ] Document secure file path handling

**Estimated Effort**: 1 day  
**Status**: ❌ Not Started

---

### Issue #10: Strengthen Input Validation (CVSS 5.3)
- [ ] Strengthen domain name validation (RFC 1035/1123)
- [ ] Add explicit length limits to all inputs
- [ ] Validate against reserved names
- [ ] Improve username validation
- [ ] Add comprehensive validation unit tests
- [ ] Consider using FluentValidation library
- [ ] Test with various edge cases and special characters
- [ ] Document validation rules

**Estimated Effort**: 3 days  
**Status**: ❌ Not Started

---

### Issue #11: Audit Log Integrity (CVSS 5.3)
- [ ] Implement log signing (HMAC or digital signatures)
- [ ] Add log integrity verification tool
- [ ] Implement append-only logging mechanism
- [ ] Consider structured logging framework (Serilog)
- [ ] Add remote syslog/SIEM integration
- [ ] Implement log rotation with integrity checks
- [ ] Test log tampering is detectable
- [ ] Document log verification procedures

**Estimated Effort**: 3 days  
**Status**: ❌ Not Started

---

## 🟢 LOW PRIORITY - Operational Improvements

### Issue #12: Deployment Script Improvements (CVSS 4.2)
- [ ] Split deployment script into user and root operations
- [ ] Use sudo only for operations requiring it
- [ ] Build as regular user
- [ ] Document minimum required privileges
- [ ] Add privilege drop after setup
- [ ] Test deployment with least privileges
- [ ] Update deployment documentation

**Estimated Effort**: 1 day  
**Status**: ❌ Not Started

---

### Issue #13: Security Update Policy (N/A)
- [ ] Create/update SECURITY.md with disclosure policy
- [ ] Document supported versions
- [ ] Define security update process
- [ ] Set up Dependabot for dependency updates
- [ ] Subscribe to .NET security announcements
- [ ] Add security contact information
- [ ] Document vulnerability response timeline
- [ ] Create security advisory process

**Estimated Effort**: 1 day  
**Status**: ❌ Not Started

---

## 📊 Progress Tracking

### Overall Progress
- **Critical Issues**: 0/3 completed (0%)
- **High Priority**: 0/4 completed (0%)
- **Medium Priority**: 0/4 completed (0%)
- **Low Priority**: 0/2 completed (0%)
- **Total**: 0/13 completed (0%)

### Deployment Readiness

| Milestone | Issues Fixed | Ready For |
|-----------|--------------|-----------|
| 🔴 **Not Safe** | 0/13 | ❌ Do not deploy anywhere |
| 🟡 **Minimal Security** | 3/13 (Critical) | ⚠️ Isolated test environment only |
| 🟠 **Home Network** | 7/13 (Critical + High) | 🏠 Trusted home network behind firewall |
| 🟢 **Production Ready** | 13/13 (All) | ✅ Small business / intranet deployment |

**Current Status**: 🔴 **Not Safe for Deployment**

---

## ⏱️ Time Estimates

### By Priority
- **Critical Fixes**: 4-6 weeks
- **High Priority Fixes**: 3-4 weeks
- **Medium Priority Fixes**: 1-2 weeks
- **Low Priority Fixes**: 2-3 days

### Cumulative
- **Minimum Viable Security**: 4-6 weeks (Critical only)
- **Production Ready**: 7-10 weeks (Critical + High)
- **Fully Hardened**: 8-12 weeks (All issues)

---

## 🎯 Milestones

### Milestone 1: Critical Security (Target: Week 6)
- [ ] Issue #1: Authentication/Authorization
- [ ] Issue #2: Root CA Key Protection
- [ ] Issue #3: Private Key Download Protection

**Deliverable**: Application can be deployed in isolated test environment

---

### Milestone 2: Production Security (Target: Week 10)
- [ ] Issue #4: Rate Limiting
- [ ] Issue #5: CSP Fixes
- [ ] Issue #6: AllowedHosts
- [ ] Issue #7: CRL/OCSP

**Deliverable**: Application can be deployed in trusted network

---

### Milestone 3: Hardened Security (Target: Week 12)
- [ ] Issue #8: Session Security
- [ ] Issue #9: Path Validation
- [ ] Issue #10: Input Validation
- [ ] Issue #11: Audit Log Integrity

**Deliverable**: Application ready for production use

---

### Milestone 4: Operational Excellence (Target: Week 13)
- [ ] Issue #12: Deployment Improvements
- [ ] Issue #13: Security Policy

**Deliverable**: Complete production-ready system with documentation

---

## 🧪 Testing Checklist

### Security Testing (After Each Fix)
- [ ] Unit tests pass
- [ ] Integration tests pass
- [ ] Manual security testing completed
- [ ] Penetration testing (after major fixes)
- [ ] Code review completed
- [ ] Documentation updated

### Pre-Deployment Testing
- [ ] All critical issues fixed and tested
- [ ] Security scan completed (OWASP ZAP, etc.)
- [ ] Dependency vulnerability scan completed
- [ ] Performance testing under security controls
- [ ] User acceptance testing
- [ ] Disaster recovery procedures tested

---

## 📝 Notes

### Version History
- **v1.0** (Dec 4, 2025): Initial security review and checklist creation
- **v1.1** (TBD): After first fixes

### Review Schedule
- **Next Review**: After critical issues are fixed
- **Regular Reviews**: Quarterly after initial hardening
- **Emergency Reviews**: Upon discovery of new vulnerabilities

---

**Last Updated**: December 4, 2025  
**Next Update**: After first milestone completion  
**Maintained By**: Project security team
