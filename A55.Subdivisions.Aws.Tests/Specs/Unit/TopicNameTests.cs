namespace A55.Subdivisions.Aws.Tests.Specs.Unit;

public class TopicNameTests
{
    [TestCase("0name")]
    [TestCase("name@bad")]
    [TestCase("name$bad")]
    [TestCase("")]
    [TestCase("a")]
    [TestCase("ab")]
    [TestCase("abcde")]
    public void ShouldThrowIfBadName(string badName)
    {
        var action = () => new TopicName(badName);
        action.Should().Throw<ArgumentException>();
    }

    [TestCase("good_name")]
    [TestCase("abcdef")]
    [TestCase("abc123")]
    public void ShouldNotThrowIfGoodName(string goodName)
    {
        var action = () => new TopicName(goodName);
        action.Should().NotThrow();
    }

    [Test]
    public void ShouldNormalizeName()
    {
        const string name = "NameToNormalize";
        const string expected = "name_to_normalize";
        var topic = new TopicName(name);

        topic.Topic.Should().Be(expected);
    }

    [Test]
    public void ShouldNormalizeNameWithPrefixAndSufix()
    {
        const string prefix = "ThePrefix";
        const string sufix = "TheSufix";
        const string name = "NameToNormalize";
        const string expected = "ThePrefixNameToNormalizeTheSufix";
        var topic = new TopicName(name, prefix, sufix);

        topic.FullNamePascalCase.Should().Be(expected);
    }

}
