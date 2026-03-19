namespace CurlDotNet.Internal;

/// <summary>
/// Provides a cached HttpClient that skips SSL certificate validation.
/// Thread-safe via Lazy initialization.
/// </summary>
internal static class InsecureHttpClientFactory
{
    private static readonly Lazy<HttpClient> LazyClient = new(() =>
    {
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (_, _, _, _) => true
        };
        return new HttpClient(handler);
    });

    public static HttpClient GetClient() => LazyClient.Value;
}
