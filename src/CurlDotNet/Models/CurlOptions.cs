namespace CurlDotNet.Models;

/// <summary>
/// Intermediate representation of a parsed curl command.
/// </summary>
public class CurlOptions
{
    public string Url { get; set; } = string.Empty;
    public string? Method { get; set; }
    public List<(string Name, string Value)> Headers { get; set; } = new();
    public string? DataBody { get; set; }
    public byte[]? BinaryData { get; set; }
    public string? UserCredentials { get; set; }
    public string? BearerToken { get; set; }
    public List<FormField> FormFields { get; set; } = new();
    public bool FollowRedirects { get; set; }
    public bool Insecure { get; set; }
    public int? ConnectTimeoutSeconds { get; set; }
    public int? MaxTimeSeconds { get; set; }
    public string? Cookie { get; set; }
    public string? UserAgent { get; set; }
    public string? Referer { get; set; }
    public bool Compressed { get; set; }
}
