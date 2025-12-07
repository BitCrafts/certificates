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
/// REST API controller for certificate management
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class CertificatesApiController : ControllerBase
{
    private readonly ICertificateApplicationService _certificateService;
    private readonly ILogger<CertificatesApiController> _logger;

    public CertificatesApiController(
        ICertificateApplicationService certificateService,
        ILogger<CertificatesApiController> logger)
    {
        _certificateService = certificateService;
        _logger = logger;
    }

    /// <summary>
    /// Get all certificates
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<CertificateDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<CertificateDto>>> GetAll(CancellationToken ct)
    {
        var certificates = await _certificateService.GetAllCertificatesAsync(ct);
        return Ok(certificates);
    }

    /// <summary>
    /// Get certificates by kind (server or client)
    /// </summary>
    [HttpGet("kind/{kind}")]
    [ProducesResponseType(typeof(IEnumerable<CertificateDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IEnumerable<CertificateDto>>> GetByKind(string kind, CancellationToken ct)
    {
        if (kind != "server" && kind != "client")
        {
            return BadRequest(new { error = "Kind must be 'server' or 'client'" });
        }

        var certificates = await _certificateService.GetCertificatesByKindAsync(kind, ct);
        return Ok(certificates);
    }

    /// <summary>
    /// Get a specific certificate by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(CertificateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CertificateDto>> GetById(long id, CancellationToken ct)
    {
        var certificate = await _certificateService.GetCertificateAsync(id, ct);
        if (certificate == null)
        {
            return NotFound(new { error = $"Certificate {id} not found" });
        }

        return Ok(certificate);
    }

    /// <summary>
    /// Create a new server certificate
    /// </summary>
    [HttpPost("server")]
    [ProducesResponseType(typeof(CertificateDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CertificateDto>> CreateServer(
        [FromBody] CreateServerCertificateDto request, 
        CancellationToken ct)
    {
        try
        {
            var certificate = await _certificateService.CreateServerCertificateAsync(request, ct);
            return CreatedAtAction(nameof(GetById), new { id = certificate.Id }, certificate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create server certificate");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Create a new client certificate
    /// </summary>
    [HttpPost("client")]
    [ProducesResponseType(typeof(CertificateDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CertificateDto>> CreateClient(
        [FromBody] CreateClientCertificateDto request, 
        CancellationToken ct)
    {
        try
        {
            var certificate = await _certificateService.CreateClientCertificateAsync(request, ct);
            return CreatedAtAction(nameof(GetById), new { id = certificate.Id }, certificate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create client certificate");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Revoke a certificate
    /// </summary>
    [HttpPost("{id}/revoke")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Revoke(long id, [FromBody] RevokeCertificateDto? request, CancellationToken ct)
    {
        request ??= new RevokeCertificateDto { CertificateId = id };
        request.CertificateId = id;

        var success = await _certificateService.RevokeCertificateAsync(request, ct);
        if (!success)
        {
            return NotFound(new { error = $"Certificate {id} not found" });
        }

        return Ok(new { message = $"Certificate {id} revoked successfully" });
    }

    /// <summary>
    /// Delete a certificate
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        var success = await _certificateService.DeleteCertificateAsync(id, ct);
        if (!success)
        {
            return NotFound(new { error = $"Certificate {id} not found" });
        }

        return NoContent();
    }

    /// <summary>
    /// Download certificate archive (tar.gz with cert and key)
    /// </summary>
    [HttpGet("{id}/download")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Download(long id, CancellationToken ct)
    {
        try
        {
            var archive = await _certificateService.DownloadCertificateArchiveAsync(id, ct);
            return File(archive, "application/gzip", $"certificate-{id}.tar.gz");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to download certificate {Id}", id);
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading certificate {Id}", id);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }
}
