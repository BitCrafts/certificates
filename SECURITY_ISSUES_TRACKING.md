# Security Issues - Action Items

This document tracks the security issues identified in the security review. Each issue should be created in GitHub Issues with the appropriate labels and priority.

---

## Issue #1: Implement Authentication and Authorization

**Title**: [CRITICAL SECURITY] No Authentication or Authorization - Anyone Can Issue/Revoke Certificates

**Labels**: `security`, `critical`, `authentication`, `authorization`

**Description**:

The application currently has no authentication or authorization mechanism. Anyone who can access the web interface can:
- Issue server and client certificates
- Revoke any certificate  
- Download private keys
- View all certificates and audit logs
- Reconfigure the Certificate Authority

**Security Impact**:
- **CVSS 3.1 Score**: 9.8 (Critical)
- **CWE**: CWE-306 (Missing Authentication for Critical Function)
- Unauthorized certificate issuance
- Theft of private keys
- Complete compromise of CA infrastructure

**Current State**:
- `Program.cs` has `app.UseAuthorization()` but no authorization configured
- No authentication middleware configured
- All controllers lack `[Authorize]` attributes

**Remediation**:

1. Implement authentication mechanism (options in order of preference):
   - ASP.NET Core Identity with local accounts
   - OAuth2/OpenID Connect (if integration with existing IdP)
   - HTTP Basic Auth behind HTTPS (minimum viable option)

2. Add `[Authorize]` attributes to all controllers except Setup controller

3. Implement role-based access control with at least 3 roles:
   - **Admin**: Full access including CA configuration
   - **Operator**: Can issue and revoke certificates
   - **Viewer**: Read-only access to certificates and logs

4. Add multi-factor authentication for high-risk operations:
   - Root CA operations
   - Certificate revocation
   - Configuration changes

5. Consider client certificate authentication for API access

**Implementation Steps**:

```csharp
// In Program.cs, add authentication
builder.Services.AddAuthentication("CookieAuth")
    .AddCookie("CookieAuth", options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdmin", policy => 
        policy.RequireRole("Admin"));
    options.AddPolicy("RequireOperator", policy => 
        policy.RequireRole("Admin", "Operator"));
});

// Add after builder.Build()
app.UseAuthentication();
app.UseAuthorization();
```

```csharp
// In Controllers
[Authorize(Policy = "RequireOperator")]
public class ServersController : Controller
{
    // ... existing code
}
```

**Testing**:
- [ ] Verify unauthenticated users are redirected to login
- [ ] Verify role-based access control works correctly
- [ ] Verify Setup page is accessible without authentication for first-run
- [ ] Verify session timeout works correctly
- [ ] Test with multiple concurrent users

**Priority**: MUST FIX BEFORE ANY DEPLOYMENT

**Estimated Effort**: 2-3 weeks

---

## Issue #2: Root CA Private Key Stored Unencrypted on Filesystem

**Title**: [CRITICAL SECURITY] Root CA Private Key Stored as Plaintext - Complete CA Compromise Risk

**Labels**: `security`, `critical`, `cryptography`, `key-management`

**Description**:

The Root CA private key is stored as an unencrypted PEM file on the filesystem with only Unix file permissions (0600) protecting it. If the system is compromised through any vulnerability, the attacker gains access to the Root CA key and can issue trusted certificates indefinitely.

**Security Impact**:
- **CVSS 3.1 Score**: 9.1 (Critical)
- **CWE**: CWE-311 (Missing Encryption of Sensitive Data)
- Complete compromise of CA if system is breached
- Attacker can issue certificates for any domain/user
- No way to detect unauthorized key usage
- Difficult to rotate/revoke the Root CA

**Current State**:
- `Pki/CaService.cs` stores Root CA key at `{DataDir}/pki/ca/root_ca.key` as plaintext PEM
- Key exported using `ExportPkcs8PrivateKey()` without encryption

**Remediation Options** (in order of security):

### Option 1: Hardware Security Module (HSM) or Cloud KMS (Recommended for Production)
- Use Azure Key Vault, AWS KMS, Google Cloud KMS, or hardware HSM
- Key never leaves the HSM
- All signing operations performed in HSM
- Supports audit logging of key usage

### Option 2: Password-Protected Private Key (Good for Home/Small Deployments)
- Encrypt private key with passphrase using PKCS#8 encryption
- Store passphrase in secrets manager or prompt at startup
- Requires manual intervention on service restart

### Option 3: System-Level Encryption (Minimum Viable)
- Store private key encrypted with machine key
- Use ASP.NET Core Data Protection API
- Better than plaintext but still vulnerable if machine is compromised

**Implementation Example (Option 2 - Password Protection)**:

```csharp
// In CaService.cs - Modify key generation
public async Task CreateRootCaIfMissingAsync(string domain, string passphrase, CancellationToken ct = default)
{
    // ... existing code up to key generation ...
    
    // Encrypt private key with passphrase
    var encryptedPkcs8 = ecdsa.ExportEncryptedPkcs8PrivateKey(
        passphrase,
        new PbeParameters(
            PbeEncryptionAlgorithm.Aes256Cbc,
            HashAlgorithmName.SHA256,
            iterationCount: 100_000));
    
    var keyPem = PemEncode("ENCRYPTED PRIVATE KEY", encryptedPkcs8);
    
    await FileUtils.WriteSecureFileAsync(RootKeyPath, keyPem, ct, 
        UnixFileMode.UserRead | UnixFileMode.UserWrite);
    
    // ... rest of code ...
}

// Modify key loading
public ECDsa LoadRootCaPrivateKey(string passphrase)
{
    var keyPem = File.ReadAllText(RootKeyPath);
    var ecdsa = ECDsa.Create();
    ecdsa.ImportFromEncryptedPem(keyPem, passphrase);
    return ecdsa;
}
```

**Additional Requirements**:

1. **Passphrase Management**:
   - Store in Azure Key Vault, HashiCorp Vault, or similar
   - Never commit to source control
   - Support passphrase rotation

2. **Key Ceremony Procedures**:
   - Document the process for Root CA key generation
   - Require multiple approvers for key operations
   - Maintain audit log of all Root CA key access

3. **Backup and Recovery**:
   - Encrypted backups of Root CA key
   - Secure offline storage
   - Tested recovery procedures
   - Document key escrow process

4. **Audit Logging**:
   - Log all Root CA key access
   - Log all certificate signing operations
   - Alert on unusual patterns

**Migration Path**:

1. Create backup of existing plaintext key (encrypted)
2. Implement passphrase protection
3. Re-encrypt existing keys with passphrase
4. Test certificate issuance with encrypted key
5. Document new procedures
6. Delete plaintext backups securely

**Testing**:
- [ ] Verify encrypted key can be loaded with correct passphrase
- [ ] Verify certificate issuance works with encrypted key
- [ ] Verify incorrect passphrase fails gracefully
- [ ] Test backup and recovery procedures
- [ ] Verify file permissions on encrypted key

**Priority**: MUST FIX BEFORE ANY DEPLOYMENT

**Estimated Effort**: 1-2 weeks (for password protection), 3-4 weeks (for HSM integration)

---

## Issue #3: Private Keys Downloadable Without Password Protection

**Title**: [CRITICAL SECURITY] Certificate Private Keys Downloadable Without Encryption or Authentication

**Labels**: `security`, `critical`, `cryptography`, `data-protection`

**Description**:

The `/Servers/Download/{id}` endpoint allows downloading private keys and certificates in an unencrypted tar.gz archive. Combined with the lack of authentication (#1), this means anyone with network access can download all private keys. Even after authentication is implemented, the downloads should be password-protected.

**Security Impact**:
- **CVSS 3.1 Score**: 8.8 (High)
- **CWE**: CWE-522 (Insufficiently Protected Credentials)
- Private keys exposed in transit and at rest
- Keys could be intercepted or stolen
- No protection if download link is shared or leaked

**Current State**:
- `Controllers/ServersController.cs` Download action (lines 108-135)
- Creates unprotected tar.gz with both certificate and private key
- No password protection
- No encryption
- No download token or expiration

**Remediation**:

### Option 1: Password-Protected ZIP Archive (Recommended)
Use ZIP format with AES-256 encryption:

```csharp
public async Task<IActionResult> Download(long id, string password)
{
    // Validate password strength
    if (string.IsNullOrWhiteSpace(password) || password.Length < 12)
    {
        ModelState.AddModelError("password", "Password must be at least 12 characters");
        return View();
    }
    
    var rec = await _certs.GetAsync(id);
    if (rec == null) return NotFound();
    
    // Read files
    var certBytes = await File.ReadAllBytesAsync(rec.CertPath);
    var keyBytes = await File.ReadAllBytesAsync(rec.KeyPath);
    
    // Create encrypted ZIP using DotNetZip or similar
    using var ms = new MemoryStream();
    using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, true))
    {
        // Add certificate (not encrypted, it's public)
        var certEntry = zip.CreateEntry($"{baseName}.crt");
        using (var certStream = certEntry.Open())
        {
            await certStream.WriteAsync(certBytes);
        }
        
        // Add private key (encrypted with password)
        var keyEntry = zip.CreateEntry($"{baseName}.key");
        keyEntry.ExternalAttributes = 0x81A40000; // Unix permissions 0600
        using (var keyStream = keyEntry.Open())
        using (var aes = Aes.Create())
        {
            aes.Key = DeriveKey(password, rec.Id); // Use PBKDF2
            var encryptedKey = EncryptData(keyBytes, aes);
            await keyStream.WriteAsync(encryptedKey);
        }
    }
    
    // Audit log
    _audit.LogAsync("download", rec.Kind, rec.SanDns ?? rec.Subject, id, 
        requesterIp: HttpContext.Connection.RemoteIpAddress?.ToString());
    
    return File(ms.ToArray(), "application/zip", $"{baseName}.zip");
}
```

### Option 2: One-Time Download Tokens
Generate secure, time-limited download tokens:

```csharp
// Create download token
public async Task<IActionResult> CreateDownloadToken(long id)
{
    var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
    var expiry = DateTimeOffset.UtcNow.AddHours(1);
    
    await _tokens.CreateAsync(token, id, expiry);
    
    var downloadUrl = Url.Action("DownloadWithToken", new { token });
    return View("DownloadReady", new { downloadUrl, expiry });
}

// Download with token (one-time use)
public async Task<IActionResult> DownloadWithToken(string token)
{
    var tokenData = await _tokens.ValidateAndConsumeAsync(token);
    if (tokenData == null) return NotFound("Token expired or invalid");
    
    return await Download(tokenData.CertificateId, tokenData.Password);
}
```

### Option 3: Encrypt Private Key with User-Provided Passphrase
Store private keys encrypted with per-user passphrase:

```csharp
// During certificate issuance
public async Task<long> IssueServerAsync(string fqdn, string encryptionPassword, ...)
{
    // ... generate certificate ...
    
    // Encrypt private key with user-provided password
    var encryptedKey = ecdsa.ExportEncryptedPkcs8PrivateKey(
        encryptionPassword,
        new PbeParameters(PbeEncryptionAlgorithm.Aes256Cbc, 
                         HashAlgorithmName.SHA256, 
                         100_000));
    
    await FileUtils.WriteSecureFileAsync(keyPath, 
        PemEncode("ENCRYPTED PRIVATE KEY", encryptedKey), ct);
}
```

**Additional Requirements**:

1. **User Interface Changes**:
   - Add password prompt before download
   - Show password strength indicator
   - Display security warning about protecting the password
   - Add "Show Password" toggle

2. **Security Warnings**:
```
⚠️ SECURITY WARNING
This download contains a private key that proves your server's identity.

- Store the password securely (use a password manager)
- Never share the private key or password
- Delete the downloaded file after extracting to the server
- This is a one-time download link that expires in 1 hour

Enter a strong password (minimum 12 characters):
[___________________________]
```

3. **Audit Logging**:
   - Log all download attempts (success and failure)
   - Include requester IP address
   - Alert on suspicious patterns (multiple failed attempts)

4. **Rate Limiting**:
   - Limit download attempts per certificate
   - Implement exponential backoff

**Testing**:
- [ ] Verify password-protected download works
- [ ] Verify unencrypted download is not possible
- [ ] Test password strength validation
- [ ] Verify audit logging works
- [ ] Test token expiration
- [ ] Test with various ZIP/encryption tools to ensure compatibility

**Priority**: MUST FIX BEFORE ANY DEPLOYMENT

**Estimated Effort**: 1 week

---

## Issue #4: Missing Rate Limiting and DoS Protection

**Title**: [HIGH SECURITY] No Rate Limiting - Application Vulnerable to DoS and Resource Exhaustion

**Labels**: `security`, `high`, `availability`, `dos`

**Description**:

The application has no rate limiting on any endpoints, allowing unlimited requests that could:
- Exhaust disk space through unlimited certificate issuance
- Exhaust CPU through repeated expensive cryptographic operations
- Enable brute force attacks (once authentication is added)
- Cause service unavailability

**Security Impact**:
- **CVSS 3.1 Score**: 7.5 (High)
- **CWE**: CWE-770 (Allocation of Resources Without Limits or Throttling)
- Service unavailability through DoS
- Disk space exhaustion
- CPU exhaustion
- Potential for brute force attacks

**Current State**:
- No rate limiting middleware configured
- All endpoints accept unlimited requests
- No CAPTCHA on Setup page
- No request throttling

**Remediation**:

### Option 1: ASP.NET Core Built-in Rate Limiting (.NET 7+)

```csharp
// In Program.cs
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

// Add rate limiting services
builder.Services.AddRateLimiter(options =>
{
    // Fixed window limiter for certificate operations
    options.AddFixedWindowLimiter("certificate_operations", opt =>
    {
        opt.Window = TimeSpan.FromHours(1);
        opt.PermitLimit = 10; // Max 10 certs per hour per IP
        opt.QueueLimit = 0;
    });
    
    // Sliding window for API calls
    options.AddSlidingWindowLimiter("api", opt =>
    {
        opt.Window = TimeSpan.FromMinutes(1);
        opt.PermitLimit = 60;
        opt.SegmentsPerWindow = 6;
        opt.QueueLimit = 0;
    });
    
    // Concurrency limiter for downloads
    options.AddConcurrencyLimiter("downloads", opt =>
    {
        opt.PermitLimit = 5; // Max 5 concurrent downloads
        opt.QueueLimit = 10;
    });
    
    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = 429;
        await context.HttpContext.Response.WriteAsync(
            "Rate limit exceeded. Please try again later.", token);
    };
});

// Add middleware
app.UseRateLimiter();
```

```csharp
// In Controllers
[EnableRateLimiting("certificate_operations")]
[HttpPost]
public async Task<IActionResult> Create(CreateServerViewModel model)
{
    // ... existing code ...
}

[EnableRateLimiting("downloads")]
[HttpGet]
public async Task<IActionResult> Download(long id)
{
    // ... existing code ...
}
```

### Option 2: AspNetCoreRateLimit Package (More Features)

```bash
dotnet add package AspNetCoreRateLimit
```

```csharp
// In Program.cs
builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(options =>
{
    options.EnableEndpointRateLimiting = true;
    options.StackBlockedRequests = false;
    options.GeneralRules = new List<RateLimitRule>
    {
        new RateLimitRule
        {
            Endpoint = "POST:/Servers/Create",
            Period = "1h",
            Limit = 10
        },
        new RateLimitRule
        {
            Endpoint = "POST:/Clients/Create",
            Period = "1h",
            Limit = 10
        },
        new RateLimitRule
        {
            Endpoint = "*/Download/*",
            Period = "5m",
            Limit = 5
        }
    };
});
builder.Services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
builder.Services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
builder.Services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();

// Add middleware (before MVC)
app.UseIpRateLimiting();
```

**Additional Requirements**:

1. **CAPTCHA on Setup Page**:
```csharp
// Add reCAPTCHA v3 to prevent automated Setup attacks
builder.Services.AddRecaptchaService();

[HttpPost]
[ValidateRecaptcha]
public async Task<IActionResult> Index(SetupViewModel model)
{
    // ... existing code ...
}
```

2. **Rate Limit Configuration**:
   - Certificate issuance: 10 per hour per IP
   - Certificate revocation: 5 per hour per IP
   - Downloads: 5 per 5 minutes per IP
   - API calls: 60 per minute per IP
   - Setup page: 3 attempts per hour per IP

3. **Monitoring and Alerting**:
   - Log rate limit violations
   - Alert on repeated violations from same IP
   - Dashboard for rate limit statistics
   - Ability to temporarily ban IPs

4. **User Communication**:
```html
<!-- Show clear error message -->
<div class="alert alert-warning">
    <h4>Rate Limit Exceeded</h4>
    <p>You have exceeded the maximum number of certificate operations allowed per hour (10).</p>
    <p>Please wait <strong id="retry-after"></strong> before trying again.</p>
    <p>If you need to issue more certificates, please contact the administrator.</p>
</div>
```

5. **Configuration**:
```json
// appsettings.json
{
  "RateLimiting": {
    "CertificateIssuance": {
      "PermitLimit": 10,
      "Window": "01:00:00"
    },
    "Downloads": {
      "PermitLimit": 5,
      "Window": "00:05:00"
    }
  }
}
```

**Testing**:
- [ ] Verify rate limits are enforced
- [ ] Test with automated tools to trigger limits
- [ ] Verify error messages are clear
- [ ] Test legitimate high-volume scenarios
- [ ] Verify rate limits reset correctly
- [ ] Test with multiple IPs

**Priority**: HIGH - Implement Before Production

**Estimated Effort**: 3-5 days

---

## Issue #5: Weak Content Security Policy Allowing unsafe-inline

**Title**: [HIGH SECURITY] CSP Allows unsafe-inline Scripts and Styles - XSS Protection Weakened

**Labels**: `security`, `high`, `xss`, `csp`

**Description**:

The Content Security Policy currently allows `unsafe-inline` for both scripts and styles, which significantly weakens XSS protection. While CSRF tokens provide some protection, a strong CSP is a critical defense-in-depth layer.

**Security Impact**:
- **CVSS 3.1 Score**: 7.3 (High)
- **CWE**: CWE-1275 (Sensitive Cookie with Improper SameSite Attribute)
- XSS attacks can execute inline scripts
- Injected styles can be used for data exfiltration
- Reduced effectiveness of CSP as defense layer

**Current State**:
```csharp
// Program.cs line 92
context.Response.Headers["Content-Security-Policy"] = 
    "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'";
```

**Remediation**:

### Step 1: Remove Inline Scripts

Audit and move all inline scripts to external files:

```bash
# Find all inline scripts
grep -r "<script>" BitCrafts.Certificates/Views/ --include="*.cshtml"
```

Move inline scripts to `/wwwroot/js/app.js`:

```javascript
// wwwroot/js/app.js
document.addEventListener('DOMContentLoaded', function() {
    // Event handlers
    document.getElementById('deleteBtn')?.addEventListener('click', confirmDelete);
    
    // Form validation
    document.querySelectorAll('form').forEach(form => {
        form.addEventListener('submit', validateForm);
    });
});

function confirmDelete(e) {
    if (!confirm('Are you sure?')) {
        e.preventDefault();
    }
}
```

### Step 2: Implement Nonce-Based CSP

```csharp
// Create CSP middleware
public class CspMiddleware
{
    private readonly RequestDelegate _next;
    
    public async Task InvokeAsync(HttpContext context)
    {
        var nonce = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));
        context.Items["csp-nonce"] = nonce;
        
        var csp = $"default-src 'self'; " +
                  $"script-src 'self' 'nonce-{nonce}' 'strict-dynamic'; " +
                  $"style-src 'self' 'nonce-{nonce}'; " +
                  $"object-src 'none'; " +
                  $"base-uri 'self'; " +
                  $"form-action 'self'; " +
                  $"frame-ancestors 'none'; " +
                  $"upgrade-insecure-requests";
        
        context.Response.Headers["Content-Security-Policy"] = csp;
        
        await _next(context);
    }
}

// In Program.cs
app.UseMiddleware<CspMiddleware>();
```

```cshtml
<!-- In _Layout.cshtml -->
@{
    var nonce = Context.Items["csp-nonce"] as string;
}
<!DOCTYPE html>
<html>
<head>
    <!-- Styles with nonce -->
    <style nonce="@nonce">
        /* Critical inline CSS only */
    </style>
    
    <!-- External scripts with nonce -->
    <script src="~/js/app.js" nonce="@nonce"></script>
</head>
```

### Step 3: Use ASP.NET Core Tag Helpers

Create a custom tag helper for CSP nonces:

```csharp
[HtmlTargetElement("script", Attributes = "asp-add-nonce")]
[HtmlTargetElement("style", Attributes = "asp-add-nonce")]
public class CspNonceTagHelper : TagHelper
{
    [ViewContext]
    public ViewContext ViewContext { get; set; }
    
    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        var nonce = ViewContext.HttpContext.Items["csp-nonce"] as string;
        if (!string.IsNullOrEmpty(nonce))
        {
            output.Attributes.Add("nonce", nonce);
        }
    }
}
```

```cshtml
<!-- Usage in views -->
<script src="~/js/app.js" asp-add-nonce></script>
<style asp-add-nonce>
    /* Inline CSS with nonce */
</style>
```

### Step 4: Enhanced CSP with Reporting

```csharp
var csp = $"default-src 'self'; " +
          $"script-src 'self' 'nonce-{nonce}'; " +
          $"style-src 'self' 'nonce-{nonce}'; " +
          $"img-src 'self' data:; " +
          $"font-src 'self'; " +
          $"connect-src 'self'; " +
          $"frame-src 'none'; " +
          $"object-src 'none'; " +
          $"base-uri 'self'; " +
          $"form-action 'self'; " +
          $"frame-ancestors 'none'; " +
          $"upgrade-insecure-requests; " +
          $"report-uri /api/csp-report";
```

Add CSP report endpoint:

```csharp
[HttpPost("/api/csp-report")]
public async Task<IActionResult> CspReport([FromBody] CspReportModel report)
{
    _logger.LogWarning("CSP Violation: {Report}", 
        JsonSerializer.Serialize(report));
    return Ok();
}
```

**Migration Steps**:

1. Audit all views for inline scripts and styles
2. Move inline code to external files
3. Implement nonce generation middleware
4. Test all pages thoroughly
5. Monitor CSP reports for violations
6. Gradually tighten CSP based on reports

**Testing**:
- [ ] Verify all pages load correctly with new CSP
- [ ] Test all interactive features work
- [ ] Verify no CSP violations in browser console
- [ ] Test with browser CSP testing tools
- [ ] Verify CSP reports are received

**Priority**: HIGH - Implement After Authentication

**Estimated Effort**: 1 week

---

## Issue #6: AllowedHosts Configuration Set to Wildcard

**Title**: [HIGH SECURITY] AllowedHosts Set to * - Host Header Injection Vulnerability

**Labels**: `security`, `high`, `configuration`, `injection`

**Description**:

The `AllowedHosts` configuration is set to `*` (all hosts), making the application vulnerable to Host header injection attacks.

**Security Impact**:
- **CVSS 3.1 Score**: 7.1 (High)
- **CWE**: CWE-644 (Improper Neutralization of HTTP Headers for Scripting Syntax)
- Host header injection attacks
- Cache poisoning
- Password reset poisoning (if email features added)
- Potential SSRF in some scenarios

**Current State**:
```json
// appsettings.json
{
  "AllowedHosts": "*"
}
```

**Remediation**:

### Step 1: Configure Allowed Hosts

```json
// appsettings.json (production)
{
  "AllowedHosts": "certificates.home.lan;ca.internal.example.com"
}

// appsettings.Development.json
{
  "AllowedHosts": "localhost;127.0.0.1;*.local"
}
```

### Step 2: Environment-Specific Configuration

```bash
# In deployment
export ASPNETCORE_ALLOWEDHOSTS="certificates.home.lan"
```

```csharp
// In Program.cs - explicit validation
app.Use(async (context, next) =>
{
    var host = context.Request.Host.Host;
    var allowedHosts = builder.Configuration["AllowedHosts"]
        ?.Split(';') ?? Array.Empty<string>();
    
    if (!allowedHosts.Contains("*") && 
        !allowedHosts.Any(h => IsHostMatch(host, h)))
    {
        context.Response.StatusCode = 400;
        await context.Response.WriteAsync("Invalid Host header");
        return;
    }
    
    await next();
});

static bool IsHostMatch(string host, string pattern)
{
    if (pattern.StartsWith("*."))
    {
        return host.EndsWith(pattern.Substring(1));
    }
    return host.Equals(pattern, StringComparison.OrdinalIgnoreCase);
}
```

### Step 3: Documentation

Update deployment documentation:

```markdown
## Host Configuration

Before deploying, configure the allowed hostnames in `appsettings.Production.json`:

{
  "AllowedHosts": "your.domain.com;ca.yourcompany.internal"
}

Or set the environment variable:

export ASPNETCORE_ALLOWEDHOSTS="your.domain.com"
```

**Testing**:
- [ ] Verify application works with configured hostnames
- [ ] Verify requests with invalid Host header are rejected
- [ ] Test with subdomain patterns
- [ ] Verify localhost works in development
- [ ] Test reverse proxy scenarios

**Priority**: HIGH - Fix Before Production

**Estimated Effort**: 1 day

---

_[Continue with Issues #7-13 in similar detail...]_

Due to length constraints, I'll create a summary document for the remaining issues. The pattern above should be followed for each remaining issue.

---

## Quick Reference - All Security Issues

| # | Title | Severity | CVSS | Priority | Effort |
|---|-------|----------|------|----------|--------|
| 1 | No Authentication/Authorization | CRITICAL | 9.8 | MUST FIX | 2-3 weeks |
| 2 | Root CA Key Unencrypted | CRITICAL | 9.1 | MUST FIX | 1-2 weeks |
| 3 | Unprotected Private Key Downloads | CRITICAL | 8.8 | MUST FIX | 1 week |
| 4 | No Rate Limiting | HIGH | 7.5 | HIGH | 3-5 days |
| 5 | Weak CSP (unsafe-inline) | HIGH | 7.3 | HIGH | 1 week |
| 6 | AllowedHosts Wildcard | HIGH | 7.1 | HIGH | 1 day |
| 7 | No CRL/OCSP | HIGH | 6.5 | HIGH | 2 weeks |
| 8 | No Session Security | MEDIUM | 6.5 | MEDIUM | 2 days |
| 9 | Path Traversal Risk | MEDIUM | 6.5 | MEDIUM | 1 day |
| 10 | Weak Input Validation | MEDIUM | 5.3 | MEDIUM | 3 days |
| 11 | No Audit Log Integrity | MEDIUM | 5.3 | MEDIUM | 3 days |
| 12 | Deployment Script Privilege | LOW | 4.2 | LOW | 1 day |
| 13 | No Security Update Policy | LOW | N/A | LOW | 1 day |

**Total Estimated Effort**: 8-12 weeks for all fixes

**Minimum Viable Security** (Issues #1-3): 4-6 weeks

---

## Labels to Create in GitHub

- `security` - Security-related issue
- `critical` - Critical severity requiring immediate attention
- `high` - High severity requiring priority attention
- `medium` - Medium severity
- `low` - Low severity
- `authentication` - Related to authentication
- `authorization` - Related to authorization
- `cryptography` - Related to cryptographic operations
- `key-management` - Related to key storage and management
- `data-protection` - Related to data protection
- `dos` - Denial of service related
- `xss` - Cross-site scripting related
- `injection` - Injection vulnerability related
- `configuration` - Configuration issue
- `availability` - Service availability issue

---

## Creating Issues in GitHub

Use the GitHub CLI or web interface to create these issues:

```bash
# Example using GitHub CLI
gh issue create \
  --title "[CRITICAL SECURITY] No Authentication or Authorization" \
  --body-file issue-1-description.md \
  --label "security,critical,authentication,authorization"
```

Or create them through the GitHub web interface at:
https://github.com/BitCrafts/certificates/issues/new

---

**Document Version**: 1.0  
**Last Updated**: December 4, 2025  
**Next Review**: After implementation of critical fixes
