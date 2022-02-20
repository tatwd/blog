namespace MyBlog;

public static class Util
{
    public static string ReplaceWithspaceChars(string str)
    {
        return string.Join("", str.Select(c => char.IsWhiteSpace(c) ? '_' : c));
    }
}
