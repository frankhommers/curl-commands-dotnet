namespace CurlDotNet.Exceptions;

/// <summary>
/// Thrown when a curl command string cannot be parsed.
/// </summary>
public class CurlParseException : Exception
{
    /// <summary>The zero-based token position where the error occurred, if available.</summary>
    public int? TokenPosition { get; }

    /// <summary>Creates a new <see cref="CurlParseException"/> with the specified message.</summary>
    public CurlParseException(string message) : base(message)
    {
    }

    /// <summary>Creates a new <see cref="CurlParseException"/> with the specified message and token position.</summary>
    public CurlParseException(string message, int tokenPosition)
        : base(message)
    {
        TokenPosition = tokenPosition;
    }

    /// <summary>Creates a new <see cref="CurlParseException"/> with the specified message and inner exception.</summary>
    public CurlParseException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
