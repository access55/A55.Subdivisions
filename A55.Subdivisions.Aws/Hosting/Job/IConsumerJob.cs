namespace A55.Subdivisions.Hosting.Job;

interface IConsumerJob
{
    Task Start(IReadOnlyCollection<IConsumerDescriber> describers, CancellationToken stoppingToken);
}
