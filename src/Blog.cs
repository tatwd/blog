namespace MyBlog;

/// <summary>
/// <example>
///
///     var blog = new Blog(settings: new BlogConfig {
///         Title = "",
///         Output = "./dist"
///     });
///     if (env.IsDev) {
///         blog.Serve(port: 8080); // start a http server
///     }
///     blog.Build(outputDirectory: "./dist"); // build to ./dist folder
///
/// </example>
/// </summary>
public class Blog
{
    private readonly BlogConfig _blogConfig;
    private readonly MarkdownRenderer _markdownRenderer;

    public Blog(BlogConfig blogConfig)
    {
        if (string.IsNullOrEmpty(blogConfig.BlogDirectory))
            throw new ArgumentNullException(nameof(blogConfig.BlogDirectory));

        _blogConfig = blogConfig;
        _markdownRenderer = new MarkdownRenderer();
    }

    public bool Build(string outputDirectory)
    {
        try
        {
            var posts = LoadPosts();
            var outputPath = Path.GetFullPath(outputDirectory)!;
            Util.CreateDirIfNotExists(outputPath);
            return RenderPages(outputPath, posts);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return false;
        }
    }

    private bool RenderPages(string outputPath, IEnumerable<Post> posts)
    {
        foreach (var post in posts)
        {
        }

        return false;
    }

    private IEnumerable<Post> LoadPosts()
    {
        var postsDirectory = Path.GetFullPath(_blogConfig.PostsDirectory ?? Path.Join(_blogConfig.BlogDirectory, "posts"));
        return Directory.GetFiles(postsDirectory, "*", SearchOption.AllDirectories)
            .Where(path => path.EndsWith(".md"))
            .Select(path => LoadPost(postsDirectory, path));
    }

    private Post LoadPost(string postDirectory, string path)
    {
        var contents = File.ReadAllText(path);
        var pathname = path.Replace(postDirectory, "").Replace(".md", "");

        var (html, frontMatter) = _markdownRenderer.Render(contents, pathname);

        return new Post
        {
            Title = frontMatter.Title,
            Pathname = pathname,
            HtmlContent = html
        };
    }



    /// <summary>
    /// Start a http server on port
    /// </summary>
    /// <param name="port"></param>
    public void Serve(int port)
    {
        // TODO: start a server ond watch files changes (HMR)
        throw new NotImplementedException();
    }

}
