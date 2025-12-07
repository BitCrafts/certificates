// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (C) 2025 BitCrafts

namespace BitCrafts.Certificates.Abstractions.Models;

/// <summary>
/// Represents a deployment workflow configuration.
/// </summary>
public class DeploymentWorkflow
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public DeploymentWorkflowType Type { get; set; }
    public required List<DeploymentTarget> Targets { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastExecutedAt { get; set; }
}

public enum DeploymentWorkflowType
{
    SSH,
    FileSystem
}
