using CurlCommandParser.Exceptions;
using CurlCommandParser.Models;

namespace CurlCommandParser.Parsing;

/// <summary>
/// Parses a curl command string (or pre-tokenized arguments) into a CurlOptions model.
/// </summary>
public static class CurlOptionParser
{
  private const string CurlPrefix = "curl";
  private const string LongRequest = "--request";
  private const string LongHeader = "--header";
  private const string LongData = "--data";
  private const string LongDataRaw = "--data-raw";
  private const string LongDataBinary = "--data-binary";
  private const string LongUser = "--user";
  private const string LongOAuth2Bearer = "--oauth2-bearer";
  private const string LongForm = "--form";
  private const string LongLocation = "--location";
  private const string LongInsecure = "--insecure";
  private const string LongConnectTimeout = "--connect-timeout";
  private const string LongMaxTime = "--max-time";
  private const string LongCookie = "--cookie";
  private const string LongUserAgent = "--user-agent";
  private const string LongReferer = "--referer";
  private const string LongCompressed = "--compressed";
  private const string LongDataUrlEncode = "--data-urlencode";
  private const string LongProxy = "--proxy";
  private const string LongGet = "--get";
  private const string LongHead = "--head";
  private const string LongUploadFile = "--upload-file";
  private const string LongCert = "--cert";
  private const string LongCertType = "--cert-type";
  private const string LongKey = "--key";
  private const string LongKeyType = "--key-type";
  private const string LongProxyUser = "--proxy-user";
  private const string LongHttp09 = "--http0.9";
  private const string LongHttp10 = "--http1.0";
  private const string LongHttp11 = "--http1.1";
  private const string LongHttp2 = "--http2";
  private const string LongHttp3 = "--http3";
  private const string LongUrl = "--url";
  private const string ContentTypeSeparator = ";type=";
  private const int ContentTypeSeparatorLength = 6;
  private const string NoUrlFoundMessage = "No URL found in curl command.";

  private static readonly HashSet<char> _shortBooleanFlags = ['L', 'k', 's', 'S', 'v', 'I', 'N', 'G'];

  /// <summary>
  /// Parses a curl command string into CurlOptions.
  /// The "curl" prefix is optional.
  /// </summary>
  public static CurlOptions Parse(string curlCommand)
  {
    List<string> tokens = ShellTokenizer.Tokenize(curlCommand);
    return ParseTokens(tokens);
  }

  /// <summary>
  /// Parses pre-tokenized curl arguments into CurlOptions.
  /// </summary>
  public static CurlOptions ParseTokens(List<string> tokens)
  {
    CurlOptions options = new();
    int i = 0;

    if (tokens.Count > 0 && tokens[0].Equals(CurlPrefix, StringComparison.OrdinalIgnoreCase))
    {
      i = 1;
    }

    while (i < tokens.Count)
    {
      string token = tokens[i];

      if (token.StartsWith("--"))
      {
        i = ParseLongOption(tokens, i, options);
      }
      else if (token.StartsWith("-") && token.Length > 1)
      {
        i = ParseShortOption(tokens, i, options);
      }
      else
      {
        if (string.IsNullOrEmpty(options.Url))
        {
          options.Url = token;
        }

        i++;
      }
    }

    if (string.IsNullOrEmpty(options.Url))
    {
      throw new CurlParseException(NoUrlFoundMessage);
    }

    return options;
  }

  private static int ParseLongOption(List<string> tokens, int i, CurlOptions options)
  {
    string token = tokens[i];

    switch (token)
    {
      case LongRequest:
        options.Method = GetNextArg(tokens, i, token);
        return i + 2;

      case LongHeader:
        ParseHeader(GetNextArg(tokens, i, token), options);
        return i + 2;

      case LongData:
      case LongDataRaw:
        options.DataBody = GetNextArg(tokens, i, token);
        return i + 2;

      case LongDataBinary:
        string binaryArg = GetNextArg(tokens, i, token);
        if (binaryArg.StartsWith("@"))
        {
          options.DataBody = binaryArg;
        }
        else
        {
          options.BinaryData = System.Text.Encoding.UTF8.GetBytes(binaryArg);
        }

        return i + 2;

      case LongUser:
        options.UserCredentials = GetNextArg(tokens, i, token);
        return i + 2;

      case LongOAuth2Bearer:
        options.BearerToken = GetNextArg(tokens, i, token);
        return i + 2;

      case LongForm:
        ParseFormField(GetNextArg(tokens, i, token), options);
        return i + 2;

      case LongLocation:
        options.FollowRedirects = true;
        return i + 1;

      case LongInsecure:
        options.Insecure = true;
        return i + 1;

      case LongConnectTimeout:
        options.ConnectTimeoutSeconds = ParseInt(GetNextArg(tokens, i, token), token);
        return i + 2;

      case LongMaxTime:
        options.MaxTimeSeconds = ParseInt(GetNextArg(tokens, i, token), token);
        return i + 2;

      case LongCookie:
        options.Cookie = GetNextArg(tokens, i, token);
        return i + 2;

      case LongUserAgent:
        options.UserAgent = GetNextArg(tokens, i, token);
        return i + 2;

      case LongReferer:
        options.Referer = GetNextArg(tokens, i, token);
        return i + 2;

      case LongCompressed:
        options.Compressed = true;
        return i + 1;

      case LongDataUrlEncode:
        options.DataUrlEncodeFields.Add(GetNextArg(tokens, i, token));
        return i + 2;

      case LongProxy:
        options.ProxyUrl = GetNextArg(tokens, i, token);
        return i + 2;

      case LongGet:
        options.ForceGet = true;
        return i + 1;

      case LongHead:
        options.Method ??= "HEAD";
        return i + 1;

      case LongUploadFile:
        options.UploadFile = GetNextArg(tokens, i, token);
        return i + 2;

      case LongCert:
        options.CertificateFile = GetNextArg(tokens, i, token);
        return i + 2;

      case LongCertType:
        options.CertificateType = GetNextArg(tokens, i, token);
        return i + 2;

      case LongKey:
        options.KeyFile = GetNextArg(tokens, i, token);
        return i + 2;

      case LongKeyType:
        options.KeyType = GetNextArg(tokens, i, token);
        return i + 2;

      case LongProxyUser:
        options.ProxyUserCredentials = GetNextArg(tokens, i, token);
        return i + 2;

      case LongHttp09:
        options.HttpVersion = "0.9";
        return i + 1;

      case LongHttp10:
        options.HttpVersion = "1.0";
        return i + 1;

      case LongHttp11:
        options.HttpVersion = "1.1";
        return i + 1;

      case LongHttp2:
        options.HttpVersion = "2";
        return i + 1;

      case LongHttp3:
        options.HttpVersion = "3";
        return i + 1;

      case LongUrl:
        options.Url = GetNextArg(tokens, i, token);
        return i + 2;

      default:
        throw new CurlParseException($"Unsupported curl option: '{token}'.", i);
    }
  }

  private static int ParseShortOption(List<string> tokens, int i, CurlOptions options)
  {
    string token = tokens[i];

    if (token.Length == 2)
    {
      return ParseSingleShortOption(token[1], tokens, i, options);
    }

    bool allBooleans = true;
    for (int j = 1; j < token.Length; j++)
    {
      if (!_shortBooleanFlags.Contains(token[j]))
      {
        allBooleans = false;
        break;
      }
    }

    if (allBooleans)
    {
      for (int j = 1; j < token.Length; j++)
      {
        ApplyShortBooleanFlag(token[j], options);
      }

      return i + 1;
    }

    return ParseSingleShortOption(token[1], tokens, i, options);
  }

  private static int ParseSingleShortOption(char flag, List<string> tokens, int i, CurlOptions options)
  {
    switch (flag)
    {
      case 'X':
        options.Method = GetNextArg(tokens, i, $"-{flag}");
        return i + 2;

      case 'H':
        ParseHeader(GetNextArg(tokens, i, $"-{flag}"), options);
        return i + 2;

      case 'd':
        options.DataBody = GetNextArg(tokens, i, $"-{flag}");
        return i + 2;

      case 'u':
        options.UserCredentials = GetNextArg(tokens, i, $"-{flag}");
        return i + 2;

      case 'F':
        ParseFormField(GetNextArg(tokens, i, $"-{flag}"), options);
        return i + 2;

      case 'b':
        options.Cookie = GetNextArg(tokens, i, $"-{flag}");
        return i + 2;

      case 'A':
        options.UserAgent = GetNextArg(tokens, i, $"-{flag}");
        return i + 2;

      case 'e':
        options.Referer = GetNextArg(tokens, i, $"-{flag}");
        return i + 2;

      case 'G':
        options.ForceGet = true;
        return i + 1;

      case 'T':
        options.UploadFile = GetNextArg(tokens, i, $"-{flag}");
        return i + 2;

      case 'U':
        options.ProxyUserCredentials = GetNextArg(tokens, i, $"-{flag}");
        return i + 2;

      case 'x':
        options.ProxyUrl = GetNextArg(tokens, i, $"-{flag}");
        return i + 2;

      case 'o':
      case 'O':
        if (flag == 'o')
        {
          return i + 2;
        }

        return i + 1;

      default:
        if (_shortBooleanFlags.Contains(flag))
        {
          ApplyShortBooleanFlag(flag, options);
          return i + 1;
        }

        throw new CurlParseException($"Unsupported curl option: '-{flag}'.", i);
    }
  }

  private static void ApplyShortBooleanFlag(char flag, CurlOptions options)
  {
    switch (flag)
    {
      case 'L': options.FollowRedirects = true; break;
      case 'k': options.Insecure = true; break;
      case 's': break;
      case 'S': break;
      case 'v': break;
      case 'I': options.Method ??= "HEAD"; break;
      case 'N': break;
      case 'G': options.ForceGet = true; break;
    }
  }

  private static void ParseHeader(string headerValue, CurlOptions options)
  {
    int colonIndex = headerValue.IndexOf(':');
    if (colonIndex < 0)
    {
      throw new CurlParseException($"Invalid header format: '{headerValue}'. Expected 'Name: Value'.");
    }

    string name = headerValue.Substring(0, colonIndex).Trim();
    string value = headerValue.Substring(colonIndex + 1).Trim();
    options.Headers.Add((name, value));
  }

  private static void ParseFormField(string fieldValue, CurlOptions options)
  {
    int equalsIndex = fieldValue.IndexOf('=');
    if (equalsIndex < 0)
    {
      throw new CurlParseException($"Invalid form field format: '{fieldValue}'. Expected 'name=value'.");
    }

    string name = fieldValue.Substring(0, equalsIndex);
    string value = fieldValue.Substring(equalsIndex + 1);
    FormField field = new() {Name = name};

    if (value.StartsWith("@"))
    {
      field.IsFile = true;
      string filePart = value.Substring(1);

      int typeIndex = filePart.IndexOf(ContentTypeSeparator, StringComparison.OrdinalIgnoreCase);
      if (typeIndex >= 0)
      {
        field.ContentType = filePart.Substring(typeIndex + ContentTypeSeparatorLength);
        field.Value = filePart.Substring(0, typeIndex);
      }
      else
      {
        field.Value = filePart;
      }
    }
    else
    {
      field.Value = value;
    }

    options.FormFields.Add(field);
  }

  private static string GetNextArg(List<string> tokens, int currentIndex, string optionName)
  {
    return currentIndex + 1 < tokens.Count
      ? tokens[currentIndex + 1]
      : throw new CurlParseException($"Option '{optionName}' requires an argument.", currentIndex);
  }

  private static int ParseInt(string value, string optionName)
  {
    return int.TryParse(value, out int result)
      ? result
      : throw new CurlParseException($"Option '{optionName}' requires a numeric value, got '{value}'.");
  }
}