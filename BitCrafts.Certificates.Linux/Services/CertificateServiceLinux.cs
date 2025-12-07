// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (C) 2025 BitCrafts

using BitCrafts.Certificates.Abstractions.Interfaces;
using BitCrafts.Certificates.Abstractions.Models;
using BitCrafts.Certificates.Abstractions.Results;
using BitCrafts.Certificates.Abstractions.Exceptions;
using BitCrafts.Certificates.Linux.ProcessWrappers;

namespace BitCrafts.Certificates.Linux.Services;

/// <summary>
/// Linux-specific certificate service using OpenSSL.
/// </summary>
public class CertificateServiceLinux : ICertificateService
{
    private readonly OpenSslWrapper _openSsl;
    private readonly string _workingDirectory;

    public CertificateServiceLinux(string? workingDirectory = null)
    {
        _openSsl = new OpenSslWrapper();
        _workingDirectory = workingDirectory ?? Path.Combine(Path.GetTempPath(), "certificates");
        Directory.CreateDirectory(_workingDirectory);
    }

    public async Task<CertificateResult> CreateCertificateAsync(
        CertificateMetadata metadata,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var validationResult = await ValidateMetadataAsync(metadata, cancellationToken);
            if (!validationResult.IsValid)
            {
                return CertificateResult.FailureResult(string.Join(", ", validationResult.Errors));
            }

            // Generate private key
            var keyPath = Path.Combine(_workingDirectory, $"{Guid.NewGuid()}.key");
            var keyResult = await _openSsl.GeneratePrivateKeyAsync(keyPath, cancellationToken: cancellationToken);
            if (!keyResult.Success)
            {
                throw new CertificateCreationException($"Failed to generate private key: {keyResult.Error}");
            }

            // Create CSR
            var csrPath = Path.Combine(_workingDirectory, $"{Guid.NewGuid()}.csr");
            var subject = BuildSubject(metadata);
            var csrResult = await _openSsl.GenerateCertificateSigningRequestAsync(keyPath, csrPath, subject, cancellationToken);
            if (!csrResult.Success)
            {
                throw new CertificateCreationException($"Failed to generate CSR: {csrResult.Error}");
            }

            // For now, create a self-signed certificate (in production, this would be signed by CA)
            var certPath = Path.Combine(_workingDirectory, $"{Guid.NewGuid()}.crt");
            var signResult = await _openSsl.SignCertificateAsync(
                csrPath,
                certPath, // Using cert as CA for self-signed
                keyPath,  // Using key as CA key for self-signed
                certPath,
                metadata.ValidityDays,
                cancellationToken: cancellationToken);

            if (!signResult.Success)
            {
                throw new CertificateCreationException($"Failed to sign certificate: {signResult.Error}");
            }

            // Read certificate and key data
            var certData = await File.ReadAllBytesAsync(certPath, cancellationToken);
            var keyData = await File.ReadAllBytesAsync(keyPath, cancellationToken);

            // Combine cert and key into a single blob
            var combinedData = CombineCertAndKey(certData, keyData);

            // Clean up temporary files
            File.Delete(keyPath);
            File.Delete(csrPath);
            File.Delete(certPath);

            var certificate = new Certificate
            {
                Metadata = metadata,
                EncryptedData = combinedData, // Will be encrypted by the caller
                CreatedAt = DateTime.UtcNow
            };

            return CertificateResult.SuccessResult(certificate);
        }
        catch (Exception ex)
        {
            return CertificateResult.FailureResult($"Certificate creation failed: {ex.Message}");
        }
    }

    public Task<ValidationResult> ValidateMetadataAsync(
        CertificateMetadata metadata,
        CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(metadata.CommonName))
            errors.Add("Common name is required");

        if (metadata.ValidityDays <= 0)
            errors.Add("Validity days must be greater than 0");

        if (metadata.ValidityDays > 3650)
            errors.Add("Validity days cannot exceed 3650 (10 years)");

        return Task.FromResult(errors.Any()
            ? ValidationResult.Invalid(errors.ToArray())
            : ValidationResult.Valid());
    }

    public Task<byte[]> ExportToPemAsync(Certificate certificate, CancellationToken cancellationToken = default)
    {
        if (certificate.EncryptedData == null)
            throw new CertificateException("Certificate has no data to export");

        return Task.FromResult(certificate.EncryptedData);
    }

    private string BuildSubject(CertificateMetadata metadata)
    {
        var parts = new List<string> { $"CN={metadata.CommonName}" };

        if (!string.IsNullOrEmpty(metadata.Country))
            parts.Add($"C={metadata.Country}");
        if (!string.IsNullOrEmpty(metadata.State))
            parts.Add($"ST={metadata.State}");
        if (!string.IsNullOrEmpty(metadata.Locality))
            parts.Add($"L={metadata.Locality}");
        if (!string.IsNullOrEmpty(metadata.Organization))
            parts.Add($"O={metadata.Organization}");
        if (!string.IsNullOrEmpty(metadata.OrganizationalUnit))
            parts.Add($"OU={metadata.OrganizationalUnit}");

        return "/" + string.Join("/", parts);
    }

    private byte[] CombineCertAndKey(byte[] certData, byte[] keyData)
    {
        var combined = new byte[certData.Length + keyData.Length];
        Buffer.BlockCopy(certData, 0, combined, 0, certData.Length);
        Buffer.BlockCopy(keyData, 0, combined, certData.Length, keyData.Length);
        return combined;
    }
}
