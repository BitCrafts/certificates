using System;
using System.IO;

namespace BitCrafts.Certificates.Tests.Helpers;

public static class TestDataRoot
{
    public static string Create(string name)
    {
        var pid = Environment.ProcessId;
        var root = Path.Combine("/tmp", $"bitcrafts-tests-{pid}", Sanitize(name));
        Directory.CreateDirectory(root);
        return root;
    }

    public static void Cleanup(string root)
    {
        try { if (Directory.Exists(root)) Directory.Delete(root, true); } catch { /* best-effort */ }
    }

    private static string Sanitize(string name)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        return name;
    }
}
