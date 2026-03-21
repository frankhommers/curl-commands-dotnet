using System.Net.Http.Headers;
using System.Text;
using CurlCommandParser.Models;

namespace CurlCommandParser.Building;

/// <summary>
/// Converts a CurlOptions model into an HttpRequestMessage.
/// </summary>
public static class HttpRequestBuilder
{
  private const string ContentTypeHeader = "Content-Type";
  private const string ContentLengthHeader = "Content-Length";
  private const string ContentEncodingHeader = "Content-Encoding";
  private const string ContentLanguageHeader = "Content-Language";
  private const string ContentDispositionHeader = "Content-Disposition";
  private const string ContentLocationHeader = "Content-Location";
  private const string ContentRangeHeader = "Content-Range";
  private const string BasicAuthScheme = "Basic";
  private const string BearerAuthScheme = "Bearer";
  private const string CookieHeader = "Cookie";
  private const string UserAgentHeader = "User-Agent";
  private const string GzipEncoding = "gzip";
  private const string DeflateEncoding = "deflate";

  /// <summary>
  /// Builds an HttpRequestMessage from parsed CurlOptions.
  /// </summary>
  public static HttpRequestMessage Build(CurlOptions options)
  {
    HttpMethod method = ResolveMethod(options);
    HttpRequestMessage request = new(method, options.Url);

    SetContent(request, options);
    SetHeaders(request, options);
    SetAuth(request, options);
    SetMiscHeaders(request, options);

    return request;
  }

  private static HttpMethod ResolveMethod(CurlOptions options)
  {
    if (!string.IsNullOrEmpty(options.Method))
    {
      return new HttpMethod(options.Method);
    }

    // Implicit POST when data or form fields are present
    if (options.DataBody != null || options.BinaryData != null
        || options.FormFields.Count > 0 || options.DataUrlEncodeFields.Count > 0)
    {
      return HttpMethod.Post;
    }

    return HttpMethod.Get;
  }

  private static void SetContent(HttpRequestMessage request, CurlOptions options)
  {
    // Find Content-Type from headers (if specified)
    string? contentType = null;
    foreach ((string name, string value) in options.Headers)
    {
      if (name.Equals(ContentTypeHeader, StringComparison.OrdinalIgnoreCase))
      {
        contentType = value;
        break;
      }
    }

    if (options.FormFields.Count > 0)
    {
      MultipartFormDataContent multipart = new();
      foreach (FormField field in options.FormFields)
      {
        if (field.IsFile)
        {
          byte[] fileBytes = File.ReadAllBytes(field.Value);
          ByteArrayContent fileContent = new(fileBytes);
          if (field.ContentType != null)
          {
            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(field.ContentType);
          }

          multipart.Add(fileContent, field.Name, Path.GetFileName(field.Value));
        }
        else
        {
          multipart.Add(new StringContent(field.Value), field.Name);
        }
      }

      request.Content = multipart;
      return;
    }

    if (options.DataUrlEncodeFields.Count > 0)
    {
      List<string> encodedParts = [];
      foreach (string field in options.DataUrlEncodeFields)
      {
        int equalsIndex = field.IndexOf('=');
        if (equalsIndex >= 0)
        {
          string name = field.Substring(0, equalsIndex);
          string value = field.Substring(equalsIndex + 1);
          encodedParts.Add($"{Uri.EscapeDataString(name)}={Uri.EscapeDataString(value)}");
        }
        else
        {
          encodedParts.Add(Uri.EscapeDataString(field));
        }
      }

      string encodedBody = string.Join("&", encodedParts);

      // If there's also a DataBody, prepend it
      if (options.DataBody != null)
      {
        encodedBody = options.DataBody + "&" + encodedBody;
      }

      request.Content = new StringContent(encodedBody, Encoding.UTF8, "application/x-www-form-urlencoded");
      if (contentType != null)
      {
        request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
      }

      return;
    }

    if (options.BinaryData != null)
    {
      request.Content = new ByteArrayContent(options.BinaryData);
      if (contentType != null)
      {
        request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
      }

      return;
    }

    if (options.DataBody != null)
    {
      if (options.DataBody.StartsWith("@"))
      {
        string filePath = options.DataBody.Substring(1);
        string fileContent = File.ReadAllText(filePath);
        request.Content = new StringContent(fileContent, Encoding.UTF8);
      }
      else
      {
        request.Content = new StringContent(options.DataBody, Encoding.UTF8);
      }

      if (contentType != null)
      {
        request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
      }

      return;
    }
  }

  private static void SetHeaders(HttpRequestMessage request, CurlOptions options)
  {
    foreach ((string name, string value) in options.Headers)
    {
      // Content-Type is set on content, not on request headers
      if (name.Equals(ContentTypeHeader, StringComparison.OrdinalIgnoreCase))
      {
        continue;
      }

      // Some headers must be set on Content.Headers
      if (IsContentHeader(name) && request.Content != null)
      {
        request.Content.Headers.TryAddWithoutValidation(name, value);
      }
      else
      {
        request.Headers.TryAddWithoutValidation(name, value);
      }
    }
  }

  private static bool IsContentHeader(string name)
  {
    return name.Equals(ContentTypeHeader, StringComparison.OrdinalIgnoreCase)
           || name.Equals(ContentLengthHeader, StringComparison.OrdinalIgnoreCase)
           || name.Equals(ContentEncodingHeader, StringComparison.OrdinalIgnoreCase)
           || name.Equals(ContentLanguageHeader, StringComparison.OrdinalIgnoreCase)
           || name.Equals(ContentDispositionHeader, StringComparison.OrdinalIgnoreCase)
           || name.Equals(ContentLocationHeader, StringComparison.OrdinalIgnoreCase)
           || name.Equals(ContentRangeHeader, StringComparison.OrdinalIgnoreCase);
  }

  private static void SetAuth(HttpRequestMessage request, CurlOptions options)
  {
    if (!string.IsNullOrEmpty(options.UserCredentials))
    {
      string encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(options.UserCredentials));
      request.Headers.Authorization = new AuthenticationHeaderValue(BasicAuthScheme, encoded);
    }
    else if (!string.IsNullOrEmpty(options.BearerToken))
    {
      request.Headers.Authorization = new AuthenticationHeaderValue(BearerAuthScheme, options.BearerToken);
    }
  }

  private static void SetMiscHeaders(HttpRequestMessage request, CurlOptions options)
  {
    if (!string.IsNullOrEmpty(options.Cookie))
    {
      request.Headers.TryAddWithoutValidation(CookieHeader, options.Cookie);
    }

    if (!string.IsNullOrEmpty(options.UserAgent))
    {
      request.Headers.TryAddWithoutValidation(UserAgentHeader, options.UserAgent);
    }

    if (!string.IsNullOrEmpty(options.Referer))
    {
      request.Headers.Referrer = new Uri(options.Referer);
    }

    if (options.Compressed)
    {
      request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue(GzipEncoding));
      request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue(DeflateEncoding));
    }
  }
}