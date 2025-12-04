// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (C) 2025 Younes Benmoussa <benzsoftware@pm.me>
// 
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.
// You should have received a copy of the GNU Affero General Public License along with this program. If not, see <https://www.gnu.org/licenses/>. 

using BitCrafts.Certificates.Options;

namespace BitCrafts.Certificates.Services;

public interface IDataDirectory
{
    string DataRoot { get; }
    string DbPath { get; }
    string PkiRoot { get; }
    string CaDir { get; }
    string CertsServersDir { get; }
    string CertsClientsDir { get; }
    string CrlDir { get; }
    string CsrDir { get; }
    string TmpDir { get; }
    string LogsDir { get; }

    void EnsureLayout();
}

public sealed class DataDirectory : IDataDirectory
{
    private readonly DataOptions _opts;

    public DataDirectory(DataOptions opts)
    {
        _opts = opts;
        DataRoot = string.IsNullOrWhiteSpace(opts.DataDir)
            ? Directory.GetCurrentDirectory()
            : Path.GetFullPath(opts.DataDir!);

        var defaultDb = Path.Combine(DataRoot, "metadata", "bitcrafts.db");
        DbPath = string.IsNullOrWhiteSpace(opts.DbPath) ? defaultDb : Path.GetFullPath(opts.DbPath!);

        PkiRoot = Path.Combine(DataRoot, "pki");
        CaDir = Path.Combine(PkiRoot, "ca");
        CertsServersDir = Path.Combine(PkiRoot, "certs", "servers");
        CertsClientsDir = Path.Combine(PkiRoot, "certs", "clients");
        CrlDir = Path.Combine(PkiRoot, "crl");
        CsrDir = Path.Combine(PkiRoot, "csr");
        TmpDir = Path.Combine(PkiRoot, "tmp");
        LogsDir = Path.Combine(DataRoot, "logs");
    }

    public string DataRoot { get; }
    public string DbPath { get; }
    public string PkiRoot { get; }
    public string CaDir { get; }
    public string CertsServersDir { get; }
    public string CertsClientsDir { get; }
    public string CrlDir { get; }
    public string CsrDir { get; }
    public string TmpDir { get; }
    public string LogsDir { get; }

    public void EnsureLayout()
    {
        var dirs = new[]
        {
            DataRoot,
            Path.GetDirectoryName(DbPath)!,
            PkiRoot, CaDir, CertsServersDir, CertsClientsDir, CrlDir, CsrDir, TmpDir, LogsDir
        };
        foreach (var d in dirs)
        {
            if (string.IsNullOrEmpty(d)) continue;
            Directory.CreateDirectory(d);
            TryTightenDirPermissions(d);
        }
    }

    private static void TryTightenDirPermissions(string path)
    {
        try
        {
            if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
            {
                System.IO.File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            }
        }
        catch
        {
            // best-effort; ignore if not supported
        }
    }
}
