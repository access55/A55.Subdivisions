using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Subdivisions.Models;

namespace Subdivisions.Services;

class TagsService
{
    readonly IHostEnvironment? env;
    readonly SubConfig config;

    public TagsService(
        IOptions<SubConfig> config,
        IHostEnvironment? env = null
    )
    {
        this.env = env;
        this.config = config.Value;
    }

    public Dictionary<string, string> GetTags() =>
        new()
        {
            ["CreatedBy"] = "Subdivisions.net",
            ["Source"] = config.Source,
            ["App"] = env?.ApplicationName ?? config.Source,
        };

    public List<T> GetTags<T>(Func<(string Key, string Value), T> factory) =>
        GetTags()
            .Select(x => factory((x.Key, x.Value)))
            .ToList();
}
