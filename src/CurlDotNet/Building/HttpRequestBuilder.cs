using System.Net.Http.Headers;
using System.Text;
using CurlDotNet.Models;

namespace CurlDotNet.Building;

/// <summary>
/// Converts a CurlOptions model into an HttpRequestMessage.
/// </summary>
public static class HttpRequestBuilder
{
    /// <summary>
    /// Builds an HttpRequestMessage from parsed CurlOptions.
    /// </summary>
    public static HttpRequestMessage Build(CurlOptions options)
    {
        var method = ResolveMethod(options);
        var request = new HttpRequestMessage(method, options.Url);

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
        if (options.DataBody != null || options.BinaryData != null || options.FormFields.Count > 0)
        {
            return HttpMethod.Post;
        }

        return HttpMethod.Get;
    }

    private static void SetContent(HttpRequestMessage request, CurlOptions options)
    {
        // Find Content-Type from headers (if specified)
        string? contentType = null;
        foreach (var (name, value) in options.Headers)
        {
            if (name.Equals("Content-Type", StringComparison.OrdinalIgnoreCase))
            {
                contentType = value;
                break;
            }
        }

        if (options.FormFields.Count > 0)
        {
            var multipart = new MultipartFormDataContent();
            foreach (var field in options.FormFields)
            {
                if (field.IsFile)
                {
                    var fileBytes = File.ReadAllBytes(field.Value);
                    var fileContent = new ByteArrayContent(fileBytes);
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
                var filePath = options.DataBody.Substring(1);
                var fileContent = File.ReadAllText(filePath);
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
        foreach (var (name, value) in options.Headers)
        {
            // Content-Type is set on content, not on request headers
            if (name.Equals("Content-Type", StringComparison.OrdinalIgnoreCase))
                continue;

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
        return name.Equals("Content-Type", StringComparison.OrdinalIgnoreCase)
            || name.Equals("Content-Length", StringComparison.OrdinalIgnoreCase)
            || name.Equals("Content-Encoding", StringComparison.OrdinalIgnoreCase)
            || name.Equals("Content-Language", StringComparison.OrdinalIgnoreCase)
            || name.Equals("Content-Disposition", StringComparison.OrdinalIgnoreCase)
            || name.Equals("Content-Location", StringComparison.OrdinalIgnoreCase)
            || name.Equals("Content-Range", StringComparison.OrdinalIgnoreCase);
    }

    private static void SetAuth(HttpRequestMessage request, CurlOptions options)
    {
        if (!string.IsNullOrEmpty(options.UserCredentials))
        {
            var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(options.UserCredentials));
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", encoded);
        }
        else if (!string.IsNullOrEmpty(options.BearerToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.BearerToken);
        }
    }

    private static void SetMiscHeaders(HttpRequestMessage request, CurlOptions options)
    {
        if (!string.IsNullOrEmpty(options.Cookie))
        {
            request.Headers.TryAddWithoutValidation("Cookie", options.Cookie);
        }

        if (!string.IsNullOrEmpty(options.UserAgent))
        {
            request.Headers.TryAddWithoutValidation("User-Agent", options.UserAgent);
        }

        if (!string.IsNullOrEmpty(options.Referer))
        {
            request.Headers.Referrer = new Uri(options.Referer);
        }

        if (options.Compressed)
        {
            request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
            request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("deflate"));
        }
    }
}
