using System;
using System.Collections.Generic;
using RazorEngineCore;


namespace MyBlog;

public class RazorRenderer
{
    private readonly IDictionary<string, IRazorEngineCompiledTemplate> _compiledTemplateMap;

    public RazorRenderer(string templatesDirectory)
    {
        _compiledTemplateMap = LoadCompiledTemplates(templatesDirectory);
    }

    private IDictionary<string, IRazorEngineCompiledTemplate> LoadCompiledTemplates(string templatesDirectory)
    {
        var razorEngine = new RazorEngine();
        var files = Directory.GetFiles(templatesDirectory, "*.cshtml", SearchOption.TopDirectoryOnly);
        return files.Select(path => new { name = Path.GetFileNameWithoutExtension(path), path })
            .Where(item => !item.name.StartsWith("_"))
            .Select(item => new { item.name, content =  File.ReadAllText(item.path) })
            .ToDictionary(
                item => item.name,
                item => razorEngine.Compile(item.content, option =>
                {
                    option.AddAssemblyReference(typeof(Util));
                }));
    }


    public Task<string> RenderPostPageAsync(Post post, BlogConfig blogConfig)
    {
        var compiledTemplate = _compiledTemplateMap[post.TemplateName];
        return compiledTemplate.RunAsync(new { Post = post, BlogConfig = blogConfig });
    }

}

