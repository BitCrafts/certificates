// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (C) 2025 BitCrafts

using BitCrafts.Certificates.Abstractions.Interfaces;
using BitCrafts.Certificates.Abstractions.Models;
using BitCrafts.Certificates.Linux.ProcessWrappers;

namespace BitCrafts.Certificates.Linux.Services;

/// <summary>
/// Factory for creating SSH clients using OpenSSH.
/// </summary>
public class SshClientFactoryOpenSsh : ISshClientFactory
{
    public ISshClient CreateClient(DeploymentTarget target)
    {
        return new SshClientOpenSsh(target);
    }
}

/// <summary>
/// SSH client implementation using system ssh command.
/// </summary>
internal class SshClientOpenSsh : ISshClient
{
    private readonly DeploymentTarget _target;
    private readonly SshWrapper _ssh;
    private bool _disposed;

    public SshClientOpenSsh(DeploymentTarget target)
    {
        _target = target;
        _ssh = new SshWrapper();
    }

    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        var result = await _ssh.TestConnectionAsync(
            _target.HostnameOrIp,
            _target.Username,
            _target.Port,
            _target.PrivateKeyPath,
            cancellationToken);

        if (!result.Success)
        {
            throw new InvalidOperationException($"Failed to connect to {_target.HostnameOrIp}: {result.Error}");
        }
    }

    public async Task UploadFileAsync(byte[] data, string remotePath, CancellationToken cancellationToken = default)
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            await File.WriteAllBytesAsync(tempFile, data, cancellationToken);

            var result = await _ssh.CopyFileAsync(
                tempFile,
                _target.HostnameOrIp,
                remotePath,
                _target.Username,
                _target.Port,
                _target.PrivateKeyPath,
                cancellationToken);

            if (!result.Success)
            {
                throw new InvalidOperationException($"Failed to upload file: {result.Error}");
            }
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    public async Task<string> ExecuteCommandAsync(string command, CancellationToken cancellationToken = default)
    {
        var result = await _ssh.ExecuteRemoteCommandAsync(
            _target.HostnameOrIp,
            command,
            _target.Username,
            _target.Port,
            _target.PrivateKeyPath,
            cancellationToken);

        if (!result.Success)
        {
            throw new InvalidOperationException($"Command execution failed: {result.Error}");
        }

        return result.Output;
    }

    public async Task SetPermissionsAsync(string remotePath, string permissions, CancellationToken cancellationToken = default)
    {
        var command = $"chmod {permissions} {remotePath}";
        await ExecuteCommandAsync(command, cancellationToken);
    }

    public async Task SetOwnershipAsync(string remotePath, string owner, string? group = null, CancellationToken cancellationToken = default)
    {
        var ownerGroup = string.IsNullOrEmpty(group) ? owner : $"{owner}:{group}";
        var command = $"chown {ownerGroup} {remotePath}";
        await ExecuteCommandAsync(command, cancellationToken);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
        }
    }
}
