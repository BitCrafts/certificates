// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (C) 2025 Younes Benmoussa <benzsoftware@pm.me>
// 
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.
// You should have received a copy of the GNU Affero General Public License along with this program. If not, see <https://www.gnu.org/licenses/>. 

using BitCrafts.Certificates.Domain.Interfaces;
using BitCrafts.Certificates.Domain.ValueObjects;

namespace BitCrafts.Certificates.Infrastructure.Deployment;

/// <summary>
/// Composite deployment service that routes to appropriate implementation
/// </summary>
public sealed class CompositeDeploymentService : IDeploymentService
{
    private readonly SshDeploymentService _sshService;
    private readonly NetworkFileSystemDeploymentService _nfsService;
    private readonly ILogger<CompositeDeploymentService> _logger;

    public CompositeDeploymentService(
        SshDeploymentService sshService,
        NetworkFileSystemDeploymentService nfsService,
        ILogger<CompositeDeploymentService> logger)
    {
        _sshService = sshService;
        _nfsService = nfsService;
        _logger = logger;
    }

    public Task<bool> DeployAsync(DeploymentTarget target, string certificatePath, string keyPath, CancellationToken ct = default)
    {
        return target.Type switch
        {
            DeploymentType.SSH => _sshService.DeployAsync(target, certificatePath, keyPath, ct),
            DeploymentType.NetworkFileSystem => _nfsService.DeployAsync(target, certificatePath, keyPath, ct),
            _ => throw new NotSupportedException($"Deployment type {target.Type} is not supported")
        };
    }

    public Task<bool> TestConnectionAsync(DeploymentTarget target, CancellationToken ct = default)
    {
        return target.Type switch
        {
            DeploymentType.SSH => _sshService.TestConnectionAsync(target, ct),
            DeploymentType.NetworkFileSystem => _nfsService.TestConnectionAsync(target, ct),
            _ => throw new NotSupportedException($"Deployment type {target.Type} is not supported")
        };
    }
}
