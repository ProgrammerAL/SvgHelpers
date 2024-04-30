using ProgrammerAl.SvgHelpers.IaC.Utilities;

using Pulumi;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProgrammerAl.SvgHelpers.IaC.Config.GlobalConfigs;

public record GlobalConfig(
    DeploymentPackagesConfig DeploymentPackagesConfig,
    WebClientInfrastructureConfig WebClientInfraConfig,
    CloudflareConfig CloudflareConfig
)
{
    public static async Task<GlobalConfig> LoadAsync(Pulumi.Config config)
    {
        string secretsKeyVaultUrl = config.Require("secrets-key-vault-url");
        var keyVaultSecrets = await LoadKeyVaultSecretsAsync(secretsKeyVaultUrl);

        string unzippedArtifactsDir = config.Require("unzipped-artifacts-dir");

        var cloudflareConfig = new CloudflareConfigDto
        {
            ApiToken = keyVaultSecrets.CloudflareProviderToken,
            AccountId = config.Require("cloudflare-account-id")
        }
        .GenerateValidConfigObject();

        return new GlobalConfig(
            DeploymentPackagesConfig: new DeploymentPackagesConfig(unzippedArtifactsDir),
            WebClientInfraConfig: config.RequireObject<WebClientInfrastructureConfigDto>("web-client-infra").GenerateValidConfigObject(),
            CloudflareConfig: cloudflareConfig
        );
    }

    private static async Task<KeyVaultSecrets> LoadKeyVaultSecretsAsync(string secretsKeyVaultUrl)
    {
        var keyVaultReader = new KeyVaultReader(secretsKeyVaultUrl);

        var results = await keyVaultReader.ReadSecretsAsync(new[]
        {
            "cloudflare-provider-token"
        });

        return new KeyVaultSecrets(
            CloudflareProviderToken: Output.CreateSecret(results["cloudflare-provider-token"])
            );
    }

    private record KeyVaultSecrets(
        Output<string> CloudflareProviderToken
        );
}
