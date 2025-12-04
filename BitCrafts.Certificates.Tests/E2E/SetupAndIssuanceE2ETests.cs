using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BitCrafts.Certificates.Tests.Helpers;
using FluentAssertions;
using Xunit;

namespace BitCrafts.Certificates.Tests.E2E;

public class SetupAndIssuanceE2ETests
{
    [Fact]
    public async Task FirstRun_Redirects_To_Setup_And_Setup_Post_Creates_RootCA()
    {
        var root = TestDataRoot.Create($"{nameof(SetupAndIssuanceE2ETests)}_{nameof(FirstRun_Redirects_To_Setup_And_Setup_Post_Creates_RootCA)}_{Guid.NewGuid():N}");
        await using var _ = new AsyncDisposer(() => { TestDataRoot.Cleanup(root); return Task.CompletedTask; });
        using var factory = new AppFactory(root);

        var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // First run: GET / should redirect to /Setup
        var resp = await client.GetAsync("/");
        resp.StatusCode.Should().Be(HttpStatusCode.Redirect);
        resp.Headers.Location!.ToString().Should().StartWith("/Setup");

        // Fetch anti-forgery token from GET /Setup
        var (token, cookie) = await AntiforgeryHelper.FetchTokenAsync(client, "/Setup");

        // POST /Setup with domain
        var req = AntiforgeryHelper.CreateFormPost("/Setup", new[]
        {
            ("Domain", "e2e.test.lan"),
            ("__RequestVerificationToken", token)
        }, cookie);

        var post = await client.SendAsync(req);
        post.StatusCode.Should().Be(HttpStatusCode.Redirect);

        // After setup, home should be accessible
        var home = await client.GetAsync("/");
        home.StatusCode.Should().Be(HttpStatusCode.OK);

        // Root CA files should exist
        var caDir = Path.Combine(root, "pki", "ca");
        File.Exists(Path.Combine(caDir, "root_ca.crt")).Should().BeTrue();
        File.Exists(Path.Combine(caDir, "root_ca.key")).Should().BeTrue();
    }

    [Fact]
    public async Task Issue_Server_Then_Revoke_E2E()
    {
        var root = TestDataRoot.Create($"{nameof(SetupAndIssuanceE2ETests)}_{nameof(Issue_Server_Then_Revoke_E2E)}_{Guid.NewGuid():N}");
        await using var _ = new AsyncDisposer(() => { TestDataRoot.Cleanup(root); return Task.CompletedTask; });
        using var factory = new AppFactory(root);

        var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Complete setup first
        var (tokenSetup, cookieSetup) = await AntiforgeryHelper.FetchTokenAsync(client, "/Setup");
        var setupReq = AntiforgeryHelper.CreateFormPost("/Setup", new[]
        {
            ("Domain", "e2e.test.lan"),
            ("__RequestVerificationToken", tokenSetup)
        }, cookieSetup);
        var setupResp = await client.SendAsync(setupReq);
        setupResp.StatusCode.Should().Be(HttpStatusCode.Redirect);

        // GET create page and extract token
        var (token, cookie) = await AntiforgeryHelper.FetchTokenAsync(client, "/Servers/Create");

        // POST create
        var createReq = AntiforgeryHelper.CreateFormPost("/Servers/Create", new[]
        {
            ("Fqdn", "web.e2e.test.lan"),
            ("IpAddresses", "10.10.0.10 10.10.0.11"),
            ("__RequestVerificationToken", token)
        }, cookie);
        var createResp = await client.SendAsync(createReq);
        createResp.StatusCode.Should().Be(HttpStatusCode.Redirect);
        var detailsLocation = createResp.Headers.Location!.ToString();
        detailsLocation.Should().Contain("/Servers/Details");

        // Extract id from Location (…/Details?id=123)
        var id = ExtractId(detailsLocation);
        id.Should().BeGreaterThan(0);

        // Verify files exist
        var dir = Path.Combine(root, "pki", "certs", "servers", "web.e2e.test.lan");
        File.Exists(Path.Combine(dir, "cert.crt")).Should().BeTrue();
        File.Exists(Path.Combine(dir, "key.pem")).Should().BeTrue();
        File.Exists(Path.Combine(dir, "chain.crt")).Should().BeTrue();

        // Soft check: audit contains issue_ui
        var auditFile = Path.Combine(root, "logs", "audit.jsonl");
        File.Exists(auditFile).Should().BeTrue();
        var auditBefore = await File.ReadAllTextAsync(auditFile);
        auditBefore.Should().Contain("\"issue_ui\"");

        // Prepare CRL line count before revoke
        var crlPath = Path.Combine(root, "pki", "crl", "revoked.jsonl");
        var linesBefore = File.Exists(crlPath) ? (await File.ReadAllLinesAsync(crlPath)).Length : 0;

        // Revoke flow: GET Revoke confirm to get token
        var (revokeToken, revokeCookie) = await AntiforgeryHelper.FetchTokenAsync(client, $"/Servers/Revoke?id={id}");
        var revokeReq = AntiforgeryHelper.CreateFormPost("/Servers/RevokeConfirmed", new[]
        {
            ("id", id.ToString()),
            ("__RequestVerificationToken", revokeToken)
        }, revokeCookie);
        var revokeResp = await client.SendAsync(revokeReq);
        revokeResp.StatusCode.Should().Be(HttpStatusCode.Redirect);

        // Soft checks: audit/CRL files exist and CRL appended
        File.Exists(auditFile).Should().BeTrue();
        File.Exists(crlPath).Should().BeTrue();
        var linesAfter = (await File.ReadAllLinesAsync(crlPath)).Length;
        linesAfter.Should().Be(linesBefore + 1);
    }

    [Fact]
    public async Task Issue_Client_Then_Revoke_E2E()
    {
        var root = TestDataRoot.Create($"{nameof(SetupAndIssuanceE2ETests)}_{nameof(Issue_Client_Then_Revoke_E2E)}_{Guid.NewGuid():N}");
        await using var _ = new AsyncDisposer(() => { TestDataRoot.Cleanup(root); return Task.CompletedTask; });
        using var factory = new AppFactory(root);

        var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Complete setup first
        var (tokenSetup, cookieSetup) = await AntiforgeryHelper.FetchTokenAsync(client, "/Setup");
        var setupReq = AntiforgeryHelper.CreateFormPost("/Setup", new[]
        {
            ("Domain", "e2e.test.lan"),
            ("__RequestVerificationToken", tokenSetup)
        }, cookieSetup);
        var setupResp = await client.SendAsync(setupReq);
        setupResp.StatusCode.Should().Be(HttpStatusCode.Redirect);

        // GET create page and extract token
        var (token, cookie) = await AntiforgeryHelper.FetchTokenAsync(client, "/Clients/Create");

        // POST create
        var createReq = AntiforgeryHelper.CreateFormPost("/Clients/Create", new[]
        {
            ("Username", "alice"),
            ("Email", "alice@e2e.test"),
            ("__RequestVerificationToken", token)
        }, cookie);
        var createResp = await client.SendAsync(createReq);
        createResp.StatusCode.Should().Be(HttpStatusCode.Redirect);
        var detailsLocation = createResp.Headers.Location!.ToString();
        detailsLocation.Should().Contain("/Clients/Details");
        var id = ExtractId(detailsLocation);

        // Files exist
        var dir = Path.Combine(root, "pki", "certs", "clients", "alice");
        File.Exists(Path.Combine(dir, "cert.crt")).Should().BeTrue();
        File.Exists(Path.Combine(dir, "key.pem")).Should().BeTrue();
        File.Exists(Path.Combine(dir, "chain.crt")).Should().BeTrue();

        // Revoke
        var (revokeToken, revokeCookie) = await AntiforgeryHelper.FetchTokenAsync(client, $"/Clients/Revoke?id={id}");
        var revokeReq = AntiforgeryHelper.CreateFormPost("/Clients/RevokeConfirmed", new[]
        {
            ("id", id.ToString()),
            ("__RequestVerificationToken", revokeToken)
        }, revokeCookie);
        var revokeResp = await client.SendAsync(revokeReq);
        revokeResp.StatusCode.Should().Be(HttpStatusCode.Redirect);

        // Soft check audit exists
        var auditFile = Path.Combine(root, "logs", "audit.jsonl");
        File.Exists(auditFile).Should().BeTrue();
    }

    private static long ExtractId(string location)
    {
        // Support both query string (?id=123) and route segment (/Details/123)
        var m = Regex.Match(location, @"[?&]id=(\d+)");
        if (m.Success && long.TryParse(m.Groups[1].Value, out var qid)) return qid;
        m = Regex.Match(location, @"/(Details|Clients/Details|Servers/Details)/(?<id>\d+)");
        if (m.Success && long.TryParse(m.Groups["id"].Value, out var sid)) return sid;
        var tail = location.Split('/', StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
        if (long.TryParse(tail, out var last)) return last;
        return -1;
    }
}

internal sealed class AsyncDisposer : IAsyncDisposable
{
    private readonly Func<Task> _dispose;
    public AsyncDisposer(Func<Task> dispose) => _dispose = dispose;
    public ValueTask DisposeAsync() => new ValueTask(_dispose());
}
