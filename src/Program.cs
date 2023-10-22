using Microsoft.Extensions.Configuration;
using MyBlog;
using NUglify;

// My blog global config, maybe should set in a config file.
var globalBlogConfig = new BlogConfig
{
    Title = "_king's Notes",
    Author = "_king",
    Description = "万古长空，一朝风月。",
    Email = "tatwdo@gmail.com",
    BlogLink = "https://tatwd.deno.dev",
    Links = new MyLink[]
    {
        new (){Title = "tips", Url = "/spa/tips.html" },
        new (){Title = "tools", Url = "/spa/tools.html" },
        new (){Title = "slides", Url = "https://tatwd-slides.vercel.app" },
        new (){Title = "github", Url = "https://github.com/tatwd" },
        new (){Title = "mail", Url = "mailto:tatwdo@gmail.com" },
        new (){Title = "rss", Url = "/atom.xml" },
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
            UpdateTime = frontMatter.UpdateTime,
            Tags = frontMatter.Tags,
            Pathname = RewriteIndexHtml(pathname),
            TemplateName = frontMatter.TemplateName ?? defaultTemplateName,
            IsDraft = frontMatter.Draft,
            FrontMatter = frontMatter
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

    var minified = false;

    if (path.EndsWith(".css") && !path.EndsWith(".min.css"))
    {
        var cssText = await File.ReadAllTextAsync(path);
        var miniCss = Uglify.Css(cssText);
        await File.WriteAllTextAsync(newPath, miniCss.Code);
        minified = true;
    }
    else if (path.EndsWith(".js") && !path.EndsWith(".min.js"))
    {
        var jsText = await File.ReadAllTextAsync(path);
        var miniJs = Uglify.Js(jsText);
        await File.WriteAllTextAsync(newPath, miniJs.Code);
        minified = true;
    }
    else
    {
        File.Copy(path, newPath, overwrite : true);
    }

    Console.WriteLine("Generated: {0} (copied{1})", newPath.Replace(distDir, "").Replace("\\", "/"), minified ? ",minified" : "");
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

    var feed = new FeedSharp.Feed(new FeedSharp.FeedOptions(globalBlogConfig.BlogLink, globalBlogConfig.Title)
    {
        Description = globalBlogConfig.Description,
        Link = globalBlogConfig.BlogLink,
        Feed = $"{globalBlogConfig.BlogLink}/atom.xml",
        Author = new FeedSharp.Author
        {
            Name = globalBlogConfig.Author,
            Email = globalBlogConfig.Email
        },
        Updated = DateTime.Now,
        Copyright = $"Copyright {DateTime.Now.Year} {globalBlogConfig.BlogLink}"
    });

    foreach (var post in posts.OrderByDescending(p => p.CreateTime))
    {
        var postLink = $"{globalBlogConfig.BlogLink}{post.Pathname}";
        var categories = post.Tags.Select(tag => new FeedSharp.Category { Name = tag }).ToArray();

        var item = new FeedSharp.Item(post.Title, postLink, post.CreateTime)
        {
            Description = post.AbstractText,
            Category = categories,
            Published = post.UpdateTime
        };

        feed.AddItem(item);
    }

    var xml = feed.ToAtom1();

    await using var sw = File.CreateText(distPath);
    await sw.WriteAsync(xml);
    await sw.FlushAsync();
}
