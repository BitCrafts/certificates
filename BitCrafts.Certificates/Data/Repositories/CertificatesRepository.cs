// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (C) 2025 Younes Benmoussa <benzsoftware@pm.me>
// 
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.
// You should have received a copy of the GNU Affero General Public License along with this program. If not, see <https://www.gnu.org/licenses/>. 

using BitCrafts.Certificates.Models;
using Microsoft.Data.Sqlite;

namespace BitCrafts.Certificates.Data.Repositories;

public interface ICertificatesRepository
{
    Task<long> InsertAsync(CertificateRecord rec, CancellationToken ct = default);
    Task<IReadOnlyList<CertificateRecord>> ListByKindAsync(string kind, CancellationToken ct = default);
    Task<CertificateRecord?> GetAsync(long id, CancellationToken ct = default);
    Task<bool> UpdateStatusAsync(long id, string status, CancellationToken ct = default);
}

public sealed class CertificatesRepository : ICertificatesRepository
{
    private readonly ISqliteConnectionFactory _factory;

    public CertificatesRepository(ISqliteConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<long> InsertAsync(CertificateRecord rec, CancellationToken ct = default)
    {
        await using var conn = _factory.Create();
        await conn.OpenAsync(ct);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"INSERT INTO Certificates
(kind, subject, san_dns, san_ip, key_path, cert_path, chain_path, not_before, not_after, status, created_at, updated_at)
VALUES ($kind,$sub,$sdns,$sip,$k,$c,$chain,$nb,$na,$st,$ca,$ua);
SELECT last_insert_rowid();";
        cmd.Parameters.AddWithValue("$kind", rec.Kind);
        cmd.Parameters.AddWithValue("$sub", rec.Subject);
        cmd.Parameters.AddWithValue("$sdns", (object?)rec.SanDns ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$sip", (object?)rec.SanIp ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$k", rec.KeyPath);
        cmd.Parameters.AddWithValue("$c", rec.CertPath);
        cmd.Parameters.AddWithValue("$chain", (object?)rec.ChainPath ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$nb", rec.NotBefore);
        cmd.Parameters.AddWithValue("$na", rec.NotAfter);
        cmd.Parameters.AddWithValue("$st", rec.Status);
        cmd.Parameters.AddWithValue("$ca", rec.CreatedAt);
        cmd.Parameters.AddWithValue("$ua", rec.UpdatedAt);
        var result = await cmd.ExecuteScalarAsync(ct);
        return (long)(result ?? 0L);
    }

    public async Task<IReadOnlyList<CertificateRecord>> ListByKindAsync(string kind, CancellationToken ct = default)
    {
        var list = new List<CertificateRecord>();
        await using var conn = _factory.Create();
        await conn.OpenAsync(ct);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT id, kind, subject, san_dns, san_ip, key_path, cert_path, chain_path, not_before, not_after, status, created_at, updated_at FROM Certificates WHERE kind=$k ORDER BY id DESC";
        cmd.Parameters.AddWithValue("$k", kind);
        await using var rdr = await cmd.ExecuteReaderAsync(ct);
        while (await rdr.ReadAsync(ct))
        {
            list.Add(new CertificateRecord
            {
                Id = rdr.GetInt64(0),
                Kind = rdr.GetString(1),
                Subject = rdr.IsDBNull(2) ? string.Empty : rdr.GetString(2),
                SanDns = rdr.IsDBNull(3) ? null : rdr.GetString(3),
                SanIp = rdr.IsDBNull(4) ? null : rdr.GetString(4),
                KeyPath = rdr.GetString(5),
                CertPath = rdr.GetString(6),
                ChainPath = rdr.IsDBNull(7) ? null : rdr.GetString(7),
                NotBefore = rdr.GetString(8),
                NotAfter = rdr.GetString(9),
                Status = rdr.GetString(10),
                CreatedAt = rdr.GetString(11),
                UpdatedAt = rdr.GetString(12)
            });
        }
        return list;
    }

    public async Task<CertificateRecord?> GetAsync(long id, CancellationToken ct = default)
    {
        await using var conn = _factory.Create();
        await conn.OpenAsync(ct);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT id, kind, subject, san_dns, san_ip, key_path, cert_path, chain_path, not_before, not_after, status, created_at, updated_at FROM Certificates WHERE id=$id LIMIT 1";
        cmd.Parameters.AddWithValue("$id", id);
        await using var rdr = await cmd.ExecuteReaderAsync(ct);
        if (await rdr.ReadAsync(ct))
        {
            return new CertificateRecord
            {
                Id = rdr.GetInt64(0),
                Kind = rdr.GetString(1),
                Subject = rdr.IsDBNull(2) ? string.Empty : rdr.GetString(2),
                SanDns = rdr.IsDBNull(3) ? null : rdr.GetString(3),
                SanIp = rdr.IsDBNull(4) ? null : rdr.GetString(4),
                KeyPath = rdr.GetString(5),
                CertPath = rdr.GetString(6),
                ChainPath = rdr.IsDBNull(7) ? null : rdr.GetString(7),
                NotBefore = rdr.GetString(8),
                NotAfter = rdr.GetString(9),
                Status = rdr.GetString(10),
                CreatedAt = rdr.GetString(11),
                UpdatedAt = rdr.GetString(12)
            };
        }
        return null;
    }

    public async Task<bool> UpdateStatusAsync(long id, string status, CancellationToken ct = default)
    {
        await using var conn = _factory.Create();
        await conn.OpenAsync(ct);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"UPDATE Certificates SET status=$st, updated_at=$ua WHERE id=$id";
        cmd.Parameters.AddWithValue("$st", status);
        cmd.Parameters.AddWithValue("$ua", DateTimeOffset.UtcNow.ToString("O"));
        cmd.Parameters.AddWithValue("$id", id);
        var affected = await cmd.ExecuteNonQueryAsync(ct);
        return affected > 0;
    }
}
