using A55.Subdivisions.Aws.Extensions;
using Amazon.KeyManagementService;
using Amazon.KeyManagementService.Model;
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
    public async Task OneTimeSetupLocalStackTest()
    {
        config = new()
        {
            PubKey = $"alias/{Faker.Random.Replace("Key????")}",
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
                .AddSubdivisionsClient(credentials: new AnonymousAWSCredentials(),
                    config: c =>
                    {
                        c.ServiceUrl = localstack.Url;
                        c.PubKey = config.PubKey;
                        c.MessageDelayInSeconds = config.MessageDelayInSeconds;
                        c.MessageTimeoutInSeconds = config.MessageTimeoutInSeconds;
                        c.MessageRetantionInDays = config.MessageRetantionInDays;
                        c.QueueMaxReceiveCount = config.QueueMaxReceiveCount;
                    });

        serviceProvider = services.BuildServiceProvider();
    }

    [TearDown]
    public async Task OneTimeTearDownLocalstackTest()
    {
        await localstack.DisposeAsync();
        await serviceProvider.DisposeAsync();
    }

    public T GetService<T>() where T : notnull => serviceProvider.GetRequiredService<T>();

    protected async Task<string> CreateDefaultKmsKey()
    {
        var kms = GetService<IAmazonKeyManagementService>();
        var key = await kms.CreateKeyAsync(new() {Description = "Test key"});
        await kms.CreateAliasAsync(new CreateAliasRequest
        {
            AliasName = config.PubKey, TargetKeyId = key.KeyMetadata.KeyId
        });
        return key.KeyMetadata.KeyId;
    }
}
