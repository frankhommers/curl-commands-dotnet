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
  public static HttpClient GetClient(string proxyUrl, bool insecure = false)
  {
    string cacheKey = insecure ? $"{proxyUrl}|insecure" : proxyUrl;
    return _clients.GetOrAdd(cacheKey, _ =>
    {
      HttpClientHandler handler = new()
      {
        Proxy = new WebProxy(proxyUrl),
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
