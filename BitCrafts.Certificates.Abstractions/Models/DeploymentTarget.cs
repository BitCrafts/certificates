// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (C) 2025 BitCrafts

namespace BitCrafts.Certificates.Abstractions.Models;

/// <summary>
/// Represents a deployment target with its configuration.
/// </summary>
public class DeploymentTarget
{
    public required string HostnameOrIp { get; set; }
    public required string DestinationPath { get; set; }
    public string? Owner { get; set; }
    public string? Group { get; set; }
    public string? Permissions { get; set; }
    public int? Port { get; set; }
    public string? Username { get; set; }
    public string? PrivateKeyPath { get; set; }
}
