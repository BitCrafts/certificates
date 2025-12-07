// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (C) 2025 Younes Benmoussa <benzsoftware@pm.me>
// 
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.
// You should have received a copy of the GNU Affero General Public License along with this program. If not, see <https://www.gnu.org/licenses/>. 

using Avalonia;
using System;
using Microsoft.Extensions.DependencyInjection;
using BitCrafts.Certificates.Application.Interfaces;
using BitCrafts.Certificates.Application.Services;
using BitCrafts.Certificates.Data.Repositories;
using BitCrafts.Certificates.Data;
using BitCrafts.Certificates.Services;
using BitCrafts.Certificates.Pki;
using BitCrafts.Certificates.Domain.Interfaces;
using BitCrafts.Certificates.Infrastructure.Database;
using BitCrafts.Certificates.Infrastructure.Storage;
using BitCrafts.Certificates.Infrastructure.Pki;
using BitCrafts.Certificates.Infrastructure.Deployment;
using BitCrafts.Certificates.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace BitCrafts.Certificates.Avalonia;

sealed class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();

    public static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        // Build configuration
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production"}.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();

        services.AddSingleton<IConfiguration>(configuration);

        // Configure data options from configuration
        var dataOptions = new DataOptions
        {
            DataDir = configuration["BitCrafts:DataDir"] ?? Environment.GetEnvironmentVariable("BITCRAFTS_DATA_DIR"),
            DbPath = configuration["BitCrafts:DbPath"] ?? Environment.GetEnvironmentVariable("BITCRAFTS_DB_PATH"),
            Domain = configuration["BitCrafts:Domain"] ?? Environment.GetEnvironmentVariable("BITCRAFTS_DOMAIN")
        };
        services.AddSingleton(dataOptions);

        // Core services
        services.AddSingleton<IDataDirectory, DataDirectory>();
        services.AddSingleton<ISqliteConnectionFactory, SqliteConnectionFactory>();
        services.AddSingleton<ISchemaBootstrapper, SchemaBootstrapper>();
        services.AddSingleton<ISettingsRepository, SettingsRepository>();
        services.AddSingleton<ICertificatesRepository, CertificatesRepository>();
        services.AddSingleton<IMachinesRepository, MachinesRepository>();

        // PKI services
        services.AddSingleton<ICaService, CaService>();
        services.AddSingleton<ILeafCertificateService, LeafCertificateService>();

        // Audit
        services.AddSingleton<IAuditLogger, AuditLogger>();
        services.AddSingleton<IRevocationStore, RevocationStore>();

        // Clean Architecture Layers
        services.AddSingleton<ICertificateRepository, CertificateRepositoryAdapter>();
        services.AddSingleton<ICertificateStorage, LocalFileSystemStorage>();
        services.AddSingleton<IPkiService, PkiServiceAdapter>();

        // Deployment Services
        services.AddSingleton<SshDeploymentService>();
        services.AddSingleton<NetworkFileSystemDeploymentService>();
        services.AddSingleton<IDeploymentService, CompositeDeploymentService>();

        // Application Services
        services.AddScoped<ICertificateApplicationService, CertificateApplicationService>();
        services.AddScoped<IDeploymentApplicationService, DeploymentApplicationService>();

        // Logging
        services.AddLogging(builder => 
        {
            builder.AddConfiguration(configuration.GetSection("Logging"));
            builder.AddConsole();
        });

        return services.BuildServiceProvider();
    }
}
