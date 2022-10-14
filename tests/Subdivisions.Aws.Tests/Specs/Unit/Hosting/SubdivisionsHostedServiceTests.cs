using System.Collections.Immutable;
using System.Reflection;
using Subdivisions.Aws.Tests.Builders;
using Subdivisions.Aws.Tests.TestUtils;
using Subdivisions.Aws.Tests.TestUtils.Fixtures;
using Subdivisions.Hosting;
using Subdivisions.Hosting.Job;
using Subdivisions.Models;
using Subdivisions.Services;

namespace Subdivisions.Aws.Tests.Specs.Unit.Hosting;

public class SubdivisionsHostedServiceTests : BaseTest
{
    [Test]
    public void ShouldThrowIfTopicIsDuplicated()
    {
        var builder = new ConsumerDescriberBuilder()
            .WithTopicName(faker.TopicNameString());

        mocker.Provide<IEnumerable<IConsumerDescriber>>(new[] { builder.Generate(), builder.Generate() });
        var action = () => mocker.Generate<SubdivisionsHostedService>();

        action.Should().Throw<ArgumentException>()
            .WithInnerException<TargetInvocationException>()
            .WithInnerException<SubdivisionsException>()
            .WithMessage("Duplicated topic definition");
    }

    [Test]
    public async Task BootsTrapShouldBeCalledForEachDescriber()
    {
        var describers = GetConsumerDescribers();

        mocker.Provide<IEnumerable<IConsumerDescriber>>(describers);

        var service = mocker.Generate<SubdivisionsHostedService>();

        await service.StartAsync(default);

        var bootstrapper = mocker.Resolve<ISubResourceManager>();
        foreach (var describer in describers)
            A.CallTo(() => bootstrapper.EnsureTopicExists(describer.TopicName, A<CancellationToken>._))
                .MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task BootsTrapStartShouldStartConsumerJob()
    {
        var describers = GetConsumerDescribers();

        mocker.Provide<IEnumerable<IConsumerDescriber>>(describers);

        var service = mocker.Generate<SubdivisionsHostedService>();

        await service.StartAsync(default);

        var job = mocker.Resolve<IConsumerJob>();
        A.CallTo(() => job
                .Start(A<ImmutableArray<IConsumerDescriber>>.That.IsSameSequenceAs(describers),
                    A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
    }

    static IConsumerDescriber[] GetConsumerDescribers()
    {
        var builder = new ConsumerDescriberBuilder();
        return faker
            .Random
            .Items(FakeMessageTypes.All)
            .Select(types => builder
                .WithTopicName($"topic_{faker.Random.Guid():N}")
                .WithMessageType(types.Key)
                .WithConsumerType(types.Value)
                .Generate())
            .ToArray();
    }
}

static class FakeMessageTypes
{
    public static readonly Dictionary<Type, Type> All = new()
    {
        [typeof(First)] = typeof(IConsumer<First>),
        [typeof(Seconds)] = typeof(IConsumer<Seconds>),
        [typeof(Third)] = typeof(IConsumer<Third>),
        [typeof(Fourth)] = typeof(IConsumer<Fourth>),
        [typeof(Fifth)] = typeof(IConsumer<Fifth>),
        [typeof(Sixth)] = typeof(IConsumer<Sixth>),
        [typeof(Seventh)] = typeof(IConsumer<Seventh>),
        [typeof(Eighth)] = typeof(IConsumer<Eighth>),
        [typeof(Ninth)] = typeof(IConsumer<Ninth>),
        [typeof(Tenth)] = typeof(IConsumer<Tenth>)
    };

    public record First;

    public record Seconds;

    public record Third;

    public record Fourth;

    public record Fifth;

    public record Sixth;

    public record Seventh;

    public record Eighth;

    public record Ninth;

    public record Tenth;
}
