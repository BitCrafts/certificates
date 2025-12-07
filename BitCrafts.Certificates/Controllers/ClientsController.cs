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
/// MVC Controller for client certificate management UI
/// Follows clean architecture by delegating to Application layer services
/// </summary>
public class ClientsController : Controller
{
    private readonly ICertificateApplicationService _certificateService;
    private readonly ILogger<ClientsController> _logger;

    public ClientsController(
        ICertificateApplicationService certificateService,
        ILogger<ClientsController> logger)
    {
        _certificateService = certificateService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var certificates = await _certificateService.GetCertificatesByKindAsync("client");
        return View(certificates);
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
            var dto = new CreateClientCertificateDto
            {
                Username = model.Username.Trim(),
                Email = string.IsNullOrWhiteSpace(model.Email) ? null : model.Email.Trim()
            };

            var certificate = await _certificateService.CreateClientCertificateAsync(dto);
            return RedirectToAction(nameof(Details), new { id = certificate.Id });
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
}
