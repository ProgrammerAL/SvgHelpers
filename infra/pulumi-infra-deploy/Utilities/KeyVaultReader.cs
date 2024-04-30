using Azure.Core;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

using Microsoft.Extensions.Azure;

using ProgrammerAl.SvgHelpers.IaC.Exceptions;

using Pulumi;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace ProgrammerAl.SvgHelpers.IaC.Utilities;

public class KeyVaultReader
{
    private readonly SecretClient _client;
    private readonly string _keyVaultUri;

    public KeyVaultReader(string keyVaultUri)
        : this(keyVaultUri, new DefaultAzureCredential())
    {
    }

    public KeyVaultReader(string keyVaultUri, TokenCredential credential)
    {
        _keyVaultUri = keyVaultUri;
        _client = new SecretClient(new Uri(_keyVaultUri), credential);
    }

    public async Task<ImmutableDictionary<string, string>> ReadSecretsAsync(IEnumerable<string> names)
    {
        var items = names.Select(x => (x, ReadSecretAsync(x))).ToImmutableArray();
        var tasks = items.Select(x => x.Item2).ToArray();
        _ = await Task.WhenAll(tasks);

        var exceptions = new List<AggregateException>();

        var builder = ImmutableDictionary.CreateBuilder<string, string>();
        foreach (var item in items)
        {
            var secretName = item.Item1;
            var kvTask = item.Item2;
            if (kvTask.IsFaulted && kvTask.Exception is object)
            {
                exceptions.Add(kvTask.Exception);
            }

            builder.Add(secretName, kvTask.Result);
        }

        if (exceptions.Any())
        {
            throw new AggregateException(exceptions);
        }

        var results = items.ToImmutableDictionary(x => x.Item1, y => y.Item2.Result);
        return builder.ToImmutableDictionary();
    }

    public async Task<string> ReadSecretAsync(string name)
    {
        var result = await _client.GetSecretAsync(name);

        var rawResponse = result.GetRawResponse();
        if (rawResponse.IsError)
        {
            var error = rawResponse.Content.ToString();
            throw new KeyVaultErrorException($"Error reading from key vault at `{_keyVaultUri}` with secret named `{name}`. Error is: {error}");
        }

        return result.Value.Value;
    }
}
