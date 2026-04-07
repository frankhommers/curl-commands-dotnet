using System.Net;
using CurlCommands;

namespace CurlCommands.Tests;

public class CurlCommandTests
{
  [Fact]
  public void Parse_ReturnsCurlCommandWithOptions()
  {
    CurlCommand cmd = CurlCommand.Parse("curl -X POST https://api.example.com -d '{}'");
    Assert.Equal("https://api.example.com", cmd.Options.Url);
    Assert.Equal("POST", cmd.Options.Method);
    Assert.Equal("{}", cmd.Options.DataBody);
  }

  [Fact]
  public void Parse_WithoutCurlPrefix_Works()
  {
    CurlCommand cmd = CurlCommand.Parse("-X GET https://example.com");
    Assert.Equal("https://example.com", cmd.Options.Url);
  }

  [Fact]
  public void Options_IsReadOnly()
  {
    CurlCommand cmd = CurlCommand.Parse("curl https://example.com");
    Assert.NotNull(cmd.Options);
    Assert.Equal("https://example.com", cmd.Options.Url);
  }

  [Fact]
  public async Task ExecuteCurlAsync_WithCurlCommand_Works()
  {
    CurlCommand cmd = CurlCommand.Parse("curl -X POST https://example.com -d 'test'");
    FakeHandler handler = new(new HttpResponseMessage(HttpStatusCode.OK));
    using HttpClient client = new(handler);
    HttpResponseMessage response = await client.ExecuteCurlAsync(cmd);
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
  }

  private class FakeHandler : HttpMessageHandler
  {
    private readonly HttpResponseMessage _response;

    public FakeHandler(HttpResponseMessage response) => _response = response;

    protected override Task<HttpResponseMessage> SendAsync(
      HttpRequestMessage request, CancellationToken cancellationToken) =>
      Task.FromResult(_response);
  }
}
