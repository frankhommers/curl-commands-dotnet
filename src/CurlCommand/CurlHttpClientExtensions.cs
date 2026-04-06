using CurlCommand.Building;
using CurlCommand.Internal;
using CurlCommand.Models;
using CurlCommand.Parsing;

namespace CurlCommand;

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

    HttpResponseMessage response = await client.SendAsync(request, token).ConfigureAwait(false);

    if (!string.IsNullOrEmpty(options.OutputFile))
    {
      await SaveResponseToFileAsync(response, options.OutputFile!, token).ConfigureAwait(false);
    }

    return response;
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
    if (!string.IsNullOrEmpty(options.CertificateFile))
    {
      return CertificateHttpClientFactory.GetClient(
        options.CertificateFile!,
        keyFile: options.KeyFile,
        password: options.CertificatePassword,
        insecure: options.Insecure);
    }

    if (!string.IsNullOrEmpty(options.ProxyUrl))
    {
      return ProxyHttpClientFactory.GetClient(options.ProxyUrl!, options.Insecure, options.ProxyUserCredentials);
    }

    return options.Insecure ? InsecureHttpClientFactory.GetClient() : httpClient;
  }

  private static async Task SaveResponseToFileAsync(
    HttpResponseMessage response,
    string filePath,
    CancellationToken cancellationToken)
  {
    byte[] content = await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
    string? directory = Path.GetDirectoryName(filePath);
    if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
    {
      Directory.CreateDirectory(directory);
    }
#if NET8_0_OR_GREATER
    await File.WriteAllBytesAsync(filePath, content, cancellationToken).ConfigureAwait(false);
#else
    File.WriteAllBytes(filePath, content);
#endif
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