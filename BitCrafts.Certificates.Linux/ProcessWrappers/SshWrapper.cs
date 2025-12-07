// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (C) 2025 BitCrafts

namespace BitCrafts.Certificates.Linux.ProcessWrappers;

/// <summary>
/// Wrapper for SSH/SCP operations.
/// </summary>
public class SshWrapper : ProcessWrapperBase
{
    private const string SshCommand = "ssh";
    private const string ScpCommand = "scp";

    public async Task<ProcessResult> ExecuteRemoteCommandAsync(
        string host,
        string command,
        string? username = null,
        int? port = null,
        string? privateKeyPath = null,
        CancellationToken cancellationToken = default)
    {
        var argsList = new List<string>
        {
            "-o", "StrictHostKeyChecking=yes",
            "-o", "BatchMode=yes"
        };

        if (!string.IsNullOrEmpty(privateKeyPath))
        {
            argsList.Add("-i");
            argsList.Add(EscapePath(privateKeyPath));
        }

        if (port.HasValue)
        {
            argsList.Add("-p");
            argsList.Add(port.Value.ToString());
        }

        var target = string.IsNullOrEmpty(username) ? host : $"{username}@{host}";
        argsList.Add(target);
        argsList.Add(command);

        return await ExecuteProcessAsync(SshCommand, argsList.ToArray(), cancellationToken: cancellationToken);
    }

    public async Task<ProcessResult> CopyFileAsync(
        string localPath,
        string host,
        string remotePath,
        string? username = null,
        int? port = null,
        string? privateKeyPath = null,
        CancellationToken cancellationToken = default)
    {
        var argsList = new List<string>
        {
            "-o", "StrictHostKeyChecking=yes",
            "-o", "BatchMode=yes"
        };

        if (!string.IsNullOrEmpty(privateKeyPath))
        {
            argsList.Add("-i");
            argsList.Add(EscapePath(privateKeyPath));
        }

        if (port.HasValue)
        {
            argsList.Add("-P");
            argsList.Add(port.Value.ToString());
        }

        argsList.Add(EscapePath(localPath));

        var target = string.IsNullOrEmpty(username) ? host : $"{username}@{host}";
        argsList.Add($"{target}:{remotePath}");

        return await ExecuteProcessAsync(ScpCommand, argsList.ToArray(), cancellationToken: cancellationToken);
    }

    public async Task<ProcessResult> TestConnectionAsync(
        string host,
        string? username = null,
        int? port = null,
        string? privateKeyPath = null,
        CancellationToken cancellationToken = default)
    {
        return await ExecuteRemoteCommandAsync(host, "echo 'Connection successful'", username, port, privateKeyPath, cancellationToken);
    }
}
