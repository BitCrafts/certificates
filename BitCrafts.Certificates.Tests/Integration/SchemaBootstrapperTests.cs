using System;
using System.IO;
using System.Threading.Tasks;
using BitCrafts.Certificates.Data;
using BitCrafts.Certificates.Options;
using BitCrafts.Certificates.Services;
using BitCrafts.Certificates.Tests.Helpers;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Xunit;

namespace BitCrafts.Certificates.Tests.Integration;

public class SchemaBootstrapperTests : IAsyncLifetime
{
    private string _root = string.Empty;
    private IDataDirectory _data = default!;
    private ISqliteConnectionFactory _factory = default!;

    public Task InitializeAsync()
    {
        _root = TestDataRoot.Create(nameof(SchemaBootstrapperTests));
        _data = new DataDirectory(new DataOptions { DataDir = _root });
        _data.EnsureLayout();
        _factory = new SqliteConnectionFactory(_data);
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        TestDataRoot.Cleanup(_root);
        return Task.CompletedTask;
    }

    [Fact]
    public async Task EnsureInitialized_Sets_WAL_And_ForeignKeys()
    {
        var bootstrapper = new SchemaBootstrapper(_factory);
        await bootstrapper.EnsureInitializedAsync();

        await using var conn = _factory.Create();
        await conn.OpenAsync();

        // journal_mode
        await using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "PRAGMA journal_mode;";
            var mode = (string)(await cmd.ExecuteScalarAsync() ?? string.Empty);
            mode.Should().BeOneOf("wal", "WAL");
        }

        // foreign_keys
        await using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "PRAGMA foreign_keys;";
            var on = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            on.Should().Be(1);
        }
    }
}
