// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (C) 2025 BitCrafts

using BitCrafts.Certificates.Abstractions.Models;

namespace BitCrafts.Certificates.Abstractions.Interfaces;

/// <summary>
/// Resolves and validates deployment targets.
/// </summary>
public interface ITargetResolver
{
    /// <summary>
    /// Resolves a hostname to IP addresses.
    /// </summary>
    Task<IEnumerable<string>> ResolveHostnameAsync(string hostname, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates connectivity to a target.
    /// </summary>
    Task<bool> ValidateConnectivityAsync(DeploymentTarget target, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tests if a target is reachable.
    /// </summary>
    Task<bool> IsReachableAsync(string hostnameOrIp, int? port = null, CancellationToken cancellationToken = default);
}
