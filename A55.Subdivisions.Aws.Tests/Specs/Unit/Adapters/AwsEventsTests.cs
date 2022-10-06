using A55.Subdivisions.Aws.Adapters;
using Amazon.EventBridge;
using Amazon.EventBridge.Model;
using Microsoft.Extensions.Logging;

namespace A55.Subdivisions.Aws.Tests.Specs.Unit.Adapters;

public class AwsEventsTests : BaseTest
{

    [Test]
    public async Task TopicExistsShouldReturnTrueIfRuleExists()
    {
        var topicName = new EventName(Faker.Internet.UserName());
        Mocker.Provide(A.Fake<ILogger<AwsEvents>>());
        var aws = Mocker.Generate<AwsEvents>();

        ListRulesRequest request = new()
        {
            Limit = 100,
            NamePrefix = topicName.Name
        };
        
        ListRulesResponse response = new()
        {
            Rules = new List<Rule>()
            {
                new ()
                {
                    Name = topicName.Name,
                    State = RuleState.ENABLED,
                }
            }
        };
        
        A.CallTo(() => Mocker.Resolve<IAmazonEventBridge>()
            .ListRulesAsync(A<ListRulesRequest>.That.IsEquivalentTo(request), A<CancellationToken>._))
            .Returns(response);
        
        var result = await aws.RuleExists(topicName);

        result.Should().BeTrue();
    }
}