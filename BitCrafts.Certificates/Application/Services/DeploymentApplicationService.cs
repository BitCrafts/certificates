// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (C) 2025 Younes Benmoussa <benzsoftware@pm.me>
// 
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.
// You should have received a copy of the GNU Affero General Public License along with this program. If not, see <https://www.gnu.org/licenses/>. 

using BitCrafts.Certificates.Application.DTOs;
using BitCrafts.Certificates.Application.Interfaces;
using BitCrafts.Certificates.Domain.Interfaces;
using BitCrafts.Certificates.Domain.ValueObjects;

namespace BitCrafts.Certificates.Application.Services;

public sealed class DeploymentApplicationService : IDeploymentApplicationService
{
    private readonly ICertificateRepository _repository;
    private readonly ICertificateStorage _storage;
    private readonly IDeploymentService _deploymentService;
    private readonly ILogger<DeploymentApplicationService> _logger;

    public DeploymentApplicationService(
        ICertificateRepository repository,
        ICertificateStorage storage,
        IDeploymentService deploymentService,
        ILogger<DeploymentApplicationService> logger)
    {
        _repository = repository;
        _storage = storage;
        _deploymentService = deploymentService;
        _logger = logger;
    }

    public async Task<DeploymentResultDto> DeployCertificateAsync(DeploymentRequestDto request, CancellationToken ct = default)
    {
        try
        {
            var cert = await _repository.GetByIdAsync(request.CertificateId, ct);
            if (cert == null)
            {
                return new DeploymentResultDto
                {
                    Success = false,
                    Error = $"Certificate {request.CertificateId} not found"
                };
            }

            if (string.IsNullOrEmpty(cert.CertPath) || string.IsNullOrEmpty(cert.KeyPath))
            {
                return new DeploymentResultDto
                {
                    Success = false,
                    Error = "Certificate or key path is missing"
                };
            }

            var target = MapToDeploymentTarget(request.DeploymentTarget);
            var success = await _deploymentService.DeployAsync(target, cert.CertPath, cert.KeyPath, ct);

            if (success)
            {
                _logger.LogInformation("Successfully deployed certificate {Id} to {Target}", 
                    request.CertificateId, request.DeploymentTarget.Target);
                
                return new DeploymentResultDto
                {
                    Success = true,
                    Message = $"Certificate deployed successfully to {request.DeploymentTarget.Target}"
                };
            }

            return new DeploymentResultDto
            {
                Success = false,
                Error = "Deployment failed"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deploy certificate {Id}", request.CertificateId);
            return new DeploymentResultDto
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    public async Task<DeploymentResultDto> TestConnectionAsync(DeploymentDto targetDto, CancellationToken ct = default)
    {
        try
        {
            var target = MapToDeploymentTarget(targetDto);
            var success = await _deploymentService.TestConnectionAsync(target, ct);

            return new DeploymentResultDto
            {
                Success = success,
                Message = success ? "Connection test successful" : "Connection test failed"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Connection test failed for {Target}", targetDto.Target);
            return new DeploymentResultDto
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    private static DeploymentTarget MapToDeploymentTarget(DeploymentDto dto)
    {
        return new DeploymentTarget
        {
            Type = Enum.Parse<DeploymentType>(dto.Type, ignoreCase: true),
            Target = dto.Target,
            Username = dto.Username,
            PrivateKeyPath = dto.PrivateKeyPath,
            Port = dto.Port,
            DestinationPath = dto.DestinationPath
        };
    }
}
