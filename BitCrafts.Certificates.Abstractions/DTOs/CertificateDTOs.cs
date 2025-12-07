// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (C) 2025 BitCrafts

using BitCrafts.Certificates.Abstractions.Models;

namespace BitCrafts.Certificates.Abstractions.DTOs;

/// <summary>
/// Request to create a certificate.
/// </summary>
public record CreateCertificateRequest
{
    public required string CommonName { get; init; }
    public string? Organization { get; init; }
    public string? OrganizationalUnit { get; init; }
    public string? Country { get; init; }
    public string? State { get; init; }
    public string? Locality { get; init; }
    public List<string> SubjectAlternativeNames { get; init; } = new();
    public List<string> IpAddresses { get; init; } = new();
    public CertificateType Type { get; init; }
    public int ValidityDays { get; init; } = 365;
    public required string GpgKeyId { get; init; }
}

/// <summary>
/// Response containing certificate information.
/// </summary>
public record CertificateResponse
{
    public int Id { get; init; }
    public required string CommonName { get; init; }
    public CertificateType Type { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? RevokedAt { get; init; }
    public string? SerialNumber { get; init; }
}

/// <summary>
/// Request to deploy a certificate.
/// </summary>
public record DeploymentRequest
{
    public int CertificateId { get; init; }
    public required DeploymentTarget Target { get; init; }
}

/// <summary>
/// Response from a deployment operation.
/// </summary>
public record DeploymentResponse
{
    public bool Success { get; init; }
    public required string Message { get; init; }
    public List<string> Details { get; init; } = new();
}
