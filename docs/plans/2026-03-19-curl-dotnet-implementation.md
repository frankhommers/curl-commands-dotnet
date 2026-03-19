# CurlDotNet Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Build a .NET library that parses curl command strings at runtime and executes them via HttpClient.

**Architecture:** Three-layer pipeline: ShellTokenizer (string -> tokens) -> CurlOptionParser (tokens -> CurlOptions model) -> HttpRequestBuilder (CurlOptions -> HttpRequestMessage). Public API is extension methods on HttpClient.

**Tech Stack:** C# / .NET 8 + netstandard2.0, xUnit for tests, zero external dependencies.

---

### Task 1: Project Scaffolding

**Files:**
- Create: `CurlDotNet.sln`
- Create: `src/CurlDotNet/CurlDotNet.csproj`
- Create: `tests/CurlDotNet.Tests/CurlDotNet.Tests.csproj`

**Step 1: Create solution and projects**

```bash
dotnet new sln -n CurlDotNet
mkdir -p src/CurlDotNet
dotnet new classlib -n CurlDotNet -o src/CurlDotNet -f net8.0
mkdir -p tests/CurlDotNet.Tests
dotnet new xunit -n CurlDotNet.Tests -o tests/CurlDotNet.Tests -f net8.0
dotnet sln add src/CurlDotNet/CurlDotNet.csproj
dotnet sln add tests/CurlDotNet.Tests/CurlDotNet.Tests.csproj
dotnet add tests/CurlDotNet.Tests/CurlDotNet.Tests.csproj reference src/CurlDotNet/CurlDotNet.csproj
```

**Step 2: Configure multi-targeting in CurlDotNet.csproj**

Edit `src/CurlDotNet/CurlDotNet.csproj`:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFrameworks>netstandard2.0;net8.0</TargetFrameworks>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <RootNamespace>CurlDotNet</RootNamespace>
  </PropertyGroup>
  <ItemGroup Condition="'$(TargetFramework)' == 'netstandard2.0'">
    <PackageReference Include="System.Net.Http" Version="4.3.4" />
  </ItemGroup>
</Project>
```

**Step 3: Remove auto-generated Class1.cs**

```bash
rm src/CurlDotNet/Class1.cs
rm tests/CurlDotNet.Tests/UnitTest1.cs
```

**Step 4: Verify build**

Run: `dotnet build`
Expected: Build succeeded with 0 errors.

**Step 5: Commit**

```bash
git init
git add .
git commit -m "chore: scaffold CurlDotNet solution with library and test projects"
```

---

### Task 2: ShellTokenizer

**Files:**
- Create: `src/CurlDotNet/Parsing/ShellTokenizer.cs`
- Create: `tests/CurlDotNet.Tests/ShellTokenizerTests.cs`

**Step 1: Write failing tests**

Create `tests/CurlDotNet.Tests/ShellTokenizerTests.cs`:
```csharp
using CurlDotNet.Parsing;

namespace CurlDotNet.Tests;

public class ShellTokenizerTests
{
    [Fact]
    public void Tokenize_SimpleWords_SplitsOnWhitespace()
    {
        var tokens = ShellTokenizer.Tokenize("one two three");
        Assert.Equal(new[] { "one", "two", "three" }, tokens);
    }

    [Fact]
    public void Tokenize_MultipleSpaces_IgnoresExtraWhitespace()
    {
        var tokens = ShellTokenizer.Tokenize("one   two    three");
        Assert.Equal(new[] { "one", "two", "three" }, tokens);
    }

    [Fact]
    public void Tokenize_SingleQuotedString_PreservesSpaces()
    {
        var tokens = ShellTokenizer.Tokenize("one 'two three' four");
        Assert.Equal(new[] { "one", "two three", "four" }, tokens);
    }

    [Fact]
    public void Tokenize_DoubleQuotedString_PreservesSpaces()
    {
        var tokens = ShellTokenizer.Tokenize("one \"two three\" four");
        Assert.Equal(new[] { "one", "two three", "four" }, tokens);
    }

    [Fact]
    public void Tokenize_BackslashEscapedSpace_PreservesSpace()
    {
        var tokens = ShellTokenizer.Tokenize(@"one two\ three four");
        Assert.Equal(new[] { "one", "two three", "four" }, tokens);
    }

    [Fact]
    public void Tokenize_BackslashInDoubleQuotes_EscapesQuote()
    {
        var tokens = ShellTokenizer.Tokenize("one \"two \\\"three\\\"\" four");
        Assert.Equal(new[] { "one", "two \"three\"", "four" }, tokens);
    }

    [Fact]
    public void Tokenize_MixedQuotes_HandledCorrectly()
    {
        var tokens = ShellTokenizer.Tokenize("-H 'Content-Type: application/json' -d \"{\\\"key\\\":\\\"value\\\"}\"");
        Assert.Equal(new[] { "-H", "Content-Type: application/json", "-d", "{\"key\":\"value\"}" }, tokens);
    }

    [Fact]
    public void Tokenize_EmptyString_ReturnsEmpty()
    {
        var tokens = ShellTokenizer.Tokenize("");
        Assert.Empty(tokens);
    }

    [Fact]
    public void Tokenize_AdjacentQuotedStrings_ConcatenatedIntoOneToken()
    {
        var tokens = ShellTokenizer.Tokenize("'one''two'");
        Assert.Equal(new[] { "onetwo" }, tokens);
    }

    [Fact]
    public void Tokenize_SingleQuoteInsideDoubleQuotes_Preserved()
    {
        var tokens = ShellTokenizer.Tokenize("\"it's a test\"");
        Assert.Equal(new[] { "it's a test" }, tokens);
    }

    [Fact]
    public void Tokenize_LeadingAndTrailingWhitespace_Trimmed()
    {
        var tokens = ShellTokenizer.Tokenize("  one two  ");
        Assert.Equal(new[] { "one", "two" }, tokens);
    }

    [Fact]
    public void Tokenize_LineContinuation_BackslashNewline()
    {
        var tokens = ShellTokenizer.Tokenize("one \\\ntwo");
        Assert.Equal(new[] { "one", "two" }, tokens);
    }
}
```

**Step 2: Run tests to verify they fail**

Run: `dotnet test tests/CurlDotNet.Tests`
Expected: Build failure — `ShellTokenizer` does not exist yet.

**Step 3: Implement ShellTokenizer**

Create `src/CurlDotNet/Parsing/ShellTokenizer.cs`:
```csharp
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
```

**Step 4: Run tests to verify they pass**

Run: `dotnet test tests/CurlDotNet.Tests`
Expected: All 12 tests pass.

**Step 5: Commit**

```bash
git add .
git commit -m "feat: add ShellTokenizer with POSIX shell quoting support"
```

---

### Task 3: CurlOptions & FormField Models

**Files:**
- Create: `src/CurlDotNet/Models/CurlOptions.cs`
- Create: `src/CurlDotNet/Models/FormField.cs`

**Step 1: Create the models**

Create `src/CurlDotNet/Models/CurlOptions.cs`:
```csharp
namespace CurlDotNet.Models;

/// <summary>
/// Intermediate representation of a parsed curl command.
/// </summary>
public class CurlOptions
{
    public string Url { get; set; } = string.Empty;
    public string? Method { get; set; }
    public List<(string Name, string Value)> Headers { get; set; } = new();
    public string? DataBody { get; set; }
    public byte[]? BinaryData { get; set; }
    public string? UserCredentials { get; set; }
    public string? BearerToken { get; set; }
    public List<FormField> FormFields { get; set; } = new();
    public bool FollowRedirects { get; set; }
    public bool Insecure { get; set; }
    public int? ConnectTimeoutSeconds { get; set; }
    public int? MaxTimeSeconds { get; set; }
    public string? Cookie { get; set; }
    public string? UserAgent { get; set; }
    public string? Referer { get; set; }
    public bool Compressed { get; set; }
}
```

Create `src/CurlDotNet/Models/FormField.cs`:
```csharp
namespace CurlDotNet.Models;

/// <summary>
/// Represents a single -F / --form field.
/// </summary>
public class FormField
{
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public bool IsFile { get; set; }
    public string? ContentType { get; set; }
}
```

**Step 2: Verify build**

Run: `dotnet build`
Expected: Build succeeded.

**Step 3: Commit**

```bash
git add .
git commit -m "feat: add CurlOptions and FormField models"
```

---

### Task 4: CurlParseException

**Files:**
- Create: `src/CurlDotNet/Exceptions/CurlParseException.cs`

**Step 1: Create exception class**

Create `src/CurlDotNet/Exceptions/CurlParseException.cs`:
```csharp
namespace CurlDotNet.Exceptions;

/// <summary>
/// Thrown when a curl command string cannot be parsed.
/// </summary>
public class CurlParseException : Exception
{
    public int? TokenPosition { get; }

    public CurlParseException(string message) : base(message) { }

    public CurlParseException(string message, int tokenPosition)
        : base(message)
    {
        TokenPosition = tokenPosition;
    }

    public CurlParseException(string message, Exception innerException)
        : base(message, innerException) { }
}
```

**Step 2: Verify build**

Run: `dotnet build`
Expected: Build succeeded.

**Step 3: Commit**

```bash
git add .
git commit -m "feat: add CurlParseException"
```

---

### Task 5: CurlOptionParser

**Files:**
- Create: `src/CurlDotNet/Parsing/CurlOptionParser.cs`
- Create: `tests/CurlDotNet.Tests/CurlOptionParserTests.cs`

**Step 1: Write failing tests**

Create `tests/CurlDotNet.Tests/CurlOptionParserTests.cs`:
```csharp
using CurlDotNet.Exceptions;
using CurlDotNet.Parsing;

namespace CurlDotNet.Tests;

public class CurlOptionParserTests
{
    [Fact]
    public void Parse_SimpleGetUrl_ExtractsUrl()
    {
        var options = CurlOptionParser.Parse("https://api.example.com");
        Assert.Equal("https://api.example.com", options.Url);
        Assert.Null(options.Method);
    }

    [Fact]
    public void Parse_WithCurlPrefix_StripsPrefix()
    {
        var options = CurlOptionParser.Parse("curl https://api.example.com");
        Assert.Equal("https://api.example.com", options.Url);
    }

    [Fact]
    public void Parse_ExplicitMethod_SetsMethod()
    {
        var options = CurlOptionParser.Parse("-X POST https://api.example.com");
        Assert.Equal("POST", options.Method);
    }

    [Fact]
    public void Parse_LongOptionRequest_SetsMethod()
    {
        var options = CurlOptionParser.Parse("--request PUT https://api.example.com");
        Assert.Equal("PUT", options.Method);
    }

    [Fact]
    public void Parse_Header_AddsToHeaders()
    {
        var options = CurlOptionParser.Parse("-H 'Content-Type: application/json' https://api.example.com");
        Assert.Contains(("Content-Type", "application/json"), options.Headers);
    }

    [Fact]
    public void Parse_MultipleHeaders_AddsAll()
    {
        var options = CurlOptionParser.Parse(
            "-H 'Content-Type: application/json' -H 'Authorization: Bearer token' https://api.example.com");
        Assert.Equal(2, options.Headers.Count);
    }

    [Fact]
    public void Parse_DataImpliesPost()
    {
        var options = CurlOptionParser.Parse("-d '{\"key\":\"value\"}' https://api.example.com");
        Assert.Equal("{\"key\":\"value\"}", options.DataBody);
        // Method should be null — the builder infers POST from DataBody presence
    }

    [Fact]
    public void Parse_DataRaw_SetsDataBody()
    {
        var options = CurlOptionParser.Parse("--data-raw 'hello' https://api.example.com");
        Assert.Equal("hello", options.DataBody);
    }

    [Fact]
    public void Parse_UserCredentials_SetsUser()
    {
        var options = CurlOptionParser.Parse("-u admin:secret https://api.example.com");
        Assert.Equal("admin:secret", options.UserCredentials);
    }

    [Fact]
    public void Parse_BearerToken_SetsToken()
    {
        var options = CurlOptionParser.Parse("--oauth2-bearer mytoken123 https://api.example.com");
        Assert.Equal("mytoken123", options.BearerToken);
    }

    [Fact]
    public void Parse_FormField_AddsField()
    {
        var options = CurlOptionParser.Parse("-F 'name=John' https://api.example.com");
        Assert.Single(options.FormFields);
        Assert.Equal("name", options.FormFields[0].Name);
        Assert.Equal("John", options.FormFields[0].Value);
        Assert.False(options.FormFields[0].IsFile);
    }

    [Fact]
    public void Parse_FormFieldWithFile_MarksAsFile()
    {
        var options = CurlOptionParser.Parse("-F 'file=@/path/to/doc.pdf' https://api.example.com");
        Assert.Single(options.FormFields);
        Assert.True(options.FormFields[0].IsFile);
        Assert.Equal("/path/to/doc.pdf", options.FormFields[0].Value);
    }

    [Fact]
    public void Parse_FormFieldWithContentType_SetsContentType()
    {
        var options = CurlOptionParser.Parse("-F 'file=@/path/to/doc.pdf;type=application/pdf' https://api.example.com");
        Assert.Equal("application/pdf", options.FormFields[0].ContentType);
    }

    [Fact]
    public void Parse_FollowRedirects_SetsFlag()
    {
        var options = CurlOptionParser.Parse("-L https://api.example.com");
        Assert.True(options.FollowRedirects);
    }

    [Fact]
    public void Parse_LocationLongOption_SetsFlag()
    {
        var options = CurlOptionParser.Parse("--location https://api.example.com");
        Assert.True(options.FollowRedirects);
    }

    [Fact]
    public void Parse_Insecure_SetsFlag()
    {
        var options = CurlOptionParser.Parse("-k https://api.example.com");
        Assert.True(options.Insecure);
    }

    [Fact]
    public void Parse_ConnectTimeout_SetsValue()
    {
        var options = CurlOptionParser.Parse("--connect-timeout 30 https://api.example.com");
        Assert.Equal(30, options.ConnectTimeoutSeconds);
    }

    [Fact]
    public void Parse_MaxTime_SetsValue()
    {
        var options = CurlOptionParser.Parse("--max-time 60 https://api.example.com");
        Assert.Equal(60, options.MaxTimeSeconds);
    }

    [Fact]
    public void Parse_Cookie_SetsCookie()
    {
        var options = CurlOptionParser.Parse("-b 'session=abc123' https://api.example.com");
        Assert.Equal("session=abc123", options.Cookie);
    }

    [Fact]
    public void Parse_UserAgent_SetsUserAgent()
    {
        var options = CurlOptionParser.Parse("-A 'MyApp/1.0' https://api.example.com");
        Assert.Equal("MyApp/1.0", options.UserAgent);
    }

    [Fact]
    public void Parse_Referer_SetsReferer()
    {
        var options = CurlOptionParser.Parse("-e 'https://google.com' https://api.example.com");
        Assert.Equal("https://google.com", options.Referer);
    }

    [Fact]
    public void Parse_Compressed_SetsFlag()
    {
        var options = CurlOptionParser.Parse("--compressed https://api.example.com");
        Assert.True(options.Compressed);
    }

    [Fact]
    public void Parse_CombinedShortFlags_ParsesAll()
    {
        var options = CurlOptionParser.Parse("-sLk https://api.example.com");
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
        var options = CurlOptionParser.Parse(
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
}
```

**Step 2: Run tests to verify they fail**

Run: `dotnet test tests/CurlDotNet.Tests`
Expected: Build failure — `CurlOptionParser` does not exist yet.

**Step 3: Implement CurlOptionParser**

Create `src/CurlDotNet/Parsing/CurlOptionParser.cs`:
```csharp
using CurlDotNet.Exceptions;
using CurlDotNet.Models;

namespace CurlDotNet.Parsing;

/// <summary>
/// Parses a curl command string (or pre-tokenized arguments) into a CurlOptions model.
/// </summary>
public static class CurlOptionParser
{
    // Boolean flags that don't take arguments
    private static readonly HashSet<string> BooleanFlags = new(StringComparer.OrdinalIgnoreCase)
    {
        "--location", "--insecure", "--compressed"
    };

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
```

**Step 4: Run tests to verify they pass**

Run: `dotnet test tests/CurlDotNet.Tests`
Expected: All tests pass.

**Step 5: Commit**

```bash
git add .
git commit -m "feat: add CurlOptionParser with full curl flag support"
```

---

### Task 6: HttpRequestBuilder

**Files:**
- Create: `src/CurlDotNet/Building/HttpRequestBuilder.cs`
- Create: `tests/CurlDotNet.Tests/HttpRequestBuilderTests.cs`

**Step 1: Write failing tests**

Create `tests/CurlDotNet.Tests/HttpRequestBuilderTests.cs`:
```csharp
using CurlDotNet.Building;
using CurlDotNet.Models;

namespace CurlDotNet.Tests;

public class HttpRequestBuilderTests
{
    [Fact]
    public void Build_SimpleGet_CreatesGetRequest()
    {
        var options = new CurlOptions { Url = "https://api.example.com" };
        var request = HttpRequestBuilder.Build(options);

        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Equal("https://api.example.com", request.RequestUri!.ToString());
    }

    [Fact]
    public void Build_ExplicitPost_CreatesPostRequest()
    {
        var options = new CurlOptions { Url = "https://api.example.com", Method = "POST" };
        var request = HttpRequestBuilder.Build(options);

        Assert.Equal(HttpMethod.Post, request.Method);
    }

    [Fact]
    public void Build_DataBodyWithoutMethod_ImpliesPost()
    {
        var options = new CurlOptions
        {
            Url = "https://api.example.com",
            DataBody = "{\"key\":\"value\"}"
        };
        var request = HttpRequestBuilder.Build(options);

        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.NotNull(request.Content);
    }

    [Fact]
    public async Task Build_DataBody_SetsStringContent()
    {
        var options = new CurlOptions
        {
            Url = "https://api.example.com",
            DataBody = "{\"key\":\"value\"}"
        };
        var request = HttpRequestBuilder.Build(options);

        var content = await request.Content!.ReadAsStringAsync();
        Assert.Equal("{\"key\":\"value\"}", content);
    }

    [Fact]
    public void Build_Headers_AddsToRequest()
    {
        var options = new CurlOptions
        {
            Url = "https://api.example.com",
            Headers = { ("X-Custom", "test-value") }
        };
        var request = HttpRequestBuilder.Build(options);

        Assert.Equal("test-value", request.Headers.GetValues("X-Custom").First());
    }

    [Fact]
    public async Task Build_ContentTypeHeader_SetsOnContent()
    {
        var options = new CurlOptions
        {
            Url = "https://api.example.com",
            DataBody = "{}",
            Headers = { ("Content-Type", "application/json") }
        };
        var request = HttpRequestBuilder.Build(options);

        Assert.Equal("application/json", request.Content!.Headers.ContentType!.MediaType);
    }

    [Fact]
    public void Build_BasicAuth_SetsAuthorizationHeader()
    {
        var options = new CurlOptions
        {
            Url = "https://api.example.com",
            UserCredentials = "admin:secret"
        };
        var request = HttpRequestBuilder.Build(options);

        Assert.Equal("Basic", request.Headers.Authorization!.Scheme);
        var decoded = System.Text.Encoding.UTF8.GetString(
            Convert.FromBase64String(request.Headers.Authorization.Parameter!));
        Assert.Equal("admin:secret", decoded);
    }

    [Fact]
    public void Build_BearerToken_SetsAuthorizationHeader()
    {
        var options = new CurlOptions
        {
            Url = "https://api.example.com",
            BearerToken = "mytoken123"
        };
        var request = HttpRequestBuilder.Build(options);

        Assert.Equal("Bearer", request.Headers.Authorization!.Scheme);
        Assert.Equal("mytoken123", request.Headers.Authorization.Parameter);
    }

    [Fact]
    public void Build_Cookie_SetsCookieHeader()
    {
        var options = new CurlOptions
        {
            Url = "https://api.example.com",
            Cookie = "session=abc123"
        };
        var request = HttpRequestBuilder.Build(options);

        Assert.Equal("session=abc123", request.Headers.GetValues("Cookie").First());
    }

    [Fact]
    public void Build_UserAgent_SetsUserAgentHeader()
    {
        var options = new CurlOptions
        {
            Url = "https://api.example.com",
            UserAgent = "MyApp/1.0"
        };
        var request = HttpRequestBuilder.Build(options);

        Assert.Equal("MyApp/1.0", request.Headers.UserAgent.ToString());
    }

    [Fact]
    public void Build_Referer_SetsRefererHeader()
    {
        var options = new CurlOptions
        {
            Url = "https://api.example.com",
            Referer = "https://google.com"
        };
        var request = HttpRequestBuilder.Build(options);

        Assert.Equal(new Uri("https://google.com"), request.Headers.Referrer);
    }

    [Fact]
    public void Build_Compressed_SetsAcceptEncoding()
    {
        var options = new CurlOptions
        {
            Url = "https://api.example.com",
            Compressed = true
        };
        var request = HttpRequestBuilder.Build(options);

        var encodings = request.Headers.AcceptEncoding.Select(e => e.Value).ToList();
        Assert.Contains("gzip", encodings);
        Assert.Contains("deflate", encodings);
    }

    [Fact]
    public void Build_FormFields_CreatesMultipartContent()
    {
        var options = new CurlOptions
        {
            Url = "https://api.example.com",
            FormFields =
            {
                new FormField { Name = "name", Value = "John" },
                new FormField { Name = "age", Value = "30" }
            }
        };
        var request = HttpRequestBuilder.Build(options);

        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.IsType<MultipartFormDataContent>(request.Content);
    }

    [Fact]
    public void Build_ExplicitMethodWithData_UsesExplicitMethod()
    {
        var options = new CurlOptions
        {
            Url = "https://api.example.com",
            Method = "PUT",
            DataBody = "{}"
        };
        var request = HttpRequestBuilder.Build(options);

        Assert.Equal(HttpMethod.Put, request.Method);
    }

    [Fact]
    public void Build_BinaryData_SetsByteArrayContent()
    {
        var data = new byte[] { 0x01, 0x02, 0x03 };
        var options = new CurlOptions
        {
            Url = "https://api.example.com",
            BinaryData = data
        };
        var request = HttpRequestBuilder.Build(options);

        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.IsType<ByteArrayContent>(request.Content);
    }
}
```

**Step 2: Run tests to verify they fail**

Run: `dotnet test tests/CurlDotNet.Tests`
Expected: Build failure — `HttpRequestBuilder` does not exist yet.

**Step 3: Implement HttpRequestBuilder**

Create `src/CurlDotNet/Building/HttpRequestBuilder.cs`:
```csharp
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
```

**Step 4: Run tests to verify they pass**

Run: `dotnet test tests/CurlDotNet.Tests`
Expected: All tests pass.

**Step 5: Commit**

```bash
git add .
git commit -m "feat: add HttpRequestBuilder to convert CurlOptions to HttpRequestMessage"
```

---

### Task 7: InsecureHttpClientFactory

**Files:**
- Create: `src/CurlDotNet/Internal/InsecureHttpClientFactory.cs`

**Step 1: Implement InsecureHttpClientFactory**

Create `src/CurlDotNet/Internal/InsecureHttpClientFactory.cs`:
```csharp
namespace CurlDotNet.Internal;

/// <summary>
/// Provides a cached HttpClient that skips SSL certificate validation.
/// Thread-safe via Lazy initialization.
/// </summary>
internal static class InsecureHttpClientFactory
{
    private static readonly Lazy<HttpClient> LazyClient = new(() =>
    {
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (_, _, _, _) => true
        };
        return new HttpClient(handler);
    });

    public static HttpClient GetClient() => LazyClient.Value;
}
```

**Step 2: Verify build**

Run: `dotnet build`
Expected: Build succeeded.

**Step 3: Commit**

```bash
git add .
git commit -m "feat: add InsecureHttpClientFactory for -k/--insecure support"
```

---

### Task 8: CurlHttpClientExtensions (Public API)

**Files:**
- Create: `src/CurlDotNet/CurlHttpClientExtensions.cs`
- Create: `tests/CurlDotNet.Tests/IntegrationTests.cs`

**Step 1: Write failing tests**

Create `tests/CurlDotNet.Tests/IntegrationTests.cs`:
```csharp
using CurlDotNet;
using CurlDotNet.Exceptions;

namespace CurlDotNet.Tests;

public class IntegrationTests
{
    [Fact]
    public async Task ExecuteCurlAsync_SimpleGet_ReturnsResponse()
    {
        using var httpClient = new HttpClient();
        var response = await httpClient.ExecuteCurlAsync("https://httpbin.org/get");

        Assert.True(response.IsSuccessStatusCode);
        Assert.NotNull(response.Content);
    }

    [Fact]
    public async Task ExecuteCurlAsync_PostWithJsonBody_SendsCorrectly()
    {
        using var httpClient = new HttpClient();
        var response = await httpClient.ExecuteCurlAsync(
            "curl -X POST https://httpbin.org/post " +
            "-H 'Content-Type: application/json' " +
            "-d '{\"test\":\"value\"}'");

        Assert.True(response.IsSuccessStatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"test\"", body);
    }

    [Fact]
    public async Task ExecuteCurlAsync_WithBasicAuth_SendsAuthHeader()
    {
        using var httpClient = new HttpClient();
        var response = await httpClient.ExecuteCurlAsync(
            "-u testuser:testpass https://httpbin.org/basic-auth/testuser/testpass");

        Assert.True(response.IsSuccessStatusCode);
    }

    [Fact]
    public async Task ExecuteCurlAsync_WithCustomHeaders_SendsHeaders()
    {
        using var httpClient = new HttpClient();
        var response = await httpClient.ExecuteCurlAsync(
            "-H 'X-Custom-Header: test123' https://httpbin.org/headers");

        Assert.True(response.IsSuccessStatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("test123", body);
    }

    [Fact]
    public async Task ExecuteCurlAsync_InvalidCommand_ThrowsCurlParseException()
    {
        using var httpClient = new HttpClient();

        await Assert.ThrowsAsync<CurlParseException>(
            () => httpClient.ExecuteCurlAsync("-X GET"));
    }

    [Fact]
    public async Task ExecuteCurlAsync_WithTimeout_AppliesTimeout()
    {
        using var httpClient = new HttpClient();
        var response = await httpClient.ExecuteCurlAsync(
            "--max-time 10 https://httpbin.org/get");

        Assert.True(response.IsSuccessStatusCode);
    }

    [Fact]
    public async Task ExecuteCurlAsync_WithCancellationToken_Cancellable()
    {
        using var httpClient = new HttpClient();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => httpClient.ExecuteCurlAsync("https://httpbin.org/get", cts.Token));
    }

    [Fact]
    public async Task ExecuteCurlAndForgetAsync_DoesNotThrow()
    {
        using var httpClient = new HttpClient();

        // Should not throw, just fire and forget
        await httpClient.ExecuteCurlAndForgetAsync(
            "-X POST https://httpbin.org/post -d 'fire-and-forget'");
    }
}
```

**Step 2: Run tests to verify they fail**

Run: `dotnet test tests/CurlDotNet.Tests`
Expected: Build failure — extension methods do not exist yet.

**Step 3: Implement CurlHttpClientExtensions**

Create `src/CurlDotNet/CurlHttpClientExtensions.cs`:
```csharp
using CurlDotNet.Building;
using CurlDotNet.Internal;
using CurlDotNet.Models;
using CurlDotNet.Parsing;

namespace CurlDotNet;

/// <summary>
/// Extension methods on HttpClient for executing curl commands.
/// </summary>
public static class CurlHttpClientExtensions
{
    /// <summary>
    /// Parses and executes a curl command string, returning the HttpResponseMessage.
    /// The "curl" prefix is optional.
    /// </summary>
    /// <param name="httpClient">The HttpClient to use for the request.</param>
    /// <param name="curlCommand">The curl command string (e.g., "-X POST https://example.com -d '{}'").</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The HttpResponseMessage from the executed request.</returns>
    public static async Task<HttpResponseMessage> ExecuteCurlAsync(
        this HttpClient httpClient,
        string curlCommand,
        CancellationToken cancellationToken = default)
    {
        var options = CurlOptionParser.Parse(curlCommand);
        var request = HttpRequestBuilder.Build(options);
        var client = ResolveClient(httpClient, options);

        using var timeoutCts = CreateTimeoutCts(options, cancellationToken);
        var token = timeoutCts?.Token ?? cancellationToken;

        var response = await client.SendAsync(request, token);

        if (options.FollowRedirects && IsRedirect(response))
        {
            response = await FollowRedirectsAsync(client, response, options, token);
        }

        return response;
    }

    /// <summary>
    /// Parses and executes a curl command string, discarding the response (fire-and-forget).
    /// The "curl" prefix is optional.
    /// </summary>
    /// <param name="httpClient">The HttpClient to use for the request.</param>
    /// <param name="curlCommand">The curl command string.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    public static async Task ExecuteCurlAndForgetAsync(
        this HttpClient httpClient,
        string curlCommand,
        CancellationToken cancellationToken = default)
    {
        using var response = await ExecuteCurlAsync(httpClient, curlCommand, cancellationToken);
        // Response is disposed — fire and forget
    }

    private static HttpClient ResolveClient(HttpClient httpClient, CurlOptions options)
    {
        if (options.Insecure)
        {
            return InsecureHttpClientFactory.GetClient();
        }
        return httpClient;
    }

    private static CancellationTokenSource? CreateTimeoutCts(CurlOptions options, CancellationToken cancellationToken)
    {
        var timeoutSeconds = options.MaxTimeSeconds ?? options.ConnectTimeoutSeconds;
        if (timeoutSeconds.HasValue)
        {
            var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds.Value));
            return cts;
        }
        return null;
    }

    private static bool IsRedirect(HttpResponseMessage response)
    {
        var code = (int)response.StatusCode;
        return code >= 300 && code < 400 && response.Headers.Location != null;
    }

    private static async Task<HttpResponseMessage> FollowRedirectsAsync(
        HttpClient client,
        HttpResponseMessage response,
        CurlOptions options,
        CancellationToken cancellationToken,
        int maxRedirects = 20)
    {
        var redirectCount = 0;
        while (IsRedirect(response) && redirectCount < maxRedirects)
        {
            var location = response.Headers.Location!;
            response.Dispose();

            var redirectRequest = new HttpRequestMessage(HttpMethod.Get, location);
            response = await client.SendAsync(redirectRequest, cancellationToken);
            redirectCount++;
        }
        return response;
    }
}
```

**Step 4: Run tests to verify they pass**

Run: `dotnet test tests/CurlDotNet.Tests`
Expected: All tests pass (integration tests require network access to httpbin.org).

**Step 5: Commit**

```bash
git add .
git commit -m "feat: add CurlHttpClientExtensions with ExecuteCurlAsync and ExecuteCurlAndForgetAsync"
```

---

### Task 9: Final Verification & Cleanup

**Step 1: Run all tests**

Run: `dotnet test`
Expected: All tests pass.

**Step 2: Run build for all target frameworks**

Run: `dotnet build src/CurlDotNet/CurlDotNet.csproj`
Expected: Build succeeded for both netstandard2.0 and net8.0.

**Step 3: Final commit**

```bash
git add .
git commit -m "chore: final cleanup and verification"
```
