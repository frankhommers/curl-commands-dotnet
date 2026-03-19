namespace CurlDotNet.Parsing;

/// <summary>
/// Splits a string into tokens following POSIX shell quoting rules.
/// Handles single quotes, double quotes, and backslash escaping.
/// </summary>
public static class ShellTokenizer
{
    public static List<string> Tokenize(string input)
    {
        var tokens = new List<string>();
        if (string.IsNullOrEmpty(input))
            return tokens;

        var current = new System.Text.StringBuilder();
        var inSingleQuote = false;
        var inDoubleQuote = false;
        var hasContent = false;

        for (var i = 0; i < input.Length; i++)
        {
            var c = input[i];

            if (inSingleQuote)
            {
                if (c == '\'')
                {
                    inSingleQuote = false;
                }
                else
                {
                    current.Append(c);
                }
                continue;
            }

            if (inDoubleQuote)
            {
                if (c == '\\' && i + 1 < input.Length)
                {
                    var next = input[i + 1];
                    if (next == '"' || next == '\\' || next == '$' || next == '`')
                    {
                        current.Append(next);
                        i++;
                    }
                    else
                    {
                        current.Append(c);
                    }
                }
                else if (c == '"')
                {
                    inDoubleQuote = false;
                }
                else
                {
                    current.Append(c);
                }
                continue;
            }

            // Not inside quotes
            if (c == '\\' && i + 1 < input.Length)
            {
                var next = input[i + 1];
                if (next == '\n')
                {
                    // Line continuation — skip both backslash and newline
                    i++;
                }
                else
                {
                    current.Append(next);
                    hasContent = true;
                    i++;
                }
                continue;
            }

            if (c == '\'')
            {
                inSingleQuote = true;
                hasContent = true;
                continue;
            }

            if (c == '"')
            {
                inDoubleQuote = true;
                hasContent = true;
                continue;
            }

            if (char.IsWhiteSpace(c))
            {
                if (current.Length > 0 || hasContent)
                {
                    tokens.Add(current.ToString());
                    current.Clear();
                    hasContent = false;
                }
                continue;
            }

            current.Append(c);
            hasContent = true;
        }

        if (current.Length > 0 || hasContent)
        {
            tokens.Add(current.ToString());
        }

        return tokens;
    }
}
