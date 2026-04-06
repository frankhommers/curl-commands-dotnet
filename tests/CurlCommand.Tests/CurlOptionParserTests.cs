using CurlCommand.Exceptions;
using CurlCommand.Models;
using CurlCommand.Parsing;

namespace CurlCommand.Tests;

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

  [Fact]
  public void Parse_ForceGet_SetsFlag()
  {
    CurlOptions options = CurlOptionParser.Parse("-G -d 'q=test' https://api.example.com");
    Assert.True(options.ForceGet);
    Assert.Equal("q=test", options.DataBody);
  }

  [Fact]
  public void Parse_ForceGetLongOption_SetsFlag()
  {
    CurlOptions options = CurlOptionParser.Parse("--get -d 'q=test' https://api.example.com");
    Assert.True(options.ForceGet);
  }

  [Fact]
  public void Parse_HeadLongOption_SetsMethod()
  {
    CurlOptions options = CurlOptionParser.Parse("--head https://api.example.com");
    Assert.Equal("HEAD", options.Method);
  }

  [Fact]
  public void Parse_UploadFile_SetsPath()
  {
    CurlOptions options = CurlOptionParser.Parse("-T /path/to/file.txt https://api.example.com");
    Assert.Equal("/path/to/file.txt", options.UploadFile);
  }

  [Fact]
  public void Parse_UploadFileLongOption_SetsPath()
  {
    CurlOptions options = CurlOptionParser.Parse("--upload-file /path/to/file.txt https://api.example.com");
    Assert.Equal("/path/to/file.txt", options.UploadFile);
  }

  [Fact]
  public void Parse_Cert_SetsFile()
  {
    CurlOptions options = CurlOptionParser.Parse("--cert /path/to/cert.pem https://api.example.com");
    Assert.Equal("/path/to/cert.pem", options.CertificateFile);
  }

  [Fact]
  public void Parse_CertWithPassword_SplitsPathAndPassword()
  {
    CurlOptions options = CurlOptionParser.Parse("--cert /path/to/cert.pem:mypassword https://api.example.com");
    Assert.Equal("/path/to/cert.pem", options.CertificateFile);
    Assert.Equal("mypassword", options.CertificatePassword);
  }

  [Fact]
  public void Parse_CertWithoutPassword_SetsOnlyPath()
  {
    CurlOptions options = CurlOptionParser.Parse("--cert /path/to/cert.pem https://api.example.com");
    Assert.Equal("/path/to/cert.pem", options.CertificateFile);
    Assert.Null(options.CertificatePassword);
  }

  [Fact]
  public void Parse_CertWindowsPath_WithoutPassword_DoesNotSplitDriveLetter()
  {
    // Windows paths must be quoted in a shell; double quotes preserve backslashes for non-special chars
    CurlOptions options = CurlOptionParser.Parse("--cert \"C:\\certs\\client.pfx\" https://api.example.com");
    Assert.Equal("C:\\certs\\client.pfx", options.CertificateFile);
    Assert.Null(options.CertificatePassword);
  }

  [Fact]
  public void Parse_CertWindowsPath_WithPassword_SplitsCorrectly()
  {
    CurlOptions options = CurlOptionParser.Parse("--cert \"C:\\certs\\client.pfx:secret\" https://api.example.com");
    Assert.Equal("C:\\certs\\client.pfx", options.CertificateFile);
    Assert.Equal("secret", options.CertificatePassword);
  }

  [Fact]
  public void Parse_CertPathWithSpaces_WithPassword_SplitsCorrectly()
  {
    CurlOptions options = CurlOptionParser.Parse("--cert '/path with spaces/cert.pem:pass' https://api.example.com");
    Assert.Equal("/path with spaces/cert.pem", options.CertificateFile);
    Assert.Equal("pass", options.CertificatePassword);
  }

  [Fact]
  public void Parse_CertPathWithSpaces_WithoutPassword_SetsOnlyPath()
  {
    CurlOptions options = CurlOptionParser.Parse("--cert '/path with spaces/cert.pem' https://api.example.com");
    Assert.Equal("/path with spaces/cert.pem", options.CertificateFile);
    Assert.Null(options.CertificatePassword);
  }

  [Fact]
  public void Parse_CertType_SetsType()
  {
    CurlOptions options = CurlOptionParser.Parse("--cert-type P12 https://api.example.com");
    Assert.Equal("P12", options.CertificateType);
  }

  [Fact]
  public void Parse_Key_SetsFile()
  {
    CurlOptions options = CurlOptionParser.Parse("--key /path/to/key.pem https://api.example.com");
    Assert.Equal("/path/to/key.pem", options.KeyFile);
  }

  [Fact]
  public void Parse_KeyType_SetsType()
  {
    CurlOptions options = CurlOptionParser.Parse("--key-type DER https://api.example.com");
    Assert.Equal("DER", options.KeyType);
  }

  [Fact]
  public void Parse_ProxyUser_SetsCredentials()
  {
    CurlOptions options = CurlOptionParser.Parse("--proxy-user admin:secret https://api.example.com");
    Assert.Equal("admin:secret", options.ProxyUserCredentials);
  }

  [Fact]
  public void Parse_ProxyUserShort_SetsCredentials()
  {
    CurlOptions options = CurlOptionParser.Parse("-U admin:secret https://api.example.com");
    Assert.Equal("admin:secret", options.ProxyUserCredentials);
  }

  [Fact]
  public void Parse_Http10_SetsVersion()
  {
    CurlOptions options = CurlOptionParser.Parse("--http1.0 https://api.example.com");
    Assert.Equal("1.0", options.HttpVersion);
  }

  [Fact]
  public void Parse_Http11_SetsVersion()
  {
    CurlOptions options = CurlOptionParser.Parse("--http1.1 https://api.example.com");
    Assert.Equal("1.1", options.HttpVersion);
  }

  [Fact]
  public void Parse_Http2_SetsVersion()
  {
    CurlOptions options = CurlOptionParser.Parse("--http2 https://api.example.com");
    Assert.Equal("2", options.HttpVersion);
  }

  [Fact]
  public void Parse_Http3_SetsVersion()
  {
    CurlOptions options = CurlOptionParser.Parse("--http3 https://api.example.com");
    Assert.Equal("3", options.HttpVersion);
  }

  [Fact]
  public void Parse_JsonFlag_SetsDataBodyAndJsonFlag()
  {
    CurlOptions options = CurlOptionParser.Parse("curl --json '{\"key\":\"value\"}' https://api.example.com");
    Assert.Equal("{\"key\":\"value\"}", options.DataBody);
    Assert.True(options.IsJson);
  }

  [Fact]
  public void Parse_JsonFlag_DoesNotOverrideExplicitContentType()
  {
    CurlOptions options = CurlOptionParser.Parse("curl -H 'Content-Type: text/plain' --json '{\"key\":\"value\"}' https://api.example.com");
    Assert.True(options.IsJson);
    Assert.Contains(options.Headers, h => h.Name == "Content-Type" && h.Value == "text/plain");
  }

  [Fact]
  public void Parse_OutputFlag_SetsOutputFile()
  {
    CurlOptions options = CurlOptionParser.Parse("curl -o output.json https://api.example.com");
    Assert.Equal("output.json", options.OutputFile);
  }

  [Fact]
  public void Parse_LongOutputFlag_SetsOutputFile()
  {
    CurlOptions options = CurlOptionParser.Parse("curl --output result.txt https://api.example.com");
    Assert.Equal("result.txt", options.OutputFile);
  }
}