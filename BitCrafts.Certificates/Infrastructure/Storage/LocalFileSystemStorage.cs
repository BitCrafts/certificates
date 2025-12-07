// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (C) 2025 Younes Benmoussa <benzsoftware@pm.me>
// 
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.
// You should have received a copy of the GNU Affero General Public License along with this program. If not, see <https://www.gnu.org/licenses/>. 

using BitCrafts.Certificates.Domain.Interfaces;
using BitCrafts.Certificates.Pki;

namespace BitCrafts.Certificates.Infrastructure.Storage;

/// <summary>
/// Local filesystem storage implementation
/// </summary>
public sealed class LocalFileSystemStorage : ICertificateStorage
{
    private readonly ILogger<LocalFileSystemStorage> _logger;

    public LocalFileSystemStorage(ILogger<LocalFileSystemStorage> logger)
    {
        _logger = logger;
    }

    public async Task<string> SaveCertificateAsync(string name, byte[] content, CancellationToken ct = default)
    {
        var path = Path.Combine(Path.GetTempPath(), name);
        await File.WriteAllBytesAsync(path, content, ct);
        FileUtils.TrySet0600(path);
        _logger.LogDebug("Saved certificate to {Path}", path);
        return path;
    }

    public async Task<string> SavePrivateKeyAsync(string name, byte[] content, CancellationToken ct = default)
    {
        var path = Path.Combine(Path.GetTempPath(), name);
        await File.WriteAllBytesAsync(path, content, ct);
        FileUtils.TrySet0600(path);
        _logger.LogDebug("Saved private key to {Path}", path);
        return path;
    }

    public async Task<byte[]> ReadCertificateAsync(string path, CancellationToken ct = default)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Certificate not found at {path}");
        }
        return await File.ReadAllBytesAsync(path, ct);
    }

    public async Task<byte[]> ReadPrivateKeyAsync(string path, CancellationToken ct = default)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Private key not found at {path}");
        }
        return await File.ReadAllBytesAsync(path, ct);
    }

    public Task DeleteAsync(string path, CancellationToken ct = default)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
            _logger.LogDebug("Deleted file at {Path}", path);
        }
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string path, CancellationToken ct = default)
    {
        return Task.FromResult(File.Exists(path));
    }
}
