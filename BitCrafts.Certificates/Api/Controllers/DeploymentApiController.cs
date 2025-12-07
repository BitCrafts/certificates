// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (C) 2025 Younes Benmoussa <benzsoftware@pm.me>
// 
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.
// You should have received a copy of the GNU Affero General Public License along with this program. If not, see <https://www.gnu.org/licenses/>. 

using BitCrafts.Certificates.Application.DTOs;
using BitCrafts.Certificates.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BitCrafts.Certificates.Api.Controllers;

/// <summary>
/// REST API controller for certificate deployment
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class DeploymentApiController : ControllerBase
{
    private readonly IDeploymentApplicationService _deploymentService;
    private readonly ILogger<DeploymentApiController> _logger;

    public DeploymentApiController(
        IDeploymentApplicationService deploymentService,
        ILogger<DeploymentApiController> logger)
    {
        _deploymentService = deploymentService;
        _logger = logger;
    }

    /// <summary>
    /// Deploy a certificate to a target (SSH or network filesystem)
    /// </summary>
    [HttpPost("deploy")]
    [ProducesResponseType(typeof(DeploymentResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DeploymentResultDto>> Deploy(
        [FromBody] DeploymentRequestDto request, 
        CancellationToken ct)
    {
        try
        {
            var result = await _deploymentService.DeployCertificateAsync(request, ct);
            
            if (result.Success)
            {
                return Ok(result);
            }

            return BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Deployment failed");
            return BadRequest(new DeploymentResultDto
            {
                Success = false,
                Error = ex.Message
            });
        }
    }

    /// <summary>
    /// Test connectivity to a deployment target
    /// </summary>
    [HttpPost("test")]
    [ProducesResponseType(typeof(DeploymentResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DeploymentResultDto>> TestConnection(
        [FromBody] DeploymentDto target, 
        CancellationToken ct)
    {
        try
        {
            var result = await _deploymentService.TestConnectionAsync(target, ct);
            
            if (result.Success)
            {
                return Ok(result);
            }

            return BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Connection test failed");
            return BadRequest(new DeploymentResultDto
            {
                Success = false,
                Error = ex.Message
            });
        }
    }
}
