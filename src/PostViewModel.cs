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
