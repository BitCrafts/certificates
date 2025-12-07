// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (C) 2025 BitCrafts

using BitCrafts.Certificates.Abstractions.Models;
using BitCrafts.Certificates.Abstractions.Results;

namespace BitCrafts.Certificates.Abstractions.Interfaces;

/// <summary>
/// Service for creating and managing certificates.
/// </summary>
public interface ICertificateService
{
    /// <summary>
    /// Creates a new certificate based on the provided metadata.
    /// </summary>
    Task<CertificateResult> CreateCertificateAsync(CertificateMetadata metadata, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates certificate metadata before creation.
    /// </summary>
    Task<ValidationResult> ValidateMetadataAsync(CertificateMetadata metadata, CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports a certificate to PEM format.
    /// </summary>
    Task<byte[]> ExportToPemAsync(Certificate certificate, CancellationToken cancellationToken = default);
}
