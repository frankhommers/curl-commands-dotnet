using CurlDotNet.Building;
using CurlDotNet.Internal;
using CurlDotNet.Models;
using CurlDotNet.Parsing;

namespace CurlDotNet;

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
    public static async Task<HttpResponseMessage> ExecuteCurlAsync(
        this HttpClient httpClient,
        string curlCommand,
        CancellationToken cancellationToken = default)
    {
        var options = CurlOptionParser.Parse(curlCommand);
        var request = HttpRequestBuilder.Build(options);
        var client = ResolveClient(httpClient, options);

        using var timeoutCts = CreateTimeoutCts(options, cancellationToken);
        var token = timeoutCts?.Token ?? cancellationToken;

        var response = await client.SendAsync(request, token);

        if (options.FollowRedirects && IsRedirect(response))
        {
            response = await FollowRedirectsAsync(client, response, options, token);
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
        using var response = await ExecuteCurlAsync(httpClient, curlCommand, cancellationToken);
        // Response is disposed — fire and forget
    }

    private static HttpClient ResolveClient(HttpClient httpClient, CurlOptions options)
    {
        if (options.Insecure)
        {
            return InsecureHttpClientFactory.GetClient();
        }
        return httpClient;
    }

    private static CancellationTokenSource? CreateTimeoutCts(CurlOptions options, CancellationToken cancellationToken)
    {
        var timeoutSeconds = options.MaxTimeSeconds ?? options.ConnectTimeoutSeconds;
        if (timeoutSeconds.HasValue)
        {
            var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds.Value));
            return cts;
        }
        return null;
    }

    private static bool IsRedirect(HttpResponseMessage response)
    {
        var code = (int)response.StatusCode;
        return code >= 300 && code < 400 && response.Headers.Location != null;
    }

    private static async Task<HttpResponseMessage> FollowRedirectsAsync(
        HttpClient client,
        HttpResponseMessage response,
        CurlOptions options,
        CancellationToken cancellationToken,
        int maxRedirects = 20)
    {
        var redirectCount = 0;
        while (IsRedirect(response) && redirectCount < maxRedirects)
        {
            var location = response.Headers.Location!;
            response.Dispose();

            var redirectRequest = new HttpRequestMessage(HttpMethod.Get, location);
            response = await client.SendAsync(redirectRequest, cancellationToken);
            redirectCount++;
        }
        return response;
    }
}
