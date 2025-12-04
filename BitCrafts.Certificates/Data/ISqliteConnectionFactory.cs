// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (C) 2025 Younes Benmoussa <benzsoftware@pm.me>
// 
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.
// You should have received a copy of the GNU Affero General Public License along with this program. If not, see <https://www.gnu.org/licenses/>. 

using BitCrafts.Certificates.Services;
using Microsoft.Data.Sqlite;

namespace BitCrafts.Certificates.Data;

public interface ISqliteConnectionFactory
{
    SqliteConnection Create();
}

public sealed class SqliteConnectionFactory : ISqliteConnectionFactory
{
    private readonly IDataDirectory _data;

    public SqliteConnectionFactory(IDataDirectory data)
    {
        _data = data;
    }

    public SqliteConnection Create()
    {
        var cs = new SqliteConnectionStringBuilder
        {
            DataSource = _data.DbPath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Default
        }.ToString();
        return new SqliteConnection(cs);
    }
}
