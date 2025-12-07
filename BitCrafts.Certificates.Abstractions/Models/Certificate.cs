// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (C) 2025 BitCrafts

namespace BitCrafts.Certificates.Abstractions.Models;

/// <summary>
/// Represents a certificate entity with its metadata and encrypted data.
/// </summary>
public class Certificate
{
    public int Id { get; set; }
    public required CertificateMetadata Metadata { get; set; }
    public byte[]? EncryptedData { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public bool IsRevoked => RevokedAt.HasValue;
}
