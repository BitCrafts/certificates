// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (C) 2025 Younes Benmoussa <benzsoftware@pm.me>
// 
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.
// You should have received a copy of the GNU Affero General Public License along with this program. If not, see <https://www.gnu.org/licenses/>. 

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using BitCrafts.Certificates.Application.DTOs;
using BitCrafts.Certificates.Application.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;

namespace BitCrafts.Certificates.Avalonia.ViewModels;

public partial class DeploymentViewModel : ViewModelBase
{
    private readonly IDeploymentApplicationService _deploymentService;
    private readonly ICertificateApplicationService _certificateService;

    [ObservableProperty]
    private ObservableCollection<CertificateDto> _certificates = new();

    [ObservableProperty]
    private CertificateDto? _selectedCertificate;

    [ObservableProperty]
    private string _deploymentType = "SSH";

    [ObservableProperty]
    private string _target = string.Empty;

    [ObservableProperty]
    private string _username = string.Empty;

    [ObservableProperty]
    private string _privateKeyPath = string.Empty;

    [ObservableProperty]
    private int _port = 22;

    [ObservableProperty]
    private string _destinationPath = string.Empty;

    [ObservableProperty]
    private bool _isDeploying;

    [ObservableProperty]
    private bool _isTesting;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _showSshFields = true;

    public DeploymentViewModel(IServiceProvider serviceProvider)
    {
        _deploymentService = serviceProvider.GetRequiredService<IDeploymentApplicationService>();
        _certificateService = serviceProvider.GetRequiredService<ICertificateApplicationService>();
        
        LoadCertificatesAsync().ConfigureAwait(false);
    }

    private async Task LoadCertificatesAsync()
    {
        try
        {
            var certs = await _certificateService.GetAllCertificatesAsync();
            Certificates = new ObservableCollection<CertificateDto>(
                certs.Where(c => c.Status == "active" && !c.IsRevoked)
            );
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading certificates: {ex.Message}";
        }
    }

    [RelayCommand]
    private void SelectDeploymentType(string type)
    {
        DeploymentType = type;
        ShowSshFields = type == "SSH";
        StatusMessage = string.Empty;
    }

    [RelayCommand]
    private async Task TestConnectionAsync()
    {
        if (string.IsNullOrWhiteSpace(Target))
        {
            StatusMessage = "Please enter a target";
            return;
        }

        IsTesting = true;
        StatusMessage = "Testing connection...";

        try
        {
            var targetDto = new DeploymentDto
            {
                Type = DeploymentType,
                Target = Target.Trim(),
                Username = string.IsNullOrWhiteSpace(Username) ? null : Username.Trim(),
                PrivateKeyPath = string.IsNullOrWhiteSpace(PrivateKeyPath) ? null : PrivateKeyPath.Trim(),
                Port = ShowSshFields ? Port : null,
                DestinationPath = string.IsNullOrWhiteSpace(DestinationPath) ? null : DestinationPath.Trim()
            };

            var result = await _deploymentService.TestConnectionAsync(targetDto);

            if (result.Success)
            {
                StatusMessage = $"✓ {result.Message}";
            }
            else
            {
                StatusMessage = $"✗ {result.Error ?? "Connection test failed"}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"✗ Error: {ex.Message}";
        }
        finally
        {
            IsTesting = false;
        }
    }

    [RelayCommand]
    private async Task DeployCertificateAsync()
    {
        if (SelectedCertificate == null)
        {
            StatusMessage = "Please select a certificate";
            return;
        }

        if (string.IsNullOrWhiteSpace(Target))
        {
            StatusMessage = "Please enter a target";
            return;
        }

        if (string.IsNullOrWhiteSpace(DestinationPath))
        {
            StatusMessage = "Please enter a destination path";
            return;
        }

        IsDeploying = true;
        StatusMessage = "Deploying certificate...";

        try
        {
            var request = new DeploymentRequestDto
            {
                CertificateId = SelectedCertificate.Id,
                DeploymentTarget = new DeploymentDto
                {
                    Type = DeploymentType,
                    Target = Target.Trim(),
                    Username = string.IsNullOrWhiteSpace(Username) ? null : Username.Trim(),
                    PrivateKeyPath = string.IsNullOrWhiteSpace(PrivateKeyPath) ? null : PrivateKeyPath.Trim(),
                    Port = ShowSshFields ? Port : null,
                    DestinationPath = DestinationPath.Trim()
                }
            };

            var result = await _deploymentService.DeployCertificateAsync(request);

            if (result.Success)
            {
                StatusMessage = $"✓ {result.Message}";
            }
            else
            {
                StatusMessage = $"✗ {result.Error ?? "Deployment failed"}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"✗ Error: {ex.Message}";
        }
        finally
        {
            IsDeploying = false;
        }
    }
}
