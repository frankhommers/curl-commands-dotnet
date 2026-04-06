namespace CurlCommand.Models;

/// <summary>
/// Represents a single -F / --form field.
/// </summary>
public class FormField
{
  /// <summary>The field name.</summary>
  public string Name { get; set; } = string.Empty;

  /// <summary>The field value, or file path if <see cref="IsFile"/> is true.</summary>
  public string Value { get; set; } = string.Empty;

  /// <summary>Whether this field references a file (e.g., file=@/path/to/file).</summary>
  public bool IsFile { get; set; }

  /// <summary>Optional MIME content type for the field.</summary>
  public string? ContentType { get; set; }
}