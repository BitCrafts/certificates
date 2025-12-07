// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (C) 2025 BitCrafts

using BitCrafts.Certificates.Abstractions.Models;

namespace BitCrafts.Certificates.Abstractions.Interfaces;

/// <summary>
/// Service for encrypting and decrypting certificate data using GPG.
/// </summary>
public interface IEncryptionService
{
    /// <summary>
    /// Encrypts data using the specified user's GPG public key.
    /// </summary>
    Task<byte[]> EncryptAsync(byte[] data, UserKeyReference keyReference, CancellationToken cancellationToken = default);

    /// <summary>
    /// Decrypts data using the specified user's GPG private key.
    /// </summary>
    Task<byte[]> DecryptAsync(byte[] encryptedData, UserKeyReference keyReference, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates that the specified key is available for encryption.
    /// </summary>
    Task<bool> ValidateKeyAsync(UserKeyReference keyReference, CancellationToken cancellationToken = default);
}
