using Microsoft.Identity.Client;

using Pulumi;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProgrammerAl.SvgHelpers.IaC.Config;

public record CloudflareConfig(
    Output<string> ApiToken,
    string AccountId);

public class CloudflareConfigDto : ConfigDtoBase<CloudflareConfig>
{
    public Output<string>? ApiToken { get; set; }
    public string? AccountId { get; set; }

    public override CloudflareConfig GenerateValidConfigObject()
    {
        if (ApiToken != null
            && !string.IsNullOrWhiteSpace(AccountId))
        {
            return new CloudflareConfig(ApiToken, AccountId);
        }

        throw new Exception($"{GetType().Name} has invalid config");
    }
}
