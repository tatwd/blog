using System.IO;
using Xunit;

namespace MyBlog.Tests;

public class BlogTest
{
    [Fact]
    public void Build_Test()
    {
        var blog = new Blog(new BlogConfig
        {
            BlogDirectory = "./"
        });
        Assert.NotNull(blog);
        var ok = blog.Build("./dist");
        Assert.True(ok);
    }
}
