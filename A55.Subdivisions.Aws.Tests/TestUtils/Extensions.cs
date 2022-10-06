namespace A55.Subdivisions.Aws.Tests.TestUtils;

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
