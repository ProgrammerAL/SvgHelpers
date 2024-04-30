using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProgrammerAl.SvgHelpers.IaC.Config;

public record DeploymentPackagesConfig(string UnzippedArtifactsDir)
{
    public string WebsitePath => $"{UnzippedArtifactsDir}/svg-helpers-site";
}

