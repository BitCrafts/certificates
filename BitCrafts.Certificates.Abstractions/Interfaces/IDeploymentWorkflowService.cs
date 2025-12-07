// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (C) 2025 BitCrafts

using BitCrafts.Certificates.Abstractions.Models;
using BitCrafts.Certificates.Abstractions.Results;

namespace BitCrafts.Certificates.Abstractions.Interfaces;

/// <summary>
/// Service for managing deployment workflows.
/// </summary>
public interface IDeploymentWorkflowService
{
    /// <summary>
    /// Executes a deployment workflow.
    /// </summary>
    Task<DeploymentResult> ExecuteWorkflowAsync(DeploymentWorkflow workflow, Certificate certificate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tests connectivity to all targets in a workflow.
    /// </summary>
    Task<DeploymentResult> TestConnectivityAsync(DeploymentWorkflow workflow, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a workflow configuration.
    /// </summary>
    Task<ValidationResult> ValidateWorkflowAsync(DeploymentWorkflow workflow, CancellationToken cancellationToken = default);
}
