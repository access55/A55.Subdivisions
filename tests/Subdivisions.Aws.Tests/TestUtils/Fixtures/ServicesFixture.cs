using Amazon.Runtime;
using Bogus;
using Microsoft.Extensions.DependencyInjection;
using Subdivisions.Models;
using Subdivisions.Services;

namespace Subdivisions.Aws.Tests.TestUtils.Fixtures;

public class ServicesFixture
{
    protected static readonly Faker faker = new();

    protected readonly ISubClock fakeClock = A.Fake<ISubClock>();

    ServiceProvider serviceProvider = null!;

    [SetUp]
    public async Task OneTimeSetupServicesTest()
    {
        ClearEnv();
        await BeforeSetup();

        var services = CreateSubdivisionsServices(ConfigureSubdivisions);
        services.AddSingleton(fakeClock);
        ConfigureServices(services);
        serviceProvider = services.BuildServiceProvider();

        Fake.ClearConfiguration(fakeClock);
        Fake.ClearRecordedCalls(fakeClock);
    }

    public void ClearEnv()
    {
        Environment.SetEnvironmentVariable("SUBDIVISIONS_AWS_ACCESS_KEY_ID", null);
        Environment.SetEnvironmentVariable("SUBDIVISIONS_AWS_SECRET_ACCESS_KEY", null);
    }

    protected IServiceCollection CreateSubdivisionsServices(Action<SubConfig> configure) =>
        new ServiceCollection()
            .AddLogging()
            .AddSubdivisionsServices(
                credentials: new AnonymousAWSCredentials(),
                config: configure);

    protected virtual Task BeforeSetup() => Task.CompletedTask;

    protected virtual void ConfigureSubdivisions(SubConfig c)
    {
        c.Source = "test";
    }

    protected virtual void ConfigureServices(IServiceCollection services)
    {
    }

    [TearDown]
    public async Task OneTimeTearDownServicesTest() => await serviceProvider.DisposeAsync();

    public T GetService<T>() where T : notnull => serviceProvider.GetRequiredService<T>();
}
