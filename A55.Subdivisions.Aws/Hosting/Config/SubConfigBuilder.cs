using A55.Subdivisions.Models;
using Microsoft.Extensions.DependencyInjection;

namespace A55.Subdivisions.Hosting.Config;

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

    public void Configure(SubConfig config) => CopyPropertiesTo(this, config);

    static void CopyPropertiesTo<TSource, TDest>(TSource source, TDest dest) where TDest : new()
    {
        var sourceProps = typeof(TSource).GetProperties().Where(x => x.CanRead).ToArray();
        var destProps = typeof(TDest).GetProperties().Where(x => x.CanWrite);
        var defaultDest = new TDest();
        foreach (var destProp in destProps)
        {
            var prop = sourceProps
                .SingleOrDefault(p =>
                    p.Name == destProp.Name && destProp.PropertyType == p.PropertyType);
            if (prop is not null &&
                prop.GetValue(source, null) is { } newValue &&
                prop.GetValue(defaultDest, null) is { } defaultValue &&
                !newValue.Equals(defaultValue))
                destProp.SetValue(dest, prop.GetValue(source, null), null);
        }
    }
}
