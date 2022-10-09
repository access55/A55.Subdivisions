using A55.Subdivisions.Aws.Models;
using AutoBogus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace A55.Subdivisions.Aws.Tests.Specs.Integration;

public class ConfigurationHostTests : ServicesFixture
{
    readonly string appName = Guid.NewGuid().ToString("N");

    protected override void ConfigureServices(IServiceCollection services)
    {
        var env = A.Fake<IHostEnvironment>();
        env.ApplicationName = appName;
        services.AddSingleton(env);
    }

    [Test]
    public void ShouldUseHostEnvironmentApplicationNameAsSourceFallback()
    {
        var subConfig = GetService<IOptions<SubConfig>>().Value;
        subConfig.FallbackSource.Should().Be(appName);
    }
}

public class ConfigurationTests : ServicesFixture
{
    readonly SubConfig randomConfig = AutoFaker.Generate<SubConfig>();

    protected override void ConfigureServices(IServiceCollection services)
    {
        var mockSettings = randomConfig.GetType()
            .GetProperties()
            .ToDictionary(
                p => $"Subdivisions:{p.Name}",
                p => p.GetValue(randomConfig)?.ToString()
            );

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(mockSettings)
            .Build();
        services.AddSingleton<IConfiguration>(_ => configuration!);
    }

    [Test]
    public void ShouldUseIConfigurationFromContainer()
    {
        var subConfig = GetService<IOptions<SubConfig>>().Value;
        subConfig.Should().BeEquivalentTo(randomConfig);
    }
}
