using Pulumi;

using static ProgrammerAl.SvgHelpers.IaC.StackBuilders.Website.WebsiteInfrastructure;

namespace ProgrammerAl.SvgHelpers.IaC.StackBuilders.Website;

public record WebsiteInfrastructure(
    CloudflarePagesApp WebApp,
    DomainsInfrastructure DomainsInfra,
    string FullDomainEndpoint)
{
    public record CloudflarePagesApp(Pulumi.Cloudflare.PagesProject PagesProject);

    public record DomainsInfrastructure(
        Pulumi.Cloudflare.Record DomainRecord,
        Pulumi.Cloudflare.PagesDomain PagesDomain,
        Output<string> FullEndpoint);}

