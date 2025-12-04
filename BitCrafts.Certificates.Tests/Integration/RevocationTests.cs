using System.Threading.Tasks;
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

public class RevocationTests : IAsyncLifetime
{
    private string _root = string.Empty;
    private IDataDirectory _data = default!;
    private ISqliteConnectionFactory _factory = default!;
    private ISchemaBootstrapper _schema = default!;

    public async Task InitializeAsync()
    {
        _root = TestDataRoot.Create(nameof(RevocationTests));
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
    public async Task UpdateStatus_To_Revoked_Updates_Row()
    {
        var settings = new SettingsRepository(_factory);
        var ca = new CaService(_data, settings, NullLogger<CaService>.Instance);
        var repo = new CertificatesRepository(_factory);
        var audit = new TestAuditLogger();
        var svc = new LeafCertificateService(_data, ca, repo, NullLogger<LeafCertificateService>.Instance, audit);

        var id = await svc.IssueServerAsync("revoke.test.lan");
        var before = await repo.GetAsync(id);
        before.Should().NotBeNull();
        before!.Status.Should().Be("active");

        // Act
        var ok = await repo.UpdateStatusAsync(id, "revoked");
        ok.Should().BeTrue();

        var after = await repo.GetAsync(id);
        after.Should().NotBeNull();
        after!.Status.Should().Be("revoked");
        after.UpdatedAt.Should().NotBe(before.UpdatedAt);
    }
}
