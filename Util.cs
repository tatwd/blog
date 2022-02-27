namespace MyBlog;

public static class Util
{
    public static string ReplaceWithspaceByLodash(string str)
    {
        return string.Join("", str.Select(c => char.IsWhiteSpace(c) ? '_' : c));
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

}
