// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (C) 2025 Younes Benmoussa <benzsoftware@pm.me>
// 
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.
// You should have received a copy of the GNU Affero General Public License along with this program. If not, see <https://www.gnu.org/licenses/>. 

using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using BitCrafts.Certificates.Data.Repositories;
using BitCrafts.Certificates.Models;
using BitCrafts.Certificates.Services;

namespace BitCrafts.Certificates.Pki;

public interface ILeafCertificateService
{
    Task<long> IssueServerAsync(string fqdn, IEnumerable<string>? ipAddresses = null, IEnumerable<string>? dnsNames = null, CancellationToken ct = default);
    Task<long> IssueClientAsync(string username, string? email = null, CancellationToken ct = default);
}

public sealed class LeafCertificateService : ILeafCertificateService
{
    private readonly IDataDirectory _data;
    private readonly ICaService _ca;
    private readonly ICertificatesRepository _certs;
    private readonly ILogger<LeafCertificateService> _logger;
    private readonly IAuditLogger _audit;

    public LeafCertificateService(IDataDirectory data, ICaService ca, ICertificatesRepository certs, ILogger<LeafCertificateService> logger, IAuditLogger audit)
    {
        _data = data;
        _ca = ca;
        _certs = certs;
        _logger = logger;
        _audit = audit;
    }

    public async Task<long> IssueServerAsync(string fqdn, IEnumerable<string>? ipAddresses = null, IEnumerable<string>? dnsNames = null, CancellationToken ct = default)
    {
        if (!IsValidFqdn(fqdn)) throw new ArgumentException("Invalid FQDN", nameof(fqdn));
        var ips = (ipAddresses ?? Array.Empty<string>()).Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim()).ToArray();
        foreach (var ip in ips)
        {
            if (!IPAddress.TryParse(ip, out _)) throw new ArgumentException($"Invalid IP: {ip}");
        }
        
        var additionalDnsNames = (dnsNames ?? Array.Empty<string>()).Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim()).ToArray();
        foreach (var dns in additionalDnsNames)
        {
            if (!IsValidFqdn(dns)) throw new ArgumentException($"Invalid DNS name: {dns}");
        }

        // Load issuer (root CA)
        var issuer = X509Certificate2.CreateFromPemFile(_ca.RootCertPath, _ca.RootKeyPath);
        using var issuerKey = issuer.GetECDsaPrivateKey() ?? throw new InvalidOperationException("Root CA private key not available");

        // Generate ECDSA P-256 key for leaf
        using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var subject = new X500DistinguishedName($"CN={fqdn}");
        var req = new CertificateRequest(subject, ecdsa, HashAlgorithmName.SHA256);

        // Extensions
        req.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, false));
        req.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(req.PublicKey, false));
        req.CertificateExtensions.Add(new X509KeyUsageExtension(
            X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment, false));
        // EKU: Server Authentication
        req.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(
            new OidCollection
            {
                new Oid("1.3.6.1.5.5.7.3.1", "Server Authentication")
            }, false));
        // SANs
        var san = new SubjectAlternativeNameBuilder();
        san.AddDnsName(fqdn);
        foreach (var dns in additionalDnsNames)
        {
            san.AddDnsName(dns);
        }
        foreach (var ip in ips)
        {
            if (IPAddress.TryParse(ip, out var ipObj)) san.AddIpAddress(ipObj);
        }
        req.CertificateExtensions.Add(san.Build());

        // Validity
        var notBefore = DateTimeOffset.UtcNow.AddMinutes(-5);
        var notAfter = notBefore.AddDays(397);

        using var cert = req.Create(issuer, notBefore, notAfter, RandomNumberGenerator.GetBytes(16));

        // Export leaf as PEM and key as PKCS#8 PEM
        var certPem = ExportCertificatePem(cert);
        var keyPem = PemEncode("PRIVATE KEY", ecdsa.ExportPkcs8PrivateKey());
        var rootPem = await File.ReadAllTextAsync(_ca.RootCertPath, ct);

        // Persist to disk
        var targetDir = Path.Combine(_data.CertsServersDir, fqdn);
        Directory.CreateDirectory(targetDir);
        FileUtils.TrySet0700(targetDir);
        var certPath = Path.Combine(targetDir, "cert.crt");
        var keyPath = Path.Combine(targetDir, "key.pem");
        var chainPath = Path.Combine(targetDir, "chain.crt");
        await FileUtils.WriteSecureFileAsync(certPath, certPem, ct);
        await FileUtils.WriteSecureFileAsync(keyPath, keyPem, ct, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        await FileUtils.WriteSecureFileAsync(chainPath, certPem + rootPem, ct);
        FileUtils.TrySet0600(keyPath);

        // Record metadata
        var now = DateTimeOffset.UtcNow.ToString("O");
        var rec = new CertificateRecord
        {
            Kind = "server",
            Subject = subject.Name,
            SanDns = fqdn,
            SanIp = ips.Length > 0 ? string.Join(',', ips) : null,
            KeyPath = keyPath,
            CertPath = certPath,
            ChainPath = chainPath,
            NotBefore = notBefore.ToString("O"),
            NotAfter = notAfter.ToString("O"),
            Status = "active",
            CreatedAt = now,
            UpdatedAt = now
        };
        var id = await _certs.InsertAsync(rec, ct);
        _logger.LogInformation("Issued server cert for {Fqdn} -> {CertPath}", fqdn, certPath);
        _audit.LogAsync("issue", "server", fqdn, id, null, keyPath, certPath, chainPath, ct);
        return id;
    }

    public async Task<long> IssueClientAsync(string username, string? email = null, CancellationToken ct = default)
    {
        if (!IsValidUsername(username)) throw new ArgumentException("Invalid username", nameof(username));
        if (!string.IsNullOrWhiteSpace(email) && !IsLikelyEmail(email))
            throw new ArgumentException("Invalid email", nameof(email));

        // Load issuer (root CA)
        var issuer = X509Certificate2.CreateFromPemFile(_ca.RootCertPath, _ca.RootKeyPath);
        using var issuerKey = issuer.GetECDsaPrivateKey() ?? throw new InvalidOperationException("Root CA private key not available");

        // Generate ECDSA P-256 key for leaf
        using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var subject = new X500DistinguishedName($"CN={username}");
        var req = new CertificateRequest(subject, ecdsa, HashAlgorithmName.SHA256);

        // Extensions
        req.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, false));
        req.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(req.PublicKey, false));
        req.CertificateExtensions.Add(new X509KeyUsageExtension(
            X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment, false));
        // EKU: Client Authentication
        req.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(
            new OidCollection
            {
                new Oid("1.3.6.1.5.5.7.3.2", "Client Authentication")
            }, false));

        // SANs: RFC822Name when email provided
        if (!string.IsNullOrWhiteSpace(email))
        {
            var san = new SubjectAlternativeNameBuilder();
            san.AddEmailAddress(email);
            req.CertificateExtensions.Add(san.Build());
        }

        // Validity
        var notBefore = DateTimeOffset.UtcNow.AddMinutes(-5);
        var notAfter = notBefore.AddDays(397);

        using var cert = req.Create(issuer, notBefore, notAfter, RandomNumberGenerator.GetBytes(16));

        // Export leaf as PEM and key as PKCS#8 PEM
        var certPem = ExportCertificatePem(cert);
        var keyPem = PemEncode("PRIVATE KEY", ecdsa.ExportPkcs8PrivateKey());
        var rootPem = await File.ReadAllTextAsync(_ca.RootCertPath, ct);

        // Persist to disk under clients/<user>
        var safeUser = username.Trim();
        var targetDir = Path.Combine(_data.CertsClientsDir, safeUser);
        Directory.CreateDirectory(targetDir);
        FileUtils.TrySet0700(targetDir);
        var certPath = Path.Combine(targetDir, "cert.crt");
        var keyPath = Path.Combine(targetDir, "key.pem");
        var chainPath = Path.Combine(targetDir, "chain.crt");
        await FileUtils.WriteSecureFileAsync(certPath, certPem, ct);
        await FileUtils.WriteSecureFileAsync(keyPath, keyPem, ct, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        await FileUtils.WriteSecureFileAsync(chainPath, certPem + rootPem, ct);
        FileUtils.TrySet0600(keyPath);

        // Record metadata
        var now = DateTimeOffset.UtcNow.ToString("O");
        var rec = new CertificateRecord
        {
            Kind = "client",
            Subject = subject.Name,
            SanDns = string.IsNullOrWhiteSpace(email) ? null : email,
            SanIp = null,
            KeyPath = keyPath,
            CertPath = certPath,
            ChainPath = chainPath,
            NotBefore = notBefore.ToString("O"),
            NotAfter = notAfter.ToString("O"),
            Status = "active",
            CreatedAt = now,
            UpdatedAt = now
        };
        var id = await _certs.InsertAsync(rec, ct);
        _logger.LogInformation("Issued client cert for {User} -> {CertPath}", username, certPath);
        _audit.LogAsync("issue", "client", username, id, null, keyPath, certPath, chainPath, ct);
        return id;
    }

    private static bool IsValidFqdn(string fqdn)
    {
        if (string.IsNullOrWhiteSpace(fqdn)) return false;
        // Basic conservative check: labels with letters/digits/hyphen separated by dots
        var parts = fqdn.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2) return false;
        foreach (var p in parts)
        {
            if (p.Length == 0 || p.Length > 63) return false;
            if (!p.All(c => char.IsLetterOrDigit(c) || c == '-')) return false;
            if (p.StartsWith('-') || p.EndsWith('-')) return false;
        }
        return true;
    }

    private static string ExportCertificatePem(X509Certificate2 cert)
    {
        var der = cert.Export(X509ContentType.Cert);
        return PemEncode("CERTIFICATE", der);
    }

    private static string PemEncode(string label, byte[] der)
    {
        var b64 = Convert.ToBase64String(der, Base64FormattingOptions.InsertLineBreaks);
        return $"-----BEGIN {label}-----\n{b64}\n-----END {label}-----\n";
    }

    private static bool IsValidUsername(string username)
    {
        if (string.IsNullOrWhiteSpace(username)) return false;
        // conservative: letters, digits, dash, underscore, dot
        return username.All(c => char.IsLetterOrDigit(c) || c == '-' || c == '_' || c == '.');
    }

    private static bool IsLikelyEmail(string email)
    {
        // very basic check to avoid pulling regex deps; controller also validates via DataAnnotations
        var at = email.IndexOf('@');
        var dot = email.LastIndexOf('.');
        return at > 0 && dot > at + 1 && dot < email.Length - 1;
    }
}
