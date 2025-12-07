// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (C) 2025 BitCrafts

using BitCrafts.Certificates.Abstractions.Models;
using FluentAssertions;
using Xunit;

namespace BitCrafts.Certificates.Abstractions.Tests.Models;

public class CertificateTests
{
    [Fact]
    public void Certificate_ShouldBeRevoked_WhenRevokedAtIsSet()
    {
        // Arrange
        var certificate = new Certificate
        {
            Id = 1,
            Metadata = new CertificateMetadata
            {
                CommonName = "test.example.com",
                Type = CertificateType.Server,
                ValidityDays = 365
            },
            CreatedAt = DateTime.UtcNow,
            RevokedAt = DateTime.UtcNow
        };

        // Act & Assert
        certificate.IsRevoked.Should().BeTrue();
    }

    [Fact]
    public void Certificate_ShouldNotBeRevoked_WhenRevokedAtIsNull()
    {
        // Arrange
        var certificate = new Certificate
        {
            Id = 1,
            Metadata = new CertificateMetadata
            {
                CommonName = "test.example.com",
                Type = CertificateType.Server,
                ValidityDays = 365
            },
            CreatedAt = DateTime.UtcNow,
            RevokedAt = null
        };

        // Act & Assert
        certificate.IsRevoked.Should().BeFalse();
    }
}
