using System.Diagnostics;
using Markdig;
using Markdig.Parsers;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using PrismSharp.Core;
using PrismSharp.Highlighting.HTML;

namespace MyBlog;

public class MyPrismCodeBlockRenderer : HtmlObjectRenderer<CodeBlock>
{
    private readonly CodeBlockRenderer _codeBlockRenderer;
    private readonly HtmlHighlighter _htmlHighlighter;

    public MyPrismCodeBlockRenderer()
    {
        _codeBlockRenderer = new CodeBlockRenderer();
        _htmlHighlighter = new HtmlHighlighter();
    }

    protected override void Write(HtmlRenderer renderer, CodeBlock node)
    {
        var fencedCodeBlock = node as FencedCodeBlock;
        var parser = node.Parser as FencedCodeBlockParser;
        if (fencedCodeBlock == null || parser == null)
        {
            _codeBlockRenderer.Write(renderer, node);
            return;
        }

        // var languageCode = fencedCodeBlock.Info.Replace(parser.InfoPrefix, string.Empty);
        var languageCode = fencedCodeBlock.Info;
        if (string.IsNullOrWhiteSpace(languageCode))
        {
            languageCode = "plaintext";
        }

        var attributes = new HtmlAttributes();
        attributes.AddClass($"language-{languageCode}");

        var code = HighlightCode(node, languageCode);

        renderer
            .Write("<pre")
            .WriteAttributes(attributes)
            .Write(">")
            .Write("<code")
            .WriteAttributes(attributes)
            .Write(">")
            .Write(code)
            .Write("</code>")
            .Write("</pre>");
    }

    private string HighlightCode(LeafBlock node, string language)
    {
        var grammar = LanguageGrammars.GetGrammar(language);
        return HighlightCode(node, grammar, language);
    }

    private string HighlightCode(LeafBlock node, Grammar grammar, string language)
    {
        var text = node.Lines.ToString();
#if DEBUG
        var sw = Stopwatch.StartNew();
#endif
        var html = _htmlHighlighter.Highlight(text, grammar, language);
#if DEBUG
        sw.Stop();
        Console.WriteLine($"DEBUG: highlighter.Highlight took {sw.ElapsedMilliseconds}ms for `{language}` code.");
#endif
        return html;
    }
}


public class MyPrismExtension : IMarkdownExtension
{
    public void Setup(MarkdownPipelineBuilder pipeline)
    {
    }

    public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
    {
        if (renderer == null)
            throw new ArgumentNullException(nameof(renderer));

        if (renderer is TextRendererBase<HtmlRenderer> htmlRenderer)
            htmlRenderer.ObjectRenderers.ReplaceOrAdd<CodeBlockRenderer>(new MyPrismCodeBlockRenderer());
    }
}
