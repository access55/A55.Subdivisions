namespace A55.Subdivisions.Aws.Hosting.Job;

interface IConsumerJob
{
    Task Start(IReadOnlyCollection<IConsumerDescriber> describers, CancellationToken stoppingToken);
}
