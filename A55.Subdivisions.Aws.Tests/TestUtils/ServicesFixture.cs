using A55.Subdivisions.Aws.Extensions;
using A55.Subdivisions.Aws.Models;
using Amazon.Runtime;
using Bogus;
using Microsoft.Extensions.DependencyInjection;

namespace A55.Subdivisions.Aws.Tests.TestUtils;

public class ServicesFixture
{
    protected static readonly Faker faker = new("pt_BR");

    protected readonly ISubClock fakeClock = A.Fake<ISubClock>();

    protected readonly SubConfig config = new()
    {
        PubKey = $"alias/{faker.Random.Replace("Key????")}",
        MessageDelayInSeconds = faker.Random.Int(0, 60),
        MessageTimeoutInSeconds = faker.Random.Int(4, 60),
        MessageRetantionInDays = faker.Random.Int(4, 10),
        QueueMaxReceiveCount = faker.Random.Int(5, 10),
        Source = faker.Internet.DomainWord()
    };

    ServiceProvider serviceProvider = null!;

    [SetUp]
    public async Task OneTimeSetupServicesTest()
    {
        await BeforeSetup();

        var services =
            new ServiceCollection()
                .AddLogging()
                .AddSubdivisionsClient(
                    credentials: new AnonymousAWSCredentials(),
                    config: c =>
                    {
                        c.PubKey = config.PubKey;
                        c.Source = config.Source;
                        c.MessageDelayInSeconds = config.MessageDelayInSeconds;
                        c.MessageTimeoutInSeconds = config.MessageTimeoutInSeconds;
                        c.MessageRetantionInDays = config.MessageRetantionInDays;
                        c.QueueMaxReceiveCount = config.QueueMaxReceiveCount;
                        ConfigureSubdivisions(c);
                    });

        services.AddSingleton(fakeClock);
        ConfigureServices(services);
        serviceProvider = services.BuildServiceProvider();

        Fake.ClearConfiguration(fakeClock);
        Fake.ClearRecordedCalls(fakeClock);
    }

    public virtual Task BeforeSetup() => Task.CompletedTask;

    public virtual void ConfigureSubdivisions(SubConfig subConfig)
    {
    }

    public virtual void ConfigureServices(IServiceCollection services)
    {
    }

    [TearDown]
    public async Task OneTimeTearDownServicesTest() => await serviceProvider.DisposeAsync();

    public T GetService<T>() where T : notnull => serviceProvider.GetRequiredService<T>();
}
