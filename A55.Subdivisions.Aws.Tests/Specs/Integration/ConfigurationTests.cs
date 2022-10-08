using A55.Subdivisions.Aws.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace A55.Subdivisions.Aws.Tests.Specs.Integration;

public class ConfigurationTests : ServicesFixture
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
