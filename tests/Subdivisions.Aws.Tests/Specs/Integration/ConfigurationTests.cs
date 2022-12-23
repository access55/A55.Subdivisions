using AutoBogus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Subdivisions.Aws.Tests.TestUtils.Fixtures;
using Subdivisions.Models;

namespace Subdivisions.Aws.Tests.Specs.Integration;

public class ConfigurationHostTests : ServicesFixture
{
    readonly string appName = Guid.NewGuid().ToString("N");

    protected override void ConfigureSubdivisions(SubConfig c) { }

    protected override void ConfigureServices(IServiceCollection services)
    {
        var env = A.Fake<IHostEnvironment>();
        env.ApplicationName = appName;
        services.AddSingleton(env);
    }

    [Test]
    public void ShouldInferSourceNameByAssembly()
    {
        var subConfig = GetService<IOptions<SubConfig>>().Value;
        subConfig.Source.Should().Be(appName);
    }

    [Test]
    public void ShouldFallbackRegionToEnvironmentVariable()
    {
        var region = faker.Address.State();
        Environment.SetEnvironmentVariable("SUBDIVISIONS_AWS_REGION", region, EnvironmentVariableTarget.Process);
        var subConfig = GetService<IOptions<SubConfig>>().Value;
        subConfig.Region.Should().Be(region);
    }

    [TearDown]
    public void TearDown() =>
        Environment.SetEnvironmentVariable("SUBDIVISIONS_AWS_REGION", null, EnvironmentVariableTarget.Process);
}

public class ConfigurationTests : ServicesFixture
{
    readonly SubConfig randomConfig = new AutoFaker<SubConfig>()
        .RuleFor(x => x.Region, "us-east-1")
        .Generate();

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

    protected override void ConfigureSubdivisions(SubConfig c) { }

    [Test]
    public void ShouldUseIConfigurationFromContainer()
    {
        var subConfig = GetService<IOptions<SubConfig>>().Value;
        subConfig.Should().BeEquivalentTo(randomConfig);
    }
}
