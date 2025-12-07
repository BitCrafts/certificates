// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (C) 2025 BitCrafts

using BitCrafts.Certificates.Abstractions.Models;
using BitCrafts.Certificates.Linux.Services;
using FluentAssertions;
using Xunit;

namespace BitCrafts.Certificates.Linux.Tests.Services;

public class CertificateServiceLinuxTests
{
    [Fact]
    public async Task ValidateMetadataAsync_ShouldReturnValid_WhenMetadataIsCorrect()
    {
        // Arrange
        var service = new CertificateServiceLinux();
        var metadata = new CertificateMetadata
        {
            CommonName = "test.example.com",
            Type = CertificateType.Server,
            ValidityDays = 365
        };

        // Act
        var result = await service.ValidateMetadataAsync(metadata);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateMetadataAsync_ShouldReturnInvalid_WhenCommonNameIsEmpty()
    {
        // Arrange
        var service = new CertificateServiceLinux();
        var metadata = new CertificateMetadata
        {
            CommonName = "",
            Type = CertificateType.Server,
            ValidityDays = 365
        };

        // Act
        var result = await service.ValidateMetadataAsync(metadata);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Common name"));
    }

    [Fact]
    public async Task ValidateMetadataAsync_ShouldReturnInvalid_WhenValidityDaysIsZero()
    {
        // Arrange
        var service = new CertificateServiceLinux();
        var metadata = new CertificateMetadata
        {
            CommonName = "test.example.com",
            Type = CertificateType.Server,
            ValidityDays = 0
        };

        // Act
        var result = await service.ValidateMetadataAsync(metadata);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Validity days"));
    }

    [Fact]
    public async Task ValidateMetadataAsync_ShouldReturnInvalid_WhenValidityDaysExceedsMaximum()
    {
        // Arrange
        var service = new CertificateServiceLinux();
        var metadata = new CertificateMetadata
        {
            CommonName = "test.example.com",
            Type = CertificateType.Server,
            ValidityDays = 4000
        };

        // Act
        var result = await service.ValidateMetadataAsync(metadata);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("cannot exceed"));
    }
}
