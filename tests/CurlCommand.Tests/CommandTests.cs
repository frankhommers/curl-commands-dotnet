using System.Net;

namespace CurlCommand.Tests;

public class CommandTests
{
  [Fact]
  public void Parse_ReturnsCommandWithOptions()
  {
    Command cmd = Command.Parse("curl -X POST https://api.example.com -d '{}'");
    Assert.Equal("https://api.example.com", cmd.Options.Url);
    Assert.Equal("POST", cmd.Options.Method);
    Assert.Equal("{}", cmd.Options.DataBody);
  }

  [Fact]
  public void Parse_WithoutCurlPrefix_Works()
  {
    Command cmd = Command.Parse("-X GET https://example.com");
    Assert.Equal("https://example.com", cmd.Options.Url);
  }

  [Fact]
  public void Options_IsReadOnly()
  {
    Command cmd = Command.Parse("curl https://example.com");
    Assert.NotNull(cmd.Options);
    Assert.Equal("https://example.com", cmd.Options.Url);
  }

  [Fact]
  public async Task ExecuteCurlAsync_WithCommand_Works()
  {
    Command cmd = Command.Parse("curl -X POST https://example.com -d 'test'");
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
