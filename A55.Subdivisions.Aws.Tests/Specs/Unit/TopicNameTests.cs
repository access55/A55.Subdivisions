using A55.Subdivisions.Aws.Models;

namespace A55.Subdivisions.Aws.Tests.Specs.Unit;

public class TopicNameTests
{
    public static readonly SubTopicNameConfig EmptyConfig =
        new() { Prefix = string.Empty, Suffix = string.Empty, Source = "source" };

    [TestCase("0name")]
    [TestCase("name@bad")]
    [TestCase("name$bad")]
    [TestCase("")]
    [TestCase("a")]
    [TestCase("ab")]
    [TestCase("abcde")]
    public void ShouldThrowIfBadName(string badName)
    {
        var action = () => new TopicName(badName, EmptyConfig);
        action.Should().Throw<ArgumentException>();
    }

    [TestCase("good_name")]
    [TestCase("abcdef")]
    [TestCase("abc123")]
    public void ShouldNotThrowIfGoodName(string goodName)
    {
        var action = () => new TopicName(goodName, EmptyConfig);
        action.Should().NotThrow();
    }

    [Test]
    public void ShouldNormalizeName()
    {
        const string name = "NameToNormalize";
        const string expected = "name_to_normalize";
        var topic = new TopicName(name, EmptyConfig);

        topic.Topic.Should().Be(expected);
    }

    [Test]
    public void ShouldNormalizeNameWithPrefixAndSufix()
    {
        const string prefix = "ThePrefix";
        const string sufix = "TheSufix";
        const string name = "NameToNormalize";
        const string expected = "ThePrefixNameToNormalizeTheSufix";
        var topic = new TopicName(name, new SubTopicNameConfig { Prefix = prefix, Suffix = sufix, Source = "source" });

        topic.FullTopicName.Should().Be(expected);
    }

    [Test]
    public void ShouldNormalizeQueueName()
    {
        const string prefix = "ThePrefix";
        const string suffix = "TheSuffix";
        const string source = "TheSource";
        const string name = "NameToNormalize";
        const string expected = "the_prefix_the_source_name_to_normalize_the_suffix";

        var topic = new TopicName(name, new SubTopicNameConfig { Prefix = prefix, Suffix = suffix, Source = source });

        topic.FullQueueName.Should().Be(expected);
    }

    [TestCase("ThePrefix", "")]
    [TestCase("", "TheSuffix")]
    [TestCase("", "")]
    public void ShouldNotHaveUnderscoreAtEdges(string prefix, string suffix)
    {
        const string source = "TheSource";
        const string name = "NameToNormalize";

        var topic = new TopicName(name, new SubTopicNameConfig { Prefix = "", Suffix = suffix, Source = source });

        topic.FullQueueName.Should().NotStartWith("_").And.NotEndWith("_");
    }

    [Test]
    public void ShouldInferSourceNameByAssembly()
    {
        const string prefix = "ThePrefix";
        const string suffix = "TheSuffix";
        const string sourceFallBack = "TheFallback";
        const string name = "NameToNormalize";
        const string expected = "the_prefix_the_fallback_name_to_normalize_the_suffix";
        var config = new SubTopicNameConfig { Prefix = prefix, Suffix = suffix, Source = null };
        config.SetFallbackSource(sourceFallBack);
        var topic = new TopicName(name, config);

        topic.FullQueueName.Should().Be(expected);
    }
}
