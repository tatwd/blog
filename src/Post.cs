namespace MyBlog;

// TODO: It will be replaced by `Post` class
public class PostViewModel
{
    public BlogConfig BlogConfig { get; set; } = null!;
    public string Content { get; set; } = null!;
    public int TimeToRead { get; set; }
    public string AbstractText { get; set; } = null!;
    public string PostRoute { get; set; } = null!;
    public PostFrontMatter FrontMatter { get; set; } = null!;
    public string Title => FrontMatter.Title;
}


public class Post
{
    public string Title { get; set; } = null!;
    public string Pathname { get; set; } = null!;
    public string HtmlContent { get; set; } = null!;
    public string TemplateName { get; set; } = null!;
}
