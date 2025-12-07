// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (C) 2025 BitCrafts

namespace BitCrafts.Certificates.Abstractions.Results;

/// <summary>
/// Result of a deployment operation.
/// </summary>
public class DeploymentResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public List<string> TargetResults { get; set; } = new();
    public List<string> Errors { get; set; } = new();

    public static DeploymentResult SuccessResult(string message) =>
        new() { Success = true, Message = message };

    public static DeploymentResult FailureResult(string message, List<string>? errors = null) =>
        new() { Success = false, Message = message, Errors = errors ?? new() };
}
