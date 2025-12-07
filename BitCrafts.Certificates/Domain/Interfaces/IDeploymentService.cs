// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (C) 2025 Younes Benmoussa <benzsoftware@pm.me>
// 
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.
// You should have received a copy of the GNU Affero General Public License along with this program. If not, see <https://www.gnu.org/licenses/>. 

using BitCrafts.Certificates.Domain.ValueObjects;

namespace BitCrafts.Certificates.Domain.Interfaces;

/// <summary>
/// Deployment service interface (port)
/// </summary>
public interface IDeploymentService
{
    Task<bool> DeployAsync(DeploymentTarget target, string certificatePath, string keyPath, CancellationToken ct = default);
    Task<bool> TestConnectionAsync(DeploymentTarget target, CancellationToken ct = default);
}
