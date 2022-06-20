namespace MyBlog;

/// <summary>
/// <example>
///
///     var blog = new Blog(settings: new BlogConfig {
///         Title = "My Blog",
///         Author = "Foo",
///         Description = "bababaabba",
///     });
///     if (env.IsDev) {
///         blog.Serve(port: 8080); // start a http server
///     }
///     await blog.BuildAsync(outputDirectory: "./dist"); // build to ./dist folder
///
/// </example>
/// </summary>
public class Blog
{
    private readonly BlogConfig _blogConfig;
    private readonly MarkdownRenderer _markdownRenderer;
    private readonly RazorRenderer _razorRenderer;

    public Blog(BlogConfig blogConfig)
    {
        if (string.IsNullOrEmpty(blogConfig.BlogDirectory))
            throw new ArgumentNullException(nameof(blogConfig.BlogDirectory));

        blogConfig.PostsDirectory = blogConfig.PostsDirectory ?? $"{blogConfig.BlogDirectory}/posts";
        blogConfig.ThemeDirectory = blogConfig.ThemeDirectory ?? $"{blogConfig.BlogDirectory}/theme";

        _blogConfig = blogConfig;
        _markdownRenderer = new MarkdownRenderer();
        _razorRenderer = new RazorRenderer($"{_blogConfig.ThemeDirectory}/templates");
    }

    public Task BuildAsync(string outputDirectory)
    {
        var posts = LoadPosts();
        return RenderPagesAsync(outputDirectory!, posts);
    }

    private async Task RenderPagesAsync(string outputPath, IEnumerable<Post> posts)
    {
        Util.CreateDirIfNotExists(outputPath);

        foreach (var post in posts)
        {
            var html = await _razorRenderer.RenderPostPageAsync(post, _blogConfig);
            var distPath = $"{outputPath}/{post.Pathname}.html";
            Util.CreateDirIfNotExists(distPath);
            using StreamWriter sw = File.CreateText(distPath);
            await sw.WriteAsync(html);
        }
    }

    private IEnumerable<Post> LoadPosts()
    {
        var postsDirectory = _blogConfig.PostsDirectory!;
        return Directory.GetFiles(postsDirectory, "*.md", SearchOption.AllDirectories)
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
            HtmlContent = html,
            TemplateName = frontMatter.TemplateName ?? "post"
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
