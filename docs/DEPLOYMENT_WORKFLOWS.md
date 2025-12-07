# Deployment Workflows

This document describes the certificate deployment workflows available in BitCrafts.Certificates.

## Overview

The application supports two types of deployment workflows:
1. **SSH Deployment**: Deploy certificates to remote servers via SSH/SCP
2. **FileSystem Deployment**: Deploy certificates to local or mounted network filesystems

## SSH Deployment Workflow

### Prerequisites

- SSH access to target machines with public key authentication configured
- User account with appropriate permissions on target machines
- SSH private key accessible to the administrator running the GUI

### Configuration

An SSH deployment target requires the following configuration:

```csharp
var target = new DeploymentTarget
{
    HostnameOrIp = "server.example.com",  // Hostname or IP address
    DestinationPath = "/etc/ssl/certs",    // Remote directory path
    Username = "deploy",                    // SSH username
    Port = 22,                             // SSH port (default: 22)
    PrivateKeyPath = "/home/admin/.ssh/id_rsa",  // Path to SSH private key
    Owner = "root",                        // File owner (optional)
    Group = "root",                        // File group (optional)
    Permissions = "0600"                   // File permissions (optional)
};
```

### Operation Flow

1. Connect to the remote server using SSH with the specified private key
2. Upload the certificate file to the destination path
3. Set file permissions (chmod) if specified
4. Set file ownership (chown) if owner/group specified
5. Verify successful deployment

### Security Considerations

- SSH keys should be stored securely with appropriate file permissions (0600)
- Use dedicated deployment user accounts with minimal privileges
- Consider using SSH certificate-based authentication for additional security
- Rotate SSH keys regularly
- Never store SSH private key passphrases in configuration files

### Troubleshooting

**Connection refused:**
- Verify SSH service is running on target: `systemctl status sshd`
- Check firewall rules: `sudo firewall-cmd --list-all`
- Verify port is correct (default 22)

**Permission denied:**
- Ensure SSH key has correct permissions: `chmod 600 ~/.ssh/id_rsa`
- Verify public key is in target's authorized_keys: `~/.ssh/authorized_keys`
- Check SSH server configuration: `/etc/ssh/sshd_config`

**Host key verification failed:**
- Add host to known_hosts: `ssh-keyscan -H server.example.com >> ~/.ssh/known_hosts`
- Or disable strict host key checking (not recommended for production)

**Permission denied when setting ownership:**
- Deployment user must have sudo privileges or be root
- Consider using sudo with NOPASSWD for chown/chmod commands

## FileSystem Deployment Workflow

### Prerequisites

- Access to local filesystem or mounted network filesystem
- Write permissions to destination directory
- Appropriate user permissions to set ownership (may require root/sudo)

### Configuration

A FileSystem deployment target requires:

```csharp
var target = new DeploymentTarget
{
    HostnameOrIp = "local",                // Use "local" for local filesystem
    DestinationPath = "/mnt/nfs/certs",    // Local or mounted path
    Owner = "www-data",                    // File owner (optional)
    Group = "www-data",                    // File group (optional)
    Permissions = "0644"                   // File permissions (optional)
};
```

### Supported Filesystems

#### Local Filesystem
- Standard Linux filesystem (ext4, xfs, etc.)
- Path: `/path/to/directory`

#### NFS (Network File System)
- Mount NFS share before deployment
- Example mount: `mount -t nfs server:/export /mnt/nfs`
- Ensure proper NFS permissions configured

#### GlusterFS
- Mount GlusterFS volume before deployment
- Example mount: `mount -t glusterfs server:/volume /mnt/gluster`
- Verify volume is accessible and writable

#### CephFS
- Mount CephFS before deployment
- Example mount: `mount -t ceph monitor:/path /mnt/ceph -o name=admin,secret=KEY`
- Ensure proper Ceph credentials configured

### Operation Flow

1. Verify destination path exists and is writable
2. Write certificate file to destination path
3. Set file permissions (chmod) if specified
4. Set file ownership (chown) if owner/group specified
5. Verify successful deployment

### Security Considerations

- Ensure destination directories have appropriate permissions (typically 0755)
- Certificate files should be readable only by intended users (0600 or 0644)
- For network filesystems, use secure mount options (e.g., sec=krb5 for NFS)
- Consider encrypting certificates at rest on network filesystems
- Regularly audit file ownership and permissions

### Filesystem-Specific Considerations

#### NFS
- Use NFSv4 with Kerberos (sec=krb5) for authentication
- Configure proper ID mapping (idmapd)
- Set appropriate export options on NFS server
- Example export: `/export *(rw,sync,no_root_squash)`

#### GlusterFS
- Configure proper volume options for security
- Use SSL/TLS for client-server communication
- Set appropriate volume ACLs
- Example: `gluster volume set myvolume auth.ssl-allow 'client1,client2'`

#### CephFS
- Use CephX authentication for access control
- Configure proper capability restrictions
- Regularly rotate Ceph keys
- Example capability: `allow rw path=/certs`

### Troubleshooting

**Destination directory not found:**
- Verify mount point exists: `ls -la /mnt`
- Check if filesystem is mounted: `mount | grep /mnt/nfs`
- Verify mount was successful: `df -h`

**Permission denied writing file:**
- Check directory permissions: `ls -ld /mnt/nfs/certs`
- Verify user has write access: `touch /mnt/nfs/certs/test && rm /mnt/nfs/certs/test`
- For NFS, check export permissions on server

**Permission denied setting ownership:**
- Must run as root or use sudo
- For NFS with root_squash, ownership changes may fail
- Consider using no_root_squash (carefully) or map to non-root user

**Stale file handle (NFS):**
- Filesystem was unmounted/remounted on server
- Remount on client: `umount /mnt/nfs && mount /mnt/nfs`
- Check NFS server logs: `journalctl -u nfs-server`

## Best Practices

### General
1. Test connectivity before executing deployment workflows
2. Use the `TestConnectivityAsync` method to verify targets are reachable
3. Implement proper error handling and logging
4. Maintain audit logs of all deployment operations
5. Regularly review and update deployment configurations

### Security
1. Use least-privilege principles for all accounts
2. Encrypt certificates before storage in database
3. Never log sensitive information (private keys, certificates)
4. Rotate credentials regularly
5. Use dedicated deployment accounts separate from administrative accounts
6. Implement network segmentation between deployment systems and targets

### Monitoring
1. Monitor deployment success/failure rates
2. Alert on repeated deployment failures
3. Track certificate deployment locations for inventory
4. Monitor filesystem space on network mounts
5. Set up automated health checks for deployment targets

### Disaster Recovery
1. Maintain backups of deployment configurations
2. Document manual deployment procedures as fallback
3. Test recovery procedures regularly
4. Keep copies of certificates in secure offline storage
5. Document filesystem mount configurations

## Example Workflows

### Deploy to Multiple Web Servers via SSH

```csharp
var workflow = new DeploymentWorkflow
{
    Name = "Production Web Servers",
    Type = DeploymentWorkflowType.SSH,
    Targets = new List<DeploymentTarget>
    {
        new()
        {
            HostnameOrIp = "web1.example.com",
            DestinationPath = "/etc/nginx/ssl",
            Username = "deploy",
            PrivateKeyPath = "/home/admin/.ssh/deploy_rsa",
            Owner = "nginx",
            Group = "nginx",
            Permissions = "0600"
        },
        new()
        {
            HostnameOrIp = "web2.example.com",
            DestinationPath = "/etc/nginx/ssl",
            Username = "deploy",
            PrivateKeyPath = "/home/admin/.ssh/deploy_rsa",
            Owner = "nginx",
            Group = "nginx",
            Permissions = "0600"
        }
    }
};

var result = await deploymentService.ExecuteWorkflowAsync(workflow, certificate);
```

### Deploy to Shared NFS Storage

```csharp
var workflow = new DeploymentWorkflow
{
    Name = "Shared Certificate Storage",
    Type = DeploymentWorkflowType.FileSystem,
    Targets = new List<DeploymentTarget>
    {
        new()
        {
            HostnameOrIp = "local",
            DestinationPath = "/mnt/nfs/certificates",
            Owner = "certmanager",
            Group = "certusers",
            Permissions = "0644"
        }
    }
};

var result = await deploymentService.ExecuteWorkflowAsync(workflow, certificate);
```

## API Usage

### Creating a Workflow

```csharp
var workflowService = serviceProvider.GetRequiredService<IDeploymentWorkflowService>();

// Validate workflow configuration
var validationResult = await workflowService.ValidateWorkflowAsync(workflow);
if (!validationResult.IsValid)
{
    // Handle validation errors
    foreach (var error in validationResult.Errors)
    {
        Console.WriteLine($"Validation error: {error}");
    }
    return;
}

// Test connectivity
var connectivityResult = await workflowService.TestConnectivityAsync(workflow);
if (!connectivityResult.Success)
{
    // Handle connectivity failures
    foreach (var error in connectivityResult.Errors)
    {
        Console.WriteLine($"Connectivity error: {error}");
    }
    return;
}

// Execute workflow
var deploymentResult = await workflowService.ExecuteWorkflowAsync(workflow, certificate);
if (deploymentResult.Success)
{
    Console.WriteLine("Deployment successful!");
    foreach (var result in deploymentResult.TargetResults)
    {
        Console.WriteLine($"  {result}");
    }
}
else
{
    Console.WriteLine("Deployment failed!");
    foreach (var error in deploymentResult.Errors)
    {
        Console.WriteLine($"  {error}");
    }
}
```

## Required Permissions

### SSH Deployment
- SSH access to target machines
- Write permissions to destination directory
- Permission to execute chown/chmod (may require sudo)

### FileSystem Deployment
- Read/write access to destination directory
- Permission to execute chown/chmod (may require root/sudo)
- For network filesystems: appropriate mount credentials

## Performance Considerations

- SSH deployments are executed sequentially to avoid overwhelming targets
- Consider implementing parallel deployment with rate limiting for large target sets
- Network filesystem performance depends on network latency and server load
- Monitor deployment duration and set appropriate timeouts
- Cache DNS resolutions for frequently-used targets

## Future Enhancements

- Parallel deployment with configurable concurrency
- Deployment rollback on failure
- Certificate rotation workflows
- Integration with configuration management tools (Ansible, Puppet, Chef)
- Support for container deployments (Kubernetes secrets)
- Webhook notifications on deployment events
