// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (C) 2025 Younes Benmoussa <benzsoftware@pm.me>
// 
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.
// You should have received a copy of the GNU Affero General Public License along with this program. If not, see <https://www.gnu.org/licenses/>. 

using System;
using System.Threading.Tasks;
using BitCrafts.Certificates.Data.Repositories;
using BitCrafts.Certificates.Domain.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BitCrafts.Certificates.Avalonia.ViewModels;

public partial class SetupViewModel : ViewModelBase
{
    private readonly ISettingsRepository _settings;
    private readonly IPkiService _pkiService;
    private readonly ILogger<SetupViewModel> _logger;
    private readonly Action _onSetupComplete;

    [ObservableProperty]
    private string _domain = string.Empty;

    [ObservableProperty]
    private bool _isProcessing;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _hasError;

    public SetupViewModel(IServiceProvider serviceProvider, Action onSetupComplete)
    {
        _settings = serviceProvider.GetRequiredService<ISettingsRepository>();
        _pkiService = serviceProvider.GetRequiredService<IPkiService>();
        _logger = serviceProvider.GetRequiredService<ILogger<SetupViewModel>>();
        _onSetupComplete = onSetupComplete;

        _ = LoadExistingDomainAsync();
    }

    private async Task LoadExistingDomainAsync()
    {
        try
        {
            var existing = await _settings.GetAsync("BITCRAFTS_DOMAIN");
            if (!string.IsNullOrEmpty(existing))
            {
                Domain = existing;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load existing domain");
        }
    }

    [RelayCommand]
    private async Task CompleteSetupAsync()
    {
        if (string.IsNullOrWhiteSpace(Domain))
        {
            StatusMessage = "Please enter a domain name";
            HasError = true;
            return;
        }

        IsProcessing = true;
        StatusMessage = "Initializing Certificate Authority...";
        HasError = false;

        try
        {
            await _settings.SetAsync("BITCRAFTS_DOMAIN", Domain.Trim());
            await _pkiService.EnsureRootCAAsync(Domain.Trim());

            StatusMessage = "Setup completed successfully!";
            await Task.Delay(1000); // Brief delay to show success message
            _onSetupComplete?.Invoke();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Root CA for domain {Domain}", Domain);
            StatusMessage = $"Failed to create Root CA: {ex.Message}";
            HasError = true;
        }
        finally
        {
            IsProcessing = false;
        }
    }
}
