using Cake.Common;
using Cake.Core;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public record WebsitePaths(
    string ProjectName,
    string PathToSln,
    string ProjectFolder,
    string CsprojFile,
    string OutDir,
    string ZipOutDir,
    string ZipOutFilePath,
    string UnitTestDirectory,
    string UnitTestProj,
    string CoverletOutDir,
    string CustomJsModulesDir)
{
    public static WebsitePaths LoadFromContext(ICakeContext context, string buildConfiguration, string srcDirectory, string buildArtifactsPath)
    {
        var projectName = "SvgHelpers";
        srcDirectory += $"/{projectName}";
        var pathToSln = srcDirectory + $"/{projectName}.sln";
        var functionProjectDir = srcDirectory + $"/{projectName}";
        var functionCsprojFile = functionProjectDir + $"/{projectName}.csproj";
        var outDir = functionProjectDir + $"/bin/{buildConfiguration}/cake-build-output";
        var zipOutDir = buildArtifactsPath;
        var zipOutFilePath = zipOutDir + $"/svg-helpers-site.zip";
        var unitTestDirectory = srcDirectory + $"/UnitTests";
        var unitTestProj = unitTestDirectory + $"/UnitTests.csproj";
        var coverletOutDir = unitTestDirectory + $"/coverlet-coverage-results/";
        var customJsModulesDir = srcDirectory + $"/CustomNpmModules/";

        return new WebsitePaths(
            projectName,
            pathToSln,
            functionProjectDir,
            functionCsprojFile,
            outDir,
            zipOutDir,
            zipOutFilePath,
            unitTestDirectory,
            unitTestProj,
            coverletOutDir,
            customJsModulesDir);
    }
}
