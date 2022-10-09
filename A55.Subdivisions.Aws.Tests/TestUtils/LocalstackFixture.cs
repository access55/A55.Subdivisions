using A55.Subdivisions.Aws.Models;
using Amazon.KeyManagementService;
using Amazon.KeyManagementService.Model;
using DotNet.Testcontainers.Builders;
using Microsoft.Extensions.Options;

[assembly: LevelOfParallelism(5)]

namespace A55.Subdivisions.Aws.Tests.TestUtils;

[Parallelizable(ParallelScope.Self)]
public class LocalstackFixture : ServicesFixture
{
    LocalStackTestcontainer localstack = null!;
    protected SubConfig config = null!;

    protected override async Task BeforeSetup()
    {
        localstack = new TestcontainersBuilder<LocalStackTestcontainer>()
            .WithMessageBroker(new LocalStackTestcontainerConfiguration())
            .Build();

        await localstack.StartAsync();
    }

    protected override void ConfigureSubdivisions(SubConfig c)
    {
        c.ServiceUrl = localstack.Url;
        c.PubKey = $"alias/{faker.Random.Replace("Key????")}";
        c.MessageDelayInSeconds = faker.Random.Int(0, 60);
        c.MessageTimeoutInSeconds = faker.Random.Int(4, 60);
        c.MessageRetantionInDays = faker.Random.Int(4, 10);
        c.QueueMaxReceiveCount = faker.Random.Int(5, 10);
        c.Prefix = "The";
        c.Source = "Test";
    }

    [SetUp]
    public void LocalstackSetup() => config = GetService<IOptions<SubConfig>>().Value;

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

    public async Task WaitFor(Func<Task<bool>> checkTask, TimeSpan timeout, TimeSpan next)
    {
        async Task WaitLoop()
        {
            while (!await checkTask())
                await Task.Delay(next);
        }

        await WaitLoop().WaitAsync(timeout);
    }

    public Task WaitFor(Func<Task<bool>> checkTask, TimeSpan? timeout = null, TimeSpan? next = null) =>
        WaitFor(
            checkTask,
            timeout ?? TimeSpan.FromSeconds(5000),
            next ?? TimeSpan.FromSeconds(1)
        );
}
