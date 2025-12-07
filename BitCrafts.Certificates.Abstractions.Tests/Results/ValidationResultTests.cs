// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (C) 2025 BitCrafts

using BitCrafts.Certificates.Abstractions.Results;
using FluentAssertions;
using Xunit;

namespace BitCrafts.Certificates.Abstractions.Tests.Results;

public class ValidationResultTests
{
    [Fact]
    public void Valid_ShouldReturnValidResult()
    {
        // Act
        var result = ValidationResult.Valid();

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
        result.Warnings.Should().BeEmpty();
    }

    [Fact]
    public void Invalid_ShouldReturnInvalidResultWithErrors()
    {
        // Arrange
        var errors = new[] { "Error 1", "Error 2" };

        // Act
        var result = ValidationResult.Invalid(errors);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().BeEquivalentTo(errors);
    }

    [Fact]
    public void ValidationResult_ShouldSupportWarnings()
    {
        // Arrange
        var result = ValidationResult.Valid();
        result.Warnings.Add("Warning 1");

        // Assert
        result.IsValid.Should().BeTrue();
        result.Warnings.Should().Contain("Warning 1");
    }
}
