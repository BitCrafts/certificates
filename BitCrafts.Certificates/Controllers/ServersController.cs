// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (C) 2025 Younes Benmoussa <benzsoftware@pm.me>
// 
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.
// You should have received a copy of the GNU Affero General Public License along with this program. If not, see <https://www.gnu.org/licenses/>. 

using BitCrafts.Certificates.Application.DTOs;
using BitCrafts.Certificates.Application.Interfaces;
using BitCrafts.Certificates.Presentation.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace BitCrafts.Certificates.Controllers;

/// <summary>
/// MVC Controller for server certificate management UI
/// Follows clean architecture by delegating to Application layer services
/// </summary>
public class ServersController : Controller
{
    private readonly ICertificateApplicationService _certificateService;
    private readonly ILogger<ServersController> _logger;

    public ServersController(
        ICertificateApplicationService certificateService,
        ILogger<ServersController> logger)
    {
        _certificateService = certificateService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var certificates = await _certificateService.GetCertificatesByKindAsync("server");
        return View(certificates);
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
                .Split(new[] { ',', ' ', ';', '\n', '\r', '\t' }, 
                    StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            var dto = new CreateServerCertificateDto
            {
                Fqdn = model.Fqdn.Trim(),
                IpAddresses = ips
            };

            var certificate = await _certificateService.CreateServerCertificateAsync(dto);
            return RedirectToAction(nameof(Details), new { id = certificate.Id });
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
        var certificate = await _certificateService.GetCertificateAsync(id);
        if (certificate == null) 
            return NotFound();
        
        return View(certificate);
    }

    [HttpGet]
    public async Task<IActionResult> Revoke(long id)
    {
        var certificate = await _certificateService.GetCertificateAsync(id);
        if (certificate == null) 
            return NotFound();
        
        return View(certificate);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RevokeConfirmed(long id)
    {
        var dto = new RevokeCertificateDto { CertificateId = id };
        var success = await _certificateService.RevokeCertificateAsync(dto);
        
        if (!success)
            return NotFound();
        
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> Download(long id)
    {
        try
        {
            var archive = await _certificateService.DownloadCertificateArchiveAsync(id);
            return File(archive, "application/gzip", $"certificate-{id}.tar.gz");
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download certificate {Id}", id);
            return StatusCode(500, "Failed to download certificate");
        }
    }
}
