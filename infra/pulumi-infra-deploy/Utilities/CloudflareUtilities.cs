using Pulumi;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProgrammerAl.SvgHelpers.IaC.Utilities;

public static class CloudflareUtilities
{
    public static Pulumi.Cloudflare.Provider GenerateCloudflareProvider(string name, Output<string> apiToken)
    {
        return new Pulumi.Cloudflare.Provider(name, new Pulumi.Cloudflare.ProviderArgs
        {
            ApiToken = apiToken,
        });
    }
}
