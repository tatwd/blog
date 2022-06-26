namespace MyBlog;

public class Post
{
    public string Title { get; set; } = null!;
    public string Pathname { get; set; } = null!;
    public string HtmlContent { get; set; } = null!;

    public string? TimeToRead { get; set; }
    public string AbstractText { get; set; } = null!;
    // public string PostRoute { get; set; } = null!;
    public DateTime CreateTime { get; set; }
    public string[] Tags { get; set; } = Array.Empty<string>();
    public string TemplateName { get; set; } = null!;
    public string Lang { get; set; } = null!;

}
