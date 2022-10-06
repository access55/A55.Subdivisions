using Amazon.Runtime;
using Bogus;
using DotNet.Testcontainers.Builders;
using Microsoft.Extensions.DependencyInjection;

[assembly: LevelOfParallelism(5)]

namespace A55.Subdivisions.Aws.Tests.TestUtils;

[Parallelizable(ParallelScope.Self)]
public class LocalstackTest
{
    LocalStackTestcontainer localstack = null!;
    ServiceProvider serviceProvider = null!;

    protected SubConfig config = null!;

    protected Faker Faker = new("pt_BR");

    [SetUp]
    public async Task SetupLocalStackTest()
    {
        config = new()
        {
            PubKey = $"alias/{Faker.Random.Word()}",
        };

        localstack = new TestcontainersBuilder<LocalStackTestcontainer>()
            .WithMessageBroker(new LocalStackTestcontainerConfiguration())
            .Build();
        await localstack.StartAsync();

        var services =
            new ServiceCollection()
                .AddLogging()
                .AddSubdivisions(credentials: new AnonymousAWSCredentials(), serviceUrl: localstack.Url,
                    config: c => { c.PubKey = config.PubKey; });

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