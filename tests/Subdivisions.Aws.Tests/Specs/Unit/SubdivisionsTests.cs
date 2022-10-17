using Amazon.Runtime;

namespace Subdivisions.Aws.Tests.Specs.Unit;

public class SubdivisionsTests
{
    [Test]
    public void ShouldInstantiateClient()
    {
        var client = Subdivisions.CreateClient(c =>
        {
            c.Source = "app";
        }, new AnonymousAWSCredentials());

        client.Should().NotBeNull();
    }

    [Test]
    public void ShouldInstantiateConsumer()
    {
        var client = Subdivisions.CreateConsumer(c =>
        {
            c.Source = "app";
        }, new AnonymousAWSCredentials());

        client.Should().NotBeNull();
    }

    [Test]
    public void ShouldInstantiateProducer()
    {
        var client = Subdivisions.CreateProducer(c =>
        {
            c.Source = "app";
        }, new AnonymousAWSCredentials());

        client.Should().NotBeNull();
    }
}
