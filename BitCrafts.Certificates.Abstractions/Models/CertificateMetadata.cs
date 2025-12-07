// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (C) 2025 BitCrafts

namespace BitCrafts.Certificates.Abstractions.Models;

/// <summary>
/// Metadata describing a certificate.
/// </summary>
public class CertificateMetadata
{
    public required string CommonName { get; set; }
    public string? Organization { get; set; }
    public string? OrganizationalUnit { get; set; }
    public string? Country { get; set; }
    public string? State { get; set; }
    public string? Locality { get; set; }
    public List<string> SubjectAlternativeNames { get; set; } = new();
    public List<string> IpAddresses { get; set; } = new();
    public CertificateType Type { get; set; }
    public int ValidityDays { get; set; }
    public string? SerialNumber { get; set; }
}

public enum CertificateType
{
    Server,
    Client
}
