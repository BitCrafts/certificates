// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (C) 2025 Younes Benmoussa <benzsoftware@pm.me>
// 
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.
// You should have received a copy of the GNU Affero General Public License along with this program. If not, see <https://www.gnu.org/licenses/>. 

using System;
using System.IO;
using System.Threading.Tasks;
using BitCrafts.Certificates.Application.DTOs;
using BitCrafts.Certificates.Application.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;

namespace BitCrafts.Certificates.Avalonia.ViewModels;

public partial class CertificateDetailsViewModel : ViewModelBase
{
    private readonly ICertificateApplicationService _certificateService;
    private readonly Action? _onCertificateUpdated;

    [ObservableProperty]
    private CertificateDto? _certificate;

    [ObservableProperty]
    private bool _isProcessing;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _showRevokeConfirmation;

    public CertificateDetailsViewModel(IServiceProvider serviceProvider, Action? onCertificateUpdated = null)
    {
        _certificateService = serviceProvider.GetRequiredService<ICertificateApplicationService>();
        _onCertificateUpdated = onCertificateUpdated;
    }

    public async Task LoadCertificateAsync(long certificateId)
    {
        IsProcessing = true;
        try
        {
            Certificate = await _certificateService.GetCertificateAsync(certificateId);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading certificate: {ex.Message}";
        }
        finally
        {
            IsProcessing = false;
        }
    }

    [RelayCommand]
    private void ShowRevokeDialog()
    {
        ShowRevokeConfirmation = true;
    }

    [RelayCommand]
    private void CancelRevoke()
    {
        ShowRevokeConfirmation = false;
    }

    [RelayCommand]
    private async Task ConfirmRevokeAsync()
    {
        if (Certificate == null) return;

        ShowRevokeConfirmation = false;
        IsProcessing = true;
        StatusMessage = "Revoking certificate...";

        try
        {
            var dto = new RevokeCertificateDto { CertificateId = Certificate.Id };
            var success = await _certificateService.RevokeCertificateAsync(dto);

            if (success)
            {
                StatusMessage = "Certificate revoked successfully";
                await LoadCertificateAsync(Certificate.Id);
                _onCertificateUpdated?.Invoke();
            }
            else
            {
                StatusMessage = "Failed to revoke certificate";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsProcessing = false;
        }
    }

    [RelayCommand]
    private async Task DownloadCertificateAsync()
    {
        if (Certificate == null) return;

        IsProcessing = true;
        StatusMessage = "Preparing download...";

        try
        {
            var archive = await _certificateService.DownloadCertificateArchiveAsync(Certificate.Id);
            
            // Save to user's Downloads folder
            var downloadsPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Downloads");
            
            var fileName = $"certificate-{Certificate.Id}-{Certificate.Subject}.tar.gz";
            var filePath = Path.Combine(downloadsPath, fileName);

            await File.WriteAllBytesAsync(filePath, archive);
            StatusMessage = $"Downloaded to: {filePath}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error downloading: {ex.Message}";
        }
        finally
        {
            IsProcessing = false;
        }
    }
}
