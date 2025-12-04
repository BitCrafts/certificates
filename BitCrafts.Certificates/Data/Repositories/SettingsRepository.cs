// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (C) 2025 Younes Benmoussa <benzsoftware@pm.me>
// 
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.
// You should have received a copy of the GNU Affero General Public License along with this program. If not, see <https://www.gnu.org/licenses/>. 

using Microsoft.Data.Sqlite;

namespace BitCrafts.Certificates.Data.Repositories;

public interface ISettingsRepository
{
    Task<string?> GetAsync(string key, CancellationToken ct = default);
    Task SetAsync(string key, string value, CancellationToken ct = default);
}

public sealed class SettingsRepository : ISettingsRepository
{
    private readonly ISqliteConnectionFactory _factory;

    public SettingsRepository(ISqliteConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<string?> GetAsync(string key, CancellationToken ct = default)
    {
        await using var conn = _factory.Create();
        await conn.OpenAsync(ct);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT value FROM Settings WHERE key = $k LIMIT 1";
        cmd.Parameters.AddWithValue("$k", key);
        var result = await cmd.ExecuteScalarAsync(ct);
        return result as string;
    }

    public async Task SetAsync(string key, string value, CancellationToken ct = default)
    {
        await using var conn = _factory.Create();
        await conn.OpenAsync(ct);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"INSERT INTO Settings(key, value) VALUES($k, $v)
ON CONFLICT(key) DO UPDATE SET value = excluded.value";
        cmd.Parameters.AddWithValue("$k", key);
        cmd.Parameters.AddWithValue("$v", value);
        await cmd.ExecuteNonQueryAsync(ct);
    }
}
