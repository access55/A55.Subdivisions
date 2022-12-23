using Subdivisions.Aws.Tests.Builders;
using Subdivisions.Hosting;
using Subdivisions.Models;

namespace Subdivisions.Aws.Tests.Specs.Unit.Hosting;

public class ConsumerDescriberTests
{
    [TestCase("0name")]
    [TestCase("name@bad")]
    [TestCase("name$bad")]
    [TestCase("")]
    [TestCase("a")]
    [TestCase("ab")]
    [TestCase("abcde")]
    public void ShouldThrowIfInvalidTopic(string topicName)
    {
        var action = () => new ConsumerDescriber(
            topicName,
            typeof(FakeMessageConsumer<>),
            typeof(string));

        action.Should().Throw<SubdivisionsException>();
    }

    [Test]
    public void ShouldThrowIfInvalidConsumer()
    {
        var action = () => new ConsumerDescriber(
            "good_name",
            typeof(ConsumerDescriberTests),
            typeof(string));

        action.Should().Throw<SubdivisionsException>();
    }

    [Test]
    public void ShouldThrowIfConsumerIsAbstract()
    {
        var action = () => new ConsumerDescriber(
            "good_name",
            typeof(AbstractConsumer),
            typeof(string));

        action.Should().Throw<SubdivisionsException>();
    }

    [Test]
    public void ShouldThrowIfIsNotAConsumerOfTheType()
    {
        var action = () => new ConsumerDescriber(
            "good_name",
            typeof(FakeMessageConsumer<ConsumerDescriberTests>),
            typeof(TestMessage));

        action.Should().Throw<SubdivisionsException>();
    }

    [Test]
    public void ShouldThrowIfIsNotAConsumerMessageContravariant()
    {
        var action = () => new ConsumerDescriber(
            "good_name",
            typeof(FakeMessageConsumer<TestMessageSuper>),
            typeof(TestMessage));

        action.Should().Throw<SubdivisionsException>();
    }

    [Test]
    public void ShouldNotThrowIfIsNotAConsumerMessageCovariant()
    {
        var action = () => new ConsumerDescriber(
            "good_name",
            typeof(FakeMessageConsumer<TestMessage>),
            typeof(TestMessageSuper));

        action.Should().NotThrow<SubdivisionsException>();
    }

    [Test]
    public void ShouldNotThrow()
    {
        var action = () => new ConsumerDescriber(
            "good_name",
            typeof(FakeMessageConsumer),
            typeof(string));

        action.Should().NotThrow();
    }

    [Test]
    public void ShouldNotThrowForRefType()
    {
        var action = () => new ConsumerDescriber(
            "good_name",
            typeof(FakeMessageConsumer<TestMessage>),
            typeof(TestMessage));

        action.Should().NotThrow();
    }

    [Test]
    public void ShouldNotThrowForValueType()
    {
        var action = () => new ConsumerDescriber(
            "good_name",
            typeof(FakeMessageConsumer<TestMessageValue>),
            typeof(TestMessageValue));

        action.Should().NotThrow();
    }

    [Test]
    public void ShouldNotThrowIfConsumerIsDerivedClass()
    {
        var action = () => new ConsumerDescriber(
            "good_name",
            typeof(TestDerivedMessageConsumer),
            typeof(string));

        action.Should().NotThrow<SubdivisionsException>();
    }

    [Test]
    public void ShouldNotThrowIfConsumerIsAnInterface()
    {
        var action = () => new ConsumerDescriber(
            "good_name",
            typeof(ITestConsumerInterface),
            typeof(string));

        action.Should().NotThrow<SubdivisionsException>();
    }

    public abstract class AbstractConsumer : IMessageConsumer
    {
        public Task Consume(string message, MessageMeta meta, CancellationToken ctx) =>
            Task.CompletedTask;
    }

    public interface ITestConsumerInterface : IMessageConsumer
    {
    }

    public class TestDerivedMessageConsumer : FakeMessageConsumer<string>
    {
    }
}
