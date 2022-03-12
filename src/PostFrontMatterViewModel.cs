using YamlDotNet.Serialization;

namespace MyBlog;

public class PostFrontMatterViewModel
{
    [YamlMember(Alias = "title")]
    public string Title { get; set; } = null!;

    [YamlMember(Alias = "create_time")]
    public DateTime CreateTime { get; set; }

    [YamlMember(Alias = "tags")]
    public string[] Tags { get; set; } = Array.Empty<string>();
}
