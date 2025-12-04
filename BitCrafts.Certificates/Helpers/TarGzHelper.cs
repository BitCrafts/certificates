// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (C) 2025 Younes Benmoussa <benzsoftware@pm.me>
// 
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.
// You should have received a copy of the GNU Affero General Public License along with this program. If not, see <https://www.gnu.org/licenses/>. 

using System.IO.Compression;
using System.Text;

namespace BitCrafts.Certificates.Helpers;

public static class TarGzHelper
{
    // Create a tar.gz archive in memory containing the provided files (Name, Content).
    public static byte[] CreateTarGz(IEnumerable<(string Name, byte[] Content)> files)
    {
        // Build tar into memory then gzip it
        using var tarStream = new MemoryStream();
        foreach (var (name, content) in files)
        {
            WriteTarEntry(tarStream, name, content);
        }

        // Two 512-byte blocks of zeros as tar EOF
        tarStream.Write(new byte[1024], 0, 1024);
        tarStream.Seek(0, SeekOrigin.Begin);

        using var gzStream = new MemoryStream();
        using (var gzip = new GZipStream(gzStream, CompressionLevel.Optimal, leaveOpen: true))
        {
            tarStream.CopyTo(gzip);
        }

        gzStream.Seek(0, SeekOrigin.Begin);
        return gzStream.ToArray();
    }

    private static void WriteTarEntry(Stream output, string name, byte[] content)
    {
        var header = new byte[512];
        var nameBytes = Encoding.ASCII.GetBytes(PrepareTarName(name));
        Array.Copy(nameBytes, 0, header, 0, Math.Min(nameBytes.Length, 100));

        // mode, uid, gid default to 0/0/0
        WriteOctal(header, 100, 8, 0); // mode
        WriteOctal(header, 108, 8, 0); // uid
        WriteOctal(header, 116, 8, 0); // gid

        WriteOctal(header, 124, 12, content.Length); // size
        WriteOctal(header, 136, 12, GetUnixTimestamp()); // mtime

        // checksum field filled with spaces for checksum calculation
        for (int i = 148; i < 156; i++) header[i] = 0x20;

        header[156] = (byte)'0'; // typeflag '0' normal file

        // magic "ustar\0" and version "00"
        var magic = Encoding.ASCII.GetBytes("ustar\0");
        Array.Copy(magic, 0, header, 257, magic.Length);
        var version = Encoding.ASCII.GetBytes("00");
        Array.Copy(version, 0, header, 263, version.Length);

        // compute checksum
        long chksum = 0;
        foreach (var b in header) chksum += b;
        var chksumStr = Convert.ToString(chksum, 8).PadLeft(6, '0') + "\0 ";
        var chksumBytes = Encoding.ASCII.GetBytes(chksumStr);
        Array.Copy(chksumBytes, 0, header, 148, Math.Min(chksumBytes.Length, 8));

        output.Write(header, 0, 512);

        // write file content
        output.Write(content, 0, content.Length);
        // pad to 512
        var pad = (512 - (content.Length % 512)) % 512;
        if (pad > 0) output.Write(new byte[pad], 0, pad);
    }

    private static void WriteOctal(byte[] header, int offset, int length, long value)
    {
        var s = Convert.ToString(value, 8).PadLeft(length - 1, '0');
        var b = Encoding.ASCII.GetBytes(s);
        Array.Copy(b, 0, header, offset, Math.Min(b.Length, length - 1));
        header[offset + length - 1] = 0; // null terminator
    }

    private static int GetUnixTimestamp()
    {
        return (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }

    private static string PrepareTarName(string name)
    {
        // sanitize and ensure <= 100 chars
        var invalid = Path.GetInvalidFileNameChars();
        var sb = new StringBuilder();
        foreach (var c in name)
        {
            if (Array.IndexOf(invalid, c) >= 0) sb.Append('_'); else sb.Append(c);
        }
        var result = sb.ToString();
        if (result.Length > 100) result = result.Substring(result.Length - 100, 100);
        return result;
    }
}

