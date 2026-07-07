using Glide.Engine;
using Xunit;

namespace Glide.Tests;

public class ExclusionCheckerTests
{
    [Theory]
    [InlineData("valorant.exe", "valorant.exe", true)]
    [InlineData("VALORANT.EXE", "valorant.exe", true)]
    [InlineData("valorant", "valorant.exe", true)]
    [InlineData("valorant.exe", "valorant", true)]
    [InlineData(@"C:\Games\valorant.exe", "valorant.exe", true)]
    [InlineData("chrome.exe", "valorant.exe", false)]
    public void MatchesNamesFlexibly(string foreground, string excluded, bool expected)
    {
        var result = ExclusionChecker.IsExcluded(foreground, [excluded]);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void NullOrEmptyProcessIsNeverExcluded()
    {
        Assert.False(ExclusionChecker.IsExcluded(null, ["a.exe"]));
        Assert.False(ExclusionChecker.IsExcluded("", ["a.exe"]));
        Assert.False(ExclusionChecker.IsExcluded("   ", ["a.exe"]));
    }

    [Fact]
    public void EmptyBlacklistExcludesNothing()
    {
        Assert.False(ExclusionChecker.IsExcluded("anything.exe", []));
    }
}
