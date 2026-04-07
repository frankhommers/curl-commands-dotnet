using CurlCommands.Building;
using CurlCommands.Models;

namespace CurlCommands.Tests;

public class HttpRequestBuilderTests
{
  [Fact]
  public void Build_SimpleGet_CreatesGetRequest()
  {
    CurlOptions options = new() {Url = "https://api.example.com"};
    HttpRequestMessage request = HttpRequestBuilder.Build(options);

    Assert.Equal(HttpMethod.Get, request.Method);
    Assert.Equal("https://api.example.com/", request.RequestUri!.ToString());
  }

  [Fact]
  public void Build_ExplicitPost_CreatesPostRequest()
  {
    CurlOptions options = new() {Url = "https://api.example.com", Method = "POST"};
    HttpRequestMessage request = HttpRequestBuilder.Build(options);

    Assert.Equal(HttpMethod.Post, request.Method);
  }

  [Fact]
  public void Build_DataBodyWithoutMethod_ImpliesPost()
  {
    CurlOptions options = new()
    {
      Url = "https://api.example.com",
      DataBody = "{\"key\":\"value\"}",
    };
    HttpRequestMessage request = HttpRequestBuilder.Build(options);

    Assert.Equal(HttpMethod.Post, request.Method);
    Assert.NotNull(request.Content);
  }

  [Fact]
  public async Task Build_DataBody_SetsStringContent()
  {
    CurlOptions options = new()
    {
      Url = "https://api.example.com",
      DataBody = "{\"key\":\"value\"}",
    };
    HttpRequestMessage request = HttpRequestBuilder.Build(options);

    string content = await request.Content!.ReadAsStringAsync();
    Assert.Equal("{\"key\":\"value\"}", content);
  }

  [Fact]
  public void Build_Headers_AddsToRequest()
  {
    CurlOptions options = new()
    {
      Url = "https://api.example.com",
      Headers = {("X-Custom", "test-value")},
    };
    HttpRequestMessage request = HttpRequestBuilder.Build(options);

    Assert.Equal("test-value", request.Headers.GetValues("X-Custom").First());
  }

  [Fact]
  public async Task Build_ContentTypeHeader_SetsOnContent()
  {
    CurlOptions options = new()
    {
      Url = "https://api.example.com",
      DataBody = "{}",
      Headers = {("Content-Type", "application/json")},
    };
    HttpRequestMessage request = HttpRequestBuilder.Build(options);

    Assert.Equal("application/json", request.Content!.Headers.ContentType!.MediaType);
  }

  [Fact]
  public void Build_BasicAuth_SetsAuthorizationHeader()
  {
    CurlOptions options = new()
    {
      Url = "https://api.example.com",
      UserCredentials = "admin:secret",
    };
    HttpRequestMessage request = HttpRequestBuilder.Build(options);

    Assert.Equal("Basic", request.Headers.Authorization!.Scheme);
    string decoded = System.Text.Encoding.UTF8.GetString(
      Convert.FromBase64String(request.Headers.Authorization.Parameter!));
    Assert.Equal("admin:secret", decoded);
  }

  [Fact]
  public void Build_BearerToken_SetsAuthorizationHeader()
  {
    CurlOptions options = new()
    {
      Url = "https://api.example.com",
      BearerToken = "mytoken123",
    };
    HttpRequestMessage request = HttpRequestBuilder.Build(options);

    Assert.Equal("Bearer", request.Headers.Authorization!.Scheme);
    Assert.Equal("mytoken123", request.Headers.Authorization.Parameter);
  }

  [Fact]
  public void Build_Cookie_SetsCookieHeader()
  {
    CurlOptions options = new()
    {
      Url = "https://api.example.com",
      Cookie = "session=abc123",
    };
    HttpRequestMessage request = HttpRequestBuilder.Build(options);

    Assert.Equal("session=abc123", request.Headers.GetValues("Cookie").First());
  }

  [Fact]
  public void Build_UserAgent_SetsUserAgentHeader()
  {
    CurlOptions options = new()
    {
      Url = "https://api.example.com",
      UserAgent = "MyApp/1.0",
    };
    HttpRequestMessage request = HttpRequestBuilder.Build(options);

    Assert.Equal("MyApp/1.0", request.Headers.UserAgent.ToString());
  }

  [Fact]
  public void Build_Referer_SetsRefererHeader()
  {
    CurlOptions options = new()
    {
      Url = "https://api.example.com",
      Referer = "https://google.com",
    };
    HttpRequestMessage request = HttpRequestBuilder.Build(options);

    Assert.Equal(new Uri("https://google.com"), request.Headers.Referrer);
  }

  [Fact]
  public void Build_Compressed_SetsAcceptEncoding()
  {
    CurlOptions options = new()
    {
      Url = "https://api.example.com",
      Compressed = true,
    };
    HttpRequestMessage request = HttpRequestBuilder.Build(options);

    List<string> encodings = request.Headers.AcceptEncoding.Select(e => e.Value!).ToList();
    Assert.Contains("gzip", encodings);
    Assert.Contains("deflate", encodings);
  }

  [Fact]
  public void Build_FormFields_CreatesMultipartContent()
  {
    CurlOptions options = new()
    {
      Url = "https://api.example.com",
      FormFields =
      {
        new FormField {Name = "name", Value = "John"},
        new FormField {Name = "age", Value = "30"},
      },
    };
    HttpRequestMessage request = HttpRequestBuilder.Build(options);

    Assert.Equal(HttpMethod.Post, request.Method);
    Assert.IsType<MultipartFormDataContent>(request.Content);
  }

  [Fact]
  public void Build_ExplicitMethodWithData_UsesExplicitMethod()
  {
    CurlOptions options = new()
    {
      Url = "https://api.example.com",
      Method = "PUT",
      DataBody = "{}",
    };
    HttpRequestMessage request = HttpRequestBuilder.Build(options);

    Assert.Equal(HttpMethod.Put, request.Method);
  }

  [Fact]
  public void Build_BinaryData_SetsByteArrayContent()
  {
    byte[] data = [0x01, 0x02, 0x03];
    CurlOptions options = new()
    {
      Url = "https://api.example.com",
      BinaryData = data,
    };
    HttpRequestMessage request = HttpRequestBuilder.Build(options);

    Assert.Equal(HttpMethod.Post, request.Method);
    Assert.IsType<ByteArrayContent>(request.Content);
  }

  [Fact]
  public async Task Build_DataUrlEncode_UrlEncodesValues()
  {
    CurlOptions options = new()
    {
      Url = "https://api.example.com",
      DataUrlEncodeFields = { "name=Frank Hommers", "city=Den Haag" },
    };
    HttpRequestMessage request = HttpRequestBuilder.Build(options);

    Assert.Equal(HttpMethod.Post, request.Method);
    string content = await request.Content!.ReadAsStringAsync();
    Assert.Equal("name=Frank%20Hommers&city=Den%20Haag", content);
  }

  [Fact]
  public async Task Build_DataUrlEncode_WithoutEquals_EncodesEntireValue()
  {
    CurlOptions options = new()
    {
      Url = "https://api.example.com",
      DataUrlEncodeFields = { "hello world" },
    };
    HttpRequestMessage request = HttpRequestBuilder.Build(options);

    string content = await request.Content!.ReadAsStringAsync();
    Assert.Equal("hello%20world", content);
  }

  [Fact]
  public async Task Build_DataUrlEncode_SetsFormUrlencodedContentType()
  {
    CurlOptions options = new()
    {
      Url = "https://api.example.com",
      DataUrlEncodeFields = { "key=value" },
    };
    HttpRequestMessage request = HttpRequestBuilder.Build(options);

    Assert.Equal("application/x-www-form-urlencoded", request.Content!.Headers.ContentType!.MediaType);
  }

  [Fact]
  public async Task Build_DataUrlEncode_CombinedWithDataBody_PrependDataBody()
  {
    CurlOptions options = new()
    {
      Url = "https://api.example.com",
      DataBody = "existing=data",
      DataUrlEncodeFields = { "name=Frank Hommers" },
    };
    HttpRequestMessage request = HttpRequestBuilder.Build(options);

    string content = await request.Content!.ReadAsStringAsync();
    Assert.Equal("existing=data&name=Frank%20Hommers", content);
  }

  [Fact]
  public void Build_DataUrlEncode_ImpliesPost()
  {
    CurlOptions options = new()
    {
      Url = "https://api.example.com",
      DataUrlEncodeFields = { "q=search term" },
    };
    HttpRequestMessage request = HttpRequestBuilder.Build(options);

    Assert.Equal(HttpMethod.Post, request.Method);
  }

  [Fact]
  public void Build_ForceGet_MovesDataToQueryString()
  {
    CurlOptions options = new()
    {
      Url = "https://api.example.com/search",
      DataBody = "q=test&lang=en",
      ForceGet = true,
    };
    HttpRequestMessage request = HttpRequestBuilder.Build(options);

    Assert.Equal(HttpMethod.Get, request.Method);
    Assert.Equal("https://api.example.com/search?q=test&lang=en", request.RequestUri!.ToString());
    Assert.Null(request.Content);
  }

  [Fact]
  public void Build_ForceGet_AppendsToExistingQueryString()
  {
    CurlOptions options = new()
    {
      Url = "https://api.example.com/search?page=1",
      DataBody = "q=test",
      ForceGet = true,
    };
    HttpRequestMessage request = HttpRequestBuilder.Build(options);

    Assert.Equal("https://api.example.com/search?page=1&q=test", request.RequestUri!.ToString());
  }

  [Fact]
  public void Build_ForceGet_WithDataUrlEncode_EncodesQueryString()
  {
    CurlOptions options = new()
    {
      Url = "https://api.example.com/search",
      DataUrlEncodeFields = { "q=hello world" },
      ForceGet = true,
    };
    HttpRequestMessage request = HttpRequestBuilder.Build(options);

    Assert.Equal(HttpMethod.Get, request.Method);
    Assert.Equal("https://api.example.com/search?q=hello%20world", request.RequestUri!.AbsoluteUri);
  }

  [Fact]
  public void Build_UploadFile_ImpliesPut()
  {
    // Create a temp file for the test
    string tempFile = Path.GetTempFileName();
    File.WriteAllText(tempFile, "file content");
    try
    {
      CurlOptions options = new()
      {
        Url = "https://api.example.com/upload",
        UploadFile = tempFile,
      };
      HttpRequestMessage request = HttpRequestBuilder.Build(options);

      Assert.Equal(HttpMethod.Put, request.Method);
      Assert.NotNull(request.Content);
      Assert.IsType<ByteArrayContent>(request.Content);
    }
    finally
    {
      File.Delete(tempFile);
    }
  }

  [Fact]
  public void Build_HttpVersion_SetsOnRequest()
  {
    CurlOptions options = new()
    {
      Url = "https://api.example.com",
      HttpVersion = "2",
    };
    HttpRequestMessage request = HttpRequestBuilder.Build(options);

    Assert.Equal(new Version(2, 0), request.Version);
  }

  [Fact]
  public void Build_HttpVersion11_SetsOnRequest()
  {
    CurlOptions options = new()
    {
      Url = "https://api.example.com",
      HttpVersion = "1.1",
    };
    HttpRequestMessage request = HttpRequestBuilder.Build(options);

    Assert.Equal(new Version(1, 1), request.Version);
  }

  [Fact]
  public void Build_HeadMethod_CreatesHeadRequest()
  {
    CurlOptions options = new()
    {
      Url = "https://api.example.com",
      Method = "HEAD",
    };
    HttpRequestMessage request = HttpRequestBuilder.Build(options);

    Assert.Equal(HttpMethod.Head, request.Method);
  }

  [Fact]
  public void Build_JsonFlag_SetsContentTypeAndAcceptHeaders()
  {
    CurlOptions options = new()
    {
      Url = "https://example.com",
      DataBody = "{\"key\":\"value\"}",
      IsJson = true
    };
    HttpRequestMessage request = HttpRequestBuilder.Build(options);
    Assert.Equal("application/json", request.Content?.Headers.ContentType?.MediaType);
    Assert.Contains(request.Headers.Accept, a => a.MediaType == "application/json");
  }

  [Fact]
  public void Build_JsonFlag_ImpliesPost()
  {
    CurlOptions options = new()
    {
      Url = "https://example.com",
      DataBody = "{}",
      IsJson = true
    };
    HttpRequestMessage request = HttpRequestBuilder.Build(options);
    Assert.Equal(HttpMethod.Post, request.Method);
  }

  [Fact]
  public void Build_JsonFlag_DoesNotOverrideExplicitContentType()
  {
    CurlOptions options = new()
    {
      Url = "https://example.com",
      DataBody = "{}",
      IsJson = true,
      Headers = [("Content-Type", "text/plain")]
    };
    HttpRequestMessage request = HttpRequestBuilder.Build(options);
    Assert.Equal("text/plain", request.Content?.Headers.ContentType?.MediaType);
  }
}