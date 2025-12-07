// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (C) 2025 BitCrafts

using BitCrafts.Certificates.Abstractions.Models;
using BitCrafts.Certificates.Abstractions.Results;
using FluentAssertions;
using Xunit;

namespace BitCrafts.Certificates.Abstractions.Tests.Results;

public class CertificateResultTests
{
    [Fact]
    public void SuccessResult_ShouldReturnSuccessfulResult()
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
            CreatedAt = DateTime.UtcNow
        };

        // Act
        var result = CertificateResult.SuccessResult(certificate);

        // Assert
        result.Success.Should().BeTrue();
        result.Certificate.Should().Be(certificate);
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void FailureResult_ShouldReturnFailedResult()
    {
        // Arrange
        var errorMessage = "Certificate creation failed";

        // Act
        var result = CertificateResult.FailureResult(errorMessage);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be(errorMessage);
        result.Certificate.Should().BeNull();
    }
}
