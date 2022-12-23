using Subdivisions.Aws.Tests.TestUtils.Fixtures;
using Subdivisions.Models;
using Subdivisions.Services;

namespace Subdivisions.Aws.Tests.Specs.Integration;

public class ServicesTests : ServicesFixture
{
    [Test]
    public void ShouldRegisterPublicServices()
    {
        var producer = GetService<IProducerClient>();
        var consumer = GetService<IConsumerClient>();
        var client = GetService<ISubdivisionsClient>();
        new object[]
            {
                producer, consumer, client
            }
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

    [Test]
    public void ShouldSerializeRawMessage()
    {
        var message =
            """
            {"event":"lead_analysis_completed","datetime":"2022-11-25T00:46:28.328508+00:00","payload":{"tax_id":"23519244000181","request_id":951,"status":"SUCCESS"}}
            """;

        var response = GetService<ISubMessageSerializer>().Deserialize<MessageEnvelope>(message);

        response.Payload.RootElement.GetRawText().Should().BeEquivalentTo(
            """
            {"tax_id":"23519244000181","request_id":951,"status":"SUCCESS"}
            """);
    }
}
