using System;
using System.Collections.Generic;
using RazorEngineCore;


namespace MyBlog;

public class RazorRenderer
{
    private readonly IDictionary<string, MyCompiledTemplate> _compiledTemplateMap;

    public RazorRenderer(string templatesDirectory)
    {
        _compiledTemplateMap = LoadCompiledTemplates(templatesDirectory);
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


    public Task<string> RenderPostPageAsync(Post post, BlogConfig blogConfig)
    {
        return RenderRazorPageAsync(post.TemplateName, new { Post = post, BlogConfig = blogConfig });
    }

    public Task<string> RenderRazorPageAsync<T>(string templateName, T model)
    {
        var compiledTemplate = _compiledTemplateMap[templateName];
        var result = compiledTemplate.Run(model);
        return Task.FromResult(result);
    }

}

