using System.Diagnostics;
using AutoBogus;
using Bogus;

namespace A55.Subdivisions.Aws.Tests.TestUtils;

public class BaseTest
{
    protected static readonly Faker faker = new("pt_BR");
    protected AutoFakeIt mocker = null!;

    [OneTimeSetUp]
    public void SetUpOneTimeBase()
    {
        AutoFaker.Configure(builder => builder
            .WithRecursiveDepth(1)
            .WithRepeatCount(1));

        AssertionOptions.AssertEquivalencyUsing(options => options
            .Using<DateTime>(ctx =>
                ctx.Subject.Should().BeCloseTo(ctx.Expectation, TimeSpan.FromMilliseconds(100)))
            .WhenTypeIs<DateTime>()
            .Using<decimal>(ctx =>
                ctx.Subject.Should().BeApproximately(ctx.Expectation, .0001M))
            .WhenTypeIs<decimal>()
            .Using<double>(ctx =>
                ctx.Subject.Should().BeApproximately(ctx.Expectation, .0001))
            .WhenTypeIs<double>());
    }

    [SetUp]
    public void SetupBase() => mocker = new AutoFakeIt();

    [OneTimeSetUp]
    public void StartTest() => Trace.Listeners.Add(new ConsoleTraceListener());

    [OneTimeTearDown]
    public void EndTest() => Trace.Flush();
}
