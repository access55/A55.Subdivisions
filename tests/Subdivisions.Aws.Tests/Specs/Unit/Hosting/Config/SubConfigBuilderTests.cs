using Amazon.Runtime;
using AutoBogus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Subdivisions.Aws.Tests.Builders;
using Subdivisions.Aws.Tests.TestUtils.Fixtures;
using Subdivisions.Hosting;
using Subdivisions.Hosting.Config;
using Subdivisions.Models;

namespace Subdivisions.Aws.Tests.Specs.Unit.Hosting.Config;

public class SubConfigBuilderTests : BaseTest
{
    [Test]
    public async Task ShouldMapProducers()
    {
        var services = new ServiceCollection();
        var client = A.Fake<IProducerClient>();
        services
            .AddSingleton(client)
            .AddSingleton(Options.Create(new SubConfig { CompressMessages = false }));

        var config = new SubConfigBuilder(services);
        config.MapTopic<TestMessage>("test-topic");

        var sp = services.BuildServiceProvider();
        var producer = sp.GetRequiredService<IProducer<TestMessage>>();

        var message = TestMessage.New();
        await producer.Publish(message);

        A.CallTo(() => client.Publish("test-topic", message, null, false, default))
            .MustHaveHappened();
    }

    [Test]
    public async Task ShouldMapProducersWithCompression()
    {
        var services = new ServiceCollection();
        var client = A.Fake<IProducerClient>();
        services
            .AddSingleton(client)
            .AddSingleton(Options.Create(new SubConfig { CompressMessages = false }));

        var config = new SubConfigBuilder(services);
        config.MapTopic<TestMessage>("test-topic")
            .EnableCompression();

        var sp = services.BuildServiceProvider();
        var producer = sp.GetRequiredService<IProducer<TestMessage>>();

        var message = TestMessage.New();
        await producer.Publish(message);
        A.CallTo(() => client.Publish("test-topic", message, null, true, default))
            .MustHaveHappened();
    }

    [Test]
    public async Task ShouldMapProducersWithCompressionSettings()
    {
        var services = new ServiceCollection();
        var client = A.Fake<IProducerClient>();
        services
            .AddSingleton(client)
            .AddSingleton(Options.Create(new SubConfig { CompressMessages = true }));

        var config = new SubConfigBuilder(services);
        config.MapTopic<TestMessage>("test-topic");

        var sp = services.BuildServiceProvider();
        var producer = sp.GetRequiredService<IProducer<TestMessage>>();

        var message = TestMessage.New();
        await producer.Publish(message);
        A.CallTo(() => client.Publish("test-topic", message, null, true, default))
            .MustHaveHappened();
    }

    [Test]
    public async Task ShouldMapProducersDisablingCompression()
    {
        var services = new ServiceCollection();
        var client = A.Fake<IProducerClient>();
        services
            .AddSingleton(client)
            .AddSingleton(Options.Create(new SubConfig { CompressMessages = true }));

        var config = new SubConfigBuilder(services);
        config.MapTopic<TestMessage>("test-topic")
            .DisableCompression();

        var sp = services.BuildServiceProvider();
        var producer = sp.GetRequiredService<IProducer<TestMessage>>();

        var message = TestMessage.New();
        await producer.Publish(message);
        A.CallTo(() => client.Publish("test-topic", message, null, false, default))
            .MustHaveHappened();
    }

    [Test]
    public void ShouldMapDelegateConsumers()
    {
        var services = new ServiceCollection();

        var polling = faker.Date.Timespan();
        var concurrency = faker.Random.Int(1);

        services.AddSubdivisions(sub =>
        {
            sub.Source = "source";
            sub.MapTopic<TestMessage>("test_topic")
                .WithConsumer((TestMessage message) => { })
                .Configure(polling, concurrency);
        });

        var sp = services.BuildServiceProvider();
        var describers = sp.GetService<IEnumerable<IConsumerDescriber>>();
        describers.Should().BeEquivalentTo(new[]
        {
            new ConsumerDescriber("test_topic", typeof(DelegateConsumer<TestMessage>), typeof(TestMessage),
                new ConsumerConfig {MaxConcurrency = concurrency, PollingInterval = polling})
        });
    }

    [Test]
    public void ShouldThrowMapDelegateConsumersWithoutMessageParam()
    {
        var services = new ServiceCollection();

        var polling = faker.Date.Timespan();
        var concurrency = faker.Random.Int(1);

        var action = () =>
            services.AddSubdivisions(sub =>
            {
                sub.MapTopic<TestMessage>("test_topic")
                    .WithConsumer(() => { })
                    .Configure(polling, concurrency);
            });

        action.Should()
            .Throw<SubdivisionsException>()
            .WithMessage("No parameter of type*");
    }

    [Test]
    public void ShouldMapConsumers()
    {
        var services = new ServiceCollection();

        var polling = faker.Date.Timespan();
        var concurrency = faker.Random.Int(1);

        services.AddSubdivisions(sub =>
        {
            sub.Source = "source";
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
            sub.MessageRetentionInDays = fakeConfig.MessageRetentionInDays;
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

    [Test]
    public void ShouldConfigureLocalStack()
    {
        var services = new ServiceCollection();

        services.AddSubdivisions(sub =>
        {
            sub.MapTopic<TestMessage>("test_topic");
            sub.Source = "app";
            sub.Localstack = true;
        });

        var sp = services.BuildServiceProvider();
        var credentials = sp.GetService<SubAwsCredentialWrapper>();
        credentials!.Credentials.Should().BeOfType<AnonymousAWSCredentials>();

        var config = sp.GetRequiredService<IOptions<SubConfig>>().Value;
        config.ServiceUrl.Should().Be("http://localhost:4566");
    }

    [Test]
    public void ShouldConfigureLocalStackWhenConfiguration()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?> { ["Subdivisions:Localstack"] = "true", ["Subdivisions:Source"] = "app" })
            .Build();
        var services = new ServiceCollection()
            .AddSingleton<IConfiguration>(_ => configuration!)
            .AddSubdivisions();

        var sp = services.BuildServiceProvider();
        var credentials = sp.GetRequiredService<SubAwsCredentialWrapper>().Credentials;
        credentials.Should().BeOfType<AnonymousAWSCredentials>();

        var config = sp.GetRequiredService<IOptions<SubConfig>>().Value;
        config.ServiceUrl.Should().Be("http://localhost:4566");
    }
}
