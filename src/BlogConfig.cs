namespace MyBlog;

public class BlogConfig
{
    public string Title { get; set; } = null!;
    public string Author { get; set; } = null!;
    public string Description { get; set; } = null!;
    public string Link { get; set; } = null!;
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
