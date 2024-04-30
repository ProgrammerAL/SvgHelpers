using Cake.Core.Diagnostics;

using Pulumi.Automation;

using System;
using System.IO;
using System.Threading.Tasks;

public static class Utilities
{
    public static void LogPulumiResult(BuildContext context, UpdateResult result)
    {
        context.Log.Information($"Pulumi Summary Message: {result.Summary.Message ?? "{null}"}");
        context.Log.Information($"Pulumi Standard Output: {result.StandardOutput ?? "{null}"}");
        context.Log.Information($"Pulumi Standard Error: {result.StandardError ?? "{null}"}");
    }
}
