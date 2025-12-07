// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (C) 2025 BitCrafts

using BitCrafts.Certificates.Abstractions.Interfaces;
using BitCrafts.Certificates.Abstractions.Models;
using BitCrafts.Certificates.Abstractions.Results;

namespace BitCrafts.Certificates.Linux.Services;

/// <summary>
/// Manages deployment workflows for certificates on Linux.
/// </summary>
public class DeploymentWorkflowServiceLinux : IDeploymentWorkflowService
{
    private readonly ICertificateRepository _repository;
    private readonly IEncryptionService _encryptionService;
    private readonly ISshClientFactory _sshClientFactory;
    private readonly IFileSystemDeployer _fileSystemDeployer;
    private readonly ITargetResolver _targetResolver;

    public DeploymentWorkflowServiceLinux(
        ICertificateRepository repository,
        IEncryptionService encryptionService,
        ISshClientFactory sshClientFactory,
        IFileSystemDeployer fileSystemDeployer,
        ITargetResolver targetResolver)
    {
        _repository = repository;
        _encryptionService = encryptionService;
        _sshClientFactory = sshClientFactory;
        _fileSystemDeployer = fileSystemDeployer;
        _targetResolver = targetResolver;
    }

    public async Task<DeploymentResult> ExecuteWorkflowAsync(
        DeploymentWorkflow workflow,
        Certificate certificate,
        CancellationToken cancellationToken = default)
    {
        // NOTE: The certificate parameter should contain DECRYPTED data, not encrypted
        // The caller is responsible for decrypting certificate.EncryptedData before calling this method
        // This service will deploy the decrypted certificate data to targets

        var results = new List<string>();
        var errors = new List<string>();

        foreach (var target in workflow.Targets)
        {
            try
            {
                var result = workflow.Type switch
                {
                    DeploymentWorkflowType.SSH => await ExecuteSshDeploymentAsync(certificate, target, cancellationToken),
                    DeploymentWorkflowType.FileSystem => await ExecuteFileSystemDeploymentAsync(certificate, target, cancellationToken),
                    _ => DeploymentResult.FailureResult($"Unsupported workflow type: {workflow.Type}")
                };

                if (result.Success)
                {
                    results.Add($"{target.HostnameOrIp}: {result.Message}");
                }
                else
                {
                    errors.Add($"{target.HostnameOrIp}: {result.Message}");
                }
            }
            catch (Exception ex)
            {
                errors.Add($"{target.HostnameOrIp}: {ex.Message}");
            }
        }

        var success = errors.Count == 0;
        var message = success
            ? $"Workflow '{workflow.Name}' completed successfully"
            : $"Workflow '{workflow.Name}' completed with {errors.Count} error(s)";

        return new DeploymentResult
        {
            Success = success,
            Message = message,
            TargetResults = results,
            Errors = errors
        };
    }

    public async Task<DeploymentResult> TestConnectivityAsync(
        DeploymentWorkflow workflow,
        CancellationToken cancellationToken = default)
    {
        var results = new List<string>();
        var errors = new List<string>();

        foreach (var target in workflow.Targets)
        {
            try
            {
                var isReachable = await _targetResolver.ValidateConnectivityAsync(target, cancellationToken);
                if (isReachable)
                {
                    results.Add($"{target.HostnameOrIp}: Connection successful");
                }
                else
                {
                    errors.Add($"{target.HostnameOrIp}: Connection failed");
                }
            }
            catch (Exception ex)
            {
                errors.Add($"{target.HostnameOrIp}: {ex.Message}");
            }
        }

        var success = errors.Count == 0;
        var message = success
            ? "All targets are reachable"
            : $"{errors.Count} target(s) failed connectivity test";

        return new DeploymentResult
        {
            Success = success,
            Message = message,
            TargetResults = results,
            Errors = errors
        };
    }

    public Task<ValidationResult> ValidateWorkflowAsync(
        DeploymentWorkflow workflow,
        CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(workflow.Name))
            errors.Add("Workflow name is required");

        if (workflow.Targets == null || workflow.Targets.Count == 0)
            errors.Add("At least one target is required");

        foreach (var target in workflow.Targets ?? Enumerable.Empty<DeploymentTarget>())
        {
            if (string.IsNullOrWhiteSpace(target.HostnameOrIp))
                errors.Add("Target hostname or IP is required");

            if (string.IsNullOrWhiteSpace(target.DestinationPath))
                errors.Add("Target destination path is required");

            if (workflow.Type == DeploymentWorkflowType.SSH)
            {
                if (string.IsNullOrWhiteSpace(target.Username))
                    errors.Add($"Username is required for SSH deployment to {target.HostnameOrIp}");
            }
        }

        return Task.FromResult(errors.Any()
            ? ValidationResult.Invalid(errors.ToArray())
            : ValidationResult.Valid());
    }

    private async Task<DeploymentResult> ExecuteSshDeploymentAsync(
        Certificate certificate,
        DeploymentTarget target,
        CancellationToken cancellationToken)
    {
        using var sshClient = _sshClientFactory.CreateClient(target);
        
        await sshClient.ConnectAsync(cancellationToken);

        if (certificate.EncryptedData == null)
        {
            return DeploymentResult.FailureResult("Certificate has no data to deploy");
        }

        var fileName = $"{certificate.Metadata.CommonName.Replace("*", "wildcard")}.pem";
        var remotePath = $"{target.DestinationPath}/{fileName}";

        await sshClient.UploadFileAsync(certificate.EncryptedData, remotePath, cancellationToken);

        if (!string.IsNullOrEmpty(target.Permissions))
        {
            await sshClient.SetPermissionsAsync(remotePath, target.Permissions, cancellationToken);
        }

        if (!string.IsNullOrEmpty(target.Owner))
        {
            await sshClient.SetOwnershipAsync(remotePath, target.Owner, target.Group, cancellationToken);
        }

        return DeploymentResult.SuccessResult($"Certificate deployed to {remotePath}");
    }

    private async Task<DeploymentResult> ExecuteFileSystemDeploymentAsync(
        Certificate certificate,
        DeploymentTarget target,
        CancellationToken cancellationToken)
    {
        return await _fileSystemDeployer.DeployAsync(certificate, target, cancellationToken);
    }
}
