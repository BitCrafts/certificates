// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (C) 2025 Younes Benmoussa <benzsoftware@pm.me>
// 
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.
// You should have received a copy of the GNU Affero General Public License along with this program. If not, see <https://www.gnu.org/licenses/>. 

using System.Diagnostics;
using BitCrafts.Certificates.Domain.Interfaces;
using BitCrafts.Certificates.Domain.ValueObjects;

namespace BitCrafts.Certificates.Infrastructure.Deployment;

/// <summary>
/// SSH-based deployment service using scp/ssh commands
/// </summary>
public sealed class SshDeploymentService : IDeploymentService
{
    private readonly ILogger<SshDeploymentService> _logger;

    public SshDeploymentService(ILogger<SshDeploymentService> logger)
    {
        _logger = logger;
    }

    public async Task<bool> DeployAsync(DeploymentTarget target, string certificatePath, string keyPath, CancellationToken ct = default)
    {
        if (target.Type != DeploymentType.SSH)
        {
            throw new InvalidOperationException("SshDeploymentService only supports SSH deployment");
        }

        try
        {
            var port = target.Port ?? 22;
            var userHost = string.IsNullOrEmpty(target.Username) 
                ? target.Target 
                : $"{target.Username}@{target.Target}";

            var destPath = target.DestinationPath ?? "/tmp";

            // Build scp command arguments
            var scpArgs = new List<string>();
            
            if (!string.IsNullOrEmpty(target.PrivateKeyPath))
            {
                scpArgs.Add("-i");
                scpArgs.Add(target.PrivateKeyPath);
            }

            scpArgs.Add("-P");
            scpArgs.Add(port.ToString());
            scpArgs.Add(certificatePath);
            scpArgs.Add(keyPath);
            scpArgs.Add($"{userHost}:{destPath}/");

            _logger.LogInformation("Deploying certificates via SSH to {Target}", userHost);

            // Execute scp command
            var result = await ExecuteCommandAsync("scp", scpArgs, ct);

            if (result)
            {
                _logger.LogInformation("Successfully deployed certificates to {Target}", userHost);
            }
            else
            {
                _logger.LogWarning("Failed to deploy certificates to {Target}", userHost);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deploying certificates via SSH to {Target}", target.Target);
            return false;
        }
    }

    public async Task<bool> TestConnectionAsync(DeploymentTarget target, CancellationToken ct = default)
    {
        if (target.Type != DeploymentType.SSH)
        {
            throw new InvalidOperationException("SshDeploymentService only supports SSH deployment");
        }

        try
        {
            var port = target.Port ?? 22;
            var userHost = string.IsNullOrEmpty(target.Username) 
                ? target.Target 
                : $"{target.Username}@{target.Target}";

            var sshArgs = new List<string>
            {
                "-p", port.ToString(),
                "-o", "ConnectTimeout=10",
                "-o", "BatchMode=yes"
            };

            if (!string.IsNullOrEmpty(target.PrivateKeyPath))
            {
                sshArgs.Add("-i");
                sshArgs.Add(target.PrivateKeyPath);
            }

            sshArgs.Add(userHost);
            sshArgs.Add("echo 'Connection test successful'");

            _logger.LogInformation("Testing SSH connection to {Target}", userHost);

            var result = await ExecuteCommandAsync("ssh", sshArgs, ct);

            if (result)
            {
                _logger.LogInformation("SSH connection test successful for {Target}", userHost);
            }
            else
            {
                _logger.LogWarning("SSH connection test failed for {Target}", userHost);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing SSH connection to {Target}", target.Target);
            return false;
        }
    }

    private async Task<bool> ExecuteCommandAsync(string command, List<string> arguments, CancellationToken ct)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = command,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            foreach (var arg in arguments)
            {
                psi.ArgumentList.Add(arg);
            }

            using var process = new Process { StartInfo = psi };
            process.Start();

            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync(ct);

            var output = await outputTask;
            var error = await errorTask;

            if (process.ExitCode != 0)
            {
                _logger.LogWarning("Command {Command} exited with code {ExitCode}. Error: {Error}", 
                    command, process.ExitCode, error);
                return false;
            }

            if (!string.IsNullOrWhiteSpace(output))
            {
                _logger.LogDebug("Command output: {Output}", output);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute command {Command}", command);
            return false;
        }
    }
}
