using AutoBogus;
using Bogus;
using Microsoft.Extensions.DependencyInjection;

namespace Subdivisions.Testing.Tests;

public class FakeBrokerAssertionsTests
{
    IServiceProvider provider = null!;
    IFakeBroker broker = null!;

    [SetUp]
    public void Setup()
    {
        provider = new ServiceCollection()
            .AddLogging()
            .AddSubdivisions(sub =>
            {
                sub.Source = "test";

                sub.MapTopic<MyMessage1>("my_topic");

                sub.MapTopic<MyMessage2>("my_recur_topic")
                    .WithConsumer(async (MyMessage2 message, IProducer<MyMessage1> producer) =>
                    {
                        var message2 = new MyMessage1 { Id = message.Id, Foo = message.Bar, };
                        await producer.Publish(message2);
                    });
            })
            .MockSubdivisions()
            .BuildServiceProvider();

        broker = provider.GetRequiredService<IFakeBroker>();
    }

    [Test]
    public async Task ShouldCheckProducedMessage()
    {
        var publisher = provider.GetRequiredService<IProducer<MyMessage1>>();

        var message = AutoFaker.Generate<MyMessage1>();
        await publisher.Publish(message);

        broker.Should().HaveAnyMessage("my_topic");
    }

    [Test]
    public void ShouldThrowIfHaveNoProducedMessage()
    {
        var action = () => broker.Should().HaveAnyMessage("my_topic");
        action.Should().Throw<AssertionException>()
            .WithMessage(@"Expected ""my_topic"" to contain messages, but not found any.");
    }

    [Test]
    public async Task ShouldCheckForProducedMessageBody()
    {
        var publisher = provider.GetRequiredService<IProducer<MyMessage1>>();

        var message = AutoFaker.Generate<MyMessage1>();
        await publisher.Publish(message);

        broker.Should().ContainMessage("my_topic", message.ToJson());
    }

    [Test]
    public async Task ShouldCheckForSerializedProducedMessage()
    {
        var publisher = provider.GetRequiredService<IProducer<MyMessage1>>();

        var message = AutoFaker.Generate<MyMessage1>();
        await publisher.Publish(message);

        broker.Should().ContainMessage("my_topic", new { id = message.Id, foo = message.Foo });
    }

    [Test]
    public async Task ShouldCheckForNotProducedMessageBody()
    {
        var publisher = provider.GetRequiredService<IProducer<MyMessage1>>();

        var message = AutoFaker.Generate<MyMessage1>();
        await publisher.Publish(message);

        var action = () => broker.Should().NotContainMessage("my_topic", message.ToJson());
        action.Should().Throw<AssertionException>();

        var message2 = AutoFaker.Generate<MyMessage1>();
        broker.Should().NotContainMessage("my_topic", message2.ToJson());
    }

    [Test]
    public async Task ShouldCheckForNotSerializedProducedMessage()
    {
        var publisher = provider.GetRequiredService<IProducer<MyMessage1>>();

        var message = AutoFaker.Generate<MyMessage1>();
        await publisher.Publish(message);

        var action = () => broker.Should().NotContainMessage("my_topic", new { id = message.Id, foo = message.Foo });
        action.Should().Throw<AssertionException>();

        var message2 = AutoFaker.Generate<MyMessage1>();
        broker.Should().NotContainMessage("my_topic", new { id = message2.Id, foo = message2.Foo });
    }

    [Test]
    public async Task ShouldCheckForPartialMessage()
    {
        var publisher = provider.GetRequiredService<IProducer<MyMessage1>>();

        var message = AutoFaker.Generate<MyMessage1>();
        await publisher.Publish(message);

        broker.Should().ContainsEquivalentMessage("my_topic", new { id = message.Id });
    }

    [Test]
    public async Task ShouldCheckForPartialJsonMessage()
    {
        var publisher = provider.GetRequiredService<IProducer<MyMessage1>>();

        var message = AutoFaker.Generate<MyMessage1>();
        await publisher.Publish(message);

        broker.Should().ContainsEquivalentMessage("my_topic", $@"{{""id"":""{message.Id}""}}");
    }

    [Test]
    public async Task ShouldThrowIfHaveNoProducedJsonMessageOnTopic()
    {
        var message1 = AutoFaker.Generate<MyMessage1>();
        var message2 = AutoFaker.Generate<MyMessage1>();
        var publisher = provider.GetRequiredService<IProducer<MyMessage1>>();
        await publisher.Publish(message1);

        var action = () => broker.Should().ContainMessage("my_topic", message2.ToJson());

        action.Should().Throw<AssertionException>();
    }

    [Test]
    public async Task ShouldThrowIfHaveNoProducedMessageOnTopic()
    {
        var message1 = AutoFaker.Generate<MyMessage1>();
        var message2 = AutoFaker.Generate<MyMessage1>();
        var publisher = provider.GetRequiredService<IProducer<MyMessage1>>();
        await publisher.Publish(message1);

        var action = () => broker.Should().ContainMessage("my_topic", message2);
        action.Should().Throw<AssertionException>();
    }

    [Test]
    public async Task ShouldThrowIfHaveNoProducedAnonymousMessageOnTopic()
    {
        var message1 = AutoFaker.Generate<MyMessage1>();
        var message2 = AutoFaker.Generate<MyMessage1>();
        var publisher = provider.GetRequiredService<IProducer<MyMessage1>>();
        await publisher.Publish(message1);

        var action = () => broker.Should().ContainMessage("my_topic", new { id = message2.Id, foo = message2.Foo });

        action.Should().Throw<AssertionException>();
    }

    [Test]
    public async Task ShouldCheckForProducedMessages()
    {
        var publisher = provider.GetRequiredService<IProducer<MyMessage2>>();

        var message = AutoFaker.Generate<MyMessage2>();
        await publisher.Publish(message);

        broker.Should().JsonMessagesBe(new()
        {
            ["my_recur_topic"] = new[] { message.ToJson() },
            ["my_topic"] = new[] { new MyMessage1 { Foo = message.Bar, Id = message.Id }.ToJson() }
        });
    }

    [Test]
    public async Task ShouldCheckForNotProducedMessages()
    {
        var publisher = provider.GetRequiredService<IProducer<MyMessage2>>();

        var message = AutoFaker.Generate<MyMessage2>();
        await publisher.Publish(message);

        var action = () => broker.Should().NotJsonMessagesBe(new()
        {
            ["my_recur_topic"] = new[] { message.ToJson() },
            ["my_topic"] = new[] { new MyMessage1 { Foo = message.Bar, Id = message.Id }.ToJson() }
        });

        action.Should().Throw<AssertionException>();
    }

    [Test]
    public async Task ShouldCheckForEquivalentProducedMessages()
    {
        var publisher = provider.GetRequiredService<IProducer<MyMessage2>>();

        var message = AutoFaker.Generate<MyMessage2>();
        await publisher.Publish(message);

        broker.Should().MessagesBe(new()
        {
            ["my_recur_topic"] = new[] { new { id = message.Id, bar = message.Bar } },
            ["my_topic"] = new[] { new { id = message.Id, foo = message.Bar } }
        });
    }

    [Test]
    public async Task ShouldCheckForEquivalentNotProducedMessages()
    {
        var publisher = provider.GetRequiredService<IProducer<MyMessage2>>();

        var message = AutoFaker.Generate<MyMessage2>();
        await publisher.Publish(message);

        var action = () => broker.Should().NotMessagesBe(new()
        {
            ["my_recur_topic"] = new[] { new { id = message.Id, bar = message.Bar } },
            ["my_topic"] = new[] { new { id = message.Id, foo = message.Bar } }
        });

        action.Should().Throw<AssertionException>();
    }

    [Test]
    public async Task ShouldCheckForObjectProducedMessages()
    {
        var publisher = provider.GetRequiredService<IProducer<MyMessage2>>();

        var message = AutoFaker.Generate<MyMessage2>();
        await publisher.Publish(message);

        broker.Should().MessagesBe(new
        {
            my_recur_topic = new[] { new { id = message.Id, bar = message.Bar } },
            my_topic = new[] { new { id = message.Id, foo = message.Bar } }
        });
    }

    [Test]
    public async Task ShouldCheckForObjectNotProducedMessages()
    {
        var publisher = provider.GetRequiredService<IProducer<MyMessage2>>();

        var message = AutoFaker.Generate<MyMessage2>();
        await publisher.Publish(message);

        var action = () => broker.Should().NotMessagesBe(new
        {
            my_recur_topic = new[] { new { id = message.Id, bar = message.Bar } },
            my_topic = new[] { new { id = message.Id, foo = message.Bar } }
        });

        action.Should().Throw<AssertionException>();

        broker.Should().NotMessagesBe(new { my_topic = new[] { new { id = Guid.NewGuid(), foo = "message" } } });
    }

    [Test]
    public async Task ShouldCheckForPartialObjectProducedMessages()
    {
        var publisher = provider.GetRequiredService<IProducer<MyMessage2>>();

        var message = AutoFaker.Generate<MyMessage2>();
        await publisher.Publish(message);

        broker.Should().BeMessagesEquivalent(new
        {
            my_recur_topic = new[] { new { id = message.Id } },
            my_topic = new[] { new { foo = message.Bar } }
        });
    }

    [Test]
    public async Task ShouldCheckForPartialJsonMessages()
    {
        var publisher = provider.GetRequiredService<IProducer<MyMessage2>>();

        var message = AutoFaker.Generate<MyMessage2>();
        await publisher.Publish(message);

        broker.Should().BeMessagesJsonEqual(new() { ["my_recur_topic"] = new[] { $@"{{""id"":""{message.Id}""}}" } });
    }

    [Test]
    public async Task ShouldCheckForPartialObjectMessage()
    {
        var publisher = provider.GetRequiredService<IProducer<MyMessage2>>();

        var message = AutoFaker.Generate<MyMessage2>();
        await publisher.Publish(message);

        broker.Should().BeMessagesEquivalent(new() { ["my_recur_topic"] = new[] { new { id = message.Id } } });
    }

    [Test]
    public async Task ShouldCheckWhenDeltaProducedMessages()
    {
        var publisher = provider.GetRequiredService<IProducer<MyMessage1>>();

        var message1 = AutoFaker.Generate<MyMessage1>();
        await publisher.Publish(message1);
        var message2 = AutoFaker.Generate<MyMessage1>();

        await broker
            .Should()
            .When(() => publisher.Publish(message2))
            .ContainMessage("my_topic", new { id = message2.Id, foo = message2.Foo });
    }

    [Test]
    public async Task ShouldCheckWhenDeltaProducedMessage()
    {
        var publisher = provider.GetRequiredService<IProducer<MyMessage1>>();

        var message1 = AutoFaker.Generate<MyMessage1>();
        await publisher.Publish(message1);
        var message2 = AutoFaker.Generate<MyMessage1>();

        await broker
            .Should()
            .When(() => publisher.Publish(message2))
            .MessagesBe(new { my_topic = new[] { new { id = message2.Id, foo = message2.Foo } } });
    }

    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        Randomizer.Seed = new Random(42);
        AutoFaker.Configure(builder => builder
            .WithRecursiveDepth(1)
            .WithRepeatCount(1));
    }
}
