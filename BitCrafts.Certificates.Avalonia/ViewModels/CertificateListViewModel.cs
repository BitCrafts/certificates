// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (C) 2025 Younes Benmoussa <benzsoftware@pm.me>
// 
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.
// You should have received a copy of the GNU Affero General Public License along with this program. If not, see <https://www.gnu.org/licenses/>. 

using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using BitCrafts.Certificates.Application.DTOs;
using BitCrafts.Certificates.Application.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;

namespace BitCrafts.Certificates.Avalonia.ViewModels;

public partial class CertificateListViewModel : ViewModelBase
{
    private readonly ICertificateApplicationService _certificateService;

    [ObservableProperty]
    private ObservableCollection<CertificateDto> _certificates = new();

    [ObservableProperty]
    private CertificateDto? _selectedCertificate;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _filterKind = "All";

    public CertificateListViewModel(IServiceProvider serviceProvider)
    {
        _certificateService = serviceProvider.GetRequiredService<ICertificateApplicationService>();
        _ = LoadCertificatesAsync();
    }

    [RelayCommand]
    private async Task LoadCertificatesAsync()
    {
        IsLoading = true;
        try
        {
            var certs = FilterKind == "All"
                ? await _certificateService.GetAllCertificatesAsync()
                : await _certificateService.GetCertificatesByKindAsync(FilterKind.ToLower());

            Certificates.Clear();
            foreach (var cert in certs)
            {
                Certificates.Add(cert);
            }
        }
        catch (Exception ex)
        {
            // Handle error - in a real app, you'd show this in the UI
            Console.WriteLine($"Error loading certificates: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task RevokeCertificateAsync()
    {
        if (SelectedCertificate == null) return;

        try
        {
            var dto = new RevokeCertificateDto { CertificateId = SelectedCertificate.Id };
            await _certificateService.RevokeCertificateAsync(dto);
            await LoadCertificatesAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error revoking certificate: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task DeleteCertificateAsync()
    {
        if (SelectedCertificate == null) return;

        try
        {
            await _certificateService.DeleteCertificateAsync(SelectedCertificate.Id);
            await LoadCertificatesAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting certificate: {ex.Message}");
        }
    }

    [RelayCommand]
    private void FilterByKind(string kind)
    {
        FilterKind = kind;
        _ = LoadCertificatesAsync();
    }
}
