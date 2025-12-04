using System.IO;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using BitCrafts.Certificates.Data;
using BitCrafts.Certificates.Data.Repositories;
using BitCrafts.Certificates.Options;
using BitCrafts.Certificates.Pki;
using BitCrafts.Certificates.Services;
using BitCrafts.Certificates.Tests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace BitCrafts.Certificates.Tests.Integration;

public class LeafCertificateService_Client_Tests : IAsyncLifetime
{
    private string _root = string.Empty;
    private IDataDirectory _data = default!;
    private ISqliteConnectionFactory _factory = default!;
    private ISchemaBootstrapper _schema = default!;

    public async Task InitializeAsync()
    {
        _root = TestDataRoot.Create(nameof(LeafCertificateService_Client_Tests));
        _data = new DataDirectory(new DataOptions { DataDir = _root });
        _data.EnsureLayout();
        _factory = new SqliteConnectionFactory(_data);
        _schema = new SchemaBootstrapper(_factory);
        await _schema.EnsureInitializedAsync();

        var settings = new SettingsRepository(_factory);
        var ca = new CaService(_data, settings, NullLogger<CaService>.Instance);
        await ca.CreateRootCaIfMissingAsync("test.lan");
    }

    public Task DisposeAsync()
    {
        TestDataRoot.Cleanup(_root);
        return Task.CompletedTask;
    }

    [Fact]
    public async Task IssueClient_Creates_Files_And_Db_Row()
    {
        var settings = new SettingsRepository(_factory);
        var ca = new CaService(_data, settings, NullLogger<CaService>.Instance);
        var repo = new CertificatesRepository(_factory);
        var audit = new TestAuditLogger();
        var svc = new LeafCertificateService(_data, ca, repo, NullLogger<LeafCertificateService>.Instance, audit);

        var id = await svc.IssueClientAsync("alice", "alice@example.test");

        // Files
        var dir = Path.Combine(_data.CertsClientsDir, "alice");
        File.Exists(Path.Combine(dir, "cert.crt")).Should().BeTrue();
        File.Exists(Path.Combine(dir, "key.pem")).Should().BeTrue();
        File.Exists(Path.Combine(dir, "chain.crt")).Should().BeTrue();

        // EKU contains clientAuth
        var certPem = await File.ReadAllTextAsync(Path.Combine(dir, "cert.crt"));
        using var cert = X509Certificate2.CreateFromPem(certPem);
        var eku = cert.Extensions.OfType<X509EnhancedKeyUsageExtension>().SingleOrDefault();
        eku.Should().NotBeNull();
        var ekuValues = eku!.EnhancedKeyUsages.Cast<Oid>().Select(o => o.Value).ToArray();
        ekuValues.Should().Contain("1.3.6.1.5.5.7.3.2");

        // DB row
        var rec = await repo.GetAsync(id);
        rec.Should().NotBeNull();
        rec!.Kind.Should().Be("client");
        rec.SanDns.Should().Be("alice@example.test");
        rec.Status.Should().Be("active");
    }
}
