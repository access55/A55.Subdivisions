using Amazon.Runtime;

namespace Subdivisions.Aws.Tests.Specs.Unit;

public class SubdivisionsTests
{
    [Test]
    public void ShouldInstantiateClient()
    {
        var client = Subdivisions.CreateClient(c =>
        {
        }, new AnonymousAWSCredentials());

        client.Should().NotBeNull();
    }

    [Test]
    public void ShouldInstantiateConsumer()
    {
        var client = Subdivisions.CreateConsumer(c =>
        {
        }, new AnonymousAWSCredentials());

        client.Should().NotBeNull();
    }

    [Test]
    public void ShouldInstantiateProducer()
    {
        var client = Subdivisions.CreateProducer(c =>
        {
        }, new AnonymousAWSCredentials());

        client.Should().NotBeNull();
    }
}
