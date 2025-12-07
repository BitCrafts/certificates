// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (C) 2025 Younes Benmoussa <benzsoftware@pm.me>
// 
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.
// You should have received a copy of the GNU Affero General Public License along with this program. If not, see <https://www.gnu.org/licenses/>. 

using System;
using System.Linq;
using System.Threading.Tasks;
using BitCrafts.Certificates.Application.DTOs;
using BitCrafts.Certificates.Application.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;

namespace BitCrafts.Certificates.Avalonia.ViewModels;

public partial class CreateCertificateViewModel : ViewModelBase
{
    private readonly ICertificateApplicationService _certificateService;
    private readonly Action? _onCertificateCreated;

    [ObservableProperty]
    private string _certificateType = "Server";

    [ObservableProperty]
    private string _fqdn = string.Empty;

    [ObservableProperty]
    private string _ipAddresses = string.Empty;

    [ObservableProperty]
    private string _username = string.Empty;

    [ObservableProperty]
    private string _email = string.Empty;

    [ObservableProperty]
    private bool _isCreating;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public CreateCertificateViewModel(IServiceProvider serviceProvider, Action? onCertificateCreated = null)
    {
        _certificateService = serviceProvider.GetRequiredService<ICertificateApplicationService>();
        _onCertificateCreated = onCertificateCreated;
    }

    [RelayCommand]
    private async Task CreateCertificateAsync()
    {
        IsCreating = true;
        StatusMessage = string.Empty;

        try
        {
            if (CertificateType == "Server")
            {
                var ips = IpAddresses
                    .Split(new[] { ',', ' ', ';', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(ip => ip.Trim())
                    .ToArray();

                var dto = new CreateServerCertificateDto
                {
                    Fqdn = Fqdn.Trim(),
                    IpAddresses = ips.Length > 0 ? ips : null
                };

                await _certificateService.CreateServerCertificateAsync(dto);
                StatusMessage = $"Server certificate for {Fqdn} created successfully!";
            }
            else
            {
                var dto = new CreateClientCertificateDto
                {
                    Username = Username.Trim(),
                    Email = string.IsNullOrWhiteSpace(Email) ? null : Email.Trim()
                };

                await _certificateService.CreateClientCertificateAsync(dto);
                StatusMessage = $"Client certificate for {Username} created successfully!";
            }

            // Clear form
            Fqdn = string.Empty;
            IpAddresses = string.Empty;
            Username = string.Empty;
            Email = string.Empty;

            _onCertificateCreated?.Invoke();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsCreating = false;
        }
    }

    [RelayCommand]
    private void SelectCertificateType(string type)
    {
        CertificateType = type;
        StatusMessage = string.Empty;
    }
}
