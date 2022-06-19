using System.Xml;
using Microsoft.Extensions.Configuration;
using Microsoft.SyndicationFeed;
using Microsoft.SyndicationFeed.Atom;
using MyBlog;
using RazorEngineCore;

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
        new MyLink{Title = "tips", Url = "/tips" },
        new MyLink{Title = "tools", Url = "/tools" },
        new MyLink{Title = "slides", Url = "https://slides.tatwd.me" },
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

var cwd = (cmdArgs["cwd"] ?? Directory.GetCurrentDirectory()).TrimEnd('/').TrimEnd('\\');
var distDir = cmdArgs["dist"] ?? $"{cwd}/dist";
var postsDir = cmdArgs["posts"] ?? $"{cwd}/posts";
var themeDir = cmdArgs["theme"] ?? $"{cwd}/theme";
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

var razorEngine = new RazorEngine();
var markdownRenderer = new MarkdownRenderer();

// Generate post pages
var posts = new List<PostViewModel>(16);

var markdownDirList = new []
{
    postsDir, // default template is 'post'
    $"{cwd}/spa" //default template is 'spa'
};


var compiledTemplateMap = new []
    {
        "index",
        "post",
        "tag",
        "about",
        "spa",
        "404",
    }
    .Select(name => new
        {
            name,
            content =  File.ReadAllText($"{themeTemplateDir}/{name}.cshtml")
        })
    .ToDictionary(
        item => item.name,
        item => razorEngine.Compile(item.content, option =>
        {
            option.AddAssemblyReference(typeof(Util));
        }));



// All posts use a same razor template
var themePostTemplate = compiledTemplateMap["post"];

var postFiles = Directory.GetFiles(postsDir, "*", SearchOption.AllDirectories);
foreach (var path in postFiles.AsParallel())
{
    var outputDir = $"{distDir}/posts";
    var newPath = path.Replace(postsDir, outputDir);
    Util.CreateDirIfNotExists(newPath);

    // Copy other files to dist, just like images etc.
    if (!newPath.EndsWith(".md"))
    {
        File.Copy(path, newPath, true);
        Console.WriteLine("Generated: {0} (copyed)", newPath.Replace(distDir, ""));
        continue;
    }

    var htmlFile = newPath.Replace(".md", ".html");
    var pathname = htmlFile.Replace(distDir, "");

    var mdText = File.ReadAllText(path);
    var (html, postFrontMatter) = markdownRenderer.Render(mdText, pathname);

    // Do not  publish draft item if env is not development.
    if (!isDev && postFrontMatter.Draft)
        continue;

    var plainText = Util.Html2Text(html);
    var timeToRead = Util.CalcTimeToRead(plainText);
    var abstractText = Util.GenerateAbstractText(plainText);

    var postViewModel = new PostViewModel
    {
        BlogConfig = blogConfig,
        PostContent = html,
        TimeToRead = timeToRead,
        AbstractText = abstractText,
        PostRoute = RewriteIndexHtml(pathname),
        FrontMatter = postFrontMatter
    };
    posts.Add(postViewModel);

    // Console.WriteLine("RazorCompile: {0}/{1}", postRoute, htmlFileName);
    await SaveRenderedRazorPageAsync(themePostTemplate, htmlFile, postViewModel);
    Console.WriteLine("Generated: {0}", pathname);
}

// Generate index.html
var homeViewModel = new
{
    BlogConfig = blogConfig,
    Posts = posts.OrderByDescending(p => p.FrontMatter.CreateTime)
};
await SaveRenderedRazorPageAsync(compiledTemplateMap["index"], $"{distDir}/index.html", homeViewModel);
Console.WriteLine("Generated: /index.html");

// Generate 404.html
await SaveRenderedRazorPageAsync(compiledTemplateMap["404"], $"{distDir}/404.html");
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
foreach (var(tagName, postsWithSameTag) in mapTags)
{
    var model = new
    {
        BlogConfig = blogConfig,
        TagName = tagName,
        Posts = postsWithSameTag.OrderByDescending(p => p.FrontMatter.CreateTime)
    };
    var newTagRoute = $"/tags/{Util.ReplaceWhiteSpaceByLodash(tagName)}/index.html";
    await SaveRenderedRazorPageAsync(compiledTemplateMap["tag"], $"{distDir}{newTagRoute}", model);
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
    Console.WriteLine("Generated: {0} (copyed)", newPath.Replace(distDir, ""));
}

// Generate atom.xml fro all posts
await WriteAtomFeedAync(posts, $"{distDir}/atom.xml");
Console.WriteLine("Generated: /atom.xml");


// Generate all SPA
var spaDir = $"{cwd}/spa";
var spaFiles = Directory.GetFiles(spaDir, "*", SearchOption.AllDirectories);
foreach (var path in spaFiles.AsParallel())
{
    var newPath = path.Replace(spaDir, distDir);
    Util.CreateDirIfNotExists(newPath);

    // Do not copy any files in templates dir
    if (!path.EndsWith(".md"))
    {
        File.Copy(path, newPath, true);
        Console.WriteLine("Generated: {0} (copyed)", newPath.Replace(distDir, ""));
        continue;
    }

    // var mdFileName = Path.GetFileName(newPath);
    var htmlFile = newPath.Replace(".md", "/index.html");
    var pathname = htmlFile.Replace(distDir, "");
    var mdText = File.ReadAllText(path);
    var (html, frontMatter) = markdownRenderer.Render(mdText, pathname);

    var spaViewModel = new
    {
        PageTitle = frontMatter.Title,
        PageContent = html,
        BlogConfig = blogConfig
    };

    var templateName = frontMatter?.TemplateName ?? "spa";
    var compiledTemplate = compiledTemplateMap[templateName];
    await SaveRenderedRazorPageAsync(compiledTemplate, htmlFile, spaViewModel);
    Console.WriteLine("Generated: {0}", pathname);
}

string RewriteIndexHtml(string pathname)
{
    return pathname.EndsWith("/index.html") ? pathname.Replace("/index.html", "") : pathname;
}


async Task SaveRenderedRazorPageAsync(IRazorEngineCompiledTemplate template, string distPath, object? model = null)
{
    Util.CreateDirIfNotExists(distPath);

    var html = template.Run(model);
    using StreamWriter sw = File.CreateText(distPath);
    await sw.WriteAsync(html);
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
        await writer.Write(new SyndicationLink(new Uri(blogConfig.BlogLink)));
        await writer.Write(new SyndicationPerson(blogConfig.Author, blogConfig.Email));
        // await writer.WritePubDate(DateTimeOffset.UtcNow);

        foreach (var post in posts.OrderByDescending(p => p.FrontMatter.CreateTime))
        {
            var postLink = $"{blogConfig.BlogLink}{post.PostRoute}";
            var item = new AtomEntry
            {
                Id = postLink,
                Title = post.PostTitle,
                Published = post.FrontMatter.CreateTime,
                LastUpdated = post.FrontMatter.CreateTime,
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
    }
    await sw.FlushAsync();
}
