// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (C) 2025 BitCrafts

namespace BitCrafts.Certificates.Abstractions.Models;

/// <summary>
/// Represents a reference to a user's GPG key.
/// </summary>
public class UserKeyReference
{
    public required string KeyId { get; set; }
    public string? Fingerprint { get; set; }
    public string? UserEmail { get; set; }
}
