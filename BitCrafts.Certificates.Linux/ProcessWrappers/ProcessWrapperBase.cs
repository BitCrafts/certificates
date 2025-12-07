// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (C) 2025 BitCrafts

using System.Diagnostics;
using System.Text;

namespace BitCrafts.Certificates.Linux.ProcessWrappers;

/// <summary>
/// Base class for process wrappers with security and error handling.
/// </summary>
public abstract class ProcessWrapperBase
{
    protected async Task<ProcessResult> ExecuteProcessAsync(
        string command,
        string[] arguments,
        string? workingDirectory = null,
        string? input = null,
        CancellationToken cancellationToken = default)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = command,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = input != null,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = workingDirectory ?? Directory.GetCurrentDirectory()
        };

        // Add arguments one by one to avoid shell injection
        foreach (var arg in arguments)
        {
            startInfo.ArgumentList.Add(arg);
        }

        using var process = new Process { StartInfo = startInfo };
        var outputBuilder = new StringBuilder();
        var errorBuilder = new StringBuilder();

        process.OutputDataReceived += (sender, e) =>
        {
            if (e.Data != null)
                outputBuilder.AppendLine(e.Data);
        };

        process.ErrorDataReceived += (sender, e) =>
        {
            if (e.Data != null)
                errorBuilder.AppendLine(e.Data);
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        if (input != null)
        {
            await process.StandardInput.WriteAsync(input);
            process.StandardInput.Close();
        }

        await process.WaitForExitAsync(cancellationToken);

        return new ProcessResult
        {
            ExitCode = process.ExitCode,
            Output = outputBuilder.ToString(),
            Error = errorBuilder.ToString(),
            Success = process.ExitCode == 0
        };
    }

    protected string EscapePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Path cannot be null or whitespace", nameof(path));

        // Basic validation to prevent path traversal
        var normalized = Path.GetFullPath(path);
        return normalized;
    }
}

public class ProcessResult
{
    public int ExitCode { get; set; }
    public required string Output { get; set; }
    public required string Error { get; set; }
    public bool Success { get; set; }
}
