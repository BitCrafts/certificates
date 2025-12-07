// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (C) 2025 BitCrafts

namespace BitCrafts.Certificates.Linux.ProcessWrappers;

/// <summary>
/// Wrapper for GPG command-line operations.
/// </summary>
public class GpgWrapper : ProcessWrapperBase
{
    private const string GpgCommand = "gpg";

    public async Task<ProcessResult> EncryptAsync(
        byte[] data,
        string keyId,
        CancellationToken cancellationToken = default)
    {
        var tempInputFile = Path.GetTempFileName();
        var tempOutputFile = Path.GetTempFileName();

        try
        {
            await File.WriteAllBytesAsync(tempInputFile, data, cancellationToken);

            var args = new[]
            {
                "--encrypt",
                "--recipient", keyId,
                "--trust-model", "always",
                "--output", tempOutputFile,
                tempInputFile
            };

            var result = await ExecuteProcessAsync(GpgCommand, args, cancellationToken: cancellationToken);

            if (result.Success && File.Exists(tempOutputFile))
            {
                var encryptedData = await File.ReadAllBytesAsync(tempOutputFile, cancellationToken);
                result.Output = Convert.ToBase64String(encryptedData);
            }

            return result;
        }
        finally
        {
            if (File.Exists(tempInputFile))
                File.Delete(tempInputFile);
            if (File.Exists(tempOutputFile))
                File.Delete(tempOutputFile);
        }
    }

    public async Task<ProcessResult> DecryptAsync(
        byte[] encryptedData,
        CancellationToken cancellationToken = default)
    {
        var tempInputFile = Path.GetTempFileName();
        var tempOutputFile = Path.GetTempFileName();

        try
        {
            await File.WriteAllBytesAsync(tempInputFile, encryptedData, cancellationToken);

            var args = new[]
            {
                "--decrypt",
                "--output", tempOutputFile,
                tempInputFile
            };

            var result = await ExecuteProcessAsync(GpgCommand, args, cancellationToken: cancellationToken);

            if (result.Success && File.Exists(tempOutputFile))
            {
                var decryptedData = await File.ReadAllBytesAsync(tempOutputFile, cancellationToken);
                result.Output = Convert.ToBase64String(decryptedData);
            }

            return result;
        }
        finally
        {
            if (File.Exists(tempInputFile))
                File.Delete(tempInputFile);
            if (File.Exists(tempOutputFile))
                File.Delete(tempOutputFile);
        }
    }

    public async Task<ProcessResult> ListKeysAsync(
        string? keyId = null,
        CancellationToken cancellationToken = default)
    {
        var argsList = new List<string> { "--list-keys", "--with-colons" };
        if (!string.IsNullOrEmpty(keyId))
            argsList.Add(keyId);

        return await ExecuteProcessAsync(GpgCommand, argsList.ToArray(), cancellationToken: cancellationToken);
    }

    public async Task<bool> ValidateKeyExistsAsync(
        string keyId,
        CancellationToken cancellationToken = default)
    {
        var result = await ListKeysAsync(keyId, cancellationToken);
        return result.Success && !string.IsNullOrWhiteSpace(result.Output);
    }
}
