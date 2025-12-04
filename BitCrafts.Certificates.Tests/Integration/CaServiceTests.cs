using System.IO;
using System.Text.RegularExpressions;
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

public class CaServiceTests : IAsyncLifetime
{
    private string _root = string.Empty;
    private IDataDirectory _data = default!;
    private ISqliteConnectionFactory _factory = default!;
    private ISchemaBootstrapper _schema = default!;

    public async Task InitializeAsync()
    {
        _root = TestDataRoot.Create(nameof(CaServiceTests));
        _data = new DataDirectory(new DataOptions { DataDir = _root });
        _data.EnsureLayout();
        _factory = new SqliteConnectionFactory(_data);
        _schema = new SchemaBootstrapper(_factory);
        await _schema.EnsureInitializedAsync();
    }

    public Task DisposeAsync()
    {
        TestDataRoot.Cleanup(_root);
        return Task.CompletedTask;
    }

    [Fact]
    public async Task CreateRootCa_Writes_Pem_Files()
    {
        var settings = new SettingsRepository(_factory);
        var ca = new CaService(_data, settings, NullLogger<CaService>.Instance);
        await ca.CreateRootCaIfMissingAsync("test.lan");

        File.Exists(ca.RootCertPath).Should().BeTrue();
        File.Exists(ca.RootKeyPath).Should().BeTrue();

        var cert = await File.ReadAllTextAsync(ca.RootCertPath);
        var key = await File.ReadAllTextAsync(ca.RootKeyPath);

        cert.Should().Contain("-----BEGIN CERTIFICATE-----");
        cert.Should().Contain("-----END CERTIFICATE-----");
        key.Should().Contain("-----BEGIN PRIVATE KEY-----");
        key.Should().Contain("-----END PRIVATE KEY-----");
    }
}
