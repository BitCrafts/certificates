// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (C) 2025 BitCrafts

using BitCrafts.Certificates.Abstractions.Models;

namespace BitCrafts.Certificates.Abstractions.Interfaces;

/// <summary>
/// Factory for creating SSH clients.
/// </summary>
public interface ISshClientFactory
{
    /// <summary>
    /// Creates an SSH client for the specified target.
    /// </summary>
    ISshClient CreateClient(DeploymentTarget target);
}

/// <summary>
/// SSH client for remote operations.
/// </summary>
public interface ISshClient : IDisposable
{
    /// <summary>
    /// Connects to the remote host.
    /// </summary>
    Task ConnectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Uploads a file to the remote host.
    /// </summary>
    Task UploadFileAsync(byte[] data, string remotePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a command on the remote host.
    /// </summary>
    Task<string> ExecuteCommandAsync(string command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets file permissions on the remote host.
    /// </summary>
    Task SetPermissionsAsync(string remotePath, string permissions, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets file ownership on the remote host.
    /// </summary>
    Task SetOwnershipAsync(string remotePath, string owner, string? group = null, CancellationToken cancellationToken = default);
}
