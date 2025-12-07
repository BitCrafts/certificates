// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (C) 2025 Younes Benmoussa <benzsoftware@pm.me>
// 
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.
// You should have received a copy of the GNU Affero General Public License along with this program. If not, see <https://www.gnu.org/licenses/>. 

using BitCrafts.Certificates.Application.DTOs;

namespace BitCrafts.Certificates.Application.Interfaces;

/// <summary>
/// Application service for deployment operations
/// </summary>
public interface IDeploymentApplicationService
{
    Task<DeploymentResultDto> DeployCertificateAsync(DeploymentRequestDto request, CancellationToken ct = default);
    Task<DeploymentResultDto> TestConnectionAsync(DeploymentDto target, CancellationToken ct = default);
}
