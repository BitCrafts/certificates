// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (C) 2025 Younes Benmoussa <benzsoftware@pm.me>
// 
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.
// You should have received a copy of the GNU Affero General Public License along with this program. If not, see <https://www.gnu.org/licenses/>. 

namespace BitCrafts.Certificates.Models;

public class CertificateRecord
{
    public long Id { get; set; }
    public string Kind { get; set; } = "server"; // 'server' or 'client'
    public string Subject { get; set; } = string.Empty; // e.g., CN=fqdn
    public string? SanDns { get; set; } // comma-separated DNS names
    public string? SanIp { get; set; } // comma-separated IPs
    public string KeyPath { get; set; } = string.Empty;
    public string CertPath { get; set; } = string.Empty;
    public string? ChainPath { get; set; }
    public string NotBefore { get; set; } = string.Empty;
    public string NotAfter { get; set; } = string.Empty;
    public string Status { get; set; } = "active"; // active/revoked/expired
    public string CreatedAt { get; set; } = string.Empty;
    public string UpdatedAt { get; set; } = string.Empty;
}
