// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (C) 2025 Younes Benmoussa <benzsoftware@pm.me>
// 
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.
// You should have received a copy of the GNU Affero General Public License along with this program. If not, see <https://www.gnu.org/licenses/>. 

namespace BitCrafts.Certificates.Domain.ValueObjects;

/// <summary>
/// Value object representing a deployment target (SSH or network path)
/// </summary>
public sealed class DeploymentTarget
{
    public required DeploymentType Type { get; set; }
    public required string Target { get; set; }
    public string? Username { get; set; }
    public string? PrivateKeyPath { get; set; }
    public int? Port { get; set; }
    public string? DestinationPath { get; set; }
}

public enum DeploymentType
{
    SSH,
    NetworkFileSystem
}
