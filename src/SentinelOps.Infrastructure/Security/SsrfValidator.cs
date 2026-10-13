using System.Net;
using System.Net.Sockets;

namespace SentinelOps.Infrastructure.Security;

public static class SsrfValidator
{
    private static readonly IPAddress CloudMetadataAddress = IPAddress.Parse("169.254.169.254");

    public static async Task<(bool IsAllowed, string? Reason)> ValidateDestinationUrlAsync(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return (false, "Destination URL is empty.");
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return (false, "Destination URL is not a valid absolute URI.");
        }

        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
        {
            return (false, $"Unsupported URI scheme: {uri.Scheme}. Only HTTP and HTTPS are permitted.");
        }

        if (uri.IsLoopback || uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase))
        {
            return (false, "Loopback / localhost destinations are strictly prohibited.");
        }

        // Direct IP literal validation (prevents DNS reverse-lookup overhead and spoofing)
        if (IPAddress.TryParse(uri.DnsSafeHost, out var directIp))
        {
            if (IsPrivateOrRestrictedIp(directIp))
            {
                return (false, $"Destination resolved to restricted IP address: {directIp}. SSRF protection blocked request.");
            }

            return (true, null);
        }

        try
        {
            var addresses = await Dns.GetHostAddressesAsync(uri.DnsSafeHost);
            if (addresses.Length == 0)
            {
                return (false, "Could not resolve DNS for host.");
            }

            foreach (var ip in addresses)
            {
                if (IsPrivateOrRestrictedIp(ip))
                {
                    return (false, $"Destination resolved to restricted IP address: {ip}. SSRF protection blocked request.");
                }
            }
        }
        catch (SocketException ex)
        {
            return (false, $"DNS resolution failed: {ex.Message}");
        }

        return (true, null);
    }

    public static bool IsPrivateOrRestrictedIp(IPAddress ip)
    {
        if (IPAddress.IsLoopback(ip))
            return true;

        if (ip.Equals(CloudMetadataAddress))
            return true;

        if (ip.AddressFamily == AddressFamily.InterNetworkV6)
        {
            if (ip.IsIPv6LinkLocal || ip.IsIPv6SiteLocal || ip.IsIPv6Multicast)
                return true;

            // Unique Local Address range fc00::/7
            byte[] bytes = ip.GetAddressBytes();
            if ((bytes[0] & 0xFE) == 0xFC)
                return true;

            return false;
        }

        if (ip.AddressFamily == AddressFamily.InterNetwork)
        {
            byte[] bytes = ip.GetAddressBytes();

            // 10.0.0.0/8
            if (bytes[0] == 10)
                return true;

            // 172.16.0.0/12
            if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
                return true;

            // 192.168.0.0/16
            if (bytes[0] == 192 && bytes[1] == 168)
                return true;

            // 169.254.0.0/16 (Link-local)
            if (bytes[0] == 169 && bytes[1] == 254)
                return true;

            // 0.0.0.0/8
            if (bytes[0] == 0)
                return true;

            // 127.0.0.0/8
            if (bytes[0] == 127)
                return true;

            // 224.0.0.0/4 (Multicast) or 240.0.0.0/4 (Reserved)
            if (bytes[0] >= 224)
                return true;
        }

        return false;
    }
}
