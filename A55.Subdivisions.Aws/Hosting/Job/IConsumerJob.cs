namespace A55.Subdivisions.Aws.Hosting.Job;

interface IConsumerJob
{
    public Task Start(CancellationToken stoppingToken);
}
