using CurlDotNet.Building;
using CurlDotNet.Models;

namespace CurlDotNet.Tests;

public class HttpRequestBuilderTests
{
    [Fact]
    public void Build_SimpleGet_CreatesGetRequest()
    {
        var options = new CurlOptions { Url = "https://api.example.com" };
        var request = HttpRequestBuilder.Build(options);

        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Equal("https://api.example.com/", request.RequestUri!.ToString());
    }

    [Fact]
    public void Build_ExplicitPost_CreatesPostRequest()
    {
        var options = new CurlOptions { Url = "https://api.example.com", Method = "POST" };
        var request = HttpRequestBuilder.Build(options);

        Assert.Equal(HttpMethod.Post, request.Method);
    }

    [Fact]
    public void Build_DataBodyWithoutMethod_ImpliesPost()
    {
        var options = new CurlOptions
        {
            Url = "https://api.example.com",
            DataBody = "{\"key\":\"value\"}"
        };
        var request = HttpRequestBuilder.Build(options);

        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.NotNull(request.Content);
    }

    [Fact]
    public async Task Build_DataBody_SetsStringContent()
    {
        var options = new CurlOptions
        {
            Url = "https://api.example.com",
            DataBody = "{\"key\":\"value\"}"
        };
        var request = HttpRequestBuilder.Build(options);

        var content = await request.Content!.ReadAsStringAsync();
        Assert.Equal("{\"key\":\"value\"}", content);
    }

    [Fact]
    public void Build_Headers_AddsToRequest()
    {
        var options = new CurlOptions
        {
            Url = "https://api.example.com",
            Headers = { ("X-Custom", "test-value") }
        };
        var request = HttpRequestBuilder.Build(options);

        Assert.Equal("test-value", request.Headers.GetValues("X-Custom").First());
    }

    [Fact]
    public async Task Build_ContentTypeHeader_SetsOnContent()
    {
        var options = new CurlOptions
        {
            Url = "https://api.example.com",
            DataBody = "{}",
            Headers = { ("Content-Type", "application/json") }
        };
        var request = HttpRequestBuilder.Build(options);

        Assert.Equal("application/json", request.Content!.Headers.ContentType!.MediaType);
    }

    [Fact]
    public void Build_BasicAuth_SetsAuthorizationHeader()
    {
        var options = new CurlOptions
        {
            Url = "https://api.example.com",
            UserCredentials = "admin:secret"
        };
        var request = HttpRequestBuilder.Build(options);

        Assert.Equal("Basic", request.Headers.Authorization!.Scheme);
        var decoded = System.Text.Encoding.UTF8.GetString(
            Convert.FromBase64String(request.Headers.Authorization.Parameter!));
        Assert.Equal("admin:secret", decoded);
    }

    [Fact]
    public void Build_BearerToken_SetsAuthorizationHeader()
    {
        var options = new CurlOptions
        {
            Url = "https://api.example.com",
            BearerToken = "mytoken123"
        };
        var request = HttpRequestBuilder.Build(options);

        Assert.Equal("Bearer", request.Headers.Authorization!.Scheme);
        Assert.Equal("mytoken123", request.Headers.Authorization.Parameter);
    }

    [Fact]
    public void Build_Cookie_SetsCookieHeader()
    {
        var options = new CurlOptions
        {
            Url = "https://api.example.com",
            Cookie = "session=abc123"
        };
        var request = HttpRequestBuilder.Build(options);

        Assert.Equal("session=abc123", request.Headers.GetValues("Cookie").First());
    }

    [Fact]
    public void Build_UserAgent_SetsUserAgentHeader()
    {
        var options = new CurlOptions
        {
            Url = "https://api.example.com",
            UserAgent = "MyApp/1.0"
        };
        var request = HttpRequestBuilder.Build(options);

        Assert.Equal("MyApp/1.0", request.Headers.UserAgent.ToString());
    }

    [Fact]
    public void Build_Referer_SetsRefererHeader()
    {
        var options = new CurlOptions
        {
            Url = "https://api.example.com",
            Referer = "https://google.com"
        };
        var request = HttpRequestBuilder.Build(options);

        Assert.Equal(new Uri("https://google.com"), request.Headers.Referrer);
    }

    [Fact]
    public void Build_Compressed_SetsAcceptEncoding()
    {
        var options = new CurlOptions
        {
            Url = "https://api.example.com",
            Compressed = true
        };
        var request = HttpRequestBuilder.Build(options);

        var encodings = request.Headers.AcceptEncoding.Select(e => e.Value).ToList();
        Assert.Contains("gzip", encodings);
        Assert.Contains("deflate", encodings);
    }

    [Fact]
    public void Build_FormFields_CreatesMultipartContent()
    {
        var options = new CurlOptions
        {
            Url = "https://api.example.com",
            FormFields =
            {
                new FormField { Name = "name", Value = "John" },
                new FormField { Name = "age", Value = "30" }
            }
        };
        var request = HttpRequestBuilder.Build(options);

        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.IsType<MultipartFormDataContent>(request.Content);
    }

    [Fact]
    public void Build_ExplicitMethodWithData_UsesExplicitMethod()
    {
        var options = new CurlOptions
        {
            Url = "https://api.example.com",
            Method = "PUT",
            DataBody = "{}"
        };
        var request = HttpRequestBuilder.Build(options);

        Assert.Equal(HttpMethod.Put, request.Method);
    }

    [Fact]
    public void Build_BinaryData_SetsByteArrayContent()
    {
        var data = new byte[] { 0x01, 0x02, 0x03 };
        var options = new CurlOptions
        {
            Url = "https://api.example.com",
            BinaryData = data
        };
        var request = HttpRequestBuilder.Build(options);

        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.IsType<ByteArrayContent>(request.Content);
    }
}
