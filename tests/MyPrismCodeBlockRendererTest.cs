using Markdig;
using Xunit;

namespace MyBlog.Tests;

public class MyPrismCodeBlockRendererTest
{
    private MarkdownPipeline CreateMarkdownPipeline()
    {
        return new MarkdownPipelineBuilder()
            .Use<MyPrismExtension>()
            .Build();
    }

    [Fact]
    public void render_html_code_ok()
    {
        var mdText = @"
```html
<h1>Hello world</h1>
```";
        var pipeline = CreateMarkdownPipeline();
        var html = Markdown.ToHtml(mdText, pipeline);
        Assert.Equal(@"<pre class=""language-html""><code class=""language-html"">&lt;h1&gt;Hello world&lt;/h1&gt;</code></pre>", html);
    }
}
