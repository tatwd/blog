using System.Text;
using System.Text.RegularExpressions;

namespace MyBlog;

public static class Util
{
    public static string ReplaceWhiteSpaceByLodash(string str)
    {
        return string.Join("", str.Trim().Select(c => char.IsWhiteSpace(c) ? '_' : c));
    }

    public static void CreateDirIfNotExists(string distPath)
    {
        var dir = Path.GetDirectoryName(distPath);
        if (string.IsNullOrEmpty(dir))
            throw new ArgumentNullException(nameof(dir),
                "Directory name is null or empty");
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);
    }

    public static string FormatReadingTime(double minutes)
    {
        return $"{minutes} min";
        // var cups = Math.Round(minutes / 5);
        // if (cups > 5)
        // {
        //     var cupsArr = new string[(int)Math.Round(cups / Math.E)];
        //     Array.Fill(cupsArr, "🍱");
        //     return $"{string.Join("", cupsArr)} {minutes} min read";
        // }
        // else
        // {
        //     var cupsArr = new string[cups > 0 ? (int)cups : 1];
        //     Array.Fill(cupsArr, "☕️");
        //     return $"{string.Join("", cupsArr)} {minutes} min read";
        // }
    }

    public static int CalcTimeToRead(string content)
    {
        var words = CountWords(content);
        var (minutes, seconds) = Math.DivRem(words, 200);
        return seconds > 0 ? minutes + 1 : minutes;
    }

    private static int CountWords(string content)
    {
        if (string.IsNullOrEmpty(content))
            return 0;

        int words = 0;
        bool isAlpha = false;

        foreach (var c in content)
        {
            // Chinese character check
            if (c >= 0x4e00 && c <= 0x9fbb)
            {
                if (isAlpha)
                    words++;
                isAlpha = false;
                words++;
            }
            else if (char.IsUpper(c) || char.IsLower(c))
            {
                if (!isAlpha)
                    words++;
                isAlpha = true;
            }
            else
            {
                isAlpha = false;
            }
        }

        return words;
    }

    public static string Html2Text(string html)
    {
        // simple implement
        return Regex.Replace(html, "<[^>]*>", "");
    }

    public static string GenerateAbstractText(string text)
    {
        var abstractTextBuilder = new StringBuilder();

        foreach (var c in text)
        {
            if (abstractTextBuilder.Length >= 140 && char.IsPunctuation(c))
                break;

            abstractTextBuilder.Append(char.IsWhiteSpace(c) ? " " : c.ToString());
        }

        return abstractTextBuilder.ToString();

    }

    public static bool IsLocalUrl(string url)
    {
        return !url.StartsWith("http://") &&
            !url.StartsWith("https://") &&
            !url.StartsWith("ftp://") &&
            !url.StartsWith("mailto:") &&
            !url.StartsWith("//");
    }

}
