# CurlDotNet Design Document

**Date:** 2026-03-19
**Status:** Approved

## Purpose

A .NET library that parses curl command strings at runtime and executes them via `HttpClient`. No code generation — dynamic parsing and execution.

**Primary use cases:**
1. Config-driven HTTP calls (curl commands stored in config/database, executed at runtime)
2. Developer convenience (paste a curl command from docs/Postman, execute directly in C#)

## Public API

Extension methods on `HttpClient`:

```csharp
// Returns HttpResponseMessage
HttpResponseMessage response = await httpClient.ExecuteCurlAsync(
    "-X POST https://api.example.com -H 'Authorization: Bearer token' -d '{\"key\":\"value\"}'");

// Fire-and-forget: executes request, disposes response, returns nothing
await httpClient.ExecuteCurlAndForgetAsync(
    "-X POST https://api.example.com/webhook -d '{\"event\":\"deploy\"}'");
```

- The `curl` prefix in the string is optional (stripped if present)
- Returns `HttpResponseMessage` for the standard variant
- `ExecuteCurlAndForgetAsync` executes and disposes — no return value

## Architecture

Three layers, each with a single responsibility:

```
"curl -X POST https://api.example.com -H 'Content-Type: application/json' -d '{\"name\":\"test\"}'"
                            |
                    [1. ShellTokenizer]
                            |
            ["curl", "-X", "POST", "https://api.example.com",
             "-H", "Content-Type: application/json",
             "-d", "{\"name\":\"test\"}"]
                            |
                    [2. CurlOptionParser]
                            |
                    CurlOptions {
                        Url = "https://api.example.com",
                        Method = "POST",
                        Headers = [("Content-Type", "application/json")],
                        DataBody = "{\"name\":\"test\"}"
                    }
                            |
                    [3. HttpRequestBuilder]
                            |
                    HttpRequestMessage (ready to send)
```

### Layer 1: ShellTokenizer

Splits a string on whitespace while respecting:
- Single-quoted strings (`'...'`)
- Double-quoted strings (`"..."`)
- Backslash escaping (`\ `, `\"`, `\\`)
- Strips outer quotes from tokens

Generic and reusable. No curl-specific logic.

### Layer 2: CurlOptionParser

Takes tokens from the tokenizer and produces a `CurlOptions` model. Handles:
- Short and long options (`-X` / `--request`, `-H` / `--header`, etc.)
- Combined short flags (`-sLk` -> silent + location + insecure)
- Implicit method rules (`-d` without `-X` implies POST, `-F` implies POST)
- Header removal syntax (`-H 'X-Custom:'` with empty value)
- `@filename` syntax in data options
- Strips optional `curl` prefix token

### Layer 3: HttpRequestBuilder

Converts `CurlOptions` into an `HttpRequestMessage`. Handles:
- Body encoding (string content, binary, form-urlencoded)
- Multipart form construction from `-F` fields
- Basic auth header from `-u user:pass`
- Bearer token header from `--oauth2-bearer`
- Cookie header from `-b`
- User-Agent, Referer headers

## CurlOptions Model

```csharp
public class CurlOptions
{
    public string Url { get; set; }
    public string? Method { get; set; }                             // -X, --request
    public List<(string Name, string Value)> Headers { get; set; }  // -H, --header
    public string? DataBody { get; set; }                           // -d, --data, --data-raw
    public byte[]? BinaryData { get; set; }                         // --data-binary
    public string? UserCredentials { get; set; }                    // -u, --user
    public string? BearerToken { get; set; }                        // --oauth2-bearer
    public List<FormField> FormFields { get; set; }                 // -F, --form
    public bool FollowRedirects { get; set; }                       // -L, --location
    public bool Insecure { get; set; }                              // -k, --insecure
    public int? ConnectTimeoutSeconds { get; set; }                 // --connect-timeout
    public int? MaxTimeSeconds { get; set; }                        // --max-time
    public string? Cookie { get; set; }                             // -b, --cookie
    public string? UserAgent { get; set; }                          // -A, --user-agent
    public string? Referer { get; set; }                            // -e, --referer
    public bool Compressed { get; set; }                            // --compressed
}

public class FormField
{
    public string Name { get; set; }
    public string Value { get; set; }
    public bool IsFile { get; set; }        // -F 'file=@/path/to/file'
    public string? ContentType { get; set; }
}
```

## Error Handling

- **Parse errors** throw `CurlParseException` with a descriptive message and token position
- **Unsupported options** throw `CurlParseException` with the option name and a clear message
- **HTTP errors** are not caught — standard `HttpClient` behavior applies (caller uses `EnsureSuccessStatusCode()` etc.)

## --insecure / -k Support

When `-k` is present, the library creates an internal `HttpClient` with a handler that skips certificate validation. This client is cached (thread-safe, lazy) to avoid per-call overhead. The original `HttpClient`'s default headers and base address are propagated.

## Timeout Handling

- `--max-time` maps to a `CancellationTokenSource` with timeout, passed to `SendAsync`
- `--connect-timeout` maps to the same mechanism (HttpClient does not expose connect-timeout separately; documented as a known limitation)

## Project Structure

```
curl-dotnet/
├── src/
│   └── CurlDotNet/
│       ├── CurlDotNet.csproj                (netstandard2.0 + net8.0)
│       ├── CurlHttpClientExtensions.cs
│       ├── Parsing/
│       │   ├── ShellTokenizer.cs
│       │   └── CurlOptionParser.cs
│       ├── Models/
│       │   ├── CurlOptions.cs
│       │   └── FormField.cs
│       ├── Building/
│       │   └── HttpRequestBuilder.cs
│       ├── Exceptions/
│       │   └── CurlParseException.cs
│       └── Internal/
│           └── InsecureHttpClientFactory.cs
├── tests/
│   └── CurlDotNet.Tests/
│       ├── CurlDotNet.Tests.csproj          (xUnit)
│       ├── ShellTokenizerTests.cs
│       ├── CurlOptionParserTests.cs
│       ├── HttpRequestBuilderTests.cs
│       └── IntegrationTests.cs
├── CurlDotNet.sln
└── README.md
```

## Dependencies

Zero external dependencies. Only `System.Net.Http` and BCL types.

## Target Frameworks

- `netstandard2.0` — broad compatibility (.NET Framework 4.6.1+, .NET Core 2.0+)
- `net8.0` — modern features, better performance

## Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Parser approach | Hand-rolled | Curl syntax is simple enough; full control over error messages and curl-specific semantics |
| No CommandLineParser lib | N/A | Those libs are designed for your own CLI args, not for emulating another tool's syntax |
| API surface | Extension methods on HttpClient | Familiar pattern, works with existing HttpClient/IHttpClientFactory usage |
| Response type | HttpResponseMessage | Standard .NET type, no wrapper needed |
| --insecure | Internal cached HttpClient | Enables -k without requiring caller to configure handlers |
| "curl" prefix | Optional | Supports both `"curl -X POST ..."` and `"-X POST ..."` |
