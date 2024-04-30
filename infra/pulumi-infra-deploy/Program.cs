using Pulumi;

using System;
using System.Threading.Tasks;
using System.Linq;

using System.Collections.Generic;
using ProgrammerAl.SvgHelpers.IaC.StackBuilders.Website;
using ProgrammerAl.SvgHelpers.IaC.Config.GlobalConfigs;

return await Pulumi.Deployment.RunAsync(async () =>
{
    var config = new Config();
    var globalConfig = await GlobalConfig.LoadAsync(config);

    var websiteBuilder = new WebsiteStackBuilder(globalConfig);
    var websiteInfra = websiteBuilder.GenerateResources();

    return GenerateOutputs(websiteInfra, globalConfig);
});

static Dictionary<string, object?> GenerateOutputs(
    WebsiteInfrastructure websiteInfra,
    GlobalConfig globalConfig)
{
    return new Dictionary<string, object?>
    {
        { "WebsiteDomainEndpoint", websiteInfra.FullDomainEndpoint },

        { "Readme", Output.Create(System.IO.File.ReadAllText("./Pulumi.README.md")) },
    };
}
