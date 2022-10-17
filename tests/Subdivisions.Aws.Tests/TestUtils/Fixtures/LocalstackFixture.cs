using Amazon.KeyManagementService;
using Amazon.KeyManagementService.Model;
using DotNet.Testcontainers.Builders;
using Microsoft.Extensions.Options;
using Subdivisions.Extensions;
using Subdivisions.Models;

[assembly: LevelOfParallelism(5)]

namespace Subdivisions.Aws.Tests.TestUtils.Fixtures;

[Parallelizable(ParallelScope.Self)]
public class LocalstackFixture : ServicesFixture
{
    protected SubConfig config = null!;
    protected string kmsTestKeyId = "";
    LocalStackTestcontainer localstack = null!;

    protected string ServiceUrl => localstack.Url;

    protected override async Task BeforeSetup()
    {
        localstack = new TestcontainersBuilder<LocalStackTestcontainer>()
            .WithMessageBroker(new LocalStackTestcontainerConfiguration())
            .Build();

        await localstack.StartAsync();
    }

    protected override void ConfigureSubdivisions(SubConfig c)
    {
        c.ServiceUrl = ServiceUrl;

        c.PubKey = $"alias/{faker.Random.Replace("Key?##?")}";
        c.Prefix = faker.Random.Replace("?##");
        c.Source = faker.Person.LastName.OnlyLetterOrDigit().ToPascalCase();

        c.QueueMaxReceiveCount = 10;
        c.MessageRetentionInDays = 1;
        c.MessageTimeoutInSeconds = 10000;
        c.RetriesBeforeDeadLetter = 2;

        c.MessageDelayInSeconds = 0;
        c.LongPollingWaitInSeconds = 0;
    }

    [SetUp]
    public async Task LocalstackSetup()
    {
        config = GetService<IOptions<SubConfig>>().Value;
        kmsTestKeyId = await CreateDefaultKmsKey();
    }

    async Task<string> CreateDefaultKmsKey()
    {
        var kms = GetService<IAmazonKeyManagementService>();
        var key = await kms.CreateKeyAsync(new() { Description = "Test key" });
        await kms.CreateAliasAsync(new CreateAliasRequest
        {
            AliasName = config.PubKey,
            TargetKeyId = key.KeyMetadata.KeyId
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
            next ?? TimeSpan.FromMilliseconds(500)
        );
}
