# CurlCommands

Parse and execute curl command strings at runtime via `HttpClient`. Zero external dependencies.

## Install

```
dotnet add package CurlCommands
```

## Usage

```csharp
using CurlCommands;

using var httpClient = new HttpClient();

// Simple GET
var response = await httpClient.ExecuteCurlAsync("https://api.example.com/users");

// POST with JSON body
var response = await httpClient.ExecuteCurlAsync(
    "curl -X POST https://api.example.com/users " +
    "-H 'Content-Type: application/json' " +
    "-d '{\"name\":\"John\",\"email\":\"john@example.com\"}'");

// --json flag (auto Content-Type + Accept)
var response = await httpClient.ExecuteCurlAsync(
    "curl --json '{\"name\":\"John\"}' https://api.example.com/users");

// Save response to file
var response = await httpClient.ExecuteCurlAsync(
    "curl -o result.json https://api.example.com/users");

// Pre-parse and reuse
CurlCommand cmd = CurlCommand.Parse("curl -X POST https://api.example.com/users -d '{}'");
var response = await httpClient.ExecuteCurlAsync(cmd);

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
| `--json` | JSON request body (auto Content-Type + Accept) |
| `-F`, `--form` | Multipart form field (`name=value` or `file=@path`) |
| `-o`, `--output` | Save response body to file |
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
| `-G`, `--get` | Force GET; data becomes query string |
| `-I`, `--head` | HEAD request |
| `-T`, `--upload-file` | Upload file via PUT |
| `--cert`, `--cert-type` | Client certificate (PEM, DER, P12) |
| `--key`, `--key-type` | Client certificate private key |
| `-U`, `--proxy-user` | Proxy credentials (`user:password`) |
| `--http1.0`, `--http1.1`, `--http2`, `--http3` | HTTP version |

## Features

- **Shell-accurate tokenizer** -- handles single quotes, double quotes, backslash escaping, and line continuations
- **Implicit method detection** -- `-d` implies POST, `-F` implies POST
- **Combined short flags** -- `-sLk` expands to silent + location + insecure
- **Content-Type routing** -- Content-Type headers are correctly placed on `HttpContent`, not on the request
- **JSON flag** -- `--json` auto-sets Content-Type and Accept to `application/json`
- **Output to file** -- `-o`/`--output` saves response body to disk
- **Basic and Bearer auth** -- `-u user:pass` and `--oauth2-bearer token`
- **Multipart form data** -- `-F 'file=@/path/to/file;type=application/pdf'`
- **Timeout support** -- `--max-time` and `--connect-timeout` via `CancellationToken`
- **Insecure mode** -- `-k` uses an internal `HttpClient` that skips certificate validation
- **URL encoding** -- `--data-urlencode` automatically encodes values
- **Proxy support** -- `-x`/`--proxy` routes requests through an HTTP proxy
- **Force GET** -- `-G` moves `-d` data to query string parameters
- **File upload** -- `-T` uploads files via PUT
- **Client certificates** -- `--cert`/`--key` for mutual TLS
- **Proxy auth** -- `-U`/`--proxy-user` for proxy credentials
- **HTTP version** -- `--http1.0` through `--http3`
- **Pre-parsed commands** -- `CurlCommand.Parse()` for reuse and inspection

## Error Handling

Parse errors throw `CurlParseException` with a descriptive message. HTTP errors follow standard `HttpClient` behavior -- use `response.EnsureSuccessStatusCode()` or check `response.IsSuccessStatusCode`.

```csharp
using CurlCommands.Exceptions;

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
- `net10.0` -- modern .NET

## License

MIT
