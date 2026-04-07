using CurlCommands.Models;
using CurlCommands.Parsing;

namespace CurlCommands;

/// <summary>
/// Represents a parsed curl command. Use <see cref="Parse"/> to create from a curl string.
/// </summary>
public class CurlCommand
{
  /// <summary>The parsed options from the curl command.</summary>
  public CurlOptions Options { get; }

  private CurlCommand(CurlOptions options)
  {
    Options = options;
  }

  /// <summary>
  /// Parses a curl command string into a CurlCommand instance.
  /// The "curl" prefix is optional.
  /// </summary>
  public static CurlCommand Parse(string curlCommand)
  {
    CurlOptions options = CurlOptionParser.Parse(curlCommand);
    return new CurlCommand(options);
  }
}
