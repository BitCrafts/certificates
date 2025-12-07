// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (C) 2025 BitCrafts

using BitCrafts.Certificates.Abstractions.Interfaces;
using BitCrafts.Certificates.Abstractions.Models;
using BitCrafts.Certificates.Abstractions.Exceptions;
using BitCrafts.Certificates.Linux.ProcessWrappers;

namespace BitCrafts.Certificates.Linux.Services;

/// <summary>
/// GPG-based encryption service for certificate data.
/// </summary>
public class EncryptionServiceGpg : IEncryptionService
{
    private readonly GpgWrapper _gpg;

    public EncryptionServiceGpg()
    {
        _gpg = new GpgWrapper();
    }

    public async Task<byte[]> EncryptAsync(
        byte[] data,
        UserKeyReference keyReference,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _gpg.EncryptAsync(data, keyReference.KeyId, cancellationToken);
            if (!result.Success)
            {
                throw new EncryptionException($"Encryption failed: {result.Error}");
            }

            // The output is base64-encoded encrypted data
            return Convert.FromBase64String(result.Output);
        }
        catch (Exception ex) when (ex is not EncryptionException)
        {
            throw new EncryptionException("Failed to encrypt data", ex);
        }
    }

    public async Task<byte[]> DecryptAsync(
        byte[] encryptedData,
        UserKeyReference keyReference,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _gpg.DecryptAsync(encryptedData, cancellationToken);
            if (!result.Success)
            {
                throw new EncryptionException($"Decryption failed: {result.Error}");
            }

            // The output is base64-encoded decrypted data
            return Convert.FromBase64String(result.Output);
        }
        catch (Exception ex) when (ex is not EncryptionException)
        {
            throw new EncryptionException("Failed to decrypt data", ex);
        }
    }

    public async Task<bool> ValidateKeyAsync(
        UserKeyReference keyReference,
        CancellationToken cancellationToken = default)
    {
        return await _gpg.ValidateKeyExistsAsync(keyReference.KeyId, cancellationToken);
    }
}
