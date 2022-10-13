using A55.Subdivisions.Aws.Hosting;
using A55.Subdivisions.Aws.Hosting.Job;
using A55.Subdivisions.Aws.Models;
using A55.Subdivisions.Aws.Tests.Builders;
using A55.Subdivisions.Aws.Tests.TestUtils.Fixtures;
using Microsoft.Extensions.Options;

namespace A55.Subdivisions.Aws.Tests.Specs.Unit.Hosting.Job;

public class ConcurrentConsumerJobTests : BaseTest
{
    [Test]
    public async Task ShouldProcessOneMessage()
    {
        var consumer = new ConsumerDescriberBuilder().Generate();
        var message = new FakeMessageBuilder().Generate();
        var ctx = new CancellationTokenSource();

        A.CallTo(() => mocker.Resolve<IOptionsMonitor<SubConfig>>().CurrentValue)
            .Returns(new SubConfig {MessageTimeoutInSeconds = 100, PollingIntervalInSeconds = 0.1f});

        A.CallTo(() => mocker.Resolve<ISubdivisionsClient>()
                .Receive(consumer.TopicName, A<CancellationToken>._))
            .ReturnsNextFromSequence(new[] {message});

        A.CallTo(() => mocker.Resolve<IConsumerFactory>()
                .ConsumeScoped(A<IConsumerDescriber>._, A<IMessage>._, A<CancellationToken>._))
            .Invokes(() => ctx.CancelAfter(100));

        var job = mocker.Generate<ConcurrentConsumerJob>();
        var workerTask = () => job.Start(new[] {consumer}, ctx.Token);

        await workerTask.Should().ThrowAsync<OperationCanceledException>();

        A.CallTo(() => mocker.Resolve<IConsumerFactory>().ConsumeScoped(consumer, message, A<CancellationToken>._))
            .MustHaveHappened();
    }

    [Test]
    public async Task ShouldTimeoutIfMessageIsNotProcessedInVisibleTime()
    {
        var message = new FakeMessageBuilder().Generate();
        const int timeoutInSeconds = 1;

        var consumer = new ConsumerDescriberBuilder()
            .WithErrorHandler()
            .Generate();

        A.CallTo(() => mocker.Resolve<IOptionsMonitor<SubConfig>>().CurrentValue)
            .Returns(new SubConfig {MessageTimeoutInSeconds = timeoutInSeconds, PollingIntervalInSeconds = 0.1f});

        A.CallTo(() => mocker.Resolve<ISubdivisionsClient>()
                .Receive(consumer.TopicName, A<CancellationToken>._))
            .ReturnsNextFromSequence(new[] {message});

        A.CallTo(() => mocker.Resolve<IConsumerFactory>()
                .ConsumeScoped(A<IConsumerDescriber>._, A<IMessage>._, A<CancellationToken>._))
            .ReturnsLazily(async fake =>
            {
                await Task.Delay(TimeSpan.FromSeconds(1.1));
                fake.Arguments.Get<CancellationToken>("ctx")
                    .ThrowIfCancellationRequested();
            });

        var ctx = new CancellationTokenSource();

        A.CallTo(() => consumer.ErrorHandler!(A<OperationCanceledException>._))
            .Invokes(() => ctx.Cancel());

        var job = mocker.Generate<ConcurrentConsumerJob>();
        var task = () => job.Start(new[] {consumer}, ctx.Token);
        await task.Should().ThrowAsync<OperationCanceledException>();
    }

    [Test]
    public async Task ShouldNotConcurrentlyProcessMoreMessagesThanConfigured()
    {
        var consumer = new ConsumerDescriberBuilder().WithConcurrency(1).Generate();
        var message1 = new FakeMessageBuilder().Generate();
        var message2 = new FakeMessageBuilder().Generate();

        A.CallTo(() => mocker.Resolve<IOptionsMonitor<SubConfig>>().CurrentValue)
            .Returns(new SubConfig {MessageTimeoutInSeconds = 100, PollingIntervalInSeconds = 0.1f,});

        A.CallTo(() => mocker.Resolve<ISubdivisionsClient>()
                .Receive(consumer.TopicName, A<CancellationToken>._))
            .ReturnsNextFromSequence(new[] {message1}, new[] {message2});

        A.CallTo(() => mocker.Resolve<IConsumerFactory>()
                .ConsumeScoped(A<IConsumerDescriber>._, A<IMessage>._, A<CancellationToken>._))
            .ReturnsLazily(() => Task.Delay(1000));

        var ctx = new CancellationTokenSource();
        var job = mocker.Generate<ConcurrentConsumerJob>();
        var workerTask = () => job.Start(new[] {consumer}, ctx.Token);
        ctx.CancelAfter(500);
        await workerTask.Should().ThrowAsync<OperationCanceledException>();

        A.CallTo(() => mocker.Resolve<IConsumerFactory>()
                .ConsumeScoped(consumer, message1, A<CancellationToken>._))
            .MustHaveHappened();

        A.CallTo(() => mocker.Resolve<IConsumerFactory>()
                .ConsumeScoped(consumer, message2, A<CancellationToken>._))
            .MustNotHaveHappened();
    }
}
