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
