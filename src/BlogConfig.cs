namespace MyBlog;

public class BlogConfig
{
    public string Title { get; set; } = null!;
    public string Author { get; set; } = null!;
    public string Description { get; set; } = null!;
    public string BlogLink { get; set; } = null!;
    public MyLink[] Links { get; set; } = Array.Empty<MyLink>();
    public string Email { get; set; } = null!;
}


public class MyLink
{
    public string Title { get; set; } = null!;
    public string Url { get; set; } = null!;
}

