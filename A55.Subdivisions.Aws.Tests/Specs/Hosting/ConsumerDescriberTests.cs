using A55.Subdivisions.Aws.Hosting;
using A55.Subdivisions.Aws.Models;
using A55.Subdivisions.Aws.Tests.Builders;

namespace A55.Subdivisions.Aws.Tests.Specs.Hosting;

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
            topicName: topicName,
            consumerType: typeof(FakeConsumer),
            messageType: typeof(string));

        action.Should().Throw<SubdivisionsException>();
    }

    [Test]
    public void ShouldThrowIfInvalidConsumer()
    {
        var action = () => new ConsumerDescriber(
            topicName: "good_name",
            consumerType: typeof(ConsumerDescriberTests),
            messageType: typeof(string));

        action.Should().Throw<SubdivisionsException>();
    }

    [Test]
    public void ShouldThrowIfIsNotAConsumerOfTheType()
    {
        var action = () => new ConsumerDescriber(
            topicName: "good_name",
            consumerType: typeof(FakeConsumer<ConsumerDescriberTests>),
            messageType: typeof(TestMessage));

        action.Should().Throw<SubdivisionsException>();
    }

    [Test]
    public void ShouldThrowIfIsNotAConsumerMessageContravariant()
    {
        var action = () => new ConsumerDescriber(
            topicName: "good_name",
            consumerType: typeof(FakeConsumer<TestMessageSuper>),
            messageType: typeof(TestMessage));

        action.Should().Throw<SubdivisionsException>();
    }

    [Test]
    public void ShouldNotThrowIfIsNotAConsumerMessageCovariant()
    {
        var action = () => new ConsumerDescriber(
            topicName: "good_name",
            consumerType: typeof(FakeConsumer<TestMessage>),
            messageType: typeof(TestMessageSuper));

        action.Should().NotThrow<SubdivisionsException>();
    }

    [Test]
    public void ShouldNotThrow()
    {
        var action = () => new ConsumerDescriber(
            topicName: "good_name",
            consumerType: typeof(FakeConsumer),
            messageType: typeof(string));

        action.Should().NotThrow();
    }

    [Test]
    public void ShouldNotThrowForRefType()
    {
        var action = () => new ConsumerDescriber(
            topicName: "good_name",
            consumerType: typeof(FakeConsumer<TestMessage>),
            messageType: typeof(TestMessage));

        action.Should().NotThrow();
    }

    [Test]
    public void ShouldNotThrowForValueType()
    {
        var action = () => new ConsumerDescriber(
            topicName: "good_name",
            consumerType: typeof(FakeConsumer<TestMessageValue>),
            messageType: typeof(TestMessageValue));

        action.Should().NotThrow();
    }
}
