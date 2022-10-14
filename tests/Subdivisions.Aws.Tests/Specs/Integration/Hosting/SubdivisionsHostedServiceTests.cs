using Amazon.EventBridge;
using Amazon.EventBridge.Model;
using Amazon.SimpleNotificationService;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Subdivisions.Aws.Tests.TestUtils;
using Subdivisions.Aws.Tests.TestUtils.Fixtures;
using Subdivisions.Extensions;
using Subdivisions.Hosting;
using Subdivisions.Hosting.Job;
using Subdivisions.Models;

namespace Subdivisions.Aws.Tests.Specs.Integration.Hosting;

public class SubdivisionsHostedServiceTests : LocalstackFixture
{
    ConsumerDescriber[] fakeConsumers = null!;

    protected override void ConfigureSubdivisions(SubConfig c)
    {
        base.ConfigureSubdivisions(c);
        c.Prefix = "";
        c.Source = "x";
    }

    protected override void ConfigureServices(IServiceCollection services)
    {
        fakeConsumers = Enumerable
            .Range(0, faker.Random.Int(3, 10))
            .Select(_ => new ConsumerDescriber(faker.TopicNameString(), typeof(FakeConsumer), typeof(string)))
            .ToArray();

        services
            .AddSingleton<IConsumerFactory, ConsumerFactory>()
            .AddSingleton<IConsumerJob, ConcurrentConsumerJob>()
            .AddHostedService<SubdivisionsHostedService>();

        foreach (var consumer in fakeConsumers)
            services
                .AddSingleton<IConsumerDescriber>(consumer);
    }

    [Test]
    public async Task ShouldCreateAllRules()
    {
        var hosted = (SubdivisionsHostedService)GetService<IHostedService>();

        await hosted.Bootstrap(default);

        var topics = fakeConsumers.Select(x => x.TopicName.ToPascalCase());

        var ev = GetService<IAmazonEventBridge>();
        var savedRules = await ev.ListRulesAsync(new ListRulesRequest());
        var rules = savedRules.Rules.Select(x => x.Name);

        rules.Should().BeEquivalentTo(topics);
    }

    [Test]
    public async Task ShouldCreateAllTopics()
    {
        var hosted = (SubdivisionsHostedService)GetService<IHostedService>();
        await hosted.Bootstrap(default);

        var topics = fakeConsumers.Select(x => x.TopicName.ToPascalCase());

        var sns = GetService<IAmazonSimpleNotificationService>();
        var savedRules = await sns.ListTopicsAsync();
        var snsTopics = savedRules.Topics.Select(x => x.TopicArn.Split(":").Last());

        snsTopics.Should().BeEquivalentTo(topics);
    }

    [Test]
    public async Task ShouldNotCreateAnyQueue()
    {
        var hosted = (SubdivisionsHostedService)GetService<IHostedService>();
        await hosted.Bootstrap(default);

        var normalQueues = fakeConsumers.Select(x => $"x_{x.TopicName}").ToArray();
        var expectedQs = normalQueues.Concat(normalQueues.Select(x => $"dead_letter_{x}"));

        var sqs = GetService<IAmazonSQS>();
        var queueUrls = await sqs.ListQueuesAsync(new ListQueuesRequest());
        var queues = queueUrls.QueueUrls.Select(Path.GetFileName);

        queues.Should().BeEquivalentTo(expectedQs);
    }
}
