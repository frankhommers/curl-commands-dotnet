namespace CurlCommand.Internal;

/// <summary>
/// Provides a cached HttpClient that skips SSL certificate validation.
/// Thread-safe via Lazy initialization.
/// </summary>
internal static class InsecureHttpClientFactory
{
  private static readonly Lazy<HttpClient> _lazyClient = new(() =>
  {
    HttpClientHandler handler = new()
    {
      ServerCertificateCustomValidationCallback = (_, _, _, _) => true,
    };
    return new HttpClient(handler);
  });

  public static HttpClient GetClient()
  {
    return _lazyClient.Value;
  }
}