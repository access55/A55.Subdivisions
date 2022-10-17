using Microsoft.Extensions.DependencyInjection;
using Subdivisions.Models;

namespace Subdivisions.Hosting.Config;

public sealed class SubConfigBuilder : SubConfig
{
    readonly IServiceCollection services;

    public SubConfigBuilder(IServiceCollection services)
    {
        this.services = services;
        services.AddSingleton(sp =>
            sp.GetService<IEnumerable<ITopicConfigurationBuilder>>()
                ?.Where(x => x.HasConsumer)
                .Select(x => x.CreateConsumerDescriber(sp))
            ?? ArraySegment<IConsumerDescriber>.Empty);
    }

    public TopicConfigurationBuilder<TMessage> MapTopic<TMessage>(string topicName)
        where TMessage : notnull
    {
        var builder = new TopicConfigurationBuilder<TMessage>(services, topicName);
        services.AddSingleton<ITopicConfigurationBuilder>(builder);
        return builder;
    }

    public void OnError(Func<Exception, Task> handler) =>
        services.AddSingleton<ISubErrorListener>(new ErrorListener(handler));

    public void OnError<TListener>() where TListener : class, ISubErrorListener =>
        services.AddSingleton<ISubErrorListener, TListener>();

    public void Configure(SubConfig config) => CopyProperties(this, config);

    static void CopyProperties<TSource, TDest>(TSource source, TDest dest) where TDest : new()
    {
        var sourceProps = typeof(TSource).GetProperties().Where(x => x.CanRead).ToArray();
        var destProps = typeof(TDest).GetProperties().Where(x => x.CanWrite);
        var defaultDest = new TDest();
        foreach (var destProp in destProps)
        {
            var prop = sourceProps
                .SingleOrDefault(p =>
                    p.Name == destProp.Name && destProp.PropertyType == p.PropertyType);

            if (prop is null)
                continue;

            var newValue = prop.GetValue(source, null);
            var defaultValue = prop.GetValue(defaultDest, null);

            if (newValue?.Equals(defaultValue) == false && newValue?.Equals(string.Empty) == false)
                destProp.SetValue(dest, newValue, null);
        }
    }
}
