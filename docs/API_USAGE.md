# API Usage Guide

## Overview

The BitCrafts Certificates API provides REST endpoints for managing certificates and deploying them to infrastructure.

## Base URL

- Development: `http://localhost:5000/api`
- Production: `https://your-domain/api`

## Authentication

Currently, the API does not require authentication. For production use, consider implementing:
- API Keys
- JWT tokens
- Mutual TLS authentication

## Endpoints

### Certificate Management

#### Create Server Certificate

```http
POST /api/CertificatesApi/server
Content-Type: application/json

{
  "fqdn": "server.example.com",
  "ipAddresses": ["192.168.1.100", "10.0.0.50"]
}
```

Response:
```json
{
  "id": 1,
  "kind": "server",
  "subject": "CN=server.example.com",
  "sanDns": "server.example.com",
  "sanIps": "192.168.1.100,10.0.0.50",
  "serialNumber": "",
  "thumbprint": "",
  "notBefore": "2025-12-07T12:00:00Z",
  "notAfter": "2026-12-07T12:00:00Z",
  "issuedAt": "2025-12-07T12:00:00Z",
  "status": "active",
  "isRevoked": false
}
```

#### Create Client Certificate

```http
POST /api/CertificatesApi/client
Content-Type: application/json

{
  "username": "john.doe",
  "email": "john.doe@example.com"
}
```

#### Get All Certificates

```http
GET /api/CertificatesApi
```

#### Get Certificates by Kind

```http
GET /api/CertificatesApi/kind/server
GET /api/CertificatesApi/kind/client
```

#### Get Certificate by ID

```http
GET /api/CertificatesApi/123
```

#### Revoke Certificate

```http
POST /api/CertificatesApi/123/revoke
Content-Type: application/json

{
  "reason": "Key compromised"
}
```

#### Delete Certificate

```http
DELETE /api/CertificatesApi/123
```

#### Download Certificate Archive

```http
GET /api/CertificatesApi/123/download
```

Returns a `.tar.gz` archive containing the certificate and private key.

### Deployment

#### Deploy Certificate via SSH

```http
POST /api/DeploymentApi/deploy
Content-Type: application/json

{
  "certificateId": 123,
  "deploymentTarget": {
    "type": "SSH",
    "target": "192.168.1.100",
    "username": "deploy",
    "privateKeyPath": "/path/to/ssh/key",
    "port": 22,
    "destinationPath": "/etc/ssl/certs"
  }
}
```

Response:
```json
{
  "success": true,
  "message": "Certificate deployed successfully to 192.168.1.100"
}
```

#### Deploy Certificate via Network Filesystem

```http
POST /api/DeploymentApi/deploy
Content-Type: application/json

{
  "certificateId": 123,
  "deploymentTarget": {
    "type": "NetworkFileSystem",
    "target": "/mnt/network-share",
    "destinationPath": "/mnt/network-share/certs"
  }
}
```

#### Test Deployment Connection

```http
POST /api/DeploymentApi/test
Content-Type: application/json

{
  "type": "SSH",
  "target": "192.168.1.100",
  "username": "deploy",
  "privateKeyPath": "/path/to/ssh/key",
  "port": 22
}
```

## Ansible Integration

### Example Playbook

```yaml
---
- name: Deploy certificates using BitCrafts API
  hosts: localhost
  gather_facts: no
  vars:
    api_base_url: "http://localhost:5000/api"
  
  tasks:
    - name: Create server certificate
      uri:
        url: "{{ api_base_url }}/CertificatesApi/server"
        method: POST
        body_format: json
        body:
          fqdn: "{{ inventory_hostname }}"
          ipAddresses: ["{{ ansible_default_ipv4.address }}"]
        status_code: 201
      register: cert_result
    
    - name: Deploy certificate via SSH
      uri:
        url: "{{ api_base_url }}/DeploymentApi/deploy"
        method: POST
        body_format: json
        body:
          certificateId: "{{ cert_result.json.id }}"
          deploymentTarget:
            type: "SSH"
            target: "{{ inventory_hostname }}"
            username: "{{ ansible_user }}"
            privateKeyPath: "{{ ansible_ssh_private_key_file }}"
            port: "{{ ansible_port | default(22) }}"
            destinationPath: "/etc/ssl/certs"
        status_code: 200
      register: deploy_result
    
    - name: Display result
      debug:
        var: deploy_result.json
```

## Error Handling

All API endpoints return appropriate HTTP status codes:

- `200 OK` - Success
- `201 Created` - Resource created
- `204 No Content` - Success with no content
- `400 Bad Request` - Invalid request
- `404 Not Found` - Resource not found
- `500 Internal Server Error` - Server error

Error responses include a JSON body with details:

```json
{
  "error": "Certificate 999 not found"
}
```

## Rate Limiting

Currently no rate limiting is implemented. For production, consider adding rate limiting middleware.

## Swagger/OpenAPI Documentation

Interactive API documentation is available at:
- Development: `http://localhost:5000/swagger`
- Use Swagger UI to explore and test API endpoints

## Examples

### Using curl

```bash
# Create server certificate
curl -X POST http://localhost:5000/api/CertificatesApi/server \
  -H "Content-Type: application/json" \
  -d '{"fqdn":"server.example.com","ipAddresses":["192.168.1.100"]}'

# Get all certificates
curl http://localhost:5000/api/CertificatesApi

# Download certificate
curl -o cert.tar.gz http://localhost:5000/api/CertificatesApi/1/download

# Deploy via SSH
curl -X POST http://localhost:5000/api/DeploymentApi/deploy \
  -H "Content-Type: application/json" \
  -d '{
    "certificateId": 1,
    "deploymentTarget": {
      "type": "SSH",
      "target": "192.168.1.100",
      "username": "deploy",
      "port": 22,
      "destinationPath": "/etc/ssl/certs"
    }
  }'
```

### Using Python requests

```python
import requests

api_base = "http://localhost:5000/api"

# Create server certificate
response = requests.post(
    f"{api_base}/CertificatesApi/server",
    json={
        "fqdn": "server.example.com",
        "ipAddresses": ["192.168.1.100"]
    }
)
cert = response.json()
print(f"Created certificate ID: {cert['id']}")

# Deploy certificate
response = requests.post(
    f"{api_base}/DeploymentApi/deploy",
    json={
        "certificateId": cert['id'],
        "deploymentTarget": {
            "type": "SSH",
            "target": "192.168.1.100",
            "username": "deploy",
            "port": 22,
            "destinationPath": "/etc/ssl/certs"
        }
    }
)
result = response.json()
print(f"Deployment result: {result['message']}")
```
