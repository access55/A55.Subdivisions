using Amazon.Runtime;
using AutoBogus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Subdivisions.Aws.Tests.Builders;
using Subdivisions.Aws.Tests.TestUtils;
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
            .AddSingleton(Options.Create(new SubConfig()));

        var config = new SubConfigBuilder(services);
        config.MapTopic<TestMessage>("test-topic");

        var sp = services.BuildServiceProvider();
        var producer = sp.GetRequiredService<IProducer<TestMessage>>();

        var message = TestMessage.New();
        await producer.TryPublish(message);

        A.CallTo(() =>
                client.Publish("test-topic", message, null, A<ProduceOptions>._, default))
            .MustHaveHappened();
    }

    [Test]
    public void ShouldMapDelegateConsumers()
    {
        var services = new ServiceCollection();

        var polling = faker.Date.Timespan();
        var timeout = faker.Date.Timespan();
        var concurrency = faker.Random.Int(1);

        services.AddSubdivisions(sub =>
        {
            sub.Source = "source";
            sub.MapTopic<TestMessage>("test_topic")
                .WithConsumer((TestMessage message) => { })
                .Configure(polling, concurrency)
                .WithTimeout(timeout);
        });

        var sp = services.BuildServiceProvider();
        var describers = sp.GetService<IEnumerable<IConsumerDescriber>>();
        describers.Should().BeEquivalentTo(new[]
        {
            new ConsumerDescriber("test_topic", typeof(DelegateConsumer<TestMessage>),
                typeof(TestMessage),
                new ConsumerConfig
                {
                    MaxConcurrency = concurrency,
                    PollingInterval = polling,
                    ConsumeTimeout = timeout,
                    NameOverride = new(),
                })
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
        var timeout = faker.Date.Timespan();
        var concurrency = faker.Random.Int(1);

        services.AddSubdivisions(sub =>
        {
            sub.Source = "source";
            sub.MapTopic<TestMessage>("test_topic")
                .WithConsumer<TestConsumer>()
                .WithTimeout(timeout)
                .Configure(polling, concurrency);
        });

        var sp = services.BuildServiceProvider();
        var describers = sp.GetService<IEnumerable<IConsumerDescriber>>();
        describers.Should().BeEquivalentTo(new[]
        {
            new ConsumerDescriber("test_topic", typeof(TestConsumer), typeof(TestMessage),
                new ConsumerConfig
                {
                    MaxConcurrency = concurrency,
                    PollingInterval = polling,
                    ConsumeTimeout = timeout,
                    NameOverride = new(),
                })
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
            sub.RaiseExceptions = fakeConfig.RaiseExceptions;
            sub.MapConsumerEndpoints = fakeConfig.MapConsumerEndpoints;
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
                new Dictionary<string, string?>
                {
                    ["Subdivisions:Localstack"] = "true",
                    ["Subdivisions:Source"] = "app"
                })
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

    [Test]
    public async Task ShouldMapProducersWithCustomPrefix()
    {
        var services = new ServiceCollection();
        var client = A.Fake<IProducerClient>();
        services.AddSingleton(client)
            .AddSingleton(Options.Create(new SubConfig()));

        var prefix = faker.Random.Word().ToLower();
        var config = new SubConfigBuilder(services);
        config.MapTopic<TestMessage>("test-topic")
            .WithPrefix(prefix);

        var sp = services.BuildServiceProvider();
        var producer = sp.GetRequiredService<IProducer<TestMessage>>();

        var message = TestMessage.New();
        await producer.TryPublish(message);
        A.CallTo(() => client.Publish(
                "test-topic", message, null,
                A<ProduceOptions>.That.IsEquivalentTo(
                    new ProduceOptions
                    {
                        NameOverride = new(null, prefix)
                    }),
                default))
            .MustHaveHappened();
    }

    [Test]
    public async Task ShouldMapProducersWithCustomSuffix()
    {
        var services = new ServiceCollection();
        var client = A.Fake<IProducerClient>();
        services.AddSingleton(client)
            .AddSingleton(Options.Create(new SubConfig()));

        var suffix = faker.Random.Word().ToLower();
        var config = new SubConfigBuilder(services);
        config.MapTopic<TestMessage>("test-topic")
            .WithSuffix(suffix);

        var sp = services.BuildServiceProvider();
        var producer = sp.GetRequiredService<IProducer<TestMessage>>();

        var message = TestMessage.New();
        await producer.TryPublish(message);
        A.CallTo(() => client.Publish(
                "test-topic", message, null,
                A<ProduceOptions>.That.IsEquivalentTo(
                    new ProduceOptions
                    {
                        NameOverride = new(suffix, null)
                    }),
                default))
            .MustHaveHappened();
    }

    [Test]
    public async Task ShouldMapProducersWithCustomSuffixAndPrefix()
    {
        var services = new ServiceCollection();
        var client = A.Fake<IProducerClient>();
        services.AddSingleton(client)
            .AddSingleton(Options.Create(new SubConfig()));

        var suffix = faker.Random.Word().ToLower();
        var prefix = faker.Random.Word().ToLower();
        var config = new SubConfigBuilder(services);
        config.MapTopic<TestMessage>("test-topic")
            .WithPrefix(prefix)
            .WithSuffix(suffix);

        var sp = services.BuildServiceProvider();
        var producer = sp.GetRequiredService<IProducer<TestMessage>>();

        var message = TestMessage.New();
        await producer.TryPublish(message);
        A.CallTo(() => client.Publish(
                "test-topic", message, null,
                A<ProduceOptions>.That.IsEquivalentTo(
                    new ProduceOptions
                    {
                        NameOverride = new(suffix, prefix)
                    }),
                default))
            .MustHaveHappened();
    }

    [Test]
    public async Task ShouldMapProducersWithCleanSuffixAndPrefix()
    {
        var services = new ServiceCollection();
        var client = A.Fake<IProducerClient>();
        services.AddSingleton(client)
            .AddSingleton(Options.Create(new SubConfig()));

        var config = new SubConfigBuilder(services);
        config.MapTopic<TestMessage>("test-topic")
            .RawTopicName();

        var sp = services.BuildServiceProvider();
        var producer = sp.GetRequiredService<IProducer<TestMessage>>();

        var message = TestMessage.New();
        await producer.TryPublish(message);
        A.CallTo(() => client.Publish(
                "test-topic", message, null,
                A<ProduceOptions>.That.IsEquivalentTo(
                    new ProduceOptions
                    {
                        NameOverride = new("", "")
                    }),
                default))
            .MustHaveHappened();
    }

    [Test]
    public void ShouldAddProducerDescribers()
    {
        var services = new ServiceCollection();

        services.AddSubdivisions(sub =>
        {
            sub.Source = "source";
            sub.MapTopic<TestMessage>("test_topic")
                .WithPrefix("foo")
                .WithSuffix("bar");
        });

        var sp = services.BuildServiceProvider();
        var describers = sp.GetService<IEnumerable<IProducerDescriber>>();
        describers.Should().BeEquivalentTo(new[]
        {
            new ProducerDescriber(
                "test_topic",
                typeof(TestMessage),
                new TopicNameOverride
                {
                    Prefix = "foo",
                    Suffix = "bar"
                })
        });
    }
}
