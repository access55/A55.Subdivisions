using A55.Subdivisions.Aws.Hosting;
using Bogus;

namespace A55.Subdivisions.Aws.Tests.Builders;

internal class FakeMessageBuilder
{
    readonly DateTime datetime;
    Guid id;
    string body;

    public FakeMessageBuilder()
    {
        var faker = new Faker();
        this.datetime = faker.Date.Soon().ToUniversalTime();
        this.id = faker.Random.Guid();
        this.body = TestMessage.New().ToSnakeCaseJson();
    }

    public FakeMessageBuilder WithBody(string body)
    {
        this.body = body;
        return this;
    }

    public IMessage Generate()
    {
        var value = A.Fake<IMessage>();
        A.CallTo(() => value.Id).Returns(id);
        A.CallTo(() => value.Datetime).Returns(datetime);
        A.CallTo(() => value.Body).Returns(body);
        return value;
    }
}
