using System.Text.RegularExpressions;

public static partial class Util
{
    [GeneratedRegex(@"[A-Z]|/|=|\$")]
    public static partial Regex LabelRegex();
}