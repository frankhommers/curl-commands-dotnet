using System.Collections.Concurrent;
using System.Net;

namespace CurlCommandParser.Internal;

/// <summary>
/// Provides cached HttpClient instances configured with a proxy.
/// Thread-safe via ConcurrentDictionary.
/// </summary>
internal static class ProxyHttpClientFactory
{
  private static readonly ConcurrentDictionary<string, HttpClient> _clients = new();

  /// <summary>
  /// Gets or creates an HttpClient configured with the specified proxy URL.
  /// </summary>
  public static HttpClient GetClient(string proxyUrl, bool insecure = false, string? credentials = null)
  {
    string cacheKey = $"{proxyUrl}|{insecure}|{credentials ?? ""}";
    return _clients.GetOrAdd(cacheKey, _ =>
    {
      WebProxy proxy = new(proxyUrl);
      if (!string.IsNullOrEmpty(credentials))
      {
        int colonIndex = credentials!.IndexOf(':');
        if (colonIndex >= 0)
        {
          string user = credentials.Substring(0, colonIndex);
          string pass = credentials.Substring(colonIndex + 1);
          proxy.Credentials = new NetworkCredential(user, pass);
        }
      }

      HttpClientHandler handler = new()
      {
        Proxy = proxy,
        UseProxy = true,
      };

      if (insecure)
      {
        handler.ServerCertificateCustomValidationCallback = (_, _, _, _) => true;
      }

      return new HttpClient(handler);
    });
  }
}
