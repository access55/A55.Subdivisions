using System.Globalization;
using Bogus;
using FakeItEasy.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Subdivisions.Models;

namespace Subdivisions.Aws.Tests.TestUtils;

public static class Extensions
{
    public static string OnlyLetterOrDigit(this string str) =>
        string.Concat(str.Where(char.IsLetterOrDigit));

    public static JToken AsJToken(this string json) => JToken.Parse(json);

    public static string Concat(this IEnumerable<string> strings, string separator = "") =>
        string.Join(separator, strings);

    public static string ToTitleCase(this string str) =>
        CultureInfo.InvariantCulture.TextInfo.ToTitleCase(str);

    public static IVoidArgumentValidationConfiguration CalledWith<T>(this ILogger<T> logger, LogLevel level,
        Exception? exception = null) =>
        A.CallTo(logger)
            .Where(call => call.Method.Name == "Log" &&
                           call.GetArgument<LogLevel>("logLevel") == level &&
                           call.GetArgument<Exception?>("exception") == exception
            );
}

public static class FakerExtensions
{
    public static string TopicNameString(this Faker faker) =>
        $"{faker.Person.FirstName}_{faker.Random.Guid():N}".ToLowerInvariant();

    internal static TopicId TopicName(this Faker faker, SubConfig config) =>
        new(faker.TopicNameString(), config);

    public static IEnumerable<int> Range(this Faker faker, int min, int max) =>
        Enumerable.Range(0, faker.Random.Int(min, max));

    public static IEnumerable<int> Range(this Faker faker, int max) =>
        faker.Range(0, max);

    public static IEnumerable<T> Items<T>(this Randomizer faker, IEnumerable<T> items, int? count = null) =>
        faker.ListItems(items.ToList(), count);
}

public static class FluentAssertionsComparer
{
    public static T IsEquivalentTo<T>(this IArgumentConstraintManager<T> compare, T value)
    {
        var message = string.Empty;
        return compare.Matches(x => CompareByValue(x, value, out message), x => x.Write(message));
    }

    public static T IsEquivalentInOrder<T>(this IArgumentConstraintManager<T> compare, T value)
    {
        var message = string.Empty;
        return compare.Matches(x => CompareByValue(x, value, out message, true), x => x.Write(message));
    }

    static bool CompareByValue<T>(T sut, T expected, out string message, bool strictOrdering = false)
    {
        try
        {
            if (strictOrdering)
                sut.Should().BeEquivalentTo(expected, opt => opt.WithStrictOrdering());
            else
                sut.Should().BeEquivalentTo(expected);

            message = string.Empty;
            return true;
        }
        catch (Exception e)
        {
            message = e.Message;
            return false;
        }
    }
}
