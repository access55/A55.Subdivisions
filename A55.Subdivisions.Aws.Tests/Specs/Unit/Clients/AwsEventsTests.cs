using A55.Subdivisions.Aws.Clients;
using A55.Subdivisions.Aws.Tests.TestUtils.Fixtures;
using Amazon.EventBridge;
using Amazon.EventBridge.Model;
using Microsoft.Extensions.Logging;

namespace A55.Subdivisions.Aws.Tests.Specs.Unit.Clients;

public class AwsEventsTests : BaseTest
{
    [Test]
    public async Task TopicExistsShouldReturnTrueIfRuleExists()
    {
        var topicName = faker.TopicName(new() { Source = "Source" });
        mocker.Provide(A.Fake<ILogger<AwsEvents>>());
        var aws = mocker.Generate<AwsEvents>();

        ListRulesRequest request = new() { Limit = 100, NamePrefix = topicName.FullTopicName };

        ListRulesResponse response = new()
        {
            Rules = new List<Rule> { new() { Name = topicName.FullTopicName, State = RuleState.ENABLED } }
        };

        A.CallTo(() => mocker.Resolve<IAmazonEventBridge>()
                .ListRulesAsync(A<ListRulesRequest>.That.IsEquivalentTo(request), A<CancellationToken>._))
            .Returns(response);

        var result = await aws.RuleExists(topicName, default);

        result.Should().BeTrue();
    }
}
