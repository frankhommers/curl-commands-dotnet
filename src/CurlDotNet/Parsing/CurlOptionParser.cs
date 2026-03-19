using CurlDotNet.Exceptions;
using CurlDotNet.Models;

namespace CurlDotNet.Parsing;

/// <summary>
/// Parses a curl command string (or pre-tokenized arguments) into a CurlOptions model.
/// </summary>
public static class CurlOptionParser
{
    // Short boolean flags (single character)
    private static readonly HashSet<char> ShortBooleanFlags = new()
    {
        'L', 'k', 's', 'S', 'v', 'I', 'N'
    };

    /// <summary>
    /// Parses a curl command string into CurlOptions.
    /// The "curl" prefix is optional.
    /// </summary>
    public static CurlOptions Parse(string curlCommand)
    {
        var tokens = ShellTokenizer.Tokenize(curlCommand);
        return ParseTokens(tokens);
    }

    /// <summary>
    /// Parses pre-tokenized curl arguments into CurlOptions.
    /// </summary>
    public static CurlOptions ParseTokens(List<string> tokens)
    {
        var options = new CurlOptions();
        var i = 0;

        // Strip optional "curl" prefix
        if (tokens.Count > 0 && tokens[0].Equals("curl", StringComparison.OrdinalIgnoreCase))
        {
            i = 1;
        }

        while (i < tokens.Count)
        {
            var token = tokens[i];

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
                // Positional argument — must be the URL
                if (string.IsNullOrEmpty(options.Url))
                {
                    options.Url = token;
                }
                i++;
            }
        }

        if (string.IsNullOrEmpty(options.Url))
        {
            throw new CurlParseException("No URL found in curl command.");
        }

        return options;
    }

    private static int ParseLongOption(List<string> tokens, int i, CurlOptions options)
    {
        var token = tokens[i];

        switch (token)
        {
            case "--request":
                options.Method = GetNextArg(tokens, i, token);
                return i + 2;

            case "--header":
                ParseHeader(GetNextArg(tokens, i, token), options);
                return i + 2;

            case "--data":
            case "--data-raw":
                options.DataBody = GetNextArg(tokens, i, token);
                return i + 2;

            case "--data-binary":
                var binaryArg = GetNextArg(tokens, i, token);
                if (binaryArg.StartsWith("@"))
                {
                    // File reference — store as path, builder reads at execution time
                    options.DataBody = binaryArg;
                }
                else
                {
                    options.BinaryData = System.Text.Encoding.UTF8.GetBytes(binaryArg);
                }
                return i + 2;

            case "--user":
                options.UserCredentials = GetNextArg(tokens, i, token);
                return i + 2;

            case "--oauth2-bearer":
                options.BearerToken = GetNextArg(tokens, i, token);
                return i + 2;

            case "--form":
                ParseFormField(GetNextArg(tokens, i, token), options);
                return i + 2;

            case "--location":
                options.FollowRedirects = true;
                return i + 1;

            case "--insecure":
                options.Insecure = true;
                return i + 1;

            case "--connect-timeout":
                options.ConnectTimeoutSeconds = ParseInt(GetNextArg(tokens, i, token), token);
                return i + 2;

            case "--max-time":
                options.MaxTimeSeconds = ParseInt(GetNextArg(tokens, i, token), token);
                return i + 2;

            case "--cookie":
                options.Cookie = GetNextArg(tokens, i, token);
                return i + 2;

            case "--user-agent":
                options.UserAgent = GetNextArg(tokens, i, token);
                return i + 2;

            case "--referer":
                options.Referer = GetNextArg(tokens, i, token);
                return i + 2;

            case "--compressed":
                options.Compressed = true;
                return i + 1;

            case "--url":
                options.Url = GetNextArg(tokens, i, token);
                return i + 2;

            default:
                throw new CurlParseException($"Unsupported curl option: '{token}'.", i);
        }
    }

    private static int ParseShortOption(List<string> tokens, int i, CurlOptions options)
    {
        var token = tokens[i];

        // Single short option with argument: -X POST, -H '...', -d '...', etc.
        if (token.Length == 2)
        {
            return ParseSingleShortOption(token[1], tokens, i, options);
        }

        // Combined short flags: -sLk
        // Only valid if ALL characters are boolean flags
        var allBooleans = true;
        for (var j = 1; j < token.Length; j++)
        {
            if (!ShortBooleanFlags.Contains(token[j]))
            {
                allBooleans = false;
                break;
            }
        }

        if (allBooleans)
        {
            for (var j = 1; j < token.Length; j++)
            {
                ApplyShortBooleanFlag(token[j], options);
            }
            return i + 1;
        }

        // If not all booleans, treat as single short option
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

            case 'o':
            case 'O':
                // Output options — skip (not relevant for HttpClient)
                // -o takes a filename argument, -O does not
                if (flag == 'o')
                    return i + 2;
                return i + 1;

            default:
                if (ShortBooleanFlags.Contains(flag))
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
            case 's': break; // Silent — ignore for HttpClient
            case 'S': break; // Show error — ignore
            case 'v': break; // Verbose — ignore
            case 'I': options.Method ??= "HEAD"; break;
            case 'N': break; // No buffer — ignore
        }
    }

    private static void ParseHeader(string headerValue, CurlOptions options)
    {
        var colonIndex = headerValue.IndexOf(':');
        if (colonIndex < 0)
        {
            throw new CurlParseException($"Invalid header format: '{headerValue}'. Expected 'Name: Value'.");
        }

        var name = headerValue.Substring(0, colonIndex).Trim();
        var value = headerValue.Substring(colonIndex + 1).Trim();
        options.Headers.Add((name, value));
    }

    private static void ParseFormField(string fieldValue, CurlOptions options)
    {
        var equalsIndex = fieldValue.IndexOf('=');
        if (equalsIndex < 0)
        {
            throw new CurlParseException($"Invalid form field format: '{fieldValue}'. Expected 'name=value'.");
        }

        var name = fieldValue.Substring(0, equalsIndex);
        var value = fieldValue.Substring(equalsIndex + 1);
        var field = new FormField { Name = name };

        if (value.StartsWith("@"))
        {
            field.IsFile = true;
            var filePart = value.Substring(1);

            // Check for ;type=... suffix
            var typeIndex = filePart.IndexOf(";type=", StringComparison.OrdinalIgnoreCase);
            if (typeIndex >= 0)
            {
                field.ContentType = filePart.Substring(typeIndex + 6);
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
        if (currentIndex + 1 >= tokens.Count)
        {
            throw new CurlParseException($"Option '{optionName}' requires an argument.", currentIndex);
        }
        return tokens[currentIndex + 1];
    }

    private static int ParseInt(string value, string optionName)
    {
        if (!int.TryParse(value, out var result))
        {
            throw new CurlParseException($"Option '{optionName}' requires a numeric value, got '{value}'.");
        }
        return result;
    }
}
