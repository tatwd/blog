﻿using System.Xml;
using Microsoft.Extensions.Configuration;
using Microsoft.SyndicationFeed;
using Microsoft.SyndicationFeed.Atom;
using MyBlog;

// My blog global config, maybe should set in a config file.
var globalBlogConfig = new BlogConfig
{
    Title = "_king's Notes",
    Author = "_king",
    Description = "万古长空，一朝风月。",
    Email = "tatwdo@gmail.com",
    BlogLink = "https://blog.cloong.me",
    Links = new []
    {
        new MyLink{Title = "tips", Url = "/spa/tips.html" },
        new MyLink{Title = "tools", Url = "/spa/tools.html" },
        new MyLink{Title = "slides", Url = "https://slides.cloong.me" },
        new MyLink{Title = "github", Url = "https://github.com/tatwd" },
        new MyLink{Title = "mail", Url = "mailto:tatwdo@gmail.com" },
        new MyLink{Title = "rss", Url = "/atom.xml" },
    }
};

// --posts
// --theme
// --dist
// --cwd => current work directory
var cmdArgs = new ConfigurationBuilder()
    .AddCommandLine(args)
    .Build();

var cwd = new DirectoryInfo(cmdArgs["cwd"] ?? Directory.GetCurrentDirectory()).FullName;
var distDir = cmdArgs["dist"] ??  Path.Join(cwd, "dist");
var postsDir = cmdArgs["posts"] ?? Path.Join(cwd, "posts");
var themeDir = cmdArgs["theme"] ?? Path.Join(cwd, "theme");
var themeTemplateDir = Path.Join(themeDir, "templates");
var isDev = !string.IsNullOrEmpty(cmdArgs["dev"]) || args.Contains("--dev");


Console.WriteLine("cwd: {0}", cwd);
Console.WriteLine("distDir: {0}", distDir);
Console.WriteLine("postDir: {0}", postsDir);
Console.WriteLine("themeDir: {0}", themeDir);
Console.WriteLine("isDev: {0}", isDev.ToString());

if (Directory.Exists(distDir))
    Directory.Delete(distDir, true);

var markdownRenderer = new MarkdownRenderer();
var razorRenderer = new RazorRenderer(themeTemplateDir);



// Generate post pages
var globalPosts = new List<Post>(16);
var postAssetFiles = new Dictionary<string, string>(16);


// All posts use a same razor template
var markdownDirList = new (string dirpath, string defaultTemplateName)[]
{
    (postsDir, "post"),
    (Path.Join(cwd, "spa"), "spa")
};
foreach (var (dirPath, defaultTemplateName) in markdownDirList)
{
    var dirInfo = new DirectoryInfo(dirPath);
    var dirname = dirInfo.Name;
    var postFiles = dirInfo.GetFiles("*.md", SearchOption.AllDirectories);
    var outputDir = Path.Join(distDir, dirname);

    foreach (var fileInfo in postFiles.AsParallel())
    {
        var path = fileInfo.FullName;
        var currentDir = fileInfo.Directory!.FullName;
        var newPath = path.Replace(dirPath, outputDir);

        var htmlFile = CreatePostUrl(newPath);
        var pathname = htmlFile.Replace(distDir, "").Replace("\\", "/");

        var mdText = File.ReadAllText(path);
        var (html, frontMatter, localAssetLinks) = markdownRenderer.Render(mdText, pathname);

        // Do not  publish draft item if env is not development.
        if (!isDev && frontMatter.Draft)
            continue;

        var timeToRead = frontMatter.Duration;
        var abstractText = frontMatter.Description;

        if (string.IsNullOrEmpty(timeToRead) || string.IsNullOrEmpty(abstractText))
        {
            var plainText = Util.Html2Text(html);

            if (string.IsNullOrEmpty(timeToRead))
                timeToRead = Util.CalcTimeToRead(plainText);
            if (string.IsNullOrEmpty(abstractText))
                abstractText = Util.GenerateAbstractText(plainText);
        }

        var post = new Post
        {
            Title = frontMatter.Title,
            HtmlContent = html,
            TimeToRead = timeToRead,
            AbstractText = abstractText,
            Lang = frontMatter.Lang,
            CreateTime = frontMatter.CreateTime,
            Tags = frontMatter.Tags,
            Pathname = RewriteIndexHtml(pathname),
            TemplateName = frontMatter.TemplateName ?? defaultTemplateName
        };

        if (post.TemplateName == "post")
            globalPosts.Add(post);

        // Console.WriteLine("RazorCompile: {0}/{1}", postRoute, htmlFileName);
        await SaveRenderedPostPageAsync(htmlFile, post, globalBlogConfig);
        Console.WriteLine("Generated: {0}", pathname);

        foreach (var assetLink in localAssetLinks)
        {
            // TODO: need update if use custom `pathname`
            var fullPath = Path.GetFullPath(Path.Join(currentDir, assetLink));
            if (postAssetFiles.ContainsKey(fullPath))
                continue;
            postAssetFiles[fullPath] = Path.GetFullPath(fullPath.Replace(dirPath, outputDir));
        }
    }
}

// Copy assets
// minify image here ?
foreach (var (fromPath, toPath) in postAssetFiles)
{
    Util.CreateDirIfNotExists(toPath);
    File.Copy(fromPath, toPath, true);
    Console.WriteLine("Generated: {0} (copied)", toPath.Replace(distDir, "").Replace("\\", "/"));
}


// Generate index.html
var homeViewModel = new
{
    BlogConfig = globalBlogConfig,
    Posts = globalPosts.OrderByDescending(p => p.CreateTime)
};
await SaveRenderedRazorPageAsync($"{distDir}/index.html", "index", homeViewModel);
Console.WriteLine("Generated: /index.html");

// Generate 404.html
await SaveRenderedRazorPageAsync($"{distDir}/404.html", "404");
Console.WriteLine("Generated: /404.html");

// Group all posts with same tag
var groupPostsWithSameTag = globalPosts
    .SelectMany(post => post.Tags.Select(tag => new { tag, post }))
    .ToLookup(x => x.tag, x => x.post);

// Generate tag pages
foreach (var g in groupPostsWithSameTag)
{
    var tagName = g.Key;
    var postsWithSameTag = g.OrderByDescending(p => p.CreateTime);

    var model = new
    {
        BlogConfig = globalBlogConfig,
        TagName = tagName,
        Posts = postsWithSameTag
    };
    var newTagRoute = $"/tags/{Util.ReplaceWhiteSpaceByLodash(tagName)}/index.html";
    await SaveRenderedRazorPageAsync($"{distDir}{newTagRoute}", "tag", model);
    Console.WriteLine("Generated: {0}", newTagRoute);
}

// Copy other static files in theme directory
var otherThemeFiles = Directory.GetFiles(themeDir, "*", SearchOption.AllDirectories);
foreach (var path in otherThemeFiles.AsParallel())
{
    // Do not copy any files in templates dir
    if (path.StartsWith(themeTemplateDir))
        continue;

    var newPath = path.Replace(themeDir, distDir);
    Util.CreateDirIfNotExists(newPath);
    File.Copy(path, newPath, overwrite : true);
    Console.WriteLine("Generated: {0} (copied)", newPath.Replace(distDir, "").Replace("\\", "/"));
}

// Generate atom.xml fro all posts
await WriteAtomFeedAsync(globalPosts, $"{distDir}/atom.xml");
Console.WriteLine("Generated: /atom.xml");



string CreatePostUrl(string mdPath)
{
    // var filename = Path.GetFileNameWithoutExtension(mdPath);
    return mdPath.Replace(".md", ".html");
}

string RewriteIndexHtml(string pathname)
{
    return (pathname.EndsWith("/index.html") ? pathname.Replace("/index.html", "") : pathname)
        .Replace("\\", "/"); // fix window separator char -> `/`
}


async Task SaveRenderedPostPageAsync(string distPath, Post post, BlogConfig blogConfig)
{
    var html = await  razorRenderer.RenderPostPageAsync(post, blogConfig);
    Util.CreateDirIfNotExists(distPath);
    await using var sw = File.CreateText(distPath);
    await sw.WriteAsync(html);
}

async Task SaveRenderedRazorPageAsync(string distPath, string templateName, object? model = null)
{
    var html = await  razorRenderer.RenderRazorPageAsync(templateName, model);
    Util.CreateDirIfNotExists(distPath);
    await using var sw = File.CreateText(distPath);
    await sw.WriteAsync(html);
}


async Task WriteAtomFeedAsync(IEnumerable<Post> posts, string distPath)
{
    Util.CreateDirIfNotExists(distPath);

    await using var sw = File.CreateText(distPath);
    await using var xmlWriter = XmlWriter.Create(sw, new XmlWriterSettings { Async = true, Indent = true });
    var writer = new AtomFeedWriter(xmlWriter);
    await writer.WriteTitle(globalBlogConfig.Title);
    // await writer.WriteDescription(blogConfig.Description);
    await writer.Write(new SyndicationLink(new Uri(globalBlogConfig.BlogLink)));
    await writer.Write(new SyndicationPerson(globalBlogConfig.Author, globalBlogConfig.Email));
    // await writer.WritePubDate(DateTimeOffset.UtcNow);

    foreach (var post in posts.OrderByDescending(p => p.CreateTime))
    {
        var postLink = $"{globalBlogConfig.BlogLink}{post.Pathname}";
        var item = new AtomEntry
        {
            Id = postLink,
            Title = post.Title,
            Published = post.CreateTime,
            LastUpdated = post.CreateTime,
            // ContentType = "html",
            Summary = post.AbstractText
        };

        item.AddContributor(new SyndicationPerson(globalBlogConfig.Author, globalBlogConfig.Email, AtomContributorTypes.Author));
        item.AddLink(new SyndicationLink(new Uri(postLink)));

        // foreach (var tag in post.FrontMatter.Tags)
        //     item.AddCategory(new SyndicationCategory(tag));

        await writer.Write(item);
    }

    xmlWriter.Flush();
    await sw.FlushAsync();
}
