namespace CurlCommandParser.Models;

/// <summary>
/// Intermediate representation of a parsed curl command.
/// </summary>
public class CurlOptions
{
  /// <summary>The request URL.</summary>
  public string Url { get; set; } = string.Empty;

  /// <summary>HTTP method (-X, --request). Null means auto-detect.</summary>
  public string? Method { get; set; }

  /// <summary>Request headers (-H, --header).</summary>
  public List<(string Name, string Value)> Headers { get; set; } = [];

  /// <summary>Request body (-d, --data, --data-raw).</summary>
  public string? DataBody { get; set; }

  /// <summary>Binary request body (--data-binary).</summary>
  public byte[]? BinaryData { get; set; }

  /// <summary>Basic auth credentials (-u, --user) in "user:password" format.</summary>
  public string? UserCredentials { get; set; }

  /// <summary>Bearer token (--oauth2-bearer).</summary>
  public string? BearerToken { get; set; }

  /// <summary>Multipart form fields (-F, --form).</summary>
  public List<FormField> FormFields { get; set; } = [];

  /// <summary>Whether to follow redirects (-L, --location).</summary>
  public bool FollowRedirects { get; set; }

  /// <summary>Whether to skip SSL certificate validation (-k, --insecure).</summary>
  public bool Insecure { get; set; }

  /// <summary>Connection timeout in seconds (--connect-timeout).</summary>
  public int? ConnectTimeoutSeconds { get; set; }

  /// <summary>Maximum request time in seconds (--max-time).</summary>
  public int? MaxTimeSeconds { get; set; }

  /// <summary>Cookie header value (-b, --cookie).</summary>
  public string? Cookie { get; set; }

  /// <summary>User-Agent header (-A, --user-agent).</summary>
  public string? UserAgent { get; set; }

  /// <summary>Referer header (-e, --referer).</summary>
  public string? Referer { get; set; }

  /// <summary>Whether to request compressed responses (--compressed).</summary>
  public bool Compressed { get; set; }

  /// <summary>URL-encoded data fields (--data-urlencode). Each entry is "name=value" or just "value".</summary>
  public List<string> DataUrlEncodeFields { get; set; } = [];

  /// <summary>Proxy URL (-x, --proxy).</summary>
  public string? ProxyUrl { get; set; }
}