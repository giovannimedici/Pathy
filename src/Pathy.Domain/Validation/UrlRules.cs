using System.Net;
using Pathy.Domain.Exceptions;

namespace Pathy.Domain.Validation;

public static class UrlRules
{
    private static readonly HashSet<string> AllowedSchemes =
        new(StringComparer.OrdinalIgnoreCase) { "http", "https" };

    private static readonly HashSet<string> BlockedHosts =
        new(StringComparer.OrdinalIgnoreCase) { "localhost" };

    public static void Validate(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            throw new DomainException("URL cannot be empty.");
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            throw new DomainException("URL must be a well-formed absolute URL.");
        }

        if (!AllowedSchemes.Contains(uri.Scheme))
        {
            throw new DomainException("URL must use http or https scheme.");
        }

        var host = uri.IdnHost;

        if (BlockedHosts.Contains(host) || host.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase))
        {
            throw new DomainException("URLs pointing to localhost are not allowed.");
        }

        if (IPAddress.TryParse(host, out var ipAddress) && IsBlockedIpAddress(ipAddress))
        {
            throw new DomainException("URLs pointing to private or internal IP addresses are not allowed.");
        }
    }

    private static bool IsBlockedIpAddress(IPAddress address)
    {
        if (IPAddress.IsLoopback(address))
        {
            return true;
        }

        if (address.Equals(IPAddress.Any) || address.Equals(IPAddress.IPv6Any))
        {
            return true;
        }

        if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
        {
            var bytes = address.GetAddressBytes();

            // 10.0.0.0/8
            if (bytes[0] == 10)
            {
                return true;
            }

            // 172.16.0.0/12
            if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
            {
                return true;
            }

            // 192.168.0.0/16
            if (bytes[0] == 192 && bytes[1] == 168)
            {
                return true;
            }

            // 169.254.0.0/16 (link-local)
            if (bytes[0] == 169 && bytes[1] == 254)
            {
                return true;
            }

            // 127.0.0.0/8
            if (bytes[0] == 127)
            {
                return true;
            }
        }

        if (address.IsIPv6LinkLocal || address.IsIPv6SiteLocal)
        {
            return true;
        }

        return false;
    }
}
