using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace MyBlog.Tests;

public class BlogTest
{
    [Fact]
    public async Task Build_Test()
    {
        var blog = new Blog(new BlogConfig
        {
            BlogDirectory = "./"
        });
        Assert.NotNull(blog);
        await blog.BuildAsync("./dist");
        Assert.True(Directory.Exists("./dist"));
    }
}
