using CurlCommandParser.Building;
using CurlCommandParser.Internal;
using CurlCommandParser.Models;
using CurlCommandParser.Parsing;

namespace CurlCommandParser;

/// <summary>
/// Extension methods on HttpClient for executing curl commands.
/// </summary>
public static class CurlHttpClientExtensions
{
  /// <summary>
  /// Parses and executes a curl command string, returning the HttpResponseMessage.
  /// The "curl" prefix is optional.
  /// </summary>
  /// <param name="httpClient">The HttpClient to use for the request.</param>
  /// <param name="curlCommand">The curl command string (e.g., "-X POST https://example.com -d '{}'").</param>
  /// <param name="cancellationToken">Optional cancellation token.</param>
  /// <returns>The HttpResponseMessage from the executed request.</returns>
  /// <remarks>
  /// Redirect behavior (-L/--location) follows the HttpClient's handler configuration.
  /// HttpClientHandler.AllowAutoRedirect defaults to true in .NET, so redirects are
  /// followed automatically. To disable redirects, configure the HttpClientHandler
  /// with AllowAutoRedirect = false.
  /// </remarks>
  public static async Task<HttpResponseMessage> ExecuteCurlAsync(
    this HttpClient httpClient,
    string curlCommand,
    CancellationToken cancellationToken = default)
  {
    CurlOptions options = CurlOptionParser.Parse(curlCommand);
    HttpRequestMessage request = HttpRequestBuilder.Build(options);
    HttpClient client = ResolveClient(httpClient, options);

    using CancellationTokenSource? timeoutCts = CreateTimeoutCts(options, cancellationToken);
    CancellationToken token = timeoutCts?.Token ?? cancellationToken;

    return await client.SendAsync(request, token).ConfigureAwait(false);
  }

  /// <summary>
  /// Parses and executes a curl command string, discarding the response (fire-and-forget).
  /// The "curl" prefix is optional.
  /// </summary>
  /// <param name="httpClient">The HttpClient to use for the request.</param>
  /// <param name="curlCommand">The curl command string.</param>
  /// <param name="cancellationToken">Optional cancellation token.</param>
  public static async Task ExecuteCurlAndForgetAsync(
    this HttpClient httpClient,
    string curlCommand,
    CancellationToken cancellationToken = default)
  {
    using HttpResponseMessage response = await ExecuteCurlAsync(httpClient, curlCommand, cancellationToken)
      .ConfigureAwait(false);
    // Response is disposed — fire and forget
  }

  private static HttpClient ResolveClient(HttpClient httpClient, CurlOptions options)
  {
    if (!string.IsNullOrEmpty(options.ProxyUrl))
    {
      return ProxyHttpClientFactory.GetClient(options.ProxyUrl!, options.Insecure);
    }

    return options.Insecure ? InsecureHttpClientFactory.GetClient() : httpClient;
  }

  private static CancellationTokenSource? CreateTimeoutCts(CurlOptions options, CancellationToken cancellationToken)
  {
    int? timeoutSeconds = options.MaxTimeSeconds ?? options.ConnectTimeoutSeconds;
    if (timeoutSeconds.HasValue)
    {
      CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
      cts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds.Value));
      return cts;
    }

    return null;
  }
}