// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (C) 2025 BitCrafts

using System.Diagnostics;
using BitCrafts.Certificates.Abstractions.Interfaces;
using BitCrafts.Certificates.Abstractions.Models;
using BitCrafts.Certificates.Abstractions.Results;
using BitCrafts.Certificates.Abstractions.Exceptions;

namespace BitCrafts.Certificates.Linux.Services;

/// <summary>
/// Deploys certificates to local or network file systems with Linux permissions.
/// </summary>
public class FileSystemDeployerLinux : IFileSystemDeployer
{
    public async Task<DeploymentResult> DeployAsync(
        Certificate certificate,
        DeploymentTarget target,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (certificate.EncryptedData == null)
            {
                return DeploymentResult.FailureResult("Certificate has no data to deploy");
            }

            // Validate target path
            if (!await ValidatePathAsync(target.DestinationPath, cancellationToken))
            {
                return DeploymentResult.FailureResult($"Target path is not accessible: {target.DestinationPath}");
            }

            // Construct full file path
            var fileName = $"{certificate.Metadata.CommonName.Replace("*", "wildcard")}.pem";
            var fullPath = Path.Combine(target.DestinationPath, fileName);

            // Write certificate data to file with restricted permissions
            await File.WriteAllBytesAsync(fullPath, certificate.EncryptedData, cancellationToken);

            // Set permissions and ownership
            await SetPermissionsAsync(
                fullPath,
                target.Owner,
                target.Group,
                target.Permissions ?? "0600",
                cancellationToken);

            return DeploymentResult.SuccessResult($"Certificate deployed to {fullPath}");
        }
        catch (Exception ex)
        {
            return DeploymentResult.FailureResult($"Deployment failed: {ex.Message}");
        }
    }

    public Task<bool> ValidatePathAsync(string path, CancellationToken cancellationToken = default)
    {
        try
        {
            var directoryInfo = new DirectoryInfo(path);
            return Task.FromResult(directoryInfo.Exists);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    public async Task SetPermissionsAsync(
        string path,
        string? owner,
        string? group,
        string? permissions,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Set file permissions using chmod
            if (!string.IsNullOrEmpty(permissions))
            {
                await ExecuteCommandAsync($"chmod {permissions} {path}", cancellationToken);
            }

            // Set ownership using chown
            if (!string.IsNullOrEmpty(owner))
            {
                var ownerGroup = string.IsNullOrEmpty(group) ? owner : $"{owner}:{group}";
                await ExecuteCommandAsync($"chown {ownerGroup} {path}", cancellationToken);
            }
        }
        catch (Exception ex)
        {
            throw new DeploymentException($"Failed to set permissions: {ex.Message}", ex);
        }
    }

    private async Task<string> ExecuteCommandAsync(string command, CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "/bin/sh",
            Arguments = $"-c \"{command}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo);
        if (process == null)
            throw new InvalidOperationException("Failed to start process");

        await process.WaitForExitAsync(cancellationToken);

        if (process.ExitCode != 0)
        {
            var error = await process.StandardError.ReadToEndAsync(cancellationToken);
            throw new InvalidOperationException($"Command failed with exit code {process.ExitCode}: {error}");
        }

        return await process.StandardOutput.ReadToEndAsync(cancellationToken);
    }
}
