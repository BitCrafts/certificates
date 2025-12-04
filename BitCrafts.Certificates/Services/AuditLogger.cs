// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (C) 2025 Younes Benmoussa <benzsoftware@pm.me>
// 
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.
// You should have received a copy of the GNU Affero General Public License along with this program. If not, see <https://www.gnu.org/licenses/>. 

using System.Text;
using System.Text.Json;

namespace BitCrafts.Certificates.Services;

public interface IAuditLogger
{
    void LogAsync(
        string action,
        string kind,
        string subject,
        long? id = null,
        string? requesterIp = null,
        string? keyPath = null,
        string? certPath = null,
        string? chainPath = null,
        CancellationToken ct = default);
}

public sealed class AuditLogger : IAuditLogger
{
    private readonly IDataDirectory _data;
    private readonly ILogger<AuditLogger> _logger;
    private readonly object _lock = new();
    private string AuditFilePath => Path.Combine(_data.LogsDir, "audit.jsonl");

    public AuditLogger(IDataDirectory data, ILogger<AuditLogger> logger)
    {
        _data = data;
        _logger = logger;
    }

    public void LogAsync(string action, string kind, string subject, long? id = null, string? requesterIp = null, string? keyPath = null, string? certPath = null, string? chainPath = null, CancellationToken ct = default)
    {
        try
        {
            Directory.CreateDirectory(_data.LogsDir);
            var entry = new
            {
                ts = DateTimeOffset.UtcNow.ToString("O"),
                action,
                kind,
                subject,
                id,
                requesterIp,
                paths = new { keyPath, certPath, chainPath }
            };
            var json = JsonSerializer.Serialize(entry);
            var line = json + "\n";

            // Avoid partial writes
            lock (_lock)
            {
                File.AppendAllText(AuditFilePath, line, Encoding.UTF8);
                TrySet0600(AuditFilePath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to write audit log for action {Action}", action);
        }
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
        catch
        {
            // ignore
        }
    }
}
