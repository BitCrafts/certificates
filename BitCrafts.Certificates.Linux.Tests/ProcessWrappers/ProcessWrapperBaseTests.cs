// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (C) 2025 BitCrafts

using BitCrafts.Certificates.Abstractions.Models;
using BitCrafts.Certificates.Linux.ProcessWrappers;
using FluentAssertions;
using Xunit;

namespace BitCrafts.Certificates.Linux.Tests.ProcessWrappers;

public class ProcessWrapperBaseTests
{
    private class TestProcessWrapper : ProcessWrapperBase
    {
        public string TestEscapePath(string path) => EscapePath(path);
    }

    [Fact]
    public void EscapePath_ShouldNormalizePath()
    {
        // Arrange
        var wrapper = new TestProcessWrapper();
        var path = "/tmp/../tmp/test";

        // Act
        var result = wrapper.TestEscapePath(path);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Be("/tmp/test");
    }

    [Fact]
    public void EscapePath_ShouldThrow_WhenPathIsEmpty()
    {
        // Arrange
        var wrapper = new TestProcessWrapper();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => wrapper.TestEscapePath(""));
    }
}
