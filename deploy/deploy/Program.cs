using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

using Cake.Core;
using Cake.Core.Diagnostics;
using Cake.Core.IO;
using Cake.Frosting;

using Pulumi.Automation;

public static class Program
{
    public static int Main(string[] args)
    {
        return new CakeHost()
            .UseContext<BuildContext>()
            .Run(args);
    }
}

public class BuildContext : FrostingContext
{
    public string WorkspacePath { get; }
    public string PulumiPath { get; }
    public string PulumiStackName { get; }
    public string UnzippedArtifactsDir { get; }
    public string BuildArtifactsPath { get; }

    public BuildContext(ICakeContext context)
        : base(context)
    {
        WorkspacePath = LoadParameter(context, nameof(WorkspacePath));
        BuildArtifactsPath = LoadParameter(context, nameof(BuildArtifactsPath));
        PulumiPath = WorkspacePath + "/infra/pulumi-infra-deploy";
        UnzippedArtifactsDir = (WorkspacePath + "/unzipped_artifacts").Replace("\\", "/");

        PulumiStackName = LoadParameter(context, nameof(PulumiStackName));
    }

    private string LoadParameter(ICakeContext context, string parameterName)
    {
        return context.Arguments.GetArgument(parameterName) ?? throw new Exception($"Missing parameter '{parameterName}'");
    }
}

[TaskName(nameof(UnzipAssetsTask))]
public sealed class UnzipAssetsTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        _ = Directory.CreateDirectory(context.UnzippedArtifactsDir);

        ExtractArchive("svg-helpers-site", context);
    }

    private void ExtractArchive(string zipName, BuildContext context)
    {
        var zipFilePath = $"{context.BuildArtifactsPath}/{zipName}.zip";
        var outputPath = $"{context.UnzippedArtifactsDir}/{zipName}";

        context.Log.Information($"Extracting zip '{zipFilePath}' to '{outputPath}'");

        using var fileStream = File.OpenRead(zipFilePath);
        using var archive = new ZipArchive(fileStream);
        archive.ExtractToDirectory(outputPath);
    }
}

[IsDependentOn(typeof(UnzipAssetsTask))]
[TaskName(nameof(UpdatePulumiConfigTask))]
public sealed class UpdatePulumiConfigTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        var configFilePath = $"{context.PulumiPath}/Pulumi.{context.PulumiStackName}.yaml";
        var configFileText = File.ReadAllText(configFilePath);

        configFileText = UpdateConfigValue("svghelpers-site:unzipped-artifacts-dir: ", context.UnzippedArtifactsDir, configFileText);
        configFileText = UpdateConfigValue("svghelpers-site:root-run-path: ", context.WorkspacePath, configFileText);

        File.WriteAllText(configFilePath, configFileText);
        context.Log.Information("Pulumi Config: \n" + configFileText);
    }

    private string UpdateConfigValue(string keyName, string newValue, string stringToUpdate)
    {
        var keyIndex = stringToUpdate.IndexOf(keyName);
        if (keyIndex < 0)
        {
            throw new Exception($"Could not find index of text '{keyName}' to update the config value");
        }

        var valueEndIndex = stringToUpdate.IndexOf("local", keyIndex);

        if (valueEndIndex < 0)
        {
            throw new Exception($"Could not find index of text 'local' to update the service version config value");
        }

        //Remove the default value 'local' and add the new value to it
        return stringToUpdate.Remove(valueEndIndex, "local".Length)
                             .Insert(valueEndIndex, $"\"{newValue}\"");
    }
}

[IsDependentOn(typeof(UpdatePulumiConfigTask))]
[TaskName(nameof(PulumiDeployTask))]
public sealed class PulumiDeployTask : AsyncFrostingTask<BuildContext>
{
    public override async Task RunAsync(BuildContext context)
    {
        var fullStackName = $"ProgrammerAL/{context.PulumiStackName}";

        context.Log.Information($"Loading stack {fullStackName} from path '{context.PulumiPath}'");

        var stackArgs = new LocalProgramArgs(fullStackName, context.PulumiPath);
        var pulumiStack = await LocalWorkspace.CreateOrSelectStackAsync(stackArgs);

        context.Log.Information($"Pulumi Up starting...");

        var result = await pulumiStack.UpAsync();
        context.Log.Information($"Pulumi Up completed");
        Utilities.LogPulumiResult(context, result);
    }
}

[TaskName("Default")]
[IsDependentOn(typeof(PulumiDeployTask))]
public class DefaultTask : FrostingTask
{
}
