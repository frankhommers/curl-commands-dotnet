namespace CurlDotNet.Models;

/// <summary>
/// Represents a single -F / --form field.
/// </summary>
public class FormField
{
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public bool IsFile { get; set; }
    public string? ContentType { get; set; }
}
