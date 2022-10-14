using Microsoft.Extensions.Logging;
using Subdivisions.Aws.Tests.Builders;
using Subdivisions.Aws.Tests.TestUtils;
using Subdivisions.Aws.Tests.TestUtils.Fixtures;
using Subdivisions.Hosting;
using Subdivisions.Services;

namespace Subdivisions.Aws.Tests.Specs.Unit.Hosting;

public class ConsumerFactoryTests : BaseTest
{
    [Test]
    public async Task ShouldThrowIfInvalidConsumer()
    {
        var describer = new ConsumerDescriberBuilder()
            .WithConsumerType<BaseTest>()
            .Generate();
        var factory = mocker.Generate<ConsumerFactory>();

        var action = () => factory.ConsumeScoped(describer, A.Fake<IMessage>(), default);

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Invalid consumer type:*");
    }

    [Test]
    public async Task ShouldThrowWhenSerilizeNull()
    {
        var describer = new ConsumerDescriberBuilder().Generate();

        mocker.Provide<ISubMessageSerializer>(new FakeSerializer(null));
        var factory = mocker.Generate<ConsumerFactory>();

        var action = () => factory.ConsumeScoped(describer, A.Fake<IMessage>(), default);

        await action.Should().ThrowAsync<NullReferenceException>()
            .WithMessage("Message body is NULL*");
    }

    [Test]
    public async Task ShouldConsumeStringMessage()
    {
        var describer = new ConsumerDescriberBuilder()
            .UsingConsumer<FakeConsumer, string>()
            .Generate();

        var messageBody = faker.Lorem.Sentence();
        var message = new FakeMessageBuilder().WithBody(messageBody).Generate();

        var factory = mocker.Generate<ConsumerFactory>();

        await factory.ConsumeScoped(describer, message, default);

        A.CallTo(() => mocker.Resolve<FakeConsumer>()
                .Consume(messageBody, default))
            .MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task ShouldDeleteMessageWhenSuccess()
    {
        var describer = new ConsumerDescriberBuilder()
            .UsingConsumer<FakeConsumer, string>()
            .Generate();

        var messageBody = faker.Lorem.Sentence();
        var message = new FakeMessageBuilder().WithBody(messageBody).Generate();

        var factory = mocker.Generate<ConsumerFactory>();
        await factory.ConsumeScoped(describer, message, default);

        A.CallTo(() => message.Delete()).MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task ShouldConsumeSerializableMessage()
    {
        var describer = new ConsumerDescriberBuilder()
            .UsingConsumer<TestConsumer, TestMessage>()
            .Generate();

        var payload = TestMessage.New();
        var message = new FakeMessageBuilder()
            .WithBody(payload.ToSnakeCaseJson())
            .Generate();

        mocker.Provide<ISubMessageSerializer>(new SubJsonSerializer());
        var factory = mocker.Generate<ConsumerFactory>();

        await factory.ConsumeScoped(describer, message, default);

        A.CallTo(() => mocker.Resolve<TestConsumer>()
                .Consume(A<TestMessage>.That.IsEquivalentTo(payload),
                    default))
            .MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task ShouldReleaseMessageWhenError()
    {
        var describer = new ConsumerDescriberBuilder()
            .UsingConsumer<FakeConsumer, string>()
            .Generate();

        var message = new FakeMessageBuilder().Generate();

        var factory = mocker.Generate<ConsumerFactory>();
        var consumer = mocker.Resolve<FakeConsumer>();

        A.CallTo(() => consumer
                .Consume(message.Body, A<CancellationToken>._))
            .Throws(new Exception());

        await factory.ConsumeScoped(describer, message, default);

        A.CallTo(() => message.Release(A<TimeSpan>._)).MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task ShouldReleaseMessageWithDelay()
    {
        var describer = new ConsumerDescriberBuilder()
            .UsingConsumer<FakeConsumer, string>()
            .Generate();

        var message = new FakeMessageBuilder().WithRetry(1).Generate();

        var factory = mocker.Generate<ConsumerFactory>();
        var consumer = mocker.Resolve<FakeConsumer>();
        var delay = faker.Date.Timespan();
        A.CallTo(() => mocker.Resolve<IRetryStrategy>().Evaluate(1))
            .Returns(delay);

        A.CallTo(() => consumer
                .Consume(message.Body, A<CancellationToken>._))
            .Throws(new Exception());

        await factory.ConsumeScoped(describer, message, default);

        A.CallTo(() => message.Release(delay)).MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task ShouldCallErrorHandlerWhenError()
    {
        var errorHandler = A.Fake<Func<Exception, Task>>();
        var describer = new ConsumerDescriberBuilder()
            .UsingConsumer<FakeConsumer, string>()
            .WithErrorHandler(errorHandler)
            .Generate();

        var message = new FakeMessageBuilder().Generate();

        var factory = mocker.Generate<ConsumerFactory>();
        var consumer = mocker.Resolve<FakeConsumer>();

        var ex = new Exception(faker.Random.Guid().ToString());
        A.CallTo(() => consumer
                .Consume(message.Body, A<CancellationToken>._))
            .Throws(ex);

        await factory.ConsumeScoped(describer, message, default);

        A.CallTo(() => errorHandler.Invoke(ex))
            .MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task ShouldLogErrorMessageWhenError()
    {
        var describer = new ConsumerDescriberBuilder()
            .UsingConsumer<FakeConsumer, string>()
            .Generate();

        var message = new FakeMessageBuilder().Generate();

        var factory = mocker.Generate<ConsumerFactory>();

        var ex = new Exception(faker.Random.Guid().ToString());
        A.CallTo(() => mocker.Resolve<FakeConsumer>()
                .Consume(message.Body, A<CancellationToken>._))
            .Throws(ex);

        await factory.ConsumeScoped(describer, message, default);
        mocker.Resolve<ILogger<ConsumerFactory>>()
            .CalledWith(LogLevel.Error, ex)
            .MustHaveHappenedOnceExactly();
    }
}
