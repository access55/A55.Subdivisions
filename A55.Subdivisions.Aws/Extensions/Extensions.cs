using System.Globalization;

namespace A55.Subdivisions.Aws.Extensions;

static class Extensions
{
    public static string SnakeToPascalCase(this string snakeName) =>
        string.Concat(snakeName.ToLowerInvariant().Split('_')
            .Select(CultureInfo.InvariantCulture.TextInfo.ToTitleCase));

    public static string PascalToSnakeCase(this string str)
        => string.Concat(str
                .Select((x, i) =>
                    i > 0 && i < str.Length - 1 && char.IsUpper(x) && !char.IsUpper(str[i - 1])
                        ? $"_{x}"
                        : x.ToString()))
            .ToLowerInvariant();
}
