// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (C) 2025 BitCrafts

using BitCrafts.Certificates.Linux.Services;
using FluentAssertions;
using Xunit;

namespace BitCrafts.Certificates.Linux.Tests.Services;

public class TargetResolverLinuxTests
{
    [Fact]
    public async Task ResolveHostnameAsync_ShouldReturnIpAddress_WhenHostnameIsValid()
    {
        // Arrange
        var resolver = new TargetResolverLinux();

        // Act
        var result = await resolver.ResolveHostnameAsync("localhost");

        // Assert
        result.Should().NotBeEmpty();
        result.Should().Contain(ip => ip == "127.0.0.1" || ip == "::1");
    }

    [Fact]
    public async Task ResolveHostnameAsync_ShouldReturnEmpty_WhenHostnameIsInvalid()
    {
        // Arrange
        var resolver = new TargetResolverLinux();

        // Act
        var result = await resolver.ResolveHostnameAsync("invalid.hostname.that.does.not.exist.example.com");

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task IsReachableAsync_ShouldReturnTrue_ForLocalhost()
    {
        // Arrange
        var resolver = new TargetResolverLinux();

        // Act
        var result = await resolver.IsReachableAsync("127.0.0.1");

        // Assert
        result.Should().BeTrue();
    }
}
