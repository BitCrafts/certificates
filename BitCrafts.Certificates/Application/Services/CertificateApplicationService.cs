// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (C) 2025 Younes Benmoussa <benzsoftware@pm.me>
// 
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.
// You should have received a copy of the GNU Affero General Public License along with this program. If not, see <https://www.gnu.org/licenses/>. 

using BitCrafts.Certificates.Application.DTOs;
using BitCrafts.Certificates.Application.Interfaces;
using BitCrafts.Certificates.Domain.Entities;
using BitCrafts.Certificates.Domain.Interfaces;
using BitCrafts.Certificates.Helpers;
using BitCrafts.Certificates.Services;

namespace BitCrafts.Certificates.Application.Services;

public sealed class CertificateApplicationService : ICertificateApplicationService
{
    private readonly ICertificateRepository _repository;
    private readonly IPkiService _pkiService;
    private readonly ICertificateStorage _storage;
    private readonly IAuditLogger _auditLogger;
    private readonly ILogger<CertificateApplicationService> _logger;

    public CertificateApplicationService(
        ICertificateRepository repository,
        IPkiService pkiService,
        ICertificateStorage storage,
        IAuditLogger auditLogger,
        ILogger<CertificateApplicationService> logger)
    {
        _repository = repository;
        _pkiService = pkiService;
        _storage = storage;
        _auditLogger = auditLogger;
        _logger = logger;
    }

    public async Task<CertificateDto> CreateServerCertificateAsync(CreateServerCertificateDto dto, CancellationToken ct = default)
    {
        _logger.LogInformation("Creating server certificate for {Fqdn}", dto.Fqdn);
        
        var cert = await _pkiService.IssueServerCertificateAsync(dto.Fqdn, dto.IpAddresses, dto.DnsNames, ct);
        var id = await _repository.AddAsync(cert, ct);
        cert.Id = id;
        
        _auditLogger.LogAsync("create_server_cert", "server", dto.Fqdn, id);
        
        return MapToDto(cert);
    }

    public async Task<CertificateDto> CreateClientCertificateAsync(CreateClientCertificateDto dto, CancellationToken ct = default)
    {
        _logger.LogInformation("Creating client certificate for {Username}", dto.Username);
        
        var cert = await _pkiService.IssueClientCertificateAsync(dto.Username, dto.Email, ct);
        var id = await _repository.AddAsync(cert, ct);
        cert.Id = id;
        
        _auditLogger.LogAsync("create_client_cert", "client", dto.Username, id);
        
        return MapToDto(cert);
    }

    public async Task<CertificateDto?> GetCertificateAsync(long id, CancellationToken ct = default)
    {
        var cert = await _repository.GetByIdAsync(id, ct);
        return cert != null ? MapToDto(cert) : null;
    }

    public async Task<IReadOnlyList<CertificateDto>> GetAllCertificatesAsync(CancellationToken ct = default)
    {
        var certs = await _repository.GetAllAsync(ct);
        return certs.Select(MapToDto).ToList();
    }

    public async Task<IReadOnlyList<CertificateDto>> GetCertificatesByKindAsync(string kind, CancellationToken ct = default)
    {
        var certs = await _repository.GetByKindAsync(kind, ct);
        return certs.Select(MapToDto).ToList();
    }

    public async Task<bool> RevokeCertificateAsync(RevokeCertificateDto dto, CancellationToken ct = default)
    {
        var cert = await _repository.GetByIdAsync(dto.CertificateId, ct);
        if (cert == null) return false;
        
        cert.Revoke();
        await _repository.UpdateAsync(cert, ct);
        _auditLogger.LogAsync("revoke_cert", cert.Kind, cert.Subject, dto.CertificateId);
        
        _logger.LogInformation("Certificate {Id} revoked", dto.CertificateId);
        return true;
    }

    public async Task<bool> DeleteCertificateAsync(long id, CancellationToken ct = default)
    {
        var cert = await _repository.GetByIdAsync(id, ct);
        if (cert == null) return false;
        
        // Delete files if they exist
        if (!string.IsNullOrEmpty(cert.CertPath) && await _storage.ExistsAsync(cert.CertPath, ct))
        {
            await _storage.DeleteAsync(cert.CertPath, ct);
        }
        if (!string.IsNullOrEmpty(cert.KeyPath) && await _storage.ExistsAsync(cert.KeyPath, ct))
        {
            await _storage.DeleteAsync(cert.KeyPath, ct);
        }
        
        await _repository.DeleteAsync(id, ct);
        _auditLogger.LogAsync("delete_cert", cert.Kind, cert.Subject, id);
        
        _logger.LogInformation("Certificate {Id} deleted", id);
        return true;
    }

    public async Task<byte[]> DownloadCertificateArchiveAsync(long id, CancellationToken ct = default)
    {
        var cert = await _repository.GetByIdAsync(id, ct);
        if (cert == null)
        {
            throw new InvalidOperationException($"Certificate {id} not found");
        }

        if (string.IsNullOrEmpty(cert.CertPath) || string.IsNullOrEmpty(cert.KeyPath))
        {
            throw new InvalidOperationException($"Certificate {id} has missing file paths");
        }

        var certBytes = await _storage.ReadCertificateAsync(cert.CertPath, ct);
        var keyBytes = await _storage.ReadPrivateKeyAsync(cert.KeyPath, ct);

        var baseName = SanitizeFileName(cert.SanDns ?? cert.Subject);
        var files = new List<(string Name, byte[] Content)>
        {
            ($"{baseName}.crt", certBytes),
            ($"{baseName}.key", keyBytes)
        };

        return TarGzHelper.CreateTarGz(files);
    }

    private static CertificateDto MapToDto(Certificate cert)
    {
        return new CertificateDto
        {
            Id = cert.Id,
            Kind = cert.Kind,
            Subject = cert.Subject,
            SanDns = cert.SanDns,
            SanEmail = cert.SanEmail,
            SanIps = cert.SanIps,
            SerialNumber = cert.SerialNumber,
            Thumbprint = cert.Thumbprint,
            NotBefore = cert.NotBefore,
            NotAfter = cert.NotAfter,
            IssuedAt = cert.IssuedAt,
            Status = cert.Status,
            IsRevoked = cert.IsRevoked
        };
    }

    private static string SanitizeFileName(string s)
    {
        foreach (var c in Path.GetInvalidFileNameChars()) s = s.Replace(c, '_');
        return s;
    }
}
