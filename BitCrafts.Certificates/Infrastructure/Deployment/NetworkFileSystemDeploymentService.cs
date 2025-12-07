// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (C) 2025 Younes Benmoussa <benzsoftware@pm.me>
// 
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.
// You should have received a copy of the GNU Affero General Public License along with this program. If not, see <https://www.gnu.org/licenses/>. 

using BitCrafts.Certificates.Domain.Interfaces;
using BitCrafts.Certificates.Domain.ValueObjects;

namespace BitCrafts.Certificates.Infrastructure.Deployment;

/// <summary>
/// Network filesystem deployment service (SMB/NFS/local mount)
/// </summary>
public sealed class NetworkFileSystemDeploymentService : IDeploymentService
{
    private readonly ILogger<NetworkFileSystemDeploymentService> _logger;

    public NetworkFileSystemDeploymentService(ILogger<NetworkFileSystemDeploymentService> logger)
    {
        _logger = logger;
    }

    public async Task<bool> DeployAsync(DeploymentTarget target, string certificatePath, string keyPath, CancellationToken ct = default)
    {
        if (target.Type != DeploymentType.NetworkFileSystem)
        {
            throw new InvalidOperationException("NetworkFileSystemDeploymentService only supports network filesystem deployment");
        }

        try
        {
            var destPath = target.DestinationPath ?? target.Target;

            if (!Directory.Exists(destPath))
            {
                _logger.LogWarning("Destination path {Path} does not exist. Attempting to create...", destPath);
                Directory.CreateDirectory(destPath);
            }

            var certFileName = Path.GetFileName(certificatePath);
            var keyFileName = Path.GetFileName(keyPath);

            var destCertPath = Path.Combine(destPath, certFileName);
            var destKeyPath = Path.Combine(destPath, keyFileName);

            _logger.LogInformation("Copying certificate to {DestPath}", destCertPath);
            File.Copy(certificatePath, destCertPath, overwrite: true);

            _logger.LogInformation("Copying private key to {DestPath}", destKeyPath);
            File.Copy(keyPath, destKeyPath, overwrite: true);

            // Set restrictive permissions on Unix-like systems
            if (!OperatingSystem.IsWindows())
            {
                SetUnixPermissions(destCertPath, "644");
                SetUnixPermissions(destKeyPath, "600");
            }

            _logger.LogInformation("Successfully deployed certificates to {Path}", destPath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deploying certificates to network path {Target}", target.Target);
            return false;
        }
    }

    public Task<bool> TestConnectionAsync(DeploymentTarget target, CancellationToken ct = default)
    {
        if (target.Type != DeploymentType.NetworkFileSystem)
        {
            throw new InvalidOperationException("NetworkFileSystemDeploymentService only supports network filesystem deployment");
        }

        try
        {
            var destPath = target.DestinationPath ?? target.Target;

            // Test if path is accessible
            var exists = Directory.Exists(destPath);
            
            if (!exists)
            {
                _logger.LogWarning("Destination path {Path} does not exist or is not accessible", destPath);
                return Task.FromResult(false);
            }

            // Test write permissions by creating a temporary file
            var testFile = Path.Combine(destPath, $".test_{Guid.NewGuid()}.tmp");
            try
            {
                File.WriteAllText(testFile, "test");
                File.Delete(testFile);
                
                _logger.LogInformation("Network path {Path} is accessible and writable", destPath);
                return Task.FromResult(true);
            }
            catch (UnauthorizedAccessException)
            {
                _logger.LogWarning("Network path {Path} is not writable", destPath);
                return Task.FromResult(false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing network path {Target}", target.Target);
            return Task.FromResult(false);
        }
    }

    private void SetUnixPermissions(string path, string mode)
    {
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "chmod",
                Arguments = $"{mode} \"{path}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = System.Diagnostics.Process.Start(psi);
            process?.WaitForExit();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to set Unix permissions on {Path}", path);
        }
    }
}
