# CurlCommandParser

Parse and execute curl command strings at runtime via `HttpClient`. Zero external dependencies.

## Install

```
dotnet add package CurlCommandParser
```

## Usage

```csharp
using CurlCommandParser;

using var httpClient = new HttpClient();

// Simple GET
var response = await httpClient.ExecuteCurlAsync("https://api.example.com/users");

// POST with JSON body
var response = await httpClient.ExecuteCurlAsync(
    "curl -X POST https://api.example.com/users " +
    "-H 'Content-Type: application/json' " +
    "-d '{\"name\":\"John\",\"email\":\"john@example.com\"}'");

// Fire-and-forget (executes request, disposes response)
await httpClient.ExecuteCurlAndForgetAsync(
    "-X POST https://api.example.com/webhook -d '{\"event\":\"deploy\"}'");
```

The `curl` prefix is optional.

## Supported Options

| Option | Description |
|--------|-------------|
| `-X`, `--request` | HTTP method (GET, POST, PUT, DELETE, etc.) |
| `-H`, `--header` | Request header (`Name: Value`) |
| `-d`, `--data`, `--data-raw` | Request body (implies POST) |
| `--data-binary` | Binary request body |
| `-F`, `--form` | Multipart form field (`name=value` or `file=@path`) |
| `-u`, `--user` | Basic auth credentials (`user:password`) |
| `--oauth2-bearer` | Bearer token authentication |
| `-b`, `--cookie` | Cookie header |
| `-A`, `--user-agent` | User-Agent header |
| `-e`, `--referer` | Referer header |
| `-L`, `--location` | Follow redirects |
| `-k`, `--insecure` | Skip SSL certificate validation |
| `--compressed` | Accept gzip/deflate encoding |
| `--connect-timeout` | Connection timeout in seconds |
| `--max-time` | Maximum request time in seconds |
| `--data-urlencode` | URL-encode data (`name=value`, implies POST) |
| `-x`, `--proxy` | Use HTTP proxy |

## Features

- **Shell-accurate tokenizer** -- handles single quotes, double quotes, backslash escaping, and line continuations
- **Implicit method detection** -- `-d` implies POST, `-F` implies POST
- **Combined short flags** -- `-sLk` expands to silent + location + insecure
- **Content-Type routing** -- Content-Type headers are correctly placed on `HttpContent`, not on the request
- **Basic and Bearer auth** -- `-u user:pass` and `--oauth2-bearer token`
- **Multipart form data** -- `-F 'file=@/path/to/file;type=application/pdf'`
- **Timeout support** -- `--max-time` and `--connect-timeout` via `CancellationToken`
- **Insecure mode** -- `-k` uses an internal `HttpClient` that skips certificate validation
- **URL encoding** -- `--data-urlencode` automatically encodes values
- **Proxy support** -- `-x`/`--proxy` routes requests through an HTTP proxy

## Error Handling

Parse errors throw `CurlParseException` with a descriptive message. HTTP errors follow standard `HttpClient` behavior -- use `response.EnsureSuccessStatusCode()` or check `response.IsSuccessStatusCode`.

```csharp
using CurlCommandParser.Exceptions;

try
{
    var response = await httpClient.ExecuteCurlAsync("-X GET"); // no URL
}
catch (CurlParseException ex)
{
    Console.WriteLine(ex.Message); // "No URL found in curl command."
}
```

## Target Frameworks

- `netstandard2.0` -- .NET Framework 4.6.1+, .NET Core 2.0+
- `net8.0` -- modern .NET

## License

MIT
