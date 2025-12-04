// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (C) 2025 Younes Benmoussa <benzsoftware@pm.me>
// 
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.
// You should have received a copy of the GNU Affero General Public License along with this program. If not, see <https://www.gnu.org/licenses/>. 

using Microsoft.Data.Sqlite;

namespace BitCrafts.Certificates.Data;

public interface ISchemaBootstrapper
{
    Task EnsureInitializedAsync(CancellationToken ct = default);
}

public sealed class SchemaBootstrapper : ISchemaBootstrapper
{
    private readonly ISqliteConnectionFactory _factory;

    public SchemaBootstrapper(ISqliteConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task EnsureInitializedAsync(CancellationToken ct = default)
    {
        await using var conn = _factory.Create();
        await conn.OpenAsync(ct);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
PRAGMA journal_mode = WAL;
PRAGMA foreign_keys = ON;

CREATE TABLE IF NOT EXISTS Settings (
    key TEXT PRIMARY KEY,
    value TEXT
);

CREATE TABLE IF NOT EXISTS Machines (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    host TEXT,
    ip TEXT,
    notes TEXT,
    created_at TEXT,
    updated_at TEXT
);

CREATE TABLE IF NOT EXISTS Users (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    username TEXT,
    email TEXT,
    notes TEXT,
    created_at TEXT,
    updated_at TEXT
);

CREATE TABLE IF NOT EXISTS Certificates (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    kind TEXT CHECK(kind IN ('server','client')),
    subject TEXT,
    san_dns TEXT,
    san_ip TEXT,
    owner_machine_id INTEGER NULL REFERENCES Machines(id) ON DELETE SET NULL,
    owner_user_id INTEGER NULL REFERENCES Users(id) ON DELETE SET NULL,
    key_path TEXT,
    cert_path TEXT,
    chain_path TEXT,
    not_before TEXT,
    not_after TEXT,
    status TEXT CHECK(status IN ('active','revoked','expired')),
    created_at TEXT,
    updated_at TEXT
);
";
        await cmd.ExecuteNonQueryAsync(ct);
    }
}
