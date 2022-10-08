using A55.Subdivisions.Aws.Adapters;
using Amazon.KeyManagementService;
using Amazon.KeyManagementService.Model;
using Amazon.SQS;
using Amazon.SQS.Model;
using Newtonsoft.Json.Linq;

namespace A55.Subdivisions.Aws.Tests.Specs.Integration.Adapters;

public class AwsSqsTests : LocalstackFixture
{
    [Test]
    public async Task ShouldGetQueueAttributes()
    {
        var sqs = GetService<IAmazonSQS>();
        var queue = await sqs.CreateQueueAsync(Faker.Person.FirstName.ToLowerInvariant());

        var aws = GetService<AwsSqs>();
        var result = await aws.GetQueueAttributes(queue.QueueUrl, default);

        result.Arn.Value.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task QueueExistsShouldReturnTrue()
    {
        var sqs = GetService<IAmazonSQS>();
        var queueName = Faker.Person.FirstName.ToLowerInvariant();
        await sqs.CreateQueueAsync(queueName);

        var aws = GetService<AwsSqs>();
        var result = await aws.QueueExists(queueName, default);

        result.Should().BeTrue();
    }

    [Test]
    public async Task QueueExistsShouldReturnFalse()
    {
        var queueName = Faker.Person.FirstName.ToLowerInvariant();
        var aws = GetService<AwsSqs>();
        var result = await aws.QueueExists(queueName, default);
        result.Should().BeFalse();
    }

    [Test]
    public async Task GetQueueShoulsReturnQueueData()
    {
        var sqs = GetService<IAmazonSQS>();
        var queueName = Faker.Person.FirstName.ToLowerInvariant();
        var queue = await sqs.CreateQueueAsync(queueName);

        var aws = GetService<AwsSqs>();
        var result = await aws.GetQueue(queueName, default);
        result?.Url.Should().Be(queue.QueueUrl);
        result?.Arn.Value.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task ShouldCreateNewQueue()
    {
        var queueName = Faker.Person.FirstName.ToLowerInvariant();
        var aws = GetService<AwsSqs>();
        await CreateDefaultKmsKey();

        var result = await aws.CreateQueue(queueName, default);

        var sqs = GetService<IAmazonSQS>();
        var qs = await sqs.ListQueuesAsync(new ListQueuesRequest());

        qs.QueueUrls.Should().Contain(result.Url.ToString());
    }

    [Test]
    public async Task ShouldCreateNewDeadletterQueue()
    {
        var queueName = Faker.Person.FirstName.ToLowerInvariant();
        var aws = GetService<AwsSqs>();
        await CreateDefaultKmsKey();

        await aws.CreateQueue(queueName, default);

        var sqs = GetService<IAmazonSQS>();
        var qs = await sqs.ListQueuesAsync(new ListQueuesRequest());

        qs.QueueUrls.Should().HaveCount(2).And.Contain(x => x.Contains($"dead_letter_{queueName}"));
    }

    [Test]
    public async Task ShouldCreateNewQueueWithTimedAttributes()
    {
        var queueName = Faker.Person.FirstName.ToLowerInvariant();
        await CreateDefaultKmsKey();

        var result = await GetService<AwsSqs>().CreateQueue(queueName, default);

        var attr = await GetService<IAmazonSQS>()
            .GetQueueAttributesAsync(result.Url.ToString(),
                new List<string>
                {
                    QueueAttributeName.VisibilityTimeout,
                    QueueAttributeName.MessageRetentionPeriod,
                    QueueAttributeName.DelaySeconds
                });

        attr.VisibilityTimeout.Should().Be(config.MessageTimeoutInSeconds);
        attr.MessageRetentionPeriod.Should().Be(config.MessageRetantionInDays);
        attr.DelaySeconds.Should().Be(config.MessageDelayInSeconds);
    }

    [Test]
    public async Task ShouldCreateNewQueueWithKmsKey()
    {
        var queueName = Faker.Person.FirstName.ToLowerInvariant();
        var keyId = await CreateDefaultKmsKey();

        var result = await GetService<AwsSqs>().CreateQueue(queueName, default);

        var attr = await GetService<IAmazonSQS>()
            .GetQueueAttributesAsync(result.Url.ToString(), new List<string> {QueueAttributeName.KmsMasterKeyId});

        attr.Attributes[QueueAttributeName.KmsMasterKeyId].Should().Be(keyId);
    }

    [Test]
    public async Task ShouldCreateNewQueueWithRedrivePolicy()
    {
        var queueName = Faker.Person.FirstName.ToLowerInvariant();
        var sqs = GetService<IAmazonSQS>();
        await CreateDefaultKmsKey();

        var result = await GetService<AwsSqs>().CreateQueue(queueName, default);

        var attr = await sqs.GetQueueAttributesAsync(result.Url.ToString(),
            new List<string> {QueueAttributeName.RedrivePolicy});
        var policy = JToken.Parse(attr.Attributes[QueueAttributeName.RedrivePolicy]);

        var qs = await sqs.ListQueuesAsync(new ListQueuesRequest());
        var deadletter = qs.QueueUrls.Single(q => q.Contains($"dead_letter_{queueName}"));
        var deadletterAttr =
            await sqs.GetQueueAttributesAsync(deadletter, new List<string> {QueueAttributeName.QueueArn});

        var expected =
            @$"{{""deadLetterTargetArn"": ""{deadletterAttr.QueueARN}"", ""maxReceiveCount"": ""{config.QueueMaxReceiveCount}""}}";

        policy.Should().BeEquivalentTo(JToken.Parse(expected));
    }
}
