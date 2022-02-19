using Markdig;
// using Markdig.Prism;
using Markdig.Syntax;
using RazorEngineCore;

var cwd = Directory.GetCurrentDirectory();

var distDir = $"{cwd}/dist";
var postDir = $"{cwd}/posts";
var themeDir = $"{cwd}/theme";
var themeTemplateDir = $"{themeDir}/templates";
var themeStyleDir = $"{themeDir}/styles";
var themePostTemplateFilePath = $"{themeTemplateDir}/post.cshtml";
var themeHomeTemplateFilePath = $"{themeTemplateDir}/index.cshtml";


if (Directory.Exists(distDir))
    Directory.Delete(distDir, true);


var razorEngine = new RazorEngine();
var pipline = new MarkdownPipelineBuilder()
    .UseAdvancedExtensions()
    // .UseAutoIdentifiers()
    .UseYamlFrontMatter()
    // .UsePrism()
    .Build();


// Generate post pages
var posts = new List<PostViewModel>();
var themePostTemplateContent = File.ReadAllText(themePostTemplateFilePath);
foreach (var path in Directory.GetFiles(postDir, "*", SearchOption.AllDirectories))
{
    var mdText = File.ReadAllText(path);

    var newPath = path.Replace(postDir, distDir + "/posts");

    var newDir = Path.GetDirectoryName(newPath);
    var fileNameWithoutExt = Path.GetFileNameWithoutExtension(newPath);

    if (string.IsNullOrEmpty(newDir))
        continue;

    if (!Directory.Exists(newDir))
        Directory.CreateDirectory(newDir);

    // Copy others files to dist, just like images
    if (!newPath.EndsWith(".md"))
    {
        File.Copy(path, newPath, true);
        continue;
    }

    var html = Markdown.ToHtml(mdText, pipline);

    var postRoute = Path.GetDirectoryName(newPath.Replace(distDir, ""));
    var postViewModel = new PostViewModel
    {
        PostTitle = fileNameWithoutExt,
        PostContent = html,
        PostRoute = postRoute!
    };
    posts.Add(postViewModel);
    var template = razorEngine.Compile(themePostTemplateContent);
    var result = template.Run(postViewModel);

    using StreamWriter swPost = File.CreateText(newPath.Replace(".md", ".html"));
    await swPost.WriteAsync(result);
}
Console.WriteLine("Generate all post pages ok!");

// Generate index.html
var themeHomeTemplateContent = File.ReadAllText(themeHomeTemplateFilePath);
var homeTemplate = razorEngine.Compile(themeHomeTemplateContent);
var homeHtml = homeTemplate.Run(new { Posts = posts });
using StreamWriter swHome = File.CreateText(Path.Combine(distDir, "index.html"));
await swHome.WriteAsync(homeHtml);
Console.WriteLine("Generate home page ok!");


// Copy other static files
foreach (var path in Directory.GetFiles(themeStyleDir, "*", SearchOption.AllDirectories))
{
    if (path.StartsWith(themeTemplateDir))
        continue;

    var newPath = path.Replace(themeDir, distDir);
    var fileDir = Path.GetDirectoryName(newPath);

    if (string.IsNullOrEmpty(fileDir))
        continue;

    if (!Directory.Exists(fileDir))
        Directory.CreateDirectory(fileDir);

    File.Copy(path, newPath, overwrite : true);
}
Console.WriteLine("Generate other static files ok!");


public class PostViewModel
{
    public string PostTitle { get; set; } = null!;
    public string PostContent { get; set; } = null!;
    public string PostRoute { get; set; } = null!;
}
