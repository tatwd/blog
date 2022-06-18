namespace MyBlog;

public class BlogConfig
{
    public string Title { get; set; } = null!;
    public string Author { get; set; } = null!;
    public string Description { get; set; } = null!;
    public string BlogLink { get; set; } = null!;
    public MyLink[] Links { get; set; } = Array.Empty<MyLink>();
    public string Email { get; set; } = null!;

    /// <summary>
    /// Blog root directory.
    /// </summary>
    public string BlogDirectory { get; set; } = null!;

    /// <summary>
    /// The directory of `posts`
    /// </summary>
    public string? PostsDirectory { get; set; }

    /// <summary>
    /// The directory of `theme`
    /// </summary>
    public string ThemeDirectory { get; set; } = null!;
}


public class MyLink
{
    public string Title { get; set; } = null!;
    public string Url { get; set; } = null!;
}

