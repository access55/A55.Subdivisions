using System.Globalization;

namespace A55.Subdivisions.Aws.Extensions;

static class StringExtensions
{
    public static string ToPascalCase(this string snakeName) =>
        string.Concat(snakeName.ToLowerInvariant().Split('_')
            .Select(CultureInfo.InvariantCulture.TextInfo.ToTitleCase));

    public static string TrimUnderscores(this string text) => text.Trim('_');

    public static string ToSnakeCase(this string str)
        => string.Concat(str
                .Select((x, i) =>
                    i > 0 && i < str.Length - 1 && char.IsUpper(x) && !char.IsUpper(str[i - 1])
                        ? $"_{x}"
                        : x.ToString()))
            .ToLowerInvariant();
}
