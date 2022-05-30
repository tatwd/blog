﻿using System.Xml;
using Markdig;
using Markdig.Extensions.Yaml;
using Markdig.Parsers;
using Markdig.Renderers;
using Markdig.Syntax;
using Microsoft.Extensions.Configuration;
using Microsoft.SyndicationFeed;
using Microsoft.SyndicationFeed.Atom;
using MyBlog;
using RazorEngineCore;
using YamlDotNet.Serialization;

// My blog global config, maybe should set in a config file.
var blogConfig = new BlogConfig
{
    Title = "_king's Notes",
    Author = "_king",
    Description = "万古长空，一朝风月。",
    Email = "tatwdo@gmail.com",
    Link = "https://blog.tatwd.me"
};

// --posts
// --theme
// --dist
// --cwd => current work directory
var cmdArgs = new ConfigurationBuilder()
    .AddCommandLine(args)
    .Build();

var cwd = (cmdArgs["cwd"] ?? Directory.GetCurrentDirectory()).TrimEnd('/').TrimEnd('\\');
var distDir = cmdArgs["dist"] ?? $"{cwd}/dist";
var postDir = cmdArgs["posts"] ?? $"{cwd}/posts";
var themeDir = cmdArgs["theme"] ?? $"{cwd}/theme";
var themeStyleDir = $"{themeDir}/styles";
var themeTemplateDir = $"{themeDir}/templates";
var isDev = !string.IsNullOrEmpty(cmdArgs["dev"]);

Console.WriteLine("cwd: {0}", cwd);
Console.WriteLine("distDir: {0}", distDir);
Console.WriteLine("postDir: {0}", postDir);
Console.WriteLine("themeDir: {0}", themeDir);
Console.WriteLine("isDev: {0}", isDev);

if (Directory.Exists(distDir))
    Directory.Delete(distDir, true);

var razorEngine = new RazorEngine();
var pipeline = new MarkdownPipelineBuilder()
    .UseAdvancedExtensions()
    .UseYamlFrontMatter()
    .UsePreciseSourceLocation()
    .UseSoftlineBreakAsHardlineBreak()
    .Use<MyPrismExtension>()
    .Build();
var yamlDeserializer = new DeserializerBuilder()
    .IgnoreUnmatchedProperties()
    .Build();

// Generate post pages
var posts = new List<PostViewModel>(16);
var themePostTemplateFilePath = $"{themeTemplateDir}/post.cshtml";
var themePostTemplateContent = File.ReadAllText(themePostTemplateFilePath);

// All posts use a same razor template
var themePostTemplate = await razorEngine.CompileAsync(themePostTemplateContent, option =>
{
    option.AddAssemblyReference(typeof(Util));
});

var postFiles = Directory.GetFiles(postDir, "*", SearchOption.AllDirectories);
foreach (var path in postFiles.AsParallel())
{
    var newPath = path.Replace(postDir, $"{distDir}/posts");
    Util.CreateDirIfNotExists(newPath);

    // Copy other files to dist, just like images etc.
    if (!newPath.EndsWith(".md"))
    {
        File.Copy(path, newPath, true);
        Console.WriteLine("Generated: {0} (copyed)", newPath.Replace(distDir, ""));
        continue;
    }

    var htmlFile = newPath.Replace(".md", ".html");
    var htmlFileName = Path.GetFileName(htmlFile);
    var postRoute = Path.GetDirectoryName(htmlFile.Replace(distDir, "")) !;

    using var writer = new StringWriter();
    var renderer = new HtmlRenderer(writer)
    {
        LinkRewriter = (link) =>
        {
            if (link.StartsWith("./"))
                return postRoute + link.Substring(1);
            //if (!link.StartsWith("/"))
            //    return  postRoute + "/" + link;
            return link;
        }
    };
    pipeline.Setup(renderer);
    var mdText = File.ReadAllText(path);
    var document = MarkdownParser.Parse(mdText, pipeline);
    var postFrontMatter = GetPostFrontMatter(document);

    // Do not  publish draft item if env is not development.
    if (!isDev && postFrontMatter.Draft)
        continue;

    renderer.Render(document);
    writer.Flush();
    var html = writer.ToString();
    // var html = Markdown.ToHtml(mdText, pipline);

    // var plainText = Markdown.ToPlainText(mdText, pipeline);
    var plainText = Util.Html2Text(html);
    var timeToRead = Util.CalcTimeToRead(plainText);
    // var abstractText = plainText.Substring(0, Math.Min(plainText.Length, 140)).Replace("\n", " ");
    var abstractText = Util.GenerateAbstractText(plainText);

    var fileNameWithoutExt = Path.GetFileNameWithoutExtension(newPath);
    var parentDir = Directory.GetParent(newPath) !;
    var isPostPageDir = parentDir.FullName.EndsWith("/posts");
    var newPostRoute = isPostPageDir ? postRoute + "/" + htmlFileName : postRoute;

    var postViewModel = new PostViewModel
    {
        BlogConfig = blogConfig,
        PostContent = html,
        TimeToRead = timeToRead,
        AbstractText = abstractText,
        PostRoute = newPostRoute,
        FrontMatter = postFrontMatter
    };
    posts.Add(postViewModel);

    // Console.WriteLine("RazorCompile: {0}/{1}", postRoute, htmlFileName);
    var result = themePostTemplate.Run(postViewModel);

    using StreamWriter swPost = File.CreateText(htmlFile);
    await swPost.WriteAsync(result);
    Console.WriteLine("Generated: {0}/{1}", postRoute, htmlFileName);
}

// Generate index.html
var homeViewModel = new
{
    BlogConfig = blogConfig,
    Posts = posts.OrderByDescending(p => p.FrontMatter.CreateTime)
};
var themeHomeTemplateContent = File.ReadAllText($"{themeTemplateDir}/index.cshtml");
var themeHomeTemplate = await razorEngine.CompileAsync(themeHomeTemplateContent, option =>
{
    option.AddAssemblyReference(typeof(Util));
});
await SaveRenderedRazorPageAsync(themeHomeTemplate, $"{distDir}/index.html", homeViewModel);
Console.WriteLine("Generated: /index.html");

// Generate 404.html
var theme404TemplateContent = File.ReadAllText($"{themeTemplateDir}/404.cshtml");
var theme404Template = await razorEngine.CompileAsync(theme404TemplateContent);
await SaveRenderedRazorPageAsync(theme404Template, $"{distDir}/404.html");
Console.WriteLine("Generated: /404.html");

// Map all posts with same tag
var mapTags = new Dictionary<string, IList<PostViewModel>>();
foreach (var post in posts)
{
    if (post.FrontMatter.Tags is null)
        continue;

    foreach (var tagName in post.FrontMatter.Tags)
    {
        if (!mapTags.ContainsKey(tagName))
            mapTags[tagName] = new List<PostViewModel> { post };
        else
            mapTags[tagName].Add(post);
    }
}

// Generate tag pages
var themeTagTemplateContent = File.ReadAllText($"{themeTemplateDir}/tag.cshtml");
var themeTagTemplate = await razorEngine.CompileAsync(themeTagTemplateContent, option =>
{
    option.AddAssemblyReference(typeof(Util));
});
foreach (var(tagName, postsWithSameTag) in mapTags)
{
    var model = new
    {
        BlogConfig = blogConfig,
        TagName = tagName,
        Posts = postsWithSameTag.OrderByDescending(p => p.FrontMatter.CreateTime)
    };
    var newTagRoute = $"/tags/{Util.ReplaceWhiteSpaceByLodash(tagName)}/index.html";
    await SaveRenderedRazorPageAsync(themeTagTemplate, $"{distDir}{newTagRoute}", model);
    Console.WriteLine("Generated: {0}", newTagRoute);
}

// Copy other files in theme directory
var otherThemeFiles = Directory.GetFiles(themeDir, "*", SearchOption.AllDirectories);
foreach (var path in otherThemeFiles.AsParallel())
{
    // Do not copy any files in templates dir
    if (path.StartsWith(themeTemplateDir))
        continue;

    var newPath = path.Replace(themeDir, distDir);
    Util.CreateDirIfNotExists(newPath);
    File.Copy(path, newPath, overwrite : true);
    Console.WriteLine("Generated: {0} (copyed)", newPath.Replace(distDir, ""));
}

// Generate atom.xml fro all posts
await WriteAtomFeedAync(posts, $"{distDir}/atom.xml");
Console.WriteLine("Generated: /atom.xml");


// Generate all SPA
var spaTemplateContent = File.ReadAllText($"{themeTemplateDir}/spa.cshtml");
var spaTemplate = await razorEngine.CompileAsync(spaTemplateContent);
var aboutTemplateContent = File.ReadAllText($"{themeTemplateDir}/about.cshtml");
var aboutTemplate = await razorEngine.CompileAsync(aboutTemplateContent);

var spaDir = $"{cwd}/spa";
var spaFiles = Directory.GetFiles(spaDir, "*", SearchOption.AllDirectories);
foreach (var path in spaFiles.AsParallel())
{
    var newPath = path.Replace(spaDir, distDir);

    // Do not copy any files in templates dir
    if (!path.EndsWith(".md"))
    {
        Util.CreateDirIfNotExists(newPath);
        File.Copy(path, newPath, true);
        Console.WriteLine("Generated: {0} (copyed)", newPath.Replace(distDir, ""));
        continue;
    }

    var mdFileName = Path.GetFileName(newPath);
    var htmlFile = newPath.Replace(".md", "/index.html");
    var postRoute = Path.GetDirectoryName(htmlFile.Replace(distDir, ""))!;


    using var writer = new StringWriter();
    var renderer = new HtmlRenderer(writer)
    {
        LinkRewriter = (link) =>
        {
            if (link.StartsWith("./"))
                return postRoute + link.Substring(1);
            return link;
        }
    };
    pipeline.Setup(renderer);
    var mdText = File.ReadAllText(path);
    var document = MarkdownParser.Parse(mdText, pipeline);
    var postFrontMatter = GetPostFrontMatter(document);
    renderer.Render(document);
    writer.Flush();
    var html = writer.ToString();

    var spaViewModel = new
    {
        PageTitle = postFrontMatter.Title,
        PageContent = html,
        BlogConfig = blogConfig
    };

    // TODO: need refactor
    var result = "about" == postFrontMatter.TemplateName
        ? aboutTemplate.Run(spaViewModel)
        : spaTemplate.Run(spaViewModel);

    Util.CreateDirIfNotExists(htmlFile);
    using StreamWriter swPost = File.CreateText(htmlFile);
    await swPost.WriteAsync(result);
    Console.WriteLine("Generated: {0}/index.html", postRoute);

}



async Task SaveRenderedRazorPageAsync(IRazorEngineCompiledTemplate template, string distPath, object? model = null)
{
    Util.CreateDirIfNotExists(distPath);

    var html = template.Run(model);
    using StreamWriter sw = File.CreateText(distPath);
    await sw.WriteAsync(html);
}

PostFrontMatterViewModel GetPostFrontMatter(MarkdownDocument document)
{
    var block = document
        .Descendants<YamlFrontMatterBlock>()
        .FirstOrDefault();

    if (block is null)
        throw new ArgumentNullException(nameof(block), "Post must have a front matter!");

    var yaml =
        block
        // this is not a mistake
        // we have to call .Lines 2x
        .Lines // StringLineGroup[]
        .Lines // StringLine[]
        .OrderByDescending(x => x.Line)
        .Select(x => $"{x}\n")
        .ToList()
        .Select(x => x.Replace("---", string.Empty))
        .Where(x => !string.IsNullOrWhiteSpace(x))
        .Aggregate((s, agg) => agg + s);

    var frontMatter = yamlDeserializer.Deserialize<PostFrontMatterViewModel>(yaml)!;

    if (string.IsNullOrEmpty(frontMatter.Title))
        throw new ArgumentNullException(nameof(frontMatter.Title),
            "Post `title` is required in front matter!");
    if (frontMatter.CreateTime == default)
        throw new ArgumentNullException(nameof(frontMatter.CreateTime),
            "Post `create_time` is required in front matter!");

    frontMatter.Tags = frontMatter.Tags ?? Array.Empty<string>();

    return frontMatter;
}


async Task WriteAtomFeedAync(IEnumerable<PostViewModel> posts, string distPath)
{
    Util.CreateDirIfNotExists(distPath);

    using StreamWriter sw = File.CreateText(distPath);

    using (XmlWriter xmlWriter = XmlWriter.Create(sw, new XmlWriterSettings() { Async = true , Indent = true }))
    {

        var writer = new AtomFeedWriter(xmlWriter);
        await writer.WriteTitle(blogConfig.Title);
        // await writer.WriteDescription(blogConfig.Description);
        await writer.Write(new SyndicationLink(new Uri(blogConfig.Link)));
        await writer.Write(new SyndicationPerson(blogConfig.Author, blogConfig.Email));
        // await writer.WritePubDate(DateTimeOffset.UtcNow);

        foreach (var post in posts.OrderByDescending(p => p.FrontMatter.CreateTime))
        {
            var postLink = $"{blogConfig.Link}{post.PostRoute}";
            var item = new AtomEntry
            {
                Id = postLink,
                Title = post.PostTitle,
                Published = post.FrontMatter.CreateTime,
                LastUpdated = post.FrontMatter.CreateTime,
                ContentType = "html",
                Description = post.PostContent,
                Summary = post.AbstractText
            };

            item.AddContributor(new SyndicationPerson(blogConfig.Author, blogConfig.Email, AtomContributorTypes.Author));
            item.AddLink(new SyndicationLink(new Uri(postLink)));

            foreach (var tag in post.FrontMatter.Tags)
                item.AddCategory(new SyndicationCategory(tag));

            await writer.Write(item);
        }

        xmlWriter.Flush();
    }
    await sw.FlushAsync();
}
