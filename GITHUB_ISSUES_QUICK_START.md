# Quick Start: Creating Security Issues in GitHub

This guide helps you quickly create GitHub Issues for all identified security findings.

## Overview

13 security issues have been identified and need to be tracked in GitHub Issues. This document provides:
- Commands to create issues using GitHub CLI
- Issue templates ready to copy/paste
- Labels to apply

## Prerequisites

Install GitHub CLI if not already installed:
```bash
# macOS
brew install gh

# Linux
# See https://github.com/cli/cli#installation

# Authenticate
gh auth login
```

## Labels to Create First

Create these labels in your repository:

```bash
gh label create "security" --color "d73a4a" --description "Security-related issue"
gh label create "critical" --color "b60205" --description "Critical severity - must fix immediately"
gh label create "high" --color "d93f0b" --description "High severity - priority fix"
gh label create "medium" --color "fbca04" --description "Medium severity"
gh label create "low" --color "0e8a16" --description "Low severity"
gh label create "authentication" --color "1d76db" --description "Authentication related"
gh label create "authorization" --color "1d76db" --description "Authorization related"
gh label create "cryptography" --color "5319e7" --description "Cryptographic operations"
gh label create "key-management" --color "5319e7" --description "Key storage and management"
gh label create "data-protection" --color "c5def5" --description "Data protection"
gh label create "dos" --color "d93f0b" --description "Denial of service"
gh label create "xss" --color "d93f0b" --description "Cross-site scripting"
gh label create "injection" --color "d93f0b" --description "Injection vulnerability"
gh label create "configuration" --color "c2e0c6" --description "Configuration issue"
```

## Create All Issues at Once

### Option 1: Using GitHub CLI (Recommended)

Save each issue body to a file, then run:

```bash
# Issue #1 - Authentication
gh issue create \
  --title "[CRITICAL] No Authentication or Authorization - Anyone Can Issue/Revoke Certificates" \
  --body "$(cat issue-templates/issue-01-authentication.md)" \
  --label "security,critical,authentication,authorization"

# Issue #2 - Root CA Key
gh issue create \
  --title "[CRITICAL] Root CA Private Key Stored Unencrypted on Filesystem" \
  --body "$(cat issue-templates/issue-02-root-ca-key.md)" \
  --label "security,critical,cryptography,key-management"

# Issue #3 - Private Key Downloads
gh issue create \
  --title "[CRITICAL] Certificate Private Keys Downloadable Without Encryption" \
  --body "$(cat issue-templates/issue-03-key-downloads.md)" \
  --label "security,critical,cryptography,data-protection"

# Issue #4 - Rate Limiting
gh issue create \
  --title "[HIGH] No Rate Limiting - DoS Vulnerability" \
  --body "$(cat issue-templates/issue-04-rate-limiting.md)" \
  --label "security,high,dos"

# Issue #5 - CSP
gh issue create \
  --title "[HIGH] Weak Content Security Policy Allows unsafe-inline" \
  --body "$(cat issue-templates/issue-05-csp.md)" \
  --label "security,high,xss"

# Issue #6 - AllowedHosts
gh issue create \
  --title "[HIGH] AllowedHosts Set to Wildcard - Host Header Injection" \
  --body "$(cat issue-templates/issue-06-allowedhosts.md)" \
  --label "security,high,injection,configuration"

# Issue #7 - CRL/OCSP
gh issue create \
  --title "[HIGH] No CRL or OCSP Implementation" \
  --body "$(cat issue-templates/issue-07-crl-ocsp.md)" \
  --label "security,high,cryptography"

# Issue #8 - Session Security
gh issue create \
  --title "[MEDIUM] No Session Security Configuration" \
  --body "$(cat issue-templates/issue-08-session-security.md)" \
  --label "security,medium,authentication"

# Issue #9 - Path Traversal
gh issue create \
  --title "[MEDIUM] Potential Path Traversal in Downloads" \
  --body "$(cat issue-templates/issue-09-path-traversal.md)" \
  --label "security,medium,injection"

# Issue #10 - Input Validation
gh issue create \
  --title "[MEDIUM] Weak Input Validation" \
  --body "$(cat issue-templates/issue-10-input-validation.md)" \
  --label "security,medium"

# Issue #11 - Audit Logs
gh issue create \
  --title "[MEDIUM] No Audit Log Integrity Protection" \
  --body "$(cat issue-templates/issue-11-audit-logs.md)" \
  --label "security,medium"

# Issue #12 - Deployment Script
gh issue create \
  --title "[LOW] Deployment Script Runs as Root" \
  --body "$(cat issue-templates/issue-12-deployment.md)" \
  --label "security,low"

# Issue #13 - Security Policy
gh issue create \
  --title "[LOW] No Security Update Policy" \
  --body "$(cat issue-templates/issue-13-security-policy.md)" \
  --label "security,low"
```

### Option 2: Manual Creation via GitHub Web UI

1. Go to https://github.com/BitCrafts/certificates/issues/new
2. Copy the title from SECURITY_ISSUES_TRACKING.md
3. Copy the description from SECURITY_ISSUES_TRACKING.md
4. Add the appropriate labels
5. Assign to appropriate milestone (if using milestones)
6. Click "Submit new issue"
7. Repeat for all 13 issues

### Option 3: Create Issue Templates

Create `.github/ISSUE_TEMPLATE/security-issue.yml`:

```yaml
name: Security Issue
description: Report a security vulnerability or track a security fix
title: "[SECURITY] "
labels: ["security"]
body:
  - type: dropdown
    id: severity
    attributes:
      label: Severity
      options:
        - Critical
        - High
        - Medium
        - Low
    validations:
      required: true
  - type: textarea
    id: description
    attributes:
      label: Description
      description: Detailed description of the security issue
    validations:
      required: true
  - type: textarea
    id: impact
    attributes:
      label: Security Impact
      description: What could an attacker do with this vulnerability?
    validations:
      required: true
  - type: textarea
    id: remediation
    attributes:
      label: Remediation
      description: How should this be fixed?
    validations:
      required: true
  - type: textarea
    id: testing
    attributes:
      label: Testing
      description: How can the fix be tested?
```

## Issue Creation Checklist

Use this checklist when creating issues:

### For Each Issue:
- [ ] Copy title exactly from SECURITY_ISSUES_TRACKING.md
- [ ] Include CVSS score in title or description
- [ ] Add all relevant labels
- [ ] Include code location references
- [ ] Include remediation steps
- [ ] Include testing checklist
- [ ] Add to security milestone
- [ ] Assign priority
- [ ] Link related issues (if any)

### After Creating All Issues:
- [ ] Create GitHub Project for security work
- [ ] Add all issues to project
- [ ] Set up project views (by severity, by status)
- [ ] Create milestones for each phase
- [ ] Assign issues to milestones
- [ ] Update README.md with security warning
- [ ] Announce to users (if any)

## GitHub Project Setup

Create a project to track security work:

```bash
# Create project
gh project create --title "Security Hardening" --body "Track security fixes for BitCrafts Certificates"

# Add issues to project (get project number from previous command)
gh project item-add <PROJECT_NUMBER> --owner BitCrafts --url https://github.com/BitCrafts/certificates/issues/<ISSUE_NUMBER>
```

## Milestones

Create milestones to track progress:

```bash
gh api repos/BitCrafts/certificates/milestones \
  --method POST \
  --field title='Phase 1: Critical Security Fixes' \
  --field description='Must complete before any deployment' \
  --field due_on='2025-02-01T00:00:00Z'

gh api repos/BitCrafts/certificates/milestones \
  --method POST \
  --field title='Phase 2: High-Priority Fixes' \
  --field description='Required for production readiness' \
  --field due_on='2025-03-01T00:00:00Z'

gh api repos/BitCrafts/certificates/milestones \
  --method POST \
  --field title='Phase 3: Security Hardening' \
  --field description='Medium-priority improvements' \
  --field due_on='2025-03-15T00:00:00Z'

gh api repos/BitCrafts/certificates/milestones \
  --method POST \
  --field title='Phase 4: Operational Excellence' \
  --field description='Low-priority improvements' \
  --field due_on='2025-03-31T00:00:00Z'
```

## Issue Templates Directory

For detailed issue descriptions, see:
- **SECURITY_ISSUES_TRACKING.md** - Complete issue details
- **SECURITY_REVIEW.md** - Comprehensive security report
- **SECURITY_CHECKLIST.md** - Implementation tracking

## Quick Reference: Issue Titles

Copy these titles for quick issue creation:

```
1. [CRITICAL] No Authentication or Authorization - Anyone Can Issue/Revoke Certificates
2. [CRITICAL] Root CA Private Key Stored Unencrypted on Filesystem  
3. [CRITICAL] Certificate Private Keys Downloadable Without Encryption
4. [HIGH] No Rate Limiting - DoS Vulnerability
5. [HIGH] Weak Content Security Policy Allows unsafe-inline
6. [HIGH] AllowedHosts Set to Wildcard - Host Header Injection
7. [HIGH] No CRL or OCSP Implementation
8. [MEDIUM] No Session Security Configuration
9. [MEDIUM] Potential Path Traversal in Downloads
10. [MEDIUM] Weak Input Validation
11. [MEDIUM] No Audit Log Integrity Protection
12. [LOW] Deployment Script Runs as Root
13. [LOW] No Security Update Policy
```

## After Creating Issues

1. **Update README.md**:
```markdown
## ⚠️ SECURITY WARNING

This application has critical security vulnerabilities and should NOT be deployed in any environment.

See [SECURITY.md](SECURITY.md) for details and track fixes at:
- [Security Issues](https://github.com/BitCrafts/certificates/labels/security)
- [Security Project](https://github.com/BitCrafts/certificates/projects/1)

Critical issues that must be fixed:
- #1 - No authentication or authorization
- #2 - Unencrypted Root CA private key
- #3 - Unprotected private key downloads
```

2. **Create Security Advisory** (if needed):
```bash
gh api repos/BitCrafts/certificates/security-advisories \
  --method POST \
  --field summary='Multiple Critical Security Vulnerabilities' \
  --field description='See SECURITY.md for details' \
  --field severity='critical'
```

3. **Pin Important Issues**:
```bash
# Pin the critical issues to repository
gh issue pin <ISSUE_NUMBER>
```

## Automation

Consider setting up automation:

### GitHub Actions Workflow

Create `.github/workflows/security-check.yml`:

```yaml
name: Security Checklist Monitor
on:
  issues:
    types: [closed]
jobs:
  update-checklist:
    runs-on: ubuntu-latest
    steps:
      - name: Check if security issue
        if: contains(github.event.issue.labels.*.name, 'security')
        run: |
          echo "Security issue ${{ github.event.issue.number }} closed"
          # Add logic to update SECURITY_CHECKLIST.md
```

## Need Help?

- **Documentation**: See SECURITY_REVIEW_README.md
- **Detailed Issues**: See SECURITY_ISSUES_TRACKING.md
- **Implementation Guide**: See SECURITY_CHECKLIST.md
- **Contact**: benzsoftware@pm.me

---

**Last Updated**: December 4, 2025  
**Next Review**: After issue creation
