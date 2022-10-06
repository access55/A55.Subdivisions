using A55.Subdivisions.Aws.Adapters;
using Amazon.KeyManagementService;
using Amazon.KeyManagementService.Model;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Amazon.SQS.Model;

namespace A55.Subdivisions.Aws.Tests.Specs.Integration.Adapters;

public class AwsSnsTests : LocalstackTest
{
    [Test]
    public async Task ShouldCreateNewTopic()
    {
        var name = $"{Faker.Person.FirstName}_{Faker.Name.LastName()}".ToLowerInvariant();
        var topicName = new TopicName(name);
        var aws = GetService<AwsSns>();
        await CreateDefaultKey();

        await aws.CreateTopic(topicName, default);

        var sns = GetService<IAmazonSimpleNotificationService>();
        var topics = await sns.ListTopicsAsync();

        topics.Topics.Should().ContainSingle();
    }

    [Test]
    public async Task ShouldCreateNewTopicWithArn()
    {
        var topicName = new TopicName(Faker.Person.FirstName.ToLowerInvariant());
        var aws = GetService<AwsSns>();
        await CreateDefaultKey();

        var result = await aws.CreateTopic(topicName, default);

        var sns = GetService<IAmazonSimpleNotificationService>();
        var topic = await sns.GetTopicAttributesAsync(new GetTopicAttributesRequest {TopicArn = result.Value});

        topic.Attributes.Should().HaveCountGreaterThan(0);
    }

    async Task<string> CreateDefaultKey()
    {
        var kms = GetService<IAmazonKeyManagementService>();
        var key = await kms.CreateKeyAsync(new() {Description = "Test key"});
        await kms.CreateAliasAsync(new CreateAliasRequest
        {
            AliasName = config.PubKey, TargetKeyId = key.KeyMetadata.KeyId
        });
        return key.KeyMetadata.KeyId;
    }
}
