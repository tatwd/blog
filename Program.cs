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




var razorEngine = new RazorEngine();
var templateContent = File.ReadAllText(themePostTemplateFilePath);

var pipline = new MarkdownPipelineBuilder()
	// .UseAdvancedExtensions()
	.UseAutoIdentifiers()
	.UseYamlFrontMatter()
	// .UsePrism()
	.Build();

var filePaths = Directory.GetFiles(postDir, "*", SearchOption.AllDirectories);

foreach (var path in filePaths) 
{
	var mdText = File.ReadAllText(path);

	var newPath = path.Replace(postDir, distDir + "/posts");

	var newDir = Path.GetDirectoryName(newPath);
	var fileNameWithoutExt = Path.GetFileNameWithoutExtension(newPath);

	if (string.IsNullOrEmpty(newDir))
		continue;

	if (!Directory.Exists(newDir))
		Directory.CreateDirectory(newDir);

	if (!newPath.EndsWith(".md"))
	{
		File.Copy(path, newPath, true);
		continue;
	}

	var html = Markdown.ToHtml(mdText, pipline);

	newPath = newPath.Replace(".md", ".html");
	var fileName = Path.GetFileName(newPath);

	var postViewModel = new
	{
		PostTitle = fileNameWithoutExt,
		PostContent = html,
		PostUrl = newPath.Replace(distDir, "").Replace(fileName, "")
	};


	var template = razorEngine.Compile(templateContent);
	var result = template.Run(postViewModel);


	using StreamWriter sw = File.CreateText(newPath);
	sw.Write(result);
}



