// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (C) 2025 Younes Benmoussa <benzsoftware@pm.me>
// 
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.
// You should have received a copy of the GNU Affero General Public License along with this program. If not, see <https://www.gnu.org/licenses/>. 

using System.Text;

namespace BitCrafts.Certificates.Pki;

public static class FileUtils
{
    /// <summary>
    /// Write content to a temp file in the same directory, set unix mode (if provided), then atomically move to target.
    /// </summary>
    public static async Task WriteSecureFileAsync(string path, string content, CancellationToken ct, UnixFileMode? mode = null)
    {
        var dir = Path.GetDirectoryName(path) ?? ".";
        Directory.CreateDirectory(dir);
        var tmp = Path.Combine(dir, $".tmp_{Path.GetFileName(path)}_{Guid.NewGuid():N}");
        await File.WriteAllTextAsync(tmp, content, Encoding.ASCII, ct);
        try
        {
            if (mode.HasValue && (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS()))
                File.SetUnixFileMode(tmp, mode.Value);
        }
        catch
        {
            // best-effort
        }
        // Move into place (overwrite if exists)
        File.Move(tmp, path, true);
    }

    public static void TrySet0600(string path)
    {
        try
        {
            if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
                File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }
        catch
        {
            // ignore
        }
    }

    public static void TrySet0700(string path)
    {
        try
        {
            if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
                File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }
        catch
        {
            // ignore
        }
    }
}
