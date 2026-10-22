using System;
using System.Net;
using System.Net.Sockets;

namespace Strategies.Api.Configuration
{
    // The rate-limit partition for a client address (read after forwarded headers have run).
    public static class ClientPartitionKey
    {
        public const string Unknown = "unknown";

        // No address: one shared partition. An IPv4-mapped IPv6 address counts as the IPv4 address.
        // Other IPv6 addresses are grouped by /64, the block a single home or mobile connection
        // normally gets, so moving to another address inside it does not reset the limit.
        public static string From(IPAddress? address)
        {
            if (address == null)
                return Unknown;

            if (address.IsIPv4MappedToIPv6)
                address = address.MapToIPv4();

            if (address.AddressFamily != AddressFamily.InterNetworkV6)
                return address.ToString();

            var bytes = address.GetAddressBytes();
            Array.Clear(bytes, 8, 8);
            return new IPAddress(bytes) + "/64";
        }
    }
}
