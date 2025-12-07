// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (C) 2025 BitCrafts

namespace BitCrafts.Certificates.Linux.ProcessWrappers;

/// <summary>
/// Wrapper for OpenSSL command-line operations.
/// </summary>
public class OpenSslWrapper : ProcessWrapperBase
{
    private const string OpenSslCommand = "openssl";

    public async Task<ProcessResult> GeneratePrivateKeyAsync(
        string keyPath,
        string algorithm = "ec",
        string curve = "prime256v1",
        CancellationToken cancellationToken = default)
    {
        var args = algorithm.ToLower() switch
        {
            "ec" => new[] { "ecparam", "-genkey", "-name", curve, "-out", EscapePath(keyPath) },
            "rsa" => new[] { "genrsa", "-out", EscapePath(keyPath), "2048" },
            _ => throw new ArgumentException($"Unsupported algorithm: {algorithm}")
        };

        return await ExecuteProcessAsync(OpenSslCommand, args, cancellationToken: cancellationToken);
    }

    public async Task<ProcessResult> GenerateCertificateSigningRequestAsync(
        string keyPath,
        string csrPath,
        string subject,
        CancellationToken cancellationToken = default)
    {
        var args = new[]
        {
            "req", "-new",
            "-key", EscapePath(keyPath),
            "-out", EscapePath(csrPath),
            "-subj", subject
        };

        return await ExecuteProcessAsync(OpenSslCommand, args, cancellationToken: cancellationToken);
    }

    public async Task<ProcessResult> SignCertificateAsync(
        string csrPath,
        string caCertPath,
        string caKeyPath,
        string certPath,
        int validityDays,
        string? extensions = null,
        CancellationToken cancellationToken = default)
    {
        var argsList = new List<string>
        {
            "x509", "-req",
            "-in", EscapePath(csrPath),
            "-CA", EscapePath(caCertPath),
            "-CAkey", EscapePath(caKeyPath),
            "-CAcreateserial",
            "-out", EscapePath(certPath),
            "-days", validityDays.ToString()
        };

        if (!string.IsNullOrEmpty(extensions))
        {
            argsList.Add("-extensions");
            argsList.Add(extensions);
        }

        return await ExecuteProcessAsync(OpenSslCommand, argsList.ToArray(), cancellationToken: cancellationToken);
    }

    public async Task<ProcessResult> GetCertificateInfoAsync(
        string certPath,
        CancellationToken cancellationToken = default)
    {
        var args = new[] { "x509", "-in", EscapePath(certPath), "-noout", "-text" };
        return await ExecuteProcessAsync(OpenSslCommand, args, cancellationToken: cancellationToken);
    }
}
