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

    /// <summary>
    /// 使用的模板名称  默认 posts/ 目录下默认为 post
    /// </summary>
    [YamlMember(Alias = "template")]
    public string? TemplateName { get; set; }

    /// <summary>
    /// 是否是草稿 生产环境下不会编译输出草稿文件
    /// </summary>
    [YamlMember(Alias = "draft")]
    public bool Draft { get; set; }

    /// <summary>
    /// 阅读时长
    /// </summary>
    [YamlMember(Alias = "duration")]
    public string? Duration { get; set; }

    /// <summary>
    /// 文本内容摘要
    /// </summary>
    [YamlMember(Alias = "description")]
    public string? Description { get; set; }

    [YamlMember(Alias = "lang")]
    public string Lang { get; set; } = "zh-CN";

    /// <summary>
    /// 是否启用文章目录功能
    /// </summary>
    [YamlMember(Alias = "toc_enabled")]
    public bool EnabledToc { get; set; }

}
