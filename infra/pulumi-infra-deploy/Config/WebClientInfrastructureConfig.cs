
using System;


namespace ProgrammerAl.SvgHelpers.IaC.Config;

public record WebClientInfrastructureConfig(
    string RootDomain,
    string? Subdomain,
    string CloudflareZoneId)
{

    public string DomainEndpoint
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Subdomain))
            {
                return RootDomain;
            }

            return $"{Subdomain}.{RootDomain}";
        }
    }

    public string HttpsDomainEndpoint
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Subdomain))
            {
                return $"https://{RootDomain}";
            }

            return $"https://{Subdomain}.{RootDomain}";
        }
    }
}

public class WebClientInfrastructureConfigDto : ConfigDtoBase<WebClientInfrastructureConfig>
{
    public string? RootDomain { get; set; }
    public string? Subdomain { get; set; }
    public string? CloudflareZoneId { get; set; }

    public override WebClientInfrastructureConfig GenerateValidConfigObject()
    {
        if (!string.IsNullOrWhiteSpace(RootDomain)
            && !string.IsNullOrWhiteSpace(CloudflareZoneId)
            )
        {
            return new WebClientInfrastructureConfig(
                RootDomain,
                Subdomain,
                CloudflareZoneId
                );
        }

        throw new Exception($"{GetType().Name} has invalid config");
    }
}
