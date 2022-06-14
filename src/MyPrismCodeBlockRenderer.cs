using System.Diagnostics;
using System.Text;
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
    private static readonly IReadOnlyDictionary<string, Grammar> SupportedGrammarMap = new Dictionary<string, Grammar>
    {
        ["c"] = LanguageGrammars.C,
        ["cpp"] = LanguageGrammars.C,
        ["c++"] = LanguageGrammars.C,
        ["csharp"] = LanguageGrammars.CSharp,
        ["c#"] = LanguageGrammars.CSharp,
        ["cs"] = LanguageGrammars.CSharp,
        ["dotnet"] = LanguageGrammars.CSharp,
        ["js"] = LanguageGrammars.JavaScript,
        ["javascript"] = LanguageGrammars.JavaScript,
        ["html"] = LanguageGrammars.Html,
        ["xml"] = LanguageGrammars.Xml,
        ["aspx"] = LanguageGrammars.AspNet,
        ["asp"] = LanguageGrammars.AspNet,
        ["aspnet"] = LanguageGrammars.AspNet,
        ["sql"] = LanguageGrammars.Sql,
        ["json"] = LanguageGrammars.Json,
        ["yaml"] = LanguageGrammars.Yaml,
        ["yml"] = LanguageGrammars.Yaml,
        ["powershell"] = LanguageGrammars.PowerShell,
        ["ps1"] = LanguageGrammars.PowerShell,
        ["lua"] = LanguageGrammars.Lua
    };

    private readonly CodeBlockRenderer _codeBlockRenderer;

    public MyPrismCodeBlockRenderer()
    {
        _codeBlockRenderer = new CodeBlockRenderer();
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
            // _codeBlockRenderer.Write(renderer, node);
            // return;
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
        if (SupportedGrammarMap.TryGetValue(language, out var grammar))
            return HighlightCode(node, grammar, language);

        return ExtractSourceCode(node);
    }

    private string HighlightCode(LeafBlock node, Grammar grammar, string language)
    {
        var text = node.Lines.ToString();
        var highlighter = new HtmlHighlighter();
#if DEBUG
        var sw = Stopwatch.StartNew();
#endif
        var html = highlighter.Highlight(text, grammar, language);
#if DEBUG
        sw.Stop();
        Console.WriteLine($"DEBUG: highlighter.Highlight took {sw.ElapsedMilliseconds}ms for `{language}` code.");
#endif
        return html;
    }

    private string ExtractSourceCode(LeafBlock node)
    {
        var code = new StringBuilder();
        var lines = node.Lines.Lines;
        int totalLines = lines.Length;
        for (int i = 0; i < totalLines; i++)
        {
            var line = lines[i];
            var slice = line.Slice;
            if (slice.Text == null)
            {
                continue;
            }

            var lineText = slice.Text.Substring(slice.Start, slice.Length);
            if (i > 0)
            {
                code.AppendLine();
            }

            foreach (var c in lineText)
            {
                if (_charRemap.TryGetValue(c, out var s))
                    code.Append(s);
                else
                    code.Append(c);
            }
        }

        return code.ToString();
    }

    private readonly IDictionary<char, string> _charRemap = new Dictionary<char, string>
    {
        ['<'] = "&lt;",
        ['>'] = "&gt;",
        ['&'] = "&amp;"
    };
}


public class MyPrismExtension : IMarkdownExtension
{
    public void Setup(MarkdownPipelineBuilder pipeline)
    {
    }

    public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
    {
        if (renderer == null)
        {
            throw new ArgumentNullException(nameof(renderer));
        }

        if (renderer is TextRendererBase<HtmlRenderer> htmlRenderer)
        {
            var codeBlockRenderer = htmlRenderer.ObjectRenderers.FindExact<CodeBlockRenderer>();
            if (codeBlockRenderer != null)
            {
                htmlRenderer.ObjectRenderers.Remove(codeBlockRenderer);
            }

            htmlRenderer.ObjectRenderers.AddIfNotAlready(new MyPrismCodeBlockRenderer());
        }
    }
}
