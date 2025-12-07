// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (C) 2025 BitCrafts

using BitCrafts.Certificates.Abstractions.Models;

namespace BitCrafts.Certificates.Abstractions.Interfaces;

/// <summary>
/// Repository for certificate persistence.
/// </summary>
public interface ICertificateRepository
{
    /// <summary>
    /// Saves a certificate to the repository.
    /// </summary>
    Task SaveAsync(Certificate certificate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a certificate by its ID.
    /// </summary>
    Task<Certificate?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Queries certificates by metadata criteria.
    /// </summary>
    Task<IEnumerable<Certificate>> QueryByMetadataAsync(string? commonName = null, CertificateType? type = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets certificates ready for deployment.
    /// </summary>
    Task<IEnumerable<Certificate>> GetForDeploymentAsync(int workflowId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing certificate.
    /// </summary>
    Task UpdateAsync(Certificate certificate, CancellationToken cancellationToken = default);
}
