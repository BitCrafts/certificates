// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (C) 2025 Younes Benmoussa <benzsoftware@pm.me>
// 
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.
// You should have received a copy of the GNU Affero General Public License along with this program. If not, see <https://www.gnu.org/licenses/>. 

namespace BitCrafts.Certificates.Application.DTOs;

public sealed class CertificateDto
{
    public long Id { get; set; }
    public required string Kind { get; set; }
    public required string Subject { get; set; }
    public string? SanDns { get; set; }
    public string? SanEmail { get; set; }
    public string? SanIps { get; set; }
    public required string SerialNumber { get; set; }
    public required string Thumbprint { get; set; }
    public DateTimeOffset NotBefore { get; set; }
    public DateTimeOffset NotAfter { get; set; }
    public DateTimeOffset IssuedAt { get; set; }
    public required string Status { get; set; }
    public bool IsRevoked { get; set; }
}

public sealed class CreateServerCertificateDto
{
    public required string Fqdn { get; set; }
    public string[]? IpAddresses { get; set; }
    public string[]? DnsNames { get; set; }
}

public sealed class CreateClientCertificateDto
{
    public required string Username { get; set; }
    public string? Email { get; set; }
}

public sealed class RevokeCertificateDto
{
    public long CertificateId { get; set; }
    public string? Reason { get; set; }
}

public sealed class DeploymentDto
{
    public required string Type { get; set; } // "SSH" or "NetworkFileSystem"
    public required string Target { get; set; }
    public string? Username { get; set; }
    public string? PrivateKeyPath { get; set; }
    public int? Port { get; set; }
    public string? DestinationPath { get; set; }
}

public sealed class DeploymentRequestDto
{
    public long CertificateId { get; set; }
    public required DeploymentDto DeploymentTarget { get; set; }
}

public sealed class DeploymentResultDto
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? Error { get; set; }
}
