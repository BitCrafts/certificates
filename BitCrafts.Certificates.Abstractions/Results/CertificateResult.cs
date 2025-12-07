// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (C) 2025 BitCrafts

using BitCrafts.Certificates.Abstractions.Models;

namespace BitCrafts.Certificates.Abstractions.Results;

/// <summary>
/// Result of a certificate operation.
/// </summary>
public class CertificateResult
{
    public bool Success { get; set; }
    public Certificate? Certificate { get; set; }
    public string? ErrorMessage { get; set; }
    public List<string> Warnings { get; set; } = new();

    public static CertificateResult SuccessResult(Certificate certificate) =>
        new() { Success = true, Certificate = certificate };

    public static CertificateResult FailureResult(string errorMessage) =>
        new() { Success = false, ErrorMessage = errorMessage };
}
