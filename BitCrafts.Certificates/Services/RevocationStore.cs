// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (C) 2025 Younes Benmoussa <benzsoftware@pm.me>
// 
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.
// You should have received a copy of the GNU Affero General Public License along with this program. If not, see <https://www.gnu.org/licenses/>. 

using System.Text;
using System.Text.Json;

namespace BitCrafts.Certificates.Services;

public sealed class RevocationStore : IRevocationStore
{
    private readonly IDataDirectory _data;
    private readonly ILogger<RevocationStore> _logger;
    private readonly object _lock = new();

    public RevocationStore(IDataDirectory data, ILogger<RevocationStore> logger)
    {
        _data = data;
        _logger = logger;
    }

    public Task AppendAsync(long id, string kind, string subject, DateTimeOffset revokedAt, CancellationToken ct = default)
    {
        try
        {
            Directory.CreateDirectory(_data.CrlDir);
            var path = Path.Combine(_data.CrlDir, "revoked.jsonl");
            var entry = new { id, kind, subject, revoked_at = revokedAt.ToString("O") };
            var json = JsonSerializer.Serialize(entry);
            lock (_lock)
            {
                File.AppendAllText(path, json + "\n", Encoding.UTF8);
                TrySet0600(path);
            }
        }
        catch (Exception ex)
        {
            // Best-effort: log and continue without failing the main action
            _logger.LogWarning(ex, "Failed to append revocation entry to CRL stub");
        }

        return Task.CompletedTask;
    }

    private static void TrySet0600(string path)
    {
        try
        {
            if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
            {
                File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
            }
        }
        catch { /* ignore */ }
    }
}
