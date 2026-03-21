using CurlCommandParser.Exceptions;
using CurlCommandParser.Models;
using CurlCommandParser.Parsing;

namespace CurlCommandParser.Tests;

public class CurlOptionParserTests
{
  [Fact]
  public void Parse_SimpleGetUrl_ExtractsUrl()
  {
    CurlOptions options = CurlOptionParser.Parse("https://api.example.com");
    Assert.Equal("https://api.example.com", options.Url);
    Assert.Null(options.Method);
  }

  [Fact]
  public void Parse_WithCurlPrefix_StripsPrefix()
  {
    CurlOptions options = CurlOptionParser.Parse("curl https://api.example.com");
    Assert.Equal("https://api.example.com", options.Url);
  }

  [Fact]
  public void Parse_ExplicitMethod_SetsMethod()
  {
    CurlOptions options = CurlOptionParser.Parse("-X POST https://api.example.com");
    Assert.Equal("POST", options.Method);
  }

  [Fact]
  public void Parse_LongOptionRequest_SetsMethod()
  {
    CurlOptions options = CurlOptionParser.Parse("--request PUT https://api.example.com");
    Assert.Equal("PUT", options.Method);
  }

  [Fact]
  public void Parse_Header_AddsToHeaders()
  {
    CurlOptions options = CurlOptionParser.Parse("-H 'Content-Type: application/json' https://api.example.com");
    Assert.Contains(("Content-Type", "application/json"), options.Headers);
  }

  [Fact]
  public void Parse_MultipleHeaders_AddsAll()
  {
    CurlOptions options = CurlOptionParser.Parse(
      "-H 'Content-Type: application/json' -H 'Authorization: Bearer token' https://api.example.com");
    Assert.Equal(2, options.Headers.Count);
  }

  [Fact]
  public void Parse_DataImpliesPost()
  {
    CurlOptions options = CurlOptionParser.Parse("-d '{\"key\":\"value\"}' https://api.example.com");
    Assert.Equal("{\"key\":\"value\"}", options.DataBody);
    // Method should be null — the builder infers POST from DataBody presence
  }

  [Fact]
  public void Parse_DataRaw_SetsDataBody()
  {
    CurlOptions options = CurlOptionParser.Parse("--data-raw 'hello' https://api.example.com");
    Assert.Equal("hello", options.DataBody);
  }

  [Fact]
  public void Parse_UserCredentials_SetsUser()
  {
    CurlOptions options = CurlOptionParser.Parse("-u admin:secret https://api.example.com");
    Assert.Equal("admin:secret", options.UserCredentials);
  }

  [Fact]
  public void Parse_BearerToken_SetsToken()
  {
    CurlOptions options = CurlOptionParser.Parse("--oauth2-bearer mytoken123 https://api.example.com");
    Assert.Equal("mytoken123", options.BearerToken);
  }

  [Fact]
  public void Parse_FormField_AddsField()
  {
    CurlOptions options = CurlOptionParser.Parse("-F 'name=John' https://api.example.com");
    Assert.Single(options.FormFields);
    Assert.Equal("name", options.FormFields[0].Name);
    Assert.Equal("John", options.FormFields[0].Value);
    Assert.False(options.FormFields[0].IsFile);
  }

  [Fact]
  public void Parse_FormFieldWithFile_MarksAsFile()
  {
    CurlOptions options = CurlOptionParser.Parse("-F 'file=@/path/to/doc.pdf' https://api.example.com");
    Assert.Single(options.FormFields);
    Assert.True(options.FormFields[0].IsFile);
    Assert.Equal("/path/to/doc.pdf", options.FormFields[0].Value);
  }

  [Fact]
  public void Parse_FormFieldWithContentType_SetsContentType()
  {
    CurlOptions options =
      CurlOptionParser.Parse("-F 'file=@/path/to/doc.pdf;type=application/pdf' https://api.example.com");
    Assert.Equal("application/pdf", options.FormFields[0].ContentType);
  }

  [Fact]
  public void Parse_FollowRedirects_SetsFlag()
  {
    CurlOptions options = CurlOptionParser.Parse("-L https://api.example.com");
    Assert.True(options.FollowRedirects);
  }

  [Fact]
  public void Parse_LocationLongOption_SetsFlag()
  {
    CurlOptions options = CurlOptionParser.Parse("--location https://api.example.com");
    Assert.True(options.FollowRedirects);
  }

  [Fact]
  public void Parse_Insecure_SetsFlag()
  {
    CurlOptions options = CurlOptionParser.Parse("-k https://api.example.com");
    Assert.True(options.Insecure);
  }

  [Fact]
  public void Parse_ConnectTimeout_SetsValue()
  {
    CurlOptions options = CurlOptionParser.Parse("--connect-timeout 30 https://api.example.com");
    Assert.Equal(30, options.ConnectTimeoutSeconds);
  }

  [Fact]
  public void Parse_MaxTime_SetsValue()
  {
    CurlOptions options = CurlOptionParser.Parse("--max-time 60 https://api.example.com");
    Assert.Equal(60, options.MaxTimeSeconds);
  }

  [Fact]
  public void Parse_Cookie_SetsCookie()
  {
    CurlOptions options = CurlOptionParser.Parse("-b 'session=abc123' https://api.example.com");
    Assert.Equal("session=abc123", options.Cookie);
  }

  [Fact]
  public void Parse_UserAgent_SetsUserAgent()
  {
    CurlOptions options = CurlOptionParser.Parse("-A 'MyApp/1.0' https://api.example.com");
    Assert.Equal("MyApp/1.0", options.UserAgent);
  }

  [Fact]
  public void Parse_Referer_SetsReferer()
  {
    CurlOptions options = CurlOptionParser.Parse("-e 'https://google.com' https://api.example.com");
    Assert.Equal("https://google.com", options.Referer);
  }

  [Fact]
  public void Parse_Compressed_SetsFlag()
  {
    CurlOptions options = CurlOptionParser.Parse("--compressed https://api.example.com");
    Assert.True(options.Compressed);
  }

  [Fact]
  public void Parse_CombinedShortFlags_ParsesAll()
  {
    CurlOptions options = CurlOptionParser.Parse("-sLk https://api.example.com");
    Assert.True(options.FollowRedirects);
    Assert.True(options.Insecure);
  }

  [Fact]
  public void Parse_NoUrl_ThrowsCurlParseException()
  {
    Assert.Throws<CurlParseException>(() => CurlOptionParser.Parse("-X GET"));
  }

  [Fact]
  public void Parse_ComplexRealWorldCommand_ParsesCorrectly()
  {
    CurlOptions options = CurlOptionParser.Parse(
      "curl -X POST https://api.example.com/users " +
      "-H 'Content-Type: application/json' " +
      "-H 'Authorization: Bearer mytoken' " +
      "-d '{\"name\":\"John\",\"email\":\"john@example.com\"}' " +
      "-L -k --max-time 30");

    Assert.Equal("https://api.example.com/users", options.Url);
    Assert.Equal("POST", options.Method);
    Assert.Equal(2, options.Headers.Count);
    Assert.Equal("{\"name\":\"John\",\"email\":\"john@example.com\"}", options.DataBody);
    Assert.True(options.FollowRedirects);
    Assert.True(options.Insecure);
    Assert.Equal(30, options.MaxTimeSeconds);
  }

  [Fact]
  public void Parse_DataUrlEncode_AddsField()
  {
    CurlOptions options = CurlOptionParser.Parse(
      "--data-urlencode 'name=Frank Hommers' https://api.example.com");
    Assert.Single(options.DataUrlEncodeFields);
    Assert.Equal("name=Frank Hommers", options.DataUrlEncodeFields[0]);
  }

  [Fact]
  public void Parse_MultipleDataUrlEncode_AddsAll()
  {
    CurlOptions options = CurlOptionParser.Parse(
      "--data-urlencode 'name=Frank' --data-urlencode 'city=Den Haag' https://api.example.com");
    Assert.Equal(2, options.DataUrlEncodeFields.Count);
  }

  [Fact]
  public void Parse_DataUrlEncodeWithoutEquals_AddsRawValue()
  {
    CurlOptions options = CurlOptionParser.Parse(
      "--data-urlencode 'hello world' https://api.example.com");
    Assert.Single(options.DataUrlEncodeFields);
    Assert.Equal("hello world", options.DataUrlEncodeFields[0]);
  }

  [Fact]
  public void Parse_ProxyLongOption_SetsProxy()
  {
    CurlOptions options = CurlOptionParser.Parse(
      "--proxy http://proxy.example.com:8080 https://api.example.com");
    Assert.Equal("http://proxy.example.com:8080", options.ProxyUrl);
  }

  [Fact]
  public void Parse_ProxyShortOption_SetsProxy()
  {
    CurlOptions options = CurlOptionParser.Parse(
      "-x http://proxy.example.com:8080 https://api.example.com");
    Assert.Equal("http://proxy.example.com:8080", options.ProxyUrl);
  }

  [Fact]
  public void Parse_ProxyWithInsecure_BothSet()
  {
    CurlOptions options = CurlOptionParser.Parse(
      "-x http://proxy:8080 -k https://api.example.com");
    Assert.Equal("http://proxy:8080", options.ProxyUrl);
    Assert.True(options.Insecure);
  }
}