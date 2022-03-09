using Xunit;

namespace MyBlog.Tests;

public class UtilTest
{
    [Theory]
    [InlineData("hell world", "hell_world")]
    [InlineData("foo  ", "foo")]
    [InlineData("你好，世界  ", "你好，世界")]
    public void ReplaceWithspaceByLodash_ok(string input, string expected)
    {
        var actual = Util.ReplaceWithspaceByLodash(input);
        Assert.Equal(expected, actual);
    }
}
