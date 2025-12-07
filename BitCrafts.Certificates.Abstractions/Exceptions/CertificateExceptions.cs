// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (C) 2025 BitCrafts

namespace BitCrafts.Certificates.Abstractions.Exceptions;

/// <summary>
/// Base exception for certificate-related errors.
/// </summary>
public class CertificateException : Exception
{
    public CertificateException(string message) : base(message) { }
    public CertificateException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Exception thrown when certificate creation fails.
/// </summary>
public class CertificateCreationException : CertificateException
{
    public CertificateCreationException(string message) : base(message) { }
    public CertificateCreationException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Exception thrown when encryption/decryption fails.
/// </summary>
public class EncryptionException : CertificateException
{
    public EncryptionException(string message) : base(message) { }
    public EncryptionException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Exception thrown when deployment fails.
/// </summary>
public class DeploymentException : CertificateException
{
    public DeploymentException(string message) : base(message) { }
    public DeploymentException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Exception thrown when validation fails.
/// </summary>
public class ValidationException : CertificateException
{
    public ValidationException(string message) : base(message) { }
    public ValidationException(string message, Exception innerException) : base(message, innerException) { }
}
