namespace MyBlog;

public class PostViewModel
{
    public BlogConfig BlogConfig { get; set; } = null!;
    public string PostContent { get; set; } = null!;
    public int TimeToRead { get; set; }
    public string AbstractText { get; set; } = null!;
    public string PostRoute { get; set; } = null!;
    public PostFrontMatterViewModel FrontMatter { get; set; } = null!;
    public string PostTitle => FrontMatter.Title;
}


public class Post
{
    public string Title { get; set; } = null!;
    public string Pathname { get; set; } = null!;
    public string HtmlContent { get; set; } = null!;
}
