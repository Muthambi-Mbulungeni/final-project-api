using System.Collections.Generic;
using System.Linq;
using System.Net;
using Microsoft.AspNetCore.HttpOverrides;

namespace Strategies.Api.Configuration
{
    // "ForwardedHeaders" section. Off by default (local dev, tests): X-Forwarded-For is ignored and the
    // real connection address is the client IP. appsettings.Production.json turns it on for Azure.
    public class ForwardedHeadersSettings
    {
        public const string SectionName = "ForwardedHeaders";

        public bool Enabled { get; set; }
        public int ForwardLimit { get; set; } = 1;
        public List<string> KnownProxies { get; set; } = new();
        public List<string> KnownNetworks { get; set; } = new();

        public bool IsValid() =>
            ForwardLimit >= 1
            && KnownProxies.All(p => IPAddress.TryParse(p, out _))
            && KnownNetworks.All(n => System.Net.IPNetwork.TryParse(n, out _));

        public void Apply(ForwardedHeadersOptions options)
        {
            if (!Enabled)
            {
                options.ForwardedHeaders = ForwardedHeaders.None;
                return;
            }

            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

            // Azure App Service's front end appends the real client IP as the LAST X-Forwarded-For entry;
            // anything to its left came from the caller and can be made up. ForwardLimit = 1 reads only that
            // last entry. The defaults only trust loopback proxies, and the front end's address is not fixed,
            // so with no proxies configured the lists are cleared and ForwardLimit = 1 is what keeps
            // spoofed entries out. Configured proxies or networks replace the defaults.
            options.ForwardLimit = ForwardLimit;
            options.KnownProxies.Clear();
            options.KnownIPNetworks.Clear();
            foreach (var proxy in KnownProxies)
                options.KnownProxies.Add(IPAddress.Parse(proxy));
            foreach (var network in KnownNetworks)
                options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse(network));
        }
    }
}
