using YamlDotNet.Serialization;

namespace MyBlog;

public class PostFrontMatter
{
    [YamlMember(Alias = "title")]
    public string Title { get; set; } = null!;

    [YamlMember(Alias = "create_time")]
    public DateTime CreateTime { get; set; }
    [YamlMember(Alias = "update_time")]
    public DateTime? UpdateTime { get; set; }
    [YamlMember(Alias = "tags")]
    public string[] Tags { get; set; } = Array.Empty<string>();

    [YamlMember(Alias = "template")]
    public string? TemplateName { get; set; }

    [YamlMember(Alias = "draft")]
    public bool Draft { get; set; }

    [YamlMember(Alias = "duration")]
    public string? Duration { get; set; }

    [YamlMember(Alias = "description")]
    public string? Description { get; set; }

    [YamlMember(Alias = "lang")]
    public string Lang { get; set; } = "zh-CN";

    // [YamlMember(Alias = "pathname")]
    // public string? Pathname { get; set; }

}
