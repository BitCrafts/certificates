using BitCrafts.Certificates.Options;
using BitCrafts.Certificates.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BitCrafts.Certificates.Tests.Helpers;

public sealed class AppFactory : WebApplicationFactory<Program>
{
    private readonly string _dataRoot;
    public AppFactory(string dataRoot)
    {
        _dataRoot = dataRoot;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureServices(services =>
        {
            // Replace IDataDirectory to force a temp data root per test
            services.RemoveAll<IDataDirectory>();
            services.AddSingleton<IDataDirectory>(new DataDirectory(new DataOptions { DataDir = _dataRoot }));
        });
    }
}
