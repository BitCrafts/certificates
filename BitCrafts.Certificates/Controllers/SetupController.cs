// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (C) 2025 Younes Benmoussa <benzsoftware@pm.me>
// 
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.
// You should have received a copy of the GNU Affero General Public License along with this program. If not, see <https://www.gnu.org/licenses/>. 

using BitCrafts.Certificates.Data.Repositories;
using BitCrafts.Certificates.Domain.Interfaces;
using BitCrafts.Certificates.Presentation.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace BitCrafts.Certificates.Controllers;

/// <summary>
/// MVC Controller for initial setup
/// Uses Domain layer services for CA initialization
/// </summary>
public class SetupController : Controller
{
    private readonly ISettingsRepository _settings;
    private readonly IPkiService _pkiService;
    private readonly ILogger<SetupController> _logger;

    public SetupController(
        ISettingsRepository settings, 
        IPkiService pkiService, 
        ILogger<SetupController> logger)
    {
        _settings = settings;
        _pkiService = pkiService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var existing = await _settings.GetAsync("BITCRAFTS_DOMAIN");
        return View(new SetupViewModel { Domain = existing ?? string.Empty });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(SetupViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        await _settings.SetAsync("BITCRAFTS_DOMAIN", model.Domain);
        try
        {
            await _pkiService.EnsureRootCAAsync(model.Domain);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Root CA for domain {Domain}", model.Domain);
            ModelState.AddModelError(string.Empty, "Failed to create Root CA. See logs for details.");
            return View(model);
        }

        TempData["SetupDone"] = true;
        return RedirectToAction("Index", "Home");
    }
}
