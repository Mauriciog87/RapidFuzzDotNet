using RapidFuzz;

namespace RapidFuzzDotNet.Tests;

public sealed class UtilsTests
{
    [Theory]
    [InlineData("This is a WORD!!!", "this is a word")]
    [InlineData("  Rapid---Fuzz  ", "rapid fuzz")]
    [InlineData("", "")]
    public void DefaultProcessNormalizesText(string input, string expected)
    {
        Assert.Equal(expected, Utils.DefaultProcess(input));
    }
}
