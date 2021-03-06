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
    public void render_code_without_lang_ok()
    {
        var mdText = @"
```
Hello world
```";
        var pipeline = CreateMarkdownPipeline();
        var html = Markdown.ToHtml(mdText, pipeline);
        Assert.Equal(@"<pre class=""language-plaintext""><code class=""language-plaintext"">Hello world</code></pre>", html);
    }

    [Fact]
    public void render_html_code_ok()
    {
        var mdText = @"
```html
<h1>Hello & world</h1>
```";
        var pipeline = CreateMarkdownPipeline();
        var html = Markdown.ToHtml(mdText, pipeline);
        Assert.Contains("&lt;", html);
        Assert.Contains("Hello &amp; world", html);
    }
}
