# CurlCommand Rename + --json + -o Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Rename the library from CurlCommandParser to CurlCommand, add `CurlCommand.Parse()` API, add `--json` flag support, and add `-o` output support.

**Architecture:** The rename changes namespace, package ID, project name, and assembly name. `CurlCommand` becomes a public class wrapping `CurlOptions` with a static `Parse()` factory. `--json` sets Content-Type and Accept headers automatically. `-o` saves response body to file after execution.

**Tech Stack:** C# / .NET 8 + netstandard2.0, xUnit

---

### Task 1: Add --json flag support (parser)

**Files:**
- Modify: `src/CurlCommandParser/Parsing/CurlOptionParser.cs`
- Modify: `src/CurlCommandParser/Models/CurlOptions.cs`
- Test: `tests/CurlCommandParser.Tests/CurlOptionParserTests.cs`

**Step 1: Write failing tests**

In `CurlOptionParserTests.cs`, add:

```csharp
[Fact]
public void Parse_JsonFlag_SetsDataBodyAndJsonFlag()
{
  CurlOptions options = CurlOptionParser.Parse("curl --json '{\"key\":\"value\"}' https://api.example.com");
  Assert.Equal("{\"key\":\"value\"}", options.DataBody);
  Assert.True(options.IsJson);
}

[Fact]
public void Parse_JsonFlag_DoesNotOverrideExplicitContentType()
{
  CurlOptions options = CurlOptionParser.Parse("curl -H 'Content-Type: text/plain' --json '{\"key\":\"value\"}' https://api.example.com");
  Assert.True(options.IsJson);
  // Explicit Content-Type header should remain as set by user
  Assert.Contains(options.Headers, h => h.Name == "Content-Type" && h.Value == "text/plain");
}
```

**Step 2: Run tests to verify they fail**

Run: `dotnet test --filter "Parse_Json" --verbosity quiet`
Expected: FAIL - `CurlOptions` has no `IsJson` property, parser doesn't know `--json`

**Step 3: Add IsJson property to CurlOptions**

In `CurlOptions.cs`, add after the `Compressed` property:

```csharp
/// <summary>Whether --json flag was used. Implies POST, Content-Type: application/json, Accept: application/json.</summary>
public bool IsJson { get; set; }
```

**Step 4: Add --json parsing to CurlOptionParser**

In `CurlOptionParser.cs`:
- Add constant: `private const string LongJson = "--json";`
- Add case in `ParseLongOption` switch:

```csharp
case LongJson:
  options.DataBody = GetNextArg(tokens, i, token);
  options.IsJson = true;
  return i + 2;
```

**Step 5: Run tests to verify they pass**

Run: `dotnet test --filter "Parse_Json" --verbosity quiet`
Expected: PASS

**Step 6: Commit**

```
feat: add --json flag parsing support
```

---

### Task 2: Add --json flag support (builder)

**Files:**
- Modify: `src/CurlCommandParser/Building/HttpRequestBuilder.cs`
- Test: `tests/CurlCommandParser.Tests/HttpRequestBuilderTests.cs`

**Step 1: Write failing test**

In `HttpRequestBuilderTests.cs`, add:

```csharp
[Fact]
public void Build_JsonFlag_SetsContentTypeAndAcceptHeaders()
{
  CurlOptions options = new()
  {
    Url = "https://example.com",
    DataBody = "{\"key\":\"value\"}",
    IsJson = true
  };
  HttpRequestMessage request = HttpRequestBuilder.Build(options);
  Assert.Equal("application/json", request.Content?.Headers.ContentType?.MediaType);
  Assert.Contains(request.Headers.Accept, a => a.MediaType == "application/json");
}

[Fact]
public void Build_JsonFlag_ImpliesPost()
{
  CurlOptions options = new()
  {
    Url = "https://example.com",
    DataBody = "{}",
    IsJson = true
  };
  HttpRequestMessage request = HttpRequestBuilder.Build(options);
  Assert.Equal(HttpMethod.Post, request.Method);
}

[Fact]
public void Build_JsonFlag_DoesNotOverrideExplicitContentType()
{
  CurlOptions options = new()
  {
    Url = "https://example.com",
    DataBody = "{}",
    IsJson = true,
    Headers = [("Content-Type", "text/plain")]
  };
  HttpRequestMessage request = HttpRequestBuilder.Build(options);
  // User-specified Content-Type wins
  Assert.Equal("text/plain", request.Content?.Headers.ContentType?.MediaType);
}
```

**Step 2: Run tests to verify they fail**

Run: `dotnet test --filter "Build_JsonFlag" --verbosity quiet`
Expected: FAIL - Accept header not set, Content-Type not auto-set

**Step 3: Add --json handling to HttpRequestBuilder**

In `HttpRequestBuilder.cs`, in the `Build` method, after `SetMiscHeaders`:

```csharp
if (options.IsJson)
{
  ApplyJsonDefaults(request, options);
}
```

Add the method:

```csharp
private static void ApplyJsonDefaults(HttpRequestMessage request, CurlOptions options)
{
  // Add Accept: application/json if not already set
  if (request.Headers.Accept.Count == 0)
  {
    request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
  }

  // Set Content-Type to application/json if no explicit Content-Type was provided
  bool hasExplicitContentType = options.Headers.Any(h =>
    h.Name.Equals(ContentTypeHeader, StringComparison.OrdinalIgnoreCase));

  if (!hasExplicitContentType && request.Content != null)
  {
    request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json") { CharSet = "utf-8" };
  }
}
```

**Step 4: Run tests to verify they pass**

Run: `dotnet test --filter "Build_JsonFlag" --verbosity quiet`
Expected: PASS

**Step 5: Run all tests**

Run: `dotnet test --verbosity quiet`
Expected: All pass

**Step 6: Commit**

```
feat: add --json flag to HttpRequestBuilder with auto Content-Type and Accept headers
```

---

### Task 3: Add -o output support (parser)

**Files:**
- Modify: `src/CurlCommandParser/Parsing/CurlOptionParser.cs`
- Modify: `src/CurlCommandParser/Models/CurlOptions.cs`
- Test: `tests/CurlCommandParser.Tests/CurlOptionParserTests.cs`

**Step 1: Write failing tests**

```csharp
[Fact]
public void Parse_OutputFlag_SetsOutputFile()
{
  CurlOptions options = CurlOptionParser.Parse("curl -o output.json https://api.example.com");
  Assert.Equal("output.json", options.OutputFile);
}

[Fact]
public void Parse_LongOutputFlag_SetsOutputFile()
{
  CurlOptions options = CurlOptionParser.Parse("curl --output result.txt https://api.example.com");
  Assert.Equal("result.txt", options.OutputFile);
}
```

**Step 2: Run tests to verify they fail**

Run: `dotnet test --filter "Parse_OutputFlag OR Parse_LongOutputFlag" --verbosity quiet`
Expected: FAIL - `CurlOptions` has no `OutputFile` property

**Step 3: Add OutputFile property to CurlOptions**

In `CurlOptions.cs`, add:

```csharp
/// <summary>Output file path (-o, --output). If set, response body is saved to this file.</summary>
public string? OutputFile { get; set; }
```

**Step 4: Update parser to capture -o/--output**

In `CurlOptionParser.cs`:
- Add constant: `private const string LongOutput = "--output";`
- In `ParseLongOption` switch, add:

```csharp
case LongOutput:
  options.OutputFile = GetNextArg(tokens, i, token);
  return i + 2;
```

- In `ParseSingleShortOption`, change the existing `case 'o':` from skipping to:

```csharp
case 'o':
  options.OutputFile = GetNextArg(tokens, i, $"-{flag}");
  return i + 2;
```

**Step 5: Run tests to verify they pass**

Run: `dotnet test --filter "Parse_OutputFlag OR Parse_LongOutputFlag" --verbosity quiet`
Expected: PASS

**Step 6: Run all tests**

Run: `dotnet test --verbosity quiet`
Expected: All pass

**Step 7: Commit**

```
feat: add -o/--output flag parsing
```

---

### Task 4: Add -o output support (execution)

**Files:**
- Modify: `src/CurlCommandParser/CurlHttpClientExtensions.cs`
- Test: `tests/CurlCommandParser.Tests/IntegrationTests.cs`

**Step 1: Write failing test**

In `IntegrationTests.cs`, add a test that verifies `-o` saves the response to a file:

```csharp
[Fact]
public async Task ExecuteCurlAsync_OutputFlag_SavesResponseToFile()
{
  string tempFile = Path.Combine(Path.GetTempPath(), $"curl-test-{Guid.NewGuid()}.txt");
  try
  {
    FakeHandler handler = new(new HttpResponseMessage(HttpStatusCode.OK)
    {
      Content = new StringContent("response body content")
    });
    using HttpClient client = new(handler);
    HttpResponseMessage response = await client.ExecuteCurlAsync($"curl -o {tempFile} https://example.com");
    Assert.True(File.Exists(tempFile));
    Assert.Equal("response body content", File.ReadAllText(tempFile));
  }
  finally
  {
    if (File.Exists(tempFile)) File.Delete(tempFile);
  }
}
```

**Step 2: Run test to verify it fails**

Run: `dotnet test --filter "ExecuteCurlAsync_OutputFlag" --verbosity quiet`
Expected: FAIL - file is not created

**Step 3: Add output handling to ExecuteCurlAsync**

In `CurlHttpClientExtensions.cs`, modify `ExecuteCurlAsync`:

```csharp
public static async Task<HttpResponseMessage> ExecuteCurlAsync(
  this HttpClient httpClient,
  string curlCommand,
  CancellationToken cancellationToken = default)
{
  CurlOptions options = CurlOptionParser.Parse(curlCommand);
  HttpRequestMessage request = HttpRequestBuilder.Build(options);
  HttpClient client = ResolveClient(httpClient, options);

  using CancellationTokenSource? timeoutCts = CreateTimeoutCts(options, cancellationToken);
  CancellationToken token = timeoutCts?.Token ?? cancellationToken;

  HttpResponseMessage response = await client.SendAsync(request, token).ConfigureAwait(false);

  if (!string.IsNullOrEmpty(options.OutputFile))
  {
    await SaveResponseToFileAsync(response, options.OutputFile!, token).ConfigureAwait(false);
  }

  return response;
}

private static async Task SaveResponseToFileAsync(
  HttpResponseMessage response,
  string filePath,
  CancellationToken cancellationToken)
{
  byte[] content = await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
  string? directory = Path.GetDirectoryName(filePath);
  if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
  {
    Directory.CreateDirectory(directory);
  }
#if NET8_0_OR_GREATER
  await File.WriteAllBytesAsync(filePath, content, cancellationToken).ConfigureAwait(false);
#else
  File.WriteAllBytes(filePath, content);
#endif
}
```

**Step 4: Run test to verify it passes**

Run: `dotnet test --filter "ExecuteCurlAsync_OutputFlag" --verbosity quiet`
Expected: PASS

**Step 5: Run all tests**

Run: `dotnet test --verbosity quiet`
Expected: All pass

**Step 6: Commit**

```
feat: add -o/--output execution support - saves response to file
```

---

### Task 5: Rename to CurlCommand namespace and package

**Files:**
- Rename: `src/CurlCommandParser/` -> `src/CurlCommand/`
- Rename: `tests/CurlCommandParser.Tests/` -> `tests/CurlCommand.Tests/`
- Modify: All `.cs` files - namespace `CurlCommandParser` -> `CurlCommand`
- Modify: `.csproj` files - PackageId, RootNamespace, AssemblyName
- Modify: `InternalsVisibleTo`

This is a mechanical rename. Steps:

**Step 1: Rename project directories**

```bash
mv src/CurlCommandParser src/CurlCommand
mv tests/CurlCommandParser.Tests tests/CurlCommand.Tests
```

**Step 2: Update src .csproj**

In `src/CurlCommand/CurlCommand.csproj` (was CurlCommandParser.csproj):
- Rename file: `CurlCommandParser.csproj` -> `CurlCommand.csproj`
- `<PackageId>CurlCommand</PackageId>`
- `<RootNamespace>CurlCommand</RootNamespace>`
- `<InternalsVisibleTo Include="CurlCommand.Tests"/>`

**Step 3: Update test .csproj**

In `tests/CurlCommand.Tests/CurlCommand.Tests.csproj` (was CurlCommandParser.Tests.csproj):
- Rename file: `CurlCommandParser.Tests.csproj` -> `CurlCommand.Tests.csproj`
- Update ProjectReference path to `..\..\src\CurlCommand\CurlCommand.csproj`

**Step 4: Replace all namespaces in .cs files**

In ALL `.cs` files under `src/CurlCommand/` and `tests/CurlCommand.Tests/`:
- Replace `namespace CurlCommandParser` -> `namespace CurlCommand`
- Replace `using CurlCommandParser` -> `using CurlCommand`

**Step 5: Build and test**

Run: `dotnet build && dotnet test --verbosity quiet`
Expected: All pass

**Step 6: Commit**

```
refactor: rename library from CurlCommandParser to CurlCommand
```

---

### Task 6: Add CurlCommand.Parse() public API

**Files:**
- Create: `src/CurlCommand/CurlCommand.cs`
- Modify: `src/CurlCommand/CurlHttpClientExtensions.cs`
- Test: `tests/CurlCommand.Tests/CurlCommandTests.cs`

**Step 1: Write failing tests**

Create `tests/CurlCommand.Tests/CurlCommandTests.cs`:

```csharp
using CurlCommand.Models;

namespace CurlCommand.Tests;

public class CurlCommandTests
{
  [Fact]
  public void Parse_ReturnsCommandWithOptions()
  {
    Command cmd = Command.Parse("curl -X POST https://api.example.com -d '{}'");
    Assert.Equal("https://api.example.com", cmd.Options.Url);
    Assert.Equal("POST", cmd.Options.Method);
    Assert.Equal("{}", cmd.Options.DataBody);
  }

  [Fact]
  public void Parse_WithoutCurlPrefix_Works()
  {
    Command cmd = Command.Parse("-X GET https://example.com");
    Assert.Equal("https://example.com", cmd.Options.Url);
  }

  [Fact]
  public void Options_IsReadOnly()
  {
    Command cmd = Command.Parse("curl https://example.com");
    Assert.NotNull(cmd.Options);
    Assert.Equal("https://example.com", cmd.Options.Url);
  }
}
```

The class is called `Command` - clean and unambiguous within the `CurlCommand` namespace. Fully qualified: `CurlCommand.Command`.

**Step 2: Run tests to verify they fail**

Run: `dotnet test --filter "CurlCommandTests" --verbosity quiet`
Expected: FAIL - `Command` doesn't exist

**Step 3: Create Command class**

Create `src/CurlCommand/Command.cs`:

```csharp
using CurlCommand.Models;
using CurlCommand.Parsing;

namespace CurlCommand;

/// <summary>
/// Represents a parsed curl command. Use <see cref="Parse"/> to create from a curl string.
/// </summary>
public class Command
{
  /// <summary>The parsed options from the curl command.</summary>
  public CurlOptions Options { get; }

  private Command(CurlOptions options)
  {
    Options = options;
  }

  /// <summary>
  /// Parses a curl command string into a Command instance.
  /// The "curl" prefix is optional.
  /// </summary>
  public static Command Parse(string curlCommand)
  {
    CurlOptions options = CurlOptionParser.Parse(curlCommand);
    return new Command(options);
  }
}
```

**Step 4: Add ExecuteCurlAsync overload for Command**

In `CurlHttpClientExtensions.cs`, add an overload:

```csharp
/// <summary>
/// Executes a pre-parsed Command, returning the HttpResponseMessage.
/// </summary>
public static async Task<HttpResponseMessage> ExecuteCurlAsync(
  this HttpClient httpClient,
  Command command,
  CancellationToken cancellationToken = default)
{
  HttpRequestMessage request = HttpRequestBuilder.Build(command.Options);
  HttpClient client = ResolveClient(httpClient, command.Options);

  using CancellationTokenSource? timeoutCts = CreateTimeoutCts(command.Options, cancellationToken);
  CancellationToken token = timeoutCts?.Token ?? cancellationToken;

  HttpResponseMessage response = await client.SendAsync(request, token).ConfigureAwait(false);

  if (!string.IsNullOrEmpty(command.Options.OutputFile))
  {
    await SaveResponseToFileAsync(response, command.Options.OutputFile!, token).ConfigureAwait(false);
  }

  return response;
}
```

**Step 5: Write test for ExecuteCurlAsync with Command**

In `CurlCommandTests.cs`, add:

```csharp
[Fact]
public async Task ExecuteCurlAsync_WithCommand_Works()
{
  Command cmd = Command.Parse("curl -X POST https://example.com -d 'test'");
  FakeHandler handler = new(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
  using HttpClient client = new(handler);
  HttpResponseMessage response = await client.ExecuteCurlAsync(cmd);
  Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
}
```

**Step 6: Run tests to verify they pass**

Run: `dotnet test --verbosity quiet`
Expected: All pass

**Step 7: Commit**

```
feat: add Command.Parse() public API and ExecuteCurlAsync overload
```

---

### Task 7: Update README and package metadata

**Files:**
- Modify: `README.md`
- Modify: `src/CurlCommand/CurlCommand.csproj`

**Step 1: Update csproj metadata**

- `<PackageId>CurlCommand</PackageId>`
- `<Description>Parse and execute curl command strings at runtime via HttpClient. Supports headers, auth, form data, JSON, output files, timeouts, and more with zero external dependencies.</Description>`

**Step 2: Update README**

- Update package name references
- Add `Command.Parse()` usage example
- Add `--json` usage example
- Add `-o` usage example

**Step 3: Build and run all tests**

Run: `dotnet build && dotnet test --verbosity quiet`
Expected: All pass

**Step 4: Commit**

```
docs: update README and package metadata for CurlCommand rename
```
