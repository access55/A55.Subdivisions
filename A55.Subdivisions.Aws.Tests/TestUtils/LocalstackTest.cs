using Amazon.Runtime;
using Bogus;
using DotNet.Testcontainers.Builders;
using Microsoft.Extensions.DependencyInjection;

[assembly: LevelOfParallelism(5)]

namespace A55.Subdivisions.Aws.Tests.TestUtils;

[Parallelizable(ParallelScope.Self)]
public class LocalstackTest
{
    protected SubConfig config = null!;

    protected Faker Faker = new("pt_BR");
    LocalStackTestcontainer localstack = null!;
    ServiceProvider serviceProvider = null!;

    [SetUp]
    public async Task SetupLocalStackTest()
    {
        config = new()
        {
            PubKey = $"alias/{Faker.Random.Word()}",
            MessageDelayInSeconds = Faker.Random.Int(0, 60),
            MessageTimeoutInSeconds = Faker.Random.Int(4, 60),
            MessageRetantionInDays = Faker.Random.Int(4, 10),
            QueueMaxReceiveCount = Faker.Random.Int(5, 10)
        };

        localstack = new TestcontainersBuilder<LocalStackTestcontainer>()
            .WithMessageBroker(new LocalStackTestcontainerConfiguration())
            .Build();
        await localstack.StartAsync();

        var services =
            new ServiceCollection()
                .AddLogging()
                .AddSubdivisions(credentials: new AnonymousAWSCredentials(), serviceUrl: localstack.Url,
                    config: c =>
                    {
                        c.PubKey = config.PubKey;
                        c.MessageDelayInSeconds = config.MessageDelayInSeconds;
                        c.MessageTimeoutInSeconds = config.MessageTimeoutInSeconds;
                        c.MessageRetantionInDays = config.MessageRetantionInDays;
                        c.QueueMaxReceiveCount = config.QueueMaxReceiveCount;
                    });

        serviceProvider = services.BuildServiceProvider();
    }

    [TearDown]
    public async Task TearDownLocalstackTest()
    {
        await localstack.DisposeAsync();
        await serviceProvider.DisposeAsync();
    }

    public T GetService<T>() where T : notnull => serviceProvider.GetRequiredService<T>();
}
