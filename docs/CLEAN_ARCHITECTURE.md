# Clean Architecture Implementation

## Overview

This document describes the clean architecture refactoring that has been applied to the BitCrafts Certificates application.

## Architecture Layers

### 1. Domain Layer (`Domain/`)
The innermost layer containing business entities and interfaces (ports).

**Entities:**
- `Certificate` - Domain entity representing a certificate
- `RootCA` - Domain entity representing the Root Certificate Authority

**Value Objects:**
- `DeploymentTarget` - Represents deployment configuration (SSH or Network Filesystem)

**Interfaces (Ports):**
- `ICertificateRepository` - Repository interface for certificate persistence
- `ICertificateStorage` - Storage interface for certificate files
- `IDeploymentService` - Deployment service interface
- `IPkiService` - PKI operations interface

### 2. Application Layer (`Application/`)
Contains application business logic and use cases.

**DTOs (Data Transfer Objects):**
- `CertificateDto` - Certificate data transfer object
- `CreateServerCertificateDto` - Request DTO for creating server certificates
- `CreateClientCertificateDto` - Request DTO for creating client certificates
- `RevokeCertificateDto` - Request DTO for revoking certificates
- `DeploymentDto` - Deployment configuration DTO
- `DeploymentRequestDto` - Request DTO for deployment
- `DeploymentResultDto` - Result DTO for deployment operations

**Application Services:**
- `CertificateApplicationService` - Orchestrates certificate operations
- `DeploymentApplicationService` - Orchestrates deployment operations

### 3. Infrastructure Layer (`Infrastructure/`)
Contains implementations of domain interfaces.

**Database:**
- `CertificateRepositoryAdapter` - Bridges domain interface with existing repository

**Storage:**
- `LocalFileSystemStorage` - Local filesystem implementation for certificate storage

**PKI:**
- `PkiServiceAdapter` - Bridges domain interface with existing PKI services

**Deployment:**
- `SshDeploymentService` - SSH-based deployment implementation
- `NetworkFileSystemDeploymentService` - Network filesystem deployment implementation
- `CompositeDeploymentService` - Routes to appropriate deployment service

### 4. API Layer (`Api/Controllers/`)
REST API controllers for programmatic access.

**Controllers:**
- `CertificatesApiController` - REST API for certificate management
- `DeploymentApiController` - REST API for deployment operations

## Key Design Patterns

### 1. Repository Pattern
Abstracts data access through interfaces, allowing different storage implementations.

### 2. Adapter Pattern
Adapters bridge between the clean architecture interfaces and existing legacy code.

### 3. Strategy Pattern
Multiple deployment strategies (SSH, Network Filesystem) can be selected at runtime.

### 4. Dependency Inversion
High-level modules depend on abstractions, not concrete implementations.

### 5. Composite Pattern
`CompositeDeploymentService` routes to appropriate deployment implementation.

## Benefits

1. **Separation of Concerns**: Business logic separated from infrastructure
2. **Testability**: Each layer can be tested independently
3. **Flexibility**: Easy to swap implementations (e.g., database, storage)
4. **Maintainability**: Clear structure makes code easier to understand and modify
5. **Extensibility**: New features can be added without modifying existing code

## API Endpoints

### Certificate Management

- `GET /api/CertificatesApi` - Get all certificates
- `GET /api/CertificatesApi/kind/{kind}` - Get certificates by kind (server/client)
- `GET /api/CertificatesApi/{id}` - Get specific certificate
- `POST /api/CertificatesApi/server` - Create server certificate
- `POST /api/CertificatesApi/client` - Create client certificate
- `POST /api/CertificatesApi/{id}/revoke` - Revoke certificate
- `DELETE /api/CertificatesApi/{id}` - Delete certificate
- `GET /api/CertificatesApi/{id}/download` - Download certificate archive

### Deployment

- `POST /api/DeploymentApi/deploy` - Deploy certificate to target
- `POST /api/DeploymentApi/test` - Test deployment connection

## Swagger/OpenAPI

API documentation is available at `/swagger` in development mode.

## Future Enhancements

1. **Additional Database Support**: PostgreSQL, MySQL, SQL Server adapters
2. **Cloud Storage**: S3, Azure Blob Storage adapters
3. **Additional Deployment Methods**: Ansible, Kubernetes, Docker
4. **Authentication**: API key or JWT-based authentication
5. **CQRS**: Command Query Responsibility Segregation for complex operations
