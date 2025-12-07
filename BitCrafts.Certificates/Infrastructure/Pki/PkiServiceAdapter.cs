// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (C) 2025 Younes Benmoussa <benzsoftware@pm.me>
// 
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.
// You should have received a copy of the GNU Affero General Public License along with this program. If not, see <https://www.gnu.org/licenses/>. 

using BitCrafts.Certificates.Domain.Entities;
using BitCrafts.Certificates.Domain.Interfaces;
using BitCrafts.Certificates.Pki;
using BitCrafts.Certificates.Data.Repositories;

namespace BitCrafts.Certificates.Infrastructure.Pki;

/// <summary>
/// PKI service adapter that bridges domain IPkiService with existing services
/// </summary>
public sealed class PkiServiceAdapter : IPkiService
{
    private readonly ILeafCertificateService _leafService;
    private readonly ICaService _caService;
    private readonly ICertificatesRepository _certRepo;
    private readonly ILogger<PkiServiceAdapter> _logger;

    public PkiServiceAdapter(
        ILeafCertificateService leafService,
        ICaService caService,
        ICertificatesRepository certRepo,
        ILogger<PkiServiceAdapter> logger)
    {
        _leafService = leafService;
        _caService = caService;
        _certRepo = certRepo;
        _logger = logger;
    }

    public async Task<Certificate> IssueServerCertificateAsync(string fqdn, string[]? ipAddresses = null, string[]? dnsNames = null, CancellationToken ct = default)
    {
        var id = await _leafService.IssueServerAsync(fqdn, ipAddresses ?? Array.Empty<string>(), dnsNames ?? Array.Empty<string>());
        var record = await _certRepo.GetAsync(id);
        
        if (record == null)
        {
            throw new InvalidOperationException($"Certificate {id} was issued but could not be retrieved");
        }

        return new Certificate
        {
            Id = record.Id,
            Kind = record.Kind,
            Subject = record.Subject,
            SanDns = record.SanDns,
            SanEmail = null, // Not in CertificateRecord
            SanIps = record.SanIp,
            SerialNumber = string.Empty, // Would need to extract from cert if needed
            Thumbprint = string.Empty, // Would need to extract from cert if needed
            NotBefore = DateTimeOffset.Parse(record.NotBefore),
            NotAfter = DateTimeOffset.Parse(record.NotAfter),
            IssuedAt = DateTimeOffset.Parse(record.CreatedAt),
            Status = record.Status,
            CertPath = record.CertPath,
            KeyPath = record.KeyPath
        };
    }

    public async Task<Certificate> IssueClientCertificateAsync(string username, string? email = null, CancellationToken ct = default)
    {
        var id = await _leafService.IssueClientAsync(username, email);
        var record = await _certRepo.GetAsync(id);
        
        if (record == null)
        {
            throw new InvalidOperationException($"Certificate {id} was issued but could not be retrieved");
        }

        return new Certificate
        {
            Id = record.Id,
            Kind = record.Kind,
            Subject = record.Subject,
            SanDns = record.SanDns,
            SanEmail = null, // Not in CertificateRecord
            SanIps = record.SanIp,
            SerialNumber = string.Empty, // Would need to extract from cert if needed
            Thumbprint = string.Empty, // Would need to extract from cert if needed
            NotBefore = DateTimeOffset.Parse(record.NotBefore),
            NotAfter = DateTimeOffset.Parse(record.NotAfter),
            IssuedAt = DateTimeOffset.Parse(record.CreatedAt),
            Status = record.Status,
            CertPath = record.CertPath,
            KeyPath = record.KeyPath
        };
    }

    public async Task EnsureRootCAAsync(string domain, CancellationToken ct = default)
    {
        await _caService.CreateRootCaIfMissingAsync(domain, ct);
    }

    public Task<RootCA> GetRootCAAsync(CancellationToken ct = default)
    {
        // This is a simplified implementation - in a real scenario you'd want to read metadata
        var rootCa = new RootCA
        {
            Domain = "unknown", // Would need to fetch from settings
            CertificatePath = _caService.RootCertPath,
            PrivateKeyPath = _caService.RootKeyPath,
            CreatedAt = DateTimeOffset.UtcNow, // Would need to fetch actual creation time
            IsInitialized = File.Exists(_caService.RootCertPath) && File.Exists(_caService.RootKeyPath)
        };
        
        return Task.FromResult(rootCa);
    }
}
