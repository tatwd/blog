using System.Xml;
using Microsoft.Extensions.Configuration;
using Microsoft.SyndicationFeed;
using Microsoft.SyndicationFeed.Atom;
using MyBlog;

// My blog global config, maybe should set in a config file.
var blogConfig = new BlogConfig
{
    Title = "_king's Notes",
    Author = "_king",
    Description = "万古长空，一朝风月。",
    Email = "tatwdo@gmail.com",
    BlogLink = "https://blog.tatwd.me",
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

var cwd = cmdArgs["cwd"] ?? Directory.GetCurrentDirectory();
var distDir = cmdArgs["dist"] ??  Path.Join(cwd, "dist");
var postsDir = cmdArgs["posts"] ?? Path.Join(cwd, "posts");
var themeDir = cmdArgs["theme"] ?? Path.Join(cwd, "theme");
var themeStyleDir = $"{themeDir}/styles";
var themeTemplateDir = $"{themeDir}/templates";
var isDev = !string.IsNullOrEmpty(cmdArgs["dev"]);

Console.WriteLine("cwd: {0}", cwd);
Console.WriteLine("distDir: {0}", distDir);
Console.WriteLine("postDir: {0}", postsDir);
Console.WriteLine("themeDir: {0}", themeDir);
Console.WriteLine("isDev: {0}", isDev);

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
    ($"{cwd}/spa", "spa")
};
foreach (var (dirpath, defaultTemplateName) in markdownDirList)
{
    var dirInfo = new DirectoryInfo(dirpath);
    var dirname = dirInfo.Name;
    var postFiles = Directory.GetFiles(dirpath, "*.md", SearchOption.AllDirectories);

    foreach (var path in postFiles)
    {
        var currentDir = Path.GetDirectoryName(path);
        var outputDir = Path.Join(distDir, dirname);
        var newPath = path.Replace(dirpath, outputDir);

        var htmlFile = CreatePostUrl(newPath);
        var pathname = htmlFile.Replace(distDir, "");

        var mdText = File.ReadAllText(path);
        var (html, frontMatter, localAssetLinks) = markdownRenderer.Render(mdText, pathname);

        // Do not  publish draft item if env is not development.
        if (!isDev && frontMatter.Draft)
            continue;

        var plainText = Util.Html2Text(html);
        var timeToRead = Util.CalcTimeToRead(plainText);
        var abstractText = Util.GenerateAbstractText(plainText);

        var post = new Post
        {
            Title = frontMatter.Title,
            HtmlContent = html,
            TimeToRead = timeToRead,
            AbstractText = abstractText,
            CreateTime = frontMatter.CreateTime,
            Tags = frontMatter.Tags,
            Pathname = RewriteIndexHtml(pathname),
            TemplateName = frontMatter.TemplateName ?? defaultTemplateName
        };

        if (post.TemplateName == "post")
            globalPosts.Add(post);

        // Console.WriteLine("RazorCompile: {0}/{1}", postRoute, htmlFileName);
        await SaveRenderedPostPageAsync(htmlFile, post, blogConfig);
        Console.WriteLine("Generated: {0}", pathname);

        foreach (var assetLink in localAssetLinks)
        {
            // TODO: need update if use custom `pathname`
            var fullPath = Path.GetFullPath(Path.Join(currentDir, assetLink));
            postAssetFiles[fullPath] = Path.Join(currentDir, assetLink).Replace(dirpath, outputDir);
        }
    }
}

// Copy assets
foreach (var (fromPath, toPath) in postAssetFiles)
{
    // minify image here ?

    Util.CreateDirIfNotExists(toPath);
    File.Copy(fromPath, toPath, true);
    Console.WriteLine("Generated: {0} (copied)", toPath);
}


// Generate index.html
var homeViewModel = new
{
    BlogConfig = blogConfig,
    Posts = globalPosts.OrderByDescending(p => p.CreateTime)
};
await SaveRenderedRazorPageAsync($"{distDir}/index.html", "index", homeViewModel);
Console.WriteLine("Generated: /index.html");

// Generate 404.html
await SaveRenderedRazorPageAsync($"{distDir}/404.html", "404");
Console.WriteLine("Generated: /404.html");

// Map all posts with same tag
var mapTags = new Dictionary<string, IList<Post>>();
foreach (var post in globalPosts)
{
    if (!post.Tags.Any())
        continue;

    foreach (var tagName in post.Tags)
    {
        if (!mapTags.ContainsKey(tagName))
            mapTags[tagName] = new List<Post> { post };
        else
            mapTags[tagName].Add(post);
    }
}

// Generate tag pages
foreach (var(tagName, postsWithSameTag) in mapTags)
{
    var model = new
    {
        BlogConfig = blogConfig,
        TagName = tagName,
        Posts = postsWithSameTag.OrderByDescending(p => p.CreateTime)
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
    Console.WriteLine("Generated: {0} (copied)", newPath.Replace(distDir, ""));
}

// Generate atom.xml fro all posts
await WriteAtomFeedAsync(globalPosts, $"{distDir}/atom.xml");
Console.WriteLine("Generated: /atom.xml");



string CreatePostUrl(string mdPath)
{
    var filename = Path.GetFileNameWithoutExtension(mdPath);
    return mdPath.Replace(".md", ".html");
}

string RewriteIndexHtml(string pathname)
{
    return pathname.EndsWith("/index.html") ? pathname.Replace("/index.html", "") : pathname;
}


async Task SaveRenderedPostPageAsync(string distPath, Post post, BlogConfig blogConfig)
{
    var html = await razorRenderer.RenderPostPageAsync(post, blogConfig);
    Util.CreateDirIfNotExists(distPath);
    await using var sw = File.CreateText(distPath);
    await sw.WriteAsync(html);
}

async Task SaveRenderedRazorPageAsync(string distPath, string templateName, object? model = null)
{
    var html = await razorRenderer.RenderRazorPageAsync(templateName, model);
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
    await writer.WriteTitle(blogConfig.Title);
    // await writer.WriteDescription(blogConfig.Description);
    await writer.Write(new SyndicationLink(new Uri(blogConfig.BlogLink)));
    await writer.Write(new SyndicationPerson(blogConfig.Author, blogConfig.Email));
    // await writer.WritePubDate(DateTimeOffset.UtcNow);

    foreach (var post in posts.OrderByDescending(p => p.CreateTime))
    {
        var postLink = $"{blogConfig.BlogLink}{post.Pathname}";
        var item = new AtomEntry
        {
            Id = postLink,
            Title = post.Title,
            Published = post.CreateTime,
            LastUpdated = post.CreateTime,
            // ContentType = "html",
            Summary = post.AbstractText
        };

        item.AddContributor(new SyndicationPerson(blogConfig.Author, blogConfig.Email, AtomContributorTypes.Author));
        item.AddLink(new SyndicationLink(new Uri(postLink)));

        // foreach (var tag in post.FrontMatter.Tags)
        //     item.AddCategory(new SyndicationCategory(tag));

        await writer.Write(item);
    }

    xmlWriter.Flush();
    await sw.FlushAsync();
}
