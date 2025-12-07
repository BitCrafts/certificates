// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (C) 2025 BitCrafts

using BitCrafts.Certificates.Abstractions.Models;
using BitCrafts.Certificates.Abstractions.Results;

namespace BitCrafts.Certificates.Abstractions.Interfaces;

/// <summary>
/// Deploys certificates to local or network file systems.
/// </summary>
public interface IFileSystemDeployer
{
    /// <summary>
    /// Deploys a certificate to the specified path.
    /// </summary>
    Task<DeploymentResult> DeployAsync(Certificate certificate, DeploymentTarget target, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates that the target path is accessible.
    /// </summary>
    Task<bool> ValidatePathAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets ownership and permissions on the deployed file.
    /// </summary>
    Task SetPermissionsAsync(string path, string? owner, string? group, string? permissions, CancellationToken cancellationToken = default);
}
