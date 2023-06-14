using System.Runtime.Serialization;

namespace Subdivisions.Models;

/// <inheritdoc />
[Serializable]
public class SubdivisionsException : Exception
{
    internal SubdivisionsException(string message) : base(message)
    {
    }

    /// <inheritdoc />
    protected SubdivisionsException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
    }
}
