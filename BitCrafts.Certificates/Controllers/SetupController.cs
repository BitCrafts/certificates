// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (C) 2025 Younes Benmoussa <benzsoftware@pm.me>
// 
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.
// You should have received a copy of the GNU Affero General Public License along with this program. If not, see <https://www.gnu.org/licenses/>. 

using System.ComponentModel.DataAnnotations;
using BitCrafts.Certificates.Data.Repositories;
using BitCrafts.Certificates.Pki;
using Microsoft.AspNetCore.Mvc;
using BitCrafts.Certificates.Services;

namespace BitCrafts.Certificates.Controllers;

public class SetupController : Controller
{
    private readonly ISettingsRepository _settings;
    private readonly ICaService _caService;
    private readonly ILogger<SetupController> _logger;

    public SetupController(ISettingsRepository settings, ICaService caService, ILogger<SetupController> logger)
    {
        _settings = settings;
        _caService = caService;
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
            await _caService.CreateRootCaIfMissingAsync(model.Domain);
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

public class SetupViewModel
{
    [Required]
    [Display(Name = "Intranet base domain (e.g., home.lan)")]
    [RegularExpression("^[a-zA-Z0-9.-]+$", ErrorMessage = "Domain contains invalid characters.")]
    public string Domain { get; set; } = string.Empty;
}
