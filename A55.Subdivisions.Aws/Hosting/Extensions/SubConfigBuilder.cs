using Microsoft.Extensions.DependencyInjection;

namespace A55.Subdivisions.Aws.Hosting.Extensions;

public sealed class SubConfigBuilder : SubConfig
{
    readonly IServiceCollection services;

    public SubConfigBuilder(IServiceCollection services) => this.services = services;

    public TopicConfigurationBuilder<TMessage> MapTopic<TMessage>(string topicName)
        where TMessage : notnull
    {
        var builder = new TopicConfigurationBuilder<TMessage>(services, topicName);
        services.AddSingleton(sp => builder.CreateDescriber(sp));
        return builder;
    }

    public void Configure(SubConfig config) => CopyPropertiesTo(this, config);

    static void CopyPropertiesTo<TSource, TDest>(TSource source, TDest dest)
    {
        var sourceProps = typeof(TSource).GetProperties().Where(x => x.CanRead).ToArray();
        var destProps = typeof(TDest).GetProperties().Where(x => x.CanWrite);

        foreach (var destProp in destProps)
        {
            var prop = sourceProps
                .SingleOrDefault(p => p.Name == destProp.Name && destProp.PropertyType.IsInstanceOfType(p));
            if (prop is not null)
                destProp.SetValue(dest, prop.GetValue(source, null), null);
        }
    }
}
