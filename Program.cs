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



var razorEngine = new RazorEngine();
var pipline = new MarkdownPipelineBuilder()
	// .UseAdvancedExtensions()
	.UseAutoIdentifiers()
	.UseYamlFrontMatter()
	// .UsePrism()
	.Build();


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

    var postUrl = Path.GetDirectoryName(newPath.Replace(distDir, ""));
	var postViewModel = new
	{
		PostTitle = fileNameWithoutExt,
		PostContent = html,
		PostUrl = postUrl
	};
	var template = razorEngine.Compile(themePostTemplateContent);
	var result = template.Run(postViewModel);


	using StreamWriter sw = File.CreateText(newPath.Replace(".md", ".html"));
	sw.Write(result);
}


// Copy static files
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

	File.Copy(path, newPath, overwrite: true);
}
