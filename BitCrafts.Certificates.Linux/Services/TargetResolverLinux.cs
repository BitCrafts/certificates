// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (C) 2025 BitCrafts

using System.Net.NetworkInformation;
using System.Net.Sockets;
using BitCrafts.Certificates.Abstractions.Interfaces;
using BitCrafts.Certificates.Abstractions.Models;

namespace BitCrafts.Certificates.Linux.Services;

/// <summary>
/// Resolves and validates deployment targets on Linux.
/// </summary>
public class TargetResolverLinux : ITargetResolver
{
    public async Task<IEnumerable<string>> ResolveHostnameAsync(string hostname, CancellationToken cancellationToken = default)
    {
        try
        {
            var addresses = await System.Net.Dns.GetHostAddressesAsync(hostname, cancellationToken);
            return addresses.Select(a => a.ToString());
        }
        catch
        {
            return Array.Empty<string>();
        }
    }

    public async Task<bool> ValidateConnectivityAsync(DeploymentTarget target, CancellationToken cancellationToken = default)
    {
        return await IsReachableAsync(target.HostnameOrIp, target.Port, cancellationToken);
    }

    public async Task<bool> IsReachableAsync(string hostnameOrIp, int? port = null, CancellationToken cancellationToken = default)
    {
        try
        {
            // Try ICMP ping first
            using var ping = new Ping();
            var reply = await ping.SendPingAsync(hostnameOrIp, 5000);
            
            if (reply.Status != IPStatus.Success)
                return false;

            // If port is specified, test TCP connectivity
            if (port.HasValue)
            {
                using var client = new TcpClient();
                var connectTask = client.ConnectAsync(hostnameOrIp, port.Value, cancellationToken).AsTask();
                var timeoutTask = Task.Delay(5000, cancellationToken);
                
                var completedTask = await Task.WhenAny(connectTask, timeoutTask);
                
                if (completedTask == timeoutTask)
                    return false;
                
                return client.Connected;
            }

            return true;
        }
        catch
        {
            return false;
        }
    }
}
