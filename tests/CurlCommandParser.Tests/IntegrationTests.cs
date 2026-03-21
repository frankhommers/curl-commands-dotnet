using System.Net;
using System.Text;
using System.Text.Json;
using CurlCommandParser;
using CurlCommandParser.Exceptions;

namespace CurlCommandParser.Tests;

public class IntegrationTests
{
  [Fact]
  public async Task ExecuteCurlAsync_SimpleGet_ReturnsResponse()
  {
    using HttpClient httpClient = new(
      new FakeHandler(request =>
      {
        Assert.Equal(HttpMethod.Get, request.Method);
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
          Content = new StringContent("{\"url\":\"https://api.example.com/get\"}"),
        };
      }));

    HttpResponseMessage response = await httpClient.ExecuteCurlAsync("https://api.example.com/get");

    Assert.True(response.IsSuccessStatusCode);
    Assert.NotNull(response.Content);
  }

  [Fact]
  public async Task ExecuteCurlAsync_PostWithJsonBody_SendsCorrectly()
  {
    using HttpClient httpClient = new(
      new FakeHandler(async request =>
      {
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("application/json", request.Content!.Headers.ContentType!.MediaType);
        string body = await request.Content.ReadAsStringAsync();
        Assert.Contains("test", body);
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
          Content = new StringContent("{\"data\":{\"test\":\"value\"}}"),
        };
      }));

    HttpResponseMessage response = await httpClient.ExecuteCurlAsync(
      "curl -X POST https://api.example.com/post " +
      "-H 'Content-Type: application/json' " +
      "-d '{\"test\":\"value\"}'");

    Assert.True(response.IsSuccessStatusCode);
    string responseBody = await response.Content.ReadAsStringAsync();
    Assert.Contains("\"test\"", responseBody);
  }

  [Fact]
  public async Task ExecuteCurlAsync_WithBasicAuth_SendsAuthHeader()
  {
    using HttpClient httpClient = new(
      new FakeHandler(request =>
      {
        Assert.NotNull(request.Headers.Authorization);
        Assert.Equal("Basic", request.Headers.Authorization.Scheme);
        string decoded = Encoding.UTF8.GetString(
          Convert.FromBase64String(request.Headers.Authorization.Parameter!));
        Assert.Equal("testuser:testpass", decoded);
        return new HttpResponseMessage(HttpStatusCode.OK);
      }));

    HttpResponseMessage response = await httpClient.ExecuteCurlAsync(
      "-u testuser:testpass https://api.example.com/auth");

    Assert.True(response.IsSuccessStatusCode);
  }

  [Fact]
  public async Task ExecuteCurlAsync_WithCustomHeaders_SendsHeaders()
  {
    using HttpClient httpClient = new(
      new FakeHandler(request =>
      {
        Assert.True(request.Headers.Contains("X-Custom-Header"));
        string headerValue = request.Headers.GetValues("X-Custom-Header").First();
        Assert.Equal("test123", headerValue);
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
          Content = new StringContent("{\"headers\":{\"X-Custom-Header\":\"test123\"}}"),
        };
      }));

    HttpResponseMessage response = await httpClient.ExecuteCurlAsync(
      "-H 'X-Custom-Header: test123' https://api.example.com/headers");

    Assert.True(response.IsSuccessStatusCode);
    string body = await response.Content.ReadAsStringAsync();
    Assert.Contains("test123", body);
  }

  [Fact]
  public async Task ExecuteCurlAsync_InvalidCommand_ThrowsCurlParseException()
  {
    using HttpClient httpClient = new();

    await Assert.ThrowsAsync<CurlParseException>(() => httpClient.ExecuteCurlAsync("-X GET"));
  }

  [Fact]
  public async Task ExecuteCurlAsync_WithTimeout_AppliesTimeout()
  {
    using HttpClient httpClient = new(
      new FakeHandler(_ =>
                        new HttpResponseMessage(HttpStatusCode.OK)));

    HttpResponseMessage response = await httpClient.ExecuteCurlAsync(
      "--max-time 10 https://api.example.com/get");

    Assert.True(response.IsSuccessStatusCode);
  }

  [Fact]
  public async Task ExecuteCurlAsync_WithCancellationToken_Cancellable()
  {
    using HttpClient httpClient = new(
      new FakeHandler(async _ =>
      {
        await Task.Delay(5000);
        return new HttpResponseMessage(HttpStatusCode.OK);
      }));
    using CancellationTokenSource cts = new();
    cts.Cancel();

    await Assert.ThrowsAnyAsync<OperationCanceledException>(() => httpClient.ExecuteCurlAsync(
                                                              "https://api.example.com/get",
                                                              cts.Token));
  }

  [Fact]
  public async Task ExecuteCurlAndForgetAsync_DoesNotThrow()
  {
    using HttpClient httpClient = new(
      new FakeHandler(request =>
      {
        Assert.Equal(HttpMethod.Post, request.Method);
        return new HttpResponseMessage(HttpStatusCode.OK);
      }));

    await httpClient.ExecuteCurlAndForgetAsync(
      "-X POST https://api.example.com/post -d 'fire-and-forget'");
  }

  [Fact]
  public async Task ExecuteCurlAsync_WithBearerToken_SendsAuthHeader()
  {
    using HttpClient httpClient = new(
      new FakeHandler(request =>
      {
        Assert.NotNull(request.Headers.Authorization);
        Assert.Equal("Bearer", request.Headers.Authorization.Scheme);
        Assert.Equal("mytoken", request.Headers.Authorization.Parameter);
        return new HttpResponseMessage(HttpStatusCode.OK);
      }));

    HttpResponseMessage response = await httpClient.ExecuteCurlAsync(
      "--oauth2-bearer mytoken https://api.example.com/data");

    Assert.True(response.IsSuccessStatusCode);
  }

  [Fact]
  public async Task ExecuteCurlAsync_WithCookie_SendsCookieHeader()
  {
    using HttpClient httpClient = new(
      new FakeHandler(request =>
      {
        Assert.True(request.Headers.Contains("Cookie"));
        Assert.Equal("session=abc123", request.Headers.GetValues("Cookie").First());
        return new HttpResponseMessage(HttpStatusCode.OK);
      }));

    HttpResponseMessage response = await httpClient.ExecuteCurlAsync(
      "-b 'session=abc123' https://api.example.com/data");

    Assert.True(response.IsSuccessStatusCode);
  }

  [Fact]
  public async Task ExecuteCurlAsync_WithCompressed_SendsAcceptEncoding()
  {
    using HttpClient httpClient = new(
      new FakeHandler(request =>
      {
        List<string> encodings = request.Headers.AcceptEncoding
          .Select(e => e.Value).ToList();
        Assert.Contains("gzip", encodings);
        Assert.Contains("deflate", encodings);
        return new HttpResponseMessage(HttpStatusCode.OK);
      }));

    HttpResponseMessage response = await httpClient.ExecuteCurlAsync(
      "--compressed https://api.example.com/data");

    Assert.True(response.IsSuccessStatusCode);
  }

  [Fact]
  public async Task ExecuteCurlAsync_ComplexRealWorld_AllOptionsSentCorrectly()
  {
    using HttpClient httpClient = new(
      new FakeHandler(async request =>
      {
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("Bearer mytoken", request.Headers.Authorization!.ToString());
        Assert.Equal("application/json", request.Content!.Headers.ContentType!.MediaType);

        string body = await request.Content.ReadAsStringAsync();
        Assert.Contains("name", body);

        return new HttpResponseMessage(HttpStatusCode.OK)
        {
          Content = new StringContent("{\"success\":true}"),
        };
      }));

    HttpResponseMessage response = await httpClient.ExecuteCurlAsync(
      "curl -X POST https://api.example.com/users " +
      "-H 'Content-Type: application/json' " +
      "--oauth2-bearer mytoken " +
      "-d '{\"name\":\"Frank\"}' " +
      "--compressed --max-time 30");

    Assert.True(response.IsSuccessStatusCode);
  }

  [Fact]
  public async Task ExecuteCurlAsync_DataUrlEncode_EncodesAndSends()
  {
    using HttpClient httpClient = new(
      new FakeHandler(async request =>
      {
        Assert.Equal(HttpMethod.Post, request.Method);
        string body = await request.Content!.ReadAsStringAsync();
        Assert.Equal("name=Frank%20Hommers&city=Den%20Haag", body);
        Assert.Equal("application/x-www-form-urlencoded", request.Content.Headers.ContentType!.MediaType);
        return new HttpResponseMessage(HttpStatusCode.OK);
      }));

    HttpResponseMessage response = await httpClient.ExecuteCurlAsync(
      "--data-urlencode 'name=Frank Hommers' --data-urlencode 'city=Den Haag' https://api.example.com/search");

    Assert.True(response.IsSuccessStatusCode);
  }

  /// <summary>
  /// A fake HttpMessageHandler that delegates to a provided function.
  /// </summary>
  private class FakeHandler : HttpMessageHandler
  {
    private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> _handler;

    public FakeHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
    {
      _handler = request => Task.FromResult(handler(request));
    }

    public FakeHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> handler)
    {
      _handler = handler;
    }

    protected override Task<HttpResponseMessage> SendAsync(
      HttpRequestMessage request,
      CancellationToken cancellationToken)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return _handler(request);
    }
  }
}