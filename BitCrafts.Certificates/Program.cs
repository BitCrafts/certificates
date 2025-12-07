// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (C) 2025 Younes Benmoussa <benzsoftware@pm.me>
// 
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.
// You should have received a copy of the GNU Affero General Public License along with this program. If not, see <https://www.gnu.org/licenses/>. 

using BitCrafts.Certificates.Data;
using BitCrafts.Certificates.Data.Repositories;
using BitCrafts.Certificates.Options;
using BitCrafts.Certificates.Pki;
using BitCrafts.Certificates.Services;
using BitCrafts.Certificates.Domain.Interfaces;
using BitCrafts.Certificates.Application.Interfaces;
using BitCrafts.Certificates.Application.Services;
using BitCrafts.Certificates.Infrastructure.Database;
using BitCrafts.Certificates.Infrastructure.Storage;
using BitCrafts.Certificates.Infrastructure.Pki;
using BitCrafts.Certificates.Infrastructure.Deployment;
using Microsoft.AspNetCore.Http;

var builder = WebApplication.CreateBuilder(args);

// Options binding from environment
builder.Services.Configure<DataOptions>(builder.Configuration.GetSection("BitCrafts"));
builder.Services.AddSingleton(_ =>
{
    // Bind environment variables explicitly with fallbacks
    var cfg = new DataOptions
    {
        DataDir = Environment.GetEnvironmentVariable("BITCRAFTS_DATA_DIR"),
        DbPath = Environment.GetEnvironmentVariable("BITCRAFTS_DB_PATH"),
        Domain = Environment.GetEnvironmentVariable("BITCRAFTS_DOMAIN")
    };
    return cfg;
});

// Core services
builder.Services.AddSingleton<IDataDirectory, DataDirectory>();
builder.Services.AddSingleton<ISqliteConnectionFactory, SqliteConnectionFactory>();
builder.Services.AddSingleton<ISchemaBootstrapper, SchemaBootstrapper>();
builder.Services.AddSingleton<ISettingsRepository, SettingsRepository>();
builder.Services.AddSingleton<ICertificatesRepository, CertificatesRepository>();
// Machines repository is not used by core code paths; keep default no-op implementation registered earlier.
builder.Services.AddSingleton<IMachinesRepository, MachinesRepository>();

// PKI services (root CA creation only for now)
builder.Services.AddSingleton<ICaService, CaService>();
builder.Services.AddSingleton<ILeafCertificateService, LeafCertificateService>();

// Audit
builder.Services.AddSingleton<IAuditLogger, AuditLogger>();
builder.Services.AddSingleton<IRevocationStore, RevocationStore>();

// Clean Architecture Layers
// Domain Layer Ports (Interfaces) - Infrastructure implementations
builder.Services.AddSingleton<ICertificateRepository, CertificateRepositoryAdapter>();
builder.Services.AddSingleton<ICertificateStorage, LocalFileSystemStorage>();
builder.Services.AddSingleton<IPkiService, PkiServiceAdapter>();

// Deployment Services
builder.Services.AddSingleton<SshDeploymentService>();
builder.Services.AddSingleton<NetworkFileSystemDeploymentService>();
builder.Services.AddSingleton<IDeploymentService, CompositeDeploymentService>();

// Application Services
builder.Services.AddScoped<ICertificateApplicationService, CertificateApplicationService>();
builder.Services.AddScoped<IDeploymentApplicationService, DeploymentApplicationService>();

// MVC and API
builder.Services.AddControllersWithViews();
builder.Services.AddControllers(); // For API controllers
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { 
        Title = "BitCrafts Certificates API", 
        Version = "v1",
        Description = "REST API for certificate management and deployment"
    });
});

// Antiforgery: prefer header-based tokens for XHR and enforce secure cookie options by default (controllers already use ValidateAntiForgeryToken)
builder.Services.AddAntiforgery(options =>
{
    // Header name used by JS clients if needed
    options.HeaderName = "X-CSRF-TOKEN";
});

var app = builder.Build();

// Ensure data directories, schema and first-run state
var dataDir = app.Services.GetRequiredService<IDataDirectory>();
dataDir.EnsureLayout();

var schema = app.Services.GetRequiredService<ISchemaBootstrapper>();
await schema.EnsureInitializedAsync();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "BitCrafts Certificates API v1");
    });
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
    // In production, enforce HTTPS redirection. In Development, keep HTTP to simplify local/E2E tests.
    app.UseHttpsRedirection();
}

// Security headers middleware (minimal and conservative)
app.Use(async (HttpContext context, Func<Task> next) =>
{
    // Prevent MIME sniffing
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    // Clickjacking protection
    context.Response.Headers["X-Frame-Options"] = "DENY";
    // Basic XSS protection header (legacy browsers)
    context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
    // Referrer policy
    context.Response.Headers["Referrer-Policy"] = "no-referrer";
    // Reduce feature exposure
    context.Response.Headers["Permissions-Policy"] = "geolocation=(), microphone=()";

    // Content-Security-Policy: keep conservative default; adjust if your UI requires external resources
    // Note: this is intentionally minimal to avoid breaking the UI; tighten as you audit front-end assets.
    context.Response.Headers["Content-Security-Policy"] = "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'";

    // HSTS: only send HSTS when request is over HTTPS
    if (context.Request.IsHttps && app.Environment.IsProduction())
    {
        context.Response.Headers["Strict-Transport-Security"] = "max-age=63072000; includeSubDomains; preload";
    }

    await next();
});

app.UseStaticFiles();

app.UseRouting();

// First-run redirect to Setup when domain not configured
app.Use(async (HttpContext context, Func<Task> next) =>
{
    var settings = context.RequestServices.GetRequiredService<ISettingsRepository>();
    var path = context.Request.Path.Value ?? string.Empty;
    var isSetup = path.StartsWith("/Setup", StringComparison.OrdinalIgnoreCase) || path.StartsWith("/setup", StringComparison.OrdinalIgnoreCase);
    var hasDomain = await settings.GetAsync("BITCRAFTS_DOMAIN") is { Length: > 0 };
    if (!hasDomain && !isSetup && !path.StartsWith("/css/") && !path.StartsWith("/js/") && !path.StartsWith("/lib/") && !path.StartsWith("/favicon"))
    {
        context.Response.Redirect("/Setup");
        return;
    }
    await next();
});

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Map API controllers
app.MapControllers();

app.Run();

// Required for WebApplicationFactory-based E2E tests
public partial class Program { }