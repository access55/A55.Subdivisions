using System.Globalization;
using Amazon.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Subdivisions.Models;

namespace Subdivisions.Extensions;

static class Extensions
{
    public static string ToPascalCase(this string snakeName) =>
        string.Concat(snakeName.ToLowerInvariant().Split('_')
            .Select(CultureInfo.InvariantCulture.TextInfo.ToTitleCase));

    public static string TrimUnderscores(this string text) => text.Trim('_');

    public static string ToSnakeCase(this string str)
        => string.Concat(str
                .Select((x, i) =>
                    i > 0 && i < str.Length - 1 && char.IsUpper(x) && !char.IsUpper(str[i - 1])
                        ? $"_{x}"
                        : x.ToString()))
            .ToLowerInvariant();

    public static IServiceCollection AddAwsConfig<TConfig>(this IServiceCollection services)
        where TConfig : ClientConfig, new() =>
        services.AddTransient(sp =>
        {
            var subSettings = sp.GetRequiredService<IOptionsMonitor<SubConfig>>().CurrentValue;
            var config = new TConfig();
            if (!string.IsNullOrWhiteSpace(subSettings.ServiceUrl))
                config.ServiceURL = subSettings.ServiceUrl;
            else
                config.RegionEndpoint = subSettings.Endpoint;

            return config;
        });

    public static AWSCredentials GetAwsCredentials(this IServiceProvider provider) =>
        provider.GetRequiredService<AWSCredentials>();
}
