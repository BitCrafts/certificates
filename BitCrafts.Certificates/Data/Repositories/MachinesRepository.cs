// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (C) 2025 Younes Benmoussa <benzsoftware@pm.me>
// 
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.
// You should have received a copy of the GNU Affero General Public License along with this program. If not, see <https://www.gnu.org/licenses/>. 

using BitCrafts.Certificates.Models;

namespace BitCrafts.Certificates.Data.Repositories;

public interface IMachinesRepository
{
    Task<long> InsertOrUpdateAsync(string host, string? ip = null, string? notes = null, CancellationToken ct = default);
    Task<(long id, string host, string? ip)?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<IEnumerable<(long id, string host, string? ip)>> ListAllAsync(CancellationToken ct = default);
}

public sealed class MachinesRepository : IMachinesRepository
{
    public Task<long> InsertOrUpdateAsync(string host, string? ip = null, string? notes = null, CancellationToken ct = default)
        => Task.FromResult(0L);

    public Task<(long id, string host, string? ip)?> GetByIdAsync(long id, CancellationToken ct = default)
        => Task.FromResult<(long, string, string?)?>(null);

    public Task<IEnumerable<(long id, string host, string? ip)>> ListAllAsync(CancellationToken ct = default)
        => Task.FromResult<IEnumerable<(long, string, string?)>>(Array.Empty<(long, string, string?)>());
}
