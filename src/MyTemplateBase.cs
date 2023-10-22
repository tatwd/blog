using RazorEngineCore;

namespace MyBlog;

public class MyTemplateBase : RazorEngineTemplateBase
{
    public Func<string, object?, string>? IncludeCallback { get; set; }
    public Func<string>? RenderBodyCallback { get; set; }
    public string? Layout { get; set; }

    public string? Include(string key, object? model = null)
    {
        return IncludeCallback?.Invoke(key, model);
    }

    public string? RenderBody()
    {
        return RenderBodyCallback?.Invoke();
    }
}


public static class RazorEngineCoreExtensions
{
    public static MyCompiledTemplate Compile(this RazorEngine razorEngine,
        string template, IDictionary<string, IRazorEngineCompiledTemplate<MyTemplateBase>> parts,
        Action<IRazorEngineCompilationOptionsBuilder>? builderAction = null)
    {
        return new MyCompiledTemplate(razorEngine.Compile<MyTemplateBase>(template, builderAction), parts);
    }
}


public class MyCompiledTemplate
{
    private readonly IRazorEngineCompiledTemplate<MyTemplateBase> _compiledTemplate;
    private readonly IDictionary<string, IRazorEngineCompiledTemplate<MyTemplateBase>> _compiledParts;

    public MyCompiledTemplate(IRazorEngineCompiledTemplate<MyTemplateBase> compiledTemplate,
        IDictionary<string, IRazorEngineCompiledTemplate<MyTemplateBase>> compiledParts)
    {
        _compiledTemplate = compiledTemplate;
        _compiledParts = compiledParts;
    }

    public string Run(object? model)
    {
        return Run(_compiledTemplate, model);
    }

    public string Run(IRazorEngineCompiledTemplate<MyTemplateBase> template, object? model)
    {
        MyTemplateBase? templateReference = null;

        var result = template.Run(instance =>
        {
            if (model is not AnonymousTypeWrapper)
            {
                model = new AnonymousTypeWrapper(model);
            }

            instance.Model = model;
            instance.IncludeCallback = (key, includeModel) => Run(_compiledParts[key], includeModel);

            templateReference = instance;
        });


        return result;
        // if (templateReference is null || templateReference.Layout is null)
        // {
        //     return result;
        // }

        // return _compiledParts[templateReference.Layout].Run(instance =>
        // {
        //     if (model is not AnonymousTypeWrapper)
        //     {
        //         model = new AnonymousTypeWrapper(model);
        //     }

        //     instance.Model = model;
        //     instance.IncludeCallback = (key, includeModel) => Run(_compiledParts[key], includeModel);
        //     instance.RenderBodyCallback = () => result;
        // });
    }
}
