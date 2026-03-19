namespace CurlDotNet.Exceptions;

/// <summary>
/// Thrown when a curl command string cannot be parsed.
/// </summary>
public class CurlParseException : Exception
{
    public int? TokenPosition { get; }

    public CurlParseException(string message) : base(message) { }

    public CurlParseException(string message, int tokenPosition)
        : base(message)
    {
        TokenPosition = tokenPosition;
    }

    public CurlParseException(string message, Exception innerException)
        : base(message, innerException) { }
}
