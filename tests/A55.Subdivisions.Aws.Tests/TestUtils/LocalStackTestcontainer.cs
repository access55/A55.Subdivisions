using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using Microsoft.Extensions.Logging;

namespace A55.Subdivisions.Aws.Tests.TestUtils;

public sealed class LocalStackTestcontainer : TestcontainerMessageBroker
{
    internal LocalStackTestcontainer(ITestcontainersConfiguration configuration, ILogger logger)
        : base(configuration, logger)
    {
    }

    public string Url => $"http://localhost:{Port}";
}

public class LocalStackTestcontainerConfiguration : TestcontainerMessageBrokerConfiguration
{
    const string LocalStackImage = "localstack/localstack:1.1.0";
    const int LocalStackPort = 4566;

    public LocalStackTestcontainerConfiguration()
        : this(LocalStackImage)
    {
    }

    public LocalStackTestcontainerConfiguration(string image)
        : base(image, LocalStackPort)
    {
        Environments.Add("EXTERNAL_SERVICE_PORTS_START", "4510");
        Environments.Add("EXTERNAL_SERVICE_PORTS_END", "4559");
    }

    public override IWaitForContainerOS WaitStrategy => Wait.ForUnixContainer()
        .UntilPortIsAvailable(LocalStackPort);
}
