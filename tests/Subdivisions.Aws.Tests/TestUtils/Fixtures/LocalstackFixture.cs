using Amazon.KeyManagementService;
using Amazon.KeyManagementService.Model;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Subdivisions.Clients;
using Subdivisions.Extensions;
using Subdivisions.Models;

[assembly: LevelOfParallelism(3)]

namespace Subdivisions.Aws.Tests.TestUtils.Fixtures;

public class LocalStackConfiguration : TestcontainerMessageBrokerConfiguration
{
    const string LocalStackImage = "localstack/localstack:1.1.0";
    const int LocalStackPort = 4566;

    public LocalStackConfiguration()
        : this(LocalStackImage)
    {
    }

    public LocalStackConfiguration(string image)
        : base(image, LocalStackPort)
    {
        Environments.Add("EXTERNAL_SERVICE_PORTS_START", "4510");
        Environments.Add("EXTERNAL_SERVICE_PORTS_END", "4559");
    }

    public override IWaitForContainerOS WaitStrategy => Wait.ForUnixContainer()
        .UntilPortIsAvailable(LocalStackPort);
}

[Parallelizable(ParallelScope.Self)]
public class LocalstackFixture : ServicesFixture
{
    protected SubConfig config = null!;
    protected string kmsTestKeyId = "";
    LocalStackTestcontainer localstack = null!;

    string ServiceUrl
    {
        get
        {
            var builder = new UriBuilder(localstack.ConnectionString) { Scheme = Uri.UriSchemeHttp };
            return builder.Uri.ToString();
        }
    }

    protected override async Task BeforeSetup()
    {
        localstack = new TestcontainersBuilder<LocalStackTestcontainer>()
            .WithMessageBroker(new LocalStackConfiguration())
            .WithWaitStrategy(
                Wait.ForUnixContainer()
                    .UntilPortIsAvailable(4566))
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
        c.MapConsumerEndpoints = false;
        c.RethrowExceptions = true;
        c.CompressMessages = false;
    }

    [TearDown]
    public async Task LocalstackTearDown() => await localstack.DisposeAsync();

    [SetUp]
    public async Task LocalstackSetup()
    {
        config = GetService<IOptions<SubConfig>>().Value;
        GetService<AwsSqs>().ClearCache();
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
