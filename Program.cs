using Markdig;
// using Markdig.Prism;
using Markdig.Syntax;
using RazorEngineCore;

var cwd = Directory.GetCurrentDirectory();
var distDir = $"{cwd}/dist";
var postDir = $"{cwd}/posts";
var styleDir = $"{cwd}/theme/styles";
var postTemplateFilePath = $"{cwd}/theme/templates/post.cshtml";
// var layoutTemplateFilePath = "./theme/templates/_layout.cshtml";

var razorEngine = new RazorEngine();
// IDictionary<string, string>  parts = new Dictionary<string, string>()
// {
// 	{"MyLayout", File.ReadAllText(layoutTemplateFilePath)},
// };
var templateContent = File.ReadAllText(postTemplateFilePath);

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
	var filenameWithoutExt = Path.GetFileNameWithoutExtension(newPath);

	if (string.IsNullOrEmpty(newDir))
		continue;

	if (!Directory.Exists(newDir))
		Directory.CreateDirectory(newDir);

	if (!newPath.EndsWith(".md"))
	{
		File.Copy(path, newPath, true);
		continue;
	}

	newPath = newPath.Replace(".md", ".html");


	// Console.WriteLine(newPath);
	var html = Markdown.ToHtml(mdText, pipline);

	var template = razorEngine.Compile(templateContent);
	// await template.SaveToFileAsync(Path.Combine(newPath, filenameWithoutExt + ".html"));
	var result = template.Run(new
	{
		PostTitle = filenameWithoutExt,
		PostContent = html,
		PostUrl = newPath.Replace(distDir, "")
	});

	using StreamWriter sw = File.CreateText(newPath);

	sw.Write(result);
}


foreach (var path in Directory.GetFiles(styleDir, "*.css", SearchOption.AllDirectories)) 
{
	var newPath = path.Replace("./theme", distDir);
	var fileDir = Path.GetDirectoryName(newPath);

	if (string.IsNullOrEmpty(fileDir))
		continue;

	if (!Directory.Exists(fileDir))
		Directory.CreateDirectory(fileDir);

	File.Copy(path, newPath, overwrite: true);
}

