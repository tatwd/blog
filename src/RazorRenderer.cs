using RazorEngineCore;

namespace MyBlog;

public class RazorRenderer
{
    private readonly IDictionary<string, MyCompiledTemplate> _compiledTemplateMap;
    private readonly BlogConfig _blogConfig;

    public RazorRenderer(string templatesDirectory,
        BlogConfig blogConfig)
    {
        _compiledTemplateMap = LoadCompiledTemplates(templatesDirectory);
        _blogConfig = blogConfig;
    }

    private IDictionary<string, MyCompiledTemplate> LoadCompiledTemplates(string templatesDirectory)
    {
        var razorEngine = new RazorEngine();
        var files = Directory.GetFiles(templatesDirectory, "*.cshtml", SearchOption.TopDirectoryOnly)
            .Select(path =>
            {
                var name = Path.GetFileNameWithoutExtension(path);
                return new { name, path, isPart = name.StartsWith("_") };
            });

        var outDict = new Dictionary<string, MyCompiledTemplate>();
        var parts = new Dictionary<string, IRazorEngineCompiledTemplate<MyTemplateBase>>();

        foreach (var file in files.Where(item => item.isPart))
        {
            var content = File.ReadAllText(file.path);
            parts[file.name] = razorEngine.Compile<MyTemplateBase>(content);
        }

        foreach (var file in files.Where(item => !item.isPart))
        {
            var content = File.ReadAllText(file.path);
            outDict[file.name] = razorEngine.Compile(content, parts, builder =>
            {
                builder.AddAssemblyReference(typeof(Util));
            });
        }

        return outDict;
    }

    public Task<string> RenderRazorPageAsync<T>(string templateName, T model)
    {
        var compiledTemplate = _compiledTemplateMap[templateName];
        var result = compiledTemplate.Run(new { TemplateName = templateName, PageData = model, BlogConfig = _blogConfig });
        return Task.FromResult(result);
    }

}

