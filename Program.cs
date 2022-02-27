using Markdig;
using Markdig.Extensions.Yaml;
using Markdig.Parsers;
using Markdig.Renderers;
using Markdig.Syntax;
using MyBlog;
using RazorEngineCore;
using YamlDotNet.Serialization;

// My blog global config
var blogConfig = new BlogConfig
{
    Title = "_king's Notes",
    Author = "_king",
    Description = "万古长空，一朝风月。"
};

var cwd = Directory.GetCurrentDirectory();
var distDir = $"{cwd}/dist";
var postDir = $"{cwd}/posts";
var themeDir = $"{cwd}/theme";
var themeStyleDir = $"{themeDir}/styles";
var themeTemplateDir = $"{themeDir}/templates";

if (Directory.Exists(distDir))
    Directory.Delete(distDir, true);

var razorEngine = new RazorEngine();
var pipeline = new MarkdownPipelineBuilder()
    .UseAdvancedExtensions()
    // .UseFigures()
    .UseYamlFrontMatter()
    .Build();
var yamlDeserializer = new DeserializerBuilder()
    .IgnoreUnmatchedProperties()
    .Build();

// Generate post pages
var posts = new List<PostViewModel>(16);
var themePostTemplateFilePath = $"{themeTemplateDir}/post.cshtml";
var themePostTemplateContent = File.ReadAllText(themePostTemplateFilePath);

// All posts use a same razor template
var themePostTemplate = await razorEngine.CompileAsync(themePostTemplateContent, option =>
{
    option.AddAssemblyReference(typeof(Util));
});

var postFiles = Directory.GetFiles(postDir, "*", SearchOption.AllDirectories);
foreach (var path in postFiles.AsParallel())
{
    var newPath = path.Replace(postDir, $"{distDir}/posts");
    var postPageDir = Path.GetDirectoryName(newPath) !;

    if (!Directory.Exists(postPageDir))
        Directory.CreateDirectory(postPageDir);

    // Copy other files to dist, just like images etc.
    if (!newPath.EndsWith(".md"))
    {
        File.Copy(path, newPath, true);
        Console.WriteLine("Generated: {0} (copyed)", newPath.Replace(distDir, ""));
        continue;
    }

    var htmlFile = newPath.Replace(".md", ".html");
    var htmlFileName = Path.GetFileName(htmlFile);
    var postRoute = Path.GetDirectoryName(htmlFile.Replace(distDir, "")) !;

    using var writer = new StringWriter();
    var renderer = new HtmlRenderer(writer)
    {
        LinkRewriter = (link) =>
        {
            if (link.StartsWith("./"))
                return postRoute + link.Substring(1);
            //if (!link.StartsWith("/"))
            //    return  postRoute + "/" + link;
            return link;
        }
    };
    pipeline.Setup(renderer);
    var mdText = File.ReadAllText(path);
    var document = MarkdownParser.Parse(mdText, pipeline);
    var postFrontMatter = GetPostFrontMatter(document);
    renderer.Render(document);
    writer.Flush();
    var html = writer.ToString();
    // var html = Markdown.ToHtml(mdText, pipline);
    var plainText = Markdown.ToPlainText(mdText, pipeline);
    var timeToRead = Util.CalcTimeToRead(plainText);
    var abstractText = plainText.Substring(0, Math.Min(plainText.Length, 140)).Replace("\n", " ");

    var fileNameWithoutExt = Path.GetFileNameWithoutExtension(newPath);
    var parentDir = Directory.GetParent(newPath) !;
    var isPostPageDir = parentDir.FullName.EndsWith("/posts");
    var newPostRoute = isPostPageDir ? postRoute + "/" + htmlFileName : postRoute;

    var postViewModel = new PostViewModel
    {
        BlogConfig = blogConfig,
        PostContent = html,
        TimeToRead = timeToRead,
        AbstractText = abstractText,
        PostRoute = newPostRoute,
        FrontMatter = postFrontMatter
    };
    posts.Add(postViewModel);

    // Console.WriteLine("RazorCompile: {0}/{1}", postRoute, htmlFileName);
    var result = themePostTemplate.Run(postViewModel);

    using StreamWriter swPost = File.CreateText(htmlFile);
    await swPost.WriteAsync(result);

    Console.WriteLine("Generated: {0}/{1}", postRoute, htmlFileName);
}

// Generate index.html
var homeViewModel = new
{
    BlogConfig = blogConfig,
    Posts = posts.OrderByDescending(p => p.FrontMatter.CreateTime)
};
var themeHomeTemplateContent = File.ReadAllText($"{themeTemplateDir}/index.cshtml");
var themeHomeTemplate = await razorEngine.CompileAsync(themeHomeTemplateContent, option =>
{
    option.AddAssemblyReference(typeof(Util));
});
await SaveRenderedRazorPageAsync(themeHomeTemplate, $"{distDir}/index.html", homeViewModel);
Console.WriteLine("Generated: /index.html");

// Generate 404.html
var theme404TemplateContent = File.ReadAllText($"{themeTemplateDir}/404.cshtml");
var theme404Template = await razorEngine.CompileAsync(theme404TemplateContent);
await SaveRenderedRazorPageAsync(theme404Template, $"{distDir}/404.html");
Console.WriteLine("Generated: /404.html");

// Map all posts with same tag
var mapTags = new Dictionary<string, IList<PostViewModel>>();
foreach (var post in posts)
{
    if (post.FrontMatter.Tags is null)
        continue;

    foreach (var tagName in post.FrontMatter.Tags)
    {
        if (!mapTags.ContainsKey(tagName))
            mapTags[tagName] = new List<PostViewModel> { post };
        else
            mapTags[tagName].Add(post);
    }
}

// Generate tag pages
var themeTagTemplateContent = File.ReadAllText($"{themeTemplateDir}/tag.cshtml");
var themeTagTemplate = await razorEngine.CompileAsync(themeTagTemplateContent, option =>
{
    option.AddAssemblyReference(typeof(Util));
});
foreach (var(tagName, postsWithSameTag) in mapTags)
{
    var model = new
    {
        BlogConfig = blogConfig,
        TagName = tagName,
        Posts = postsWithSameTag.OrderByDescending(p => p.FrontMatter.CreateTime)
    };
    var newTagRoute = $"/tags/{Util.ReplaceWithspaceByLodash(tagName)}/index.html";
    await SaveRenderedRazorPageAsync(themeTagTemplate, $"{distDir}{newTagRoute}", model);
    Console.WriteLine("Generated: {0}", newTagRoute);
}

// Copy other files in theme directory
var otherThemeFiles = Directory.GetFiles(themeDir, "*", SearchOption.AllDirectories);
foreach (var path in otherThemeFiles.AsParallel())
{
    // Do not copy any files in templates dir
    if (path.StartsWith(themeTemplateDir))
        continue;

    var newPath = path.Replace(themeDir, distDir);
    var fileDir = Path.GetDirectoryName(newPath) !;

    if (!Directory.Exists(fileDir))
        Directory.CreateDirectory(fileDir);

    File.Copy(path, newPath, overwrite : true);
    Console.WriteLine("Generated: {0} (copyed)", newPath.Replace(distDir, ""));
}

async Task SaveRenderedRazorPageAsync(IRazorEngineCompiledTemplate template, string distPath, object? model = null)
{
    var dir = Path.GetDirectoryName(distPath) !;
    if (!Directory.Exists(dir))
        Directory.CreateDirectory(dir);

    var html = template.Run(model);
    using StreamWriter sw = File.CreateText(distPath);
    await sw.WriteAsync(html);
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

    var frontMatter = yamlDeserializer.Deserialize<PostFrontMatterViewModel>(yaml)!;

    if (string.IsNullOrEmpty(frontMatter.Title))
        throw new ArgumentNullException(nameof(frontMatter.Title),
            "Post `title` is required in front matter!");
    if (frontMatter.CreateTime == default)
        throw new ArgumentNullException(nameof(frontMatter.CreateTime),
            "Post `create_time` is required in front matter!");

    frontMatter.Tags = frontMatter.Tags ?? Array.Empty<string>();

    return frontMatter;
}

public class BlogConfig
{
    public string Title { get; set; } = null!;
    public string Author { get; set; } = null!;
    public string Description { get; set; } = null!;
}

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

public class PostFrontMatterViewModel
{
    [YamlMember(Alias = "title")]
    public string Title { get; set; } = null!;

    [YamlMember(Alias = "create_time")]
    public DateTime CreateTime { get; set; }

    [YamlMember(Alias = "tags")]
    public string[] Tags { get; set; } = Array.Empty<string>();
}
