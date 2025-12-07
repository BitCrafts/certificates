// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (C) 2025 Younes Benmoussa <benzsoftware@pm.me>
// 
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.
// You should have received a copy of the GNU Affero General Public License along with this program. If not, see <https://www.gnu.org/licenses/>. 

namespace BitCrafts.Certificates.Domain.Entities;

/// <summary>
/// Domain entity representing a certificate
/// </summary>
public sealed class Certificate
{
    public long Id { get; set; }
    public required string Kind { get; set; } // "server" or "client"
    public required string Subject { get; set; }
    public string? SanDns { get; set; }
    public string? SanEmail { get; set; }
    public string? SanIps { get; set; }
    public required string SerialNumber { get; set; }
    public required string Thumbprint { get; set; }
    public required DateTimeOffset NotBefore { get; set; }
    public required DateTimeOffset NotAfter { get; set; }
    public required DateTimeOffset IssuedAt { get; set; }
    public required string Status { get; set; } // "issued" or "revoked"
    public string? CertPath { get; set; }
    public string? KeyPath { get; set; }

    public bool IsRevoked => string.Equals(Status, "revoked", StringComparison.OrdinalIgnoreCase);
    
    public void Revoke()
    {
        if (!IsRevoked)
        {
            Status = "revoked";
        }
    }
}
