// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (C) 2025 Younes Benmoussa <benzsoftware@pm.me>
// 
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.
// You should have received a copy of the GNU Affero General Public License along with this program. If not, see <https://www.gnu.org/licenses/>. 

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using BitCrafts.Certificates.Data.Repositories;
using BitCrafts.Certificates.Services;

namespace BitCrafts.Certificates.Pki;

public interface ICaService
{
    Task CreateRootCaIfMissingAsync(string domain, CancellationToken ct = default);
    string RootCertPath { get; }
    string RootKeyPath { get; }
}

public sealed class CaService : ICaService
{
    private readonly IDataDirectory _data;
    private readonly ISettingsRepository _settings;
    private readonly ILogger<CaService> _logger;

    public CaService(IDataDirectory data, ISettingsRepository settings, ILogger<CaService> logger)
    {
        _data = data;
        _settings = settings;
        _logger = logger;
        RootCertPath = Path.Combine(_data.CaDir, "root_ca.crt");
        RootKeyPath = Path.Combine(_data.CaDir, "root_ca.key");
    }

    public string RootCertPath { get; }
    public string RootKeyPath { get; }

    public async Task CreateRootCaIfMissingAsync(string domain, CancellationToken ct = default)
    {
        Directory.CreateDirectory(_data.CaDir);
        if (File.Exists(RootCertPath) && File.Exists(RootKeyPath))
        {
            _logger.LogInformation("Root CA already exists at {CaDir}", _data.CaDir);
            await _settings.SetAsync("CA_INITIALIZED", "true", ct);
            return;
        }

        _logger.LogInformation("Creating new ECDSA P-256 root CA for domain {Domain}", domain);

        using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);

        var subject = new X500DistinguishedName($"CN={domain}, O=BitCrafts Root CA");
        var req = new CertificateRequest(subject, ecdsa, HashAlgorithmName.SHA256);
        req.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
        req.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(req.PublicKey, false));
        req.CertificateExtensions.Add(new X509KeyUsageExtension(
            X509KeyUsageFlags.KeyCertSign | X509KeyUsageFlags.CrlSign, true));

        var notBefore = DateTimeOffset.UtcNow.AddMinutes(-5);
        var notAfter = notBefore.AddYears(10);
        using var root = req.CreateSelfSigned(notBefore, notAfter);

        // Export cert (PEM) and private key (PKCS#8 PEM)
        var certPem = ExportCertificatePem(root);
        var pkcs8 = ecdsa.ExportPkcs8PrivateKey();
        var keyPem = PemEncode("PRIVATE KEY", pkcs8);

        // Write files atomically to avoid briefly exposing partial files.
        await FileUtils.WriteSecureFileAsync(RootCertPath, certPem, ct);
        await FileUtils.WriteSecureFileAsync(RootKeyPath, keyPem, ct, UnixFileMode.UserRead | UnixFileMode.UserWrite);

        FileUtils.TrySet0600(RootKeyPath);
        FileUtils.TrySet0700(_data.CaDir);

        await _settings.SetAsync("CA_INITIALIZED", "true", ct);
        _logger.LogInformation("Root CA created: {Cert} / {Key}", RootCertPath, RootKeyPath);
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

    // Permission and secure write helpers are provided by FileUtils.
}
