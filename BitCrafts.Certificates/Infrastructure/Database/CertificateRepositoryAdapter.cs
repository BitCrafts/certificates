// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (C) 2025 Younes Benmoussa <benzsoftware@pm.me>
// 
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.
// You should have received a copy of the GNU Affero General Public License along with this program. If not, see <https://www.gnu.org/licenses/>. 

using BitCrafts.Certificates.Domain.Entities;
using BitCrafts.Certificates.Domain.Interfaces;
using BitCrafts.Certificates.Data.Repositories;
using BitCrafts.Certificates.Models;

namespace BitCrafts.Certificates.Infrastructure.Database;

/// <summary>
/// Adapter that bridges the domain ICertificateRepository with the existing ICertificatesRepository
/// </summary>
public sealed class CertificateRepositoryAdapter : ICertificateRepository
{
    private readonly ICertificatesRepository _legacyRepo;

    public CertificateRepositoryAdapter(ICertificatesRepository legacyRepo)
    {
        _legacyRepo = legacyRepo;
    }

    public async Task<Certificate?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        var record = await _legacyRepo.GetAsync(id);
        return record != null ? MapToDomain(record) : null;
    }

    public async Task<IReadOnlyList<Certificate>> GetAllAsync(CancellationToken ct = default)
    {
        // Get both server and client certificates
        var servers = await _legacyRepo.ListByKindAsync("server");
        var clients = await _legacyRepo.ListByKindAsync("client");
        return servers.Concat(clients).Select(MapToDomain).ToList();
    }

    public async Task<IReadOnlyList<Certificate>> GetByKindAsync(string kind, CancellationToken ct = default)
    {
        var records = await _legacyRepo.ListByKindAsync(kind);
        return records.Select(MapToDomain).ToList();
    }

    public async Task<long> AddAsync(Certificate certificate, CancellationToken ct = default)
    {
        var record = MapToRecord(certificate);
        return await _legacyRepo.InsertAsync(record);
    }

    public async Task UpdateAsync(Certificate certificate, CancellationToken ct = default)
    {
        await _legacyRepo.UpdateStatusAsync(certificate.Id, certificate.Status);
    }

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        // Note: existing repository doesn't have delete, would need to be added
        // For now, we'll mark as revoked instead
        await _legacyRepo.UpdateStatusAsync(id, "deleted");
    }

    private static Certificate MapToDomain(CertificateRecord record)
    {
        return new Certificate
        {
            Id = record.Id,
            Kind = record.Kind,
            Subject = record.Subject,
            SanDns = record.SanDns,
            SanEmail = null, // Not in CertificateRecord
            SanIps = record.SanIp,
            SerialNumber = string.Empty, // Not stored in CertificateRecord, would need to extract from cert
            Thumbprint = string.Empty, // Not stored in CertificateRecord, would need to extract from cert
            NotBefore = DateTimeOffset.Parse(record.NotBefore),
            NotAfter = DateTimeOffset.Parse(record.NotAfter),
            IssuedAt = DateTimeOffset.Parse(record.CreatedAt),
            Status = record.Status,
            CertPath = record.CertPath,
            KeyPath = record.KeyPath
        };
    }

    private static CertificateRecord MapToRecord(Certificate cert)
    {
        return new CertificateRecord
        {
            Id = cert.Id,
            Kind = cert.Kind,
            Subject = cert.Subject,
            SanDns = cert.SanDns,
            SanIp = cert.SanIps,
            NotBefore = cert.NotBefore.ToString("O"),
            NotAfter = cert.NotAfter.ToString("O"),
            Status = cert.Status,
            CertPath = cert.CertPath ?? string.Empty,
            KeyPath = cert.KeyPath ?? string.Empty,
            CreatedAt = cert.IssuedAt.ToString("O"),
            UpdatedAt = DateTimeOffset.UtcNow.ToString("O")
        };
    }
}
