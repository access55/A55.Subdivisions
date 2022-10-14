using Subdivisions.Aws.Tests.TestUtils.Fixtures;

namespace Subdivisions.Aws.Tests.Specs.Integration;

public class ServicesTests : ServicesFixture
{
    [Test]
    public void ShouldRegisterPublicServices()
    {
        var producer = GetService<IProducerClient>();
        var consumer = GetService<IConsumerClient>();
        var client = GetService<ISubdivisionsClient>();
        new object[] { producer, consumer, client }
            .Should().NotContainNulls();
    }

    [Test]
    public void ShouldConsumerShouldBeSameSubClient()
    {
        var client = GetService<ISubdivisionsClient>();
        var consumer = GetService<IConsumerClient>();
        consumer.Should().BeOfType(client.GetType());
    }

    [Test]
    public void ShouldProducerShouldBeSameSubClient()
    {
        var client = GetService<ISubdivisionsClient>();
        var producer = GetService<IProducerClient>();
        producer.Should().BeOfType(client.GetType());
    }
}
