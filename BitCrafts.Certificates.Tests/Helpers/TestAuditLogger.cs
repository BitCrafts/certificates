using System.Threading;
using System.Threading.Tasks;
using BitCrafts.Certificates.Services;

namespace BitCrafts.Certificates.Tests.Helpers;

// Simple no-op audit logger for tests to avoid FS coupling
public sealed class TestAuditLogger : IAuditLogger
{
    public void LogAsync(string action, string kind, string subject, long? id = null, string? requesterIp = null,
        string? keyPath = null, string? certPath = null, string? chainPath = null, CancellationToken ct = default)
    {
        
    }
}
