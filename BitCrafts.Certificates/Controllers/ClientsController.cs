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

public class ClientsController : Controller
{
    private readonly ICertificatesRepository _certs;
    private readonly ILeafCertificateService _leaf;
    private readonly ILogger<ClientsController> _logger;
    private readonly IAuditLogger _audit;
    private readonly IRevocationStore _revocations;

    public ClientsController(ICertificatesRepository certs, ILeafCertificateService leaf, ILogger<ClientsController> logger, IAuditLogger audit, IRevocationStore revocations)
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
        var list = await _certs.ListByKindAsync("client");
        return View(list);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new CreateClientViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateClientViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var username = model.Username.Trim();
            var email = string.IsNullOrWhiteSpace(model.Email) ? null : model.Email.Trim();
            var id = await _leaf.IssueClientAsync(username, email);
            // Audit with requester IP for UI-originated issuance (in addition to service-level "issue")
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            _audit.LogAsync("issue_ui", "client", username, id, requesterIp: ip);
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to issue client certificate for {User}", model.Username);
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
            _audit.LogAsync("revoke", "client", rec.SanDns ?? rec.Subject, id, requesterIp: ip);
            // Append to CRL stub (best-effort)
            await _revocations.AppendAsync(id, "client", rec.SanDns ?? rec.Subject, DateTimeOffset.UtcNow);
        }
        return RedirectToAction(nameof(Details), new { id });
    }
}

public class CreateClientViewModel
{
    [Required]
    [Display(Name = "Username")]
    [RegularExpression("^[A-Za-z0-9._-]+$", ErrorMessage = "Username contains invalid characters.")]
    public string Username { get; set; } = string.Empty;

    [Display(Name = "Email (optional, will be added to SAN)")]
    [EmailAddress]
    public string? Email { get; set; }
}
