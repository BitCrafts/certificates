// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (C) 2025 Younes Benmoussa <benzsoftware@pm.me>
// 
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.
// You should have received a copy of the GNU Affero General Public License along with this program. If not, see <https://www.gnu.org/licenses/>. 

using BitCrafts.Certificates.Application.DTOs;

namespace BitCrafts.Certificates.Application.Interfaces;

/// <summary>
/// Application service for certificate operations
/// </summary>
public interface ICertificateApplicationService
{
    Task<CertificateDto> CreateServerCertificateAsync(CreateServerCertificateDto dto, CancellationToken ct = default);
    Task<CertificateDto> CreateClientCertificateAsync(CreateClientCertificateDto dto, CancellationToken ct = default);
    Task<CertificateDto?> GetCertificateAsync(long id, CancellationToken ct = default);
    Task<IReadOnlyList<CertificateDto>> GetAllCertificatesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<CertificateDto>> GetCertificatesByKindAsync(string kind, CancellationToken ct = default);
    Task<bool> RevokeCertificateAsync(RevokeCertificateDto dto, CancellationToken ct = default);
    Task<bool> DeleteCertificateAsync(long id, CancellationToken ct = default);
    Task<byte[]> DownloadCertificateArchiveAsync(long id, CancellationToken ct = default);
}
