using System.Diagnostics;
using System.Reflection;

namespace Subdivisions;

class Diagnostic
{
    public static readonly ActivitySource ActivitySource = new(
        "A55.Subdivisions", Assembly.GetExecutingAssembly().GetName().Version?.ToString());
}
