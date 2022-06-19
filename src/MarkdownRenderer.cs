using Markdig;
using Markdig.Extensions.Yaml;
using Markdig.Parsers;
using Markdig.Renderers;
using Markdig.Syntax;
using YamlDotNet.Serialization;

namespace MyBlog;

public class MarkdownRenderer
{
    private readonly MarkdownPipeline _markdownPipeline;

    private readonly IDeserializer _yamlDeserializer;

    public MarkdownRenderer()
    {
        _markdownPipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .UseYamlFrontMatter()
            .UsePreciseSourceLocation()
            .UseSoftlineBreakAsHardlineBreak()
            .Use<MyPrismExtension>()
            .Build();

        _yamlDeserializer = new DeserializerBuilder()
            .IgnoreUnmatchedProperties()
            .Build();
    }


    public (string html, PostFrontMatterViewModel frontMatter) Render(string markdownText, string pathname)
    {
        using var writer = new StringWriter();

        var renderer = new HtmlRenderer(writer);
        _markdownPipeline.Setup(renderer);

        var document = MarkdownParser.Parse(markdownText, _markdownPipeline);
        var frontMatter = GetPostFrontMatter(document)!;

        // var usePathname = frontMatter.Pathname ?? pathname;

        renderer.LinkRewriter = link =>
        {
            // if (link.StartsWith("./"))
            //     return usePathname + link.Substring(1);
            return link;
        };

        renderer.Render(document);
        writer.Flush();
        var html = writer.ToString();

        return (html, frontMatter);
    }


    PostFrontMatterViewModel GetPostFrontMatter(MarkdownDocument document)
    {
        var block = document
            .Descendants<YamlFrontMatterBlock>()
            .FirstOrDefault();

        if (block is null)
            throw new ArgumentNullException(nameof(block), "Post must have a front matter!");

        var yaml =
            block
                // this is not a mistake
                // we have to call .Lines 2x
                .Lines // StringLineGroup[]
                .Lines // StringLine[]
                .OrderByDescending(x => x.Line)
                .Select(x => $"{x}\n")
                .ToList()
                .Select(x => x.Replace("---", string.Empty))
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Aggregate((s, agg) => agg + s);

        var frontMatter = _yamlDeserializer.Deserialize<PostFrontMatterViewModel>(yaml)!;

        if (string.IsNullOrEmpty(frontMatter.Title))
            throw new ArgumentNullException(nameof(frontMatter.Title),
                "Post `title` is required in front matter!");
        if (frontMatter.CreateTime == default)
            throw new ArgumentNullException(nameof(frontMatter.CreateTime),
                "Post `create_time` is required in front matter!");

        frontMatter.Tags = frontMatter.Tags ?? Array.Empty<string>();

        return frontMatter;
    }
}
