// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (C) 2025 Younes Benmoussa <benzsoftware@pm.me>
// 
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.
// You should have received a copy of the GNU Affero General Public License along with this program. If not, see <https://www.gnu.org/licenses/>. 

using System.ComponentModel.DataAnnotations;
using BitCrafts.Certificates.Data.Repositories;
using BitCrafts.Certificates.Models;
using BitCrafts.Certificates.Pki;
using BitCrafts.Certificates.Services;
using Microsoft.AspNetCore.Mvc;

namespace BitCrafts.Certificates.Controllers;

public class ServersController : Controller
{
    private readonly ICertificatesRepository _certs;
    private readonly ILeafCertificateService _leaf;
    private readonly ILogger<ServersController> _logger;
    private readonly IAuditLogger _audit;
    private readonly IRevocationStore _revocations;

    public ServersController(ICertificatesRepository certs, ILeafCertificateService leaf, ILogger<ServersController> logger, IAuditLogger audit, IRevocationStore revocations)
    {
        _certs = certs;
        _leaf = leaf;
        _logger = logger;
        _audit = audit;
        _revocations = revocations;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var list = await _certs.ListByKindAsync("server");
        return View(list);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new CreateServerViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateServerViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var ips = (model.IpAddresses ?? string.Empty)
                .Split(new[] { ',', ' ', ';', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var id = await _leaf.IssueServerAsync(model.Fqdn.Trim(), ips);
            // Audit with requester IP for UI-originated issuance (in addition to service-level "issue")
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            _audit.LogAsync("issue_ui", "server", model.Fqdn.Trim(), id, requesterIp: ip);
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to issue server certificate for {Fqdn}", model.Fqdn);
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Details(long id)
    {
        var rec = await _certs.GetAsync(id);
        if (rec == null) return NotFound();
        return View(rec);
    }

    [HttpGet]
    public async Task<IActionResult> Revoke(long id)
    {
        var rec = await _certs.GetAsync(id);
        if (rec == null) return NotFound();
        return View(rec);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RevokeConfirmed(long id)
    {
        var rec = await _certs.GetAsync(id);
        if (rec == null) return NotFound();
        if (!string.Equals(rec.Status, "revoked", StringComparison.OrdinalIgnoreCase))
        {
            await _certs.UpdateStatusAsync(id, "revoked");
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            _audit.LogAsync("revoke", "server", rec.SanDns ?? rec.Subject, id, requesterIp: ip);
            // Append to CRL stub (best-effort)
            await _revocations.AppendAsync(id, "server", rec.SanDns ?? rec.Subject, DateTimeOffset.UtcNow);
        }
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> Download(long id)
    {
        var rec = await _certs.GetAsync(id);
        if (rec == null) return NotFound();

        // Determine file paths from record (absolute paths stored in record)
        var certPath = rec.CertPath;
        var keyPath = rec.KeyPath;

        if (string.IsNullOrEmpty(certPath) || string.IsNullOrEmpty(keyPath) || !System.IO.File.Exists(certPath) || !System.IO.File.Exists(keyPath))
        {
            return NotFound();
        }

        var baseName = SanitizeFileName(rec.SanDns ?? rec.Subject ?? ($"cert-{rec.Id}"));
        var certBytes = await System.IO.File.ReadAllBytesAsync(certPath);
        var keyBytes = await System.IO.File.ReadAllBytesAsync(keyPath);

        var files = new List<(string Name, byte[] Content)>
        {
            ($"{baseName}.crt", certBytes),
            ($"{baseName}.key", keyBytes)
        };

        var tarGz = BitCrafts.Certificates.Helpers.TarGzHelper.CreateTarGz(files);
        var fileName = $"{baseName}.tar.gz";
        return File(tarGz, "application/gzip", fileName);
    }
    
    private static string SanitizeFileName(string s)
    {
        foreach (var c in System.IO.Path.GetInvalidFileNameChars()) s = s.Replace(c, '_');
        return s;
    }
}

public class CreateServerViewModel
{
    [Required]
    [Display(Name = "Server FQDN")]
    [RegularExpression("^[A-Za-z0-9.-]+$", ErrorMessage = "FQDN contains invalid characters.")]
    public string Fqdn { get; set; } = string.Empty;

    [Display(Name = "IP addresses (optional, comma or space separated)")]
    public string? IpAddresses { get; set; }
}
