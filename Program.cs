using Markdig;
using Markdig.Parsers;
using Markdig.Renderers;
// using Markdig.Prism;
using Markdig.Syntax;
using RazorEngineCore;

var blogTitle = "My Blog";

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
    // .UsePrism()
    .Build();


// Generate post pages
var posts = new List<PostViewModel>(16);
var themePostTemplateFilePath = $"{themeTemplateDir}/post.cshtml";
var themePostTemplateContent = File.ReadAllText(themePostTemplateFilePath);
var postFiles = Directory.GetFiles(postDir, "*", SearchOption.AllDirectories);
foreach (var path in postFiles.AsParallel())
{
    var newPath = path.Replace(postDir,  $"{distDir}/posts");
    var postPageDir = Path.GetDirectoryName(newPath)!;


    if (!Directory.Exists(postPageDir))
        Directory.CreateDirectory(postPageDir);

    // Copy others files to dist, just like images
    if (!newPath.EndsWith(".md"))
    {
        File.Copy(path, newPath, true);
        Console.WriteLine("Generated: {0} (copyed)", newPath.Replace(distDir, ""));
        continue;
    }

    var htmlFile = newPath.Replace(".md", ".html");
    var htmlFileName = Path.GetFileName(htmlFile);
    var postRoute = Path.GetDirectoryName(htmlFile.Replace(distDir, ""))!;

    using var writer = new StringWriter();
    var renderer = new HtmlRenderer(writer)
    {
        LinkRewriter = (link) =>
        {
            if (link.StartsWith("./"))
                return postRoute + link.Substring(1);
            if (!link.StartsWith("/"))
                return  postRoute + "/" + link;
            return link;
        }
    };
    pipeline.Setup(renderer);
    var mdText = File.ReadAllText(path);
    var document = MarkdownParser.Parse(mdText, pipeline);
    renderer.Render(document);
    writer.Flush();
    var html = writer.ToString();
    // var html = Markdown.ToHtml(mdText, pipline);

    var fileNameWithoutExt = Path.GetFileNameWithoutExtension(newPath);
    var parentDir = Directory.GetParent(newPath)!;
    var isPostPageDir = parentDir.FullName.EndsWith("/posts");
    var postTitle = isPostPageDir ? fileNameWithoutExt : parentDir.Name;
    var newPostRoute = isPostPageDir ? postRoute + "/" + htmlFileName : postRoute;

    var postViewModel = new PostViewModel
    {
        PostTitle = postTitle,
        PostContent = html,
        PostRoute = newPostRoute
    };
    posts.Add(postViewModel);
    var template = razorEngine.Compile(themePostTemplateContent);
    var result = template.Run(postViewModel);

    using StreamWriter swPost = File.CreateText(htmlFile);
    await swPost.WriteAsync(result);

    Console.WriteLine("Generated: {0}/{1}", postRoute, htmlFileName);
}


// Generate index.html
await RenderRazorPageAsync($"{themeTemplateDir}/index.cshtml",
    $"{distDir}/index.html", new
    {
        BlogTitle = blogTitle,
        Posts = posts
    });
Console.WriteLine("Generated: /index.html");

// Generate 404.html
await RenderRazorPageAsync($"{themeTemplateDir}/404.cshtml", $"{distDir}/404.html");
Console.WriteLine("Generated: /404.html");


// Copy other files in theme directory
var otherThemeFiles = Directory.GetFiles(themeDir, "*", SearchOption.AllDirectories);
foreach (var path in otherThemeFiles.AsParallel())
{
    // Do not copy any files in templates dir
    if (path.StartsWith(themeTemplateDir))
        continue;

    var newPath = path.Replace(themeDir, distDir);
    var fileDir = Path.GetDirectoryName(newPath)!;

    if (!Directory.Exists(fileDir))
        Directory.CreateDirectory(fileDir);

    File.Copy(path, newPath, overwrite : true);
    Console.WriteLine("Generated: {0} (copyed)", newPath.Replace(distDir, ""));
}


async Task RenderRazorPageAsync(string templatePath, string distPath, object? model = null)
{
    var templateContent = File.ReadAllText(templatePath);
    var template = razorEngine.Compile(templateContent);
    var html = template.Run(model);
    using StreamWriter sw = File.CreateText(distPath);
    await sw.WriteAsync(html);
}

public class PostViewModel
{
    public string PostTitle { get; set; } = null!;
    public string PostContent { get; set; } = null!;
    public string PostRoute { get; set; } = null!;
}
