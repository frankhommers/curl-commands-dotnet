using System.Text;

namespace CurlCommandParser.Parsing;

/// <summary>
/// Splits a string into tokens following POSIX shell quoting rules.
/// Handles single quotes, double quotes, and backslash escaping.
/// </summary>
public static class ShellTokenizer
{
    /// <summary>
    /// Splits a string into tokens following POSIX shell quoting rules.
    /// </summary>
    /// <param name="input">The input string to tokenize.</param>
    /// <returns>A list of tokens.</returns>
    public static List<string> Tokenize(string input)
    {
        List<string> tokens = [];
        if (string.IsNullOrEmpty(input))
            return tokens;

        StringBuilder current = new();
        bool inSingleQuote = false;
        bool inDoubleQuote = false;
        bool hasContent = false;

        for (int i = 0; i < input.Length; i++)
        {
            char c = input[i];

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
                    char next = input[i + 1];
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
                char next = input[i + 1];
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
