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
