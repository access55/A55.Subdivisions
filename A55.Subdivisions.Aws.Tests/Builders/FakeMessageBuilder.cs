using Bogus;

namespace A55.Subdivisions.Aws.Tests.Builders;

class FakeMessageBuilder
{
    readonly DateTime datetime;
    string body;
    readonly Guid id;

    public FakeMessageBuilder()
    {
        var faker = new Faker();
        datetime = faker.Date.Soon().ToUniversalTime();
        id = faker.Random.Guid();
        body = TestMessage.New().ToSnakeCaseJson();
    }

    public FakeMessageBuilder WithBody(string body)
    {
        this.body = body;
        return this;
    }

    public IMessage Generate()
    {
        var value = A.Fake<IMessage>();
        A.CallTo(() => value.MessageId).Returns(id);
        A.CallTo(() => value.Datetime).Returns(datetime);
        A.CallTo(() => value.Body).Returns(body);
        return value;
    }
}
