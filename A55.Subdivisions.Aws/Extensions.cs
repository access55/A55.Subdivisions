using System.Globalization;

namespace A55.Subdivisions.Aws;

static class Extensions
{
    public static string SnakeToPascalCase(this string snakeName) =>
        string.Concat(snakeName.ToLowerInvariant().Split('_')
            .Select(CultureInfo.InvariantCulture.TextInfo.ToTitleCase));
}
