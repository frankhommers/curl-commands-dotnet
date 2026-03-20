using CurlDotNet;
using CurlDotNet.Exceptions;

namespace CurlDotNet.Tests;

public class IntegrationTests
{
    [Fact]
    public async Task ExecuteCurlAsync_SimpleGet_ReturnsResponse()
    {
        using HttpClient httpClient = new();
        HttpResponseMessage response = await httpClient.ExecuteCurlAsync("https://httpbin.org/get");

        Assert.True(response.IsSuccessStatusCode);
        Assert.NotNull(response.Content);
    }

    [Fact]
    public async Task ExecuteCurlAsync_PostWithJsonBody_SendsCorrectly()
    {
        using HttpClient httpClient = new();
        HttpResponseMessage response = await httpClient.ExecuteCurlAsync(
            "curl -X POST https://httpbin.org/post " +
            "-H 'Content-Type: application/json' " +
            "-d '{\"test\":\"value\"}'");

        Assert.True(response.IsSuccessStatusCode);
        string body = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"test\"", body);
    }

    [Fact]
    public async Task ExecuteCurlAsync_WithBasicAuth_SendsAuthHeader()
    {
        using HttpClient httpClient = new();
        HttpResponseMessage response = await httpClient.ExecuteCurlAsync(
            "-u testuser:testpass https://httpbin.org/basic-auth/testuser/testpass");

        Assert.True(response.IsSuccessStatusCode);
    }

    [Fact]
    public async Task ExecuteCurlAsync_WithCustomHeaders_SendsHeaders()
    {
        using HttpClient httpClient = new();
        HttpResponseMessage response = await httpClient.ExecuteCurlAsync(
            "-H 'X-Custom-Header: test123' https://httpbin.org/headers");

        Assert.True(response.IsSuccessStatusCode);
        string body = await response.Content.ReadAsStringAsync();
        Assert.Contains("test123", body);
    }

    [Fact]
    public async Task ExecuteCurlAsync_InvalidCommand_ThrowsCurlParseException()
    {
        using HttpClient httpClient = new();

        await Assert.ThrowsAsync<CurlParseException>(
            () => httpClient.ExecuteCurlAsync("-X GET"));
    }

    [Fact]
    public async Task ExecuteCurlAsync_WithTimeout_AppliesTimeout()
    {
        using HttpClient httpClient = new();
        HttpResponseMessage response = await httpClient.ExecuteCurlAsync(
            "--max-time 10 https://httpbin.org/get");

        Assert.True(response.IsSuccessStatusCode);
    }

    [Fact]
    public async Task ExecuteCurlAsync_WithCancellationToken_Cancellable()
    {
        using HttpClient httpClient = new();
        using CancellationTokenSource cts = new();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => httpClient.ExecuteCurlAsync("https://httpbin.org/get", cts.Token));
    }

    [Fact]
    public async Task ExecuteCurlAndForgetAsync_DoesNotThrow()
    {
        using HttpClient httpClient = new();

        // Should not throw, just fire and forget
        await httpClient.ExecuteCurlAndForgetAsync(
            "-X POST https://httpbin.org/post -d 'fire-and-forget'");
    }
}
