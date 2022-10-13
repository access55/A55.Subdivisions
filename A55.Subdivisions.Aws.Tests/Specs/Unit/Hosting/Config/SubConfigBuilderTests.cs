using A55.Subdivisions.Aws.Tests.Builders;
using A55.Subdivisions.Aws.Tests.TestUtils.Fixtures;
using A55.Subdivisions.Hosting;
using A55.Subdivisions.Hosting.Config;
using A55.Subdivisions.Models;
using AutoBogus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace A55.Subdivisions.Aws.Tests.Specs.Unit.Hosting.Config;

public class SubConfigBuilderTests : BaseTest
{
    [Test]
    public async Task ShouldMapProducers()
    {
        var services = new ServiceCollection();
        var client = A.Fake<IProducerClient>();
        services.AddSingleton(client);

        var config = new SubConfigBuilder(services);
        config.MapTopic<TestMessage>("test-topic");

        var sp = services.BuildServiceProvider();
        var producer = sp.GetRequiredService<IProducer<TestMessage>>();

        var message = TestMessage.New();
        await producer.Publish(message);

        A.CallTo(() => client.Publish("test-topic", message, default))
            .MustHaveHappened();
    }

    [Test]
    public void ShouldMapConsumers()
    {
        var services = new ServiceCollection();

        var polling = faker.Date.Timespan();
        var concurrency = faker.Random.Int(1);

        services.AddSubdivisions(sub =>
        {
            sub.MapTopic<TestMessage>("test_topic")
                .WithConsumer<TestConsumer>()
                .Configure(polling, concurrency);
        });

        var sp = services.BuildServiceProvider();
        var describers = sp.GetService<IEnumerable<IConsumerDescriber>>();
        describers.Should().BeEquivalentTo(new[]
        {
            new ConsumerDescriber("test_topic", typeof(TestConsumer), typeof(TestMessage),
                new ConsumerConfig {MaxConcurrency = concurrency, PollingInterval = polling})
        });
    }

    [Test]
    public void ShouldCopyConfiguration()
    {
        var services = new ServiceCollection();

        var fakeConfig = AutoFaker.Generate<SubConfig>();

        services.AddSubdivisions(sub =>
        {
            sub.MapTopic<TestMessage>("test_topic");

            sub.Suffix = fakeConfig.Suffix;
            sub.Prefix = fakeConfig.Prefix;
            sub.Source = fakeConfig.Source;
            sub.QueueMaxReceiveCount = fakeConfig.QueueMaxReceiveCount;
            sub.RetriesBeforeDeadLetter = fakeConfig.RetriesBeforeDeadLetter;
            sub.PubKey = fakeConfig.PubKey;
            sub.MessageRetantionInDays = fakeConfig.MessageRetantionInDays;
            sub.MessageTimeoutInSeconds = fakeConfig.MessageTimeoutInSeconds;
            sub.MessageDelayInSeconds = fakeConfig.MessageDelayInSeconds;
            sub.PollingIntervalInSeconds = fakeConfig.PollingIntervalInSeconds;
            sub.ServiceUrl = fakeConfig.ServiceUrl;
            sub.Localstack = fakeConfig.Localstack;
            sub.AutoCreateNewTopic = fakeConfig.AutoCreateNewTopic;
            sub.Region = fakeConfig.Region;
            sub.LongPollingWaitInSeconds = fakeConfig.LongPollingWaitInSeconds;
        });

        var sp = services.BuildServiceProvider();
        var config = sp.GetRequiredService<IOptions<SubConfig>>().Value;
        config.Should().BeEquivalentTo(fakeConfig);
    }
}
