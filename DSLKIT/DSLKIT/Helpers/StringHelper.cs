namespace DSLKIT.Helpers
{
    public static class StringHelper
    {
        public static string DoubleQuoteIt(this string s)
        {
            return @"""" + s + @"""";
        }

        public static string SingleQuoteIt(this string s)
        {
            return "'" + s + "'";
        }

        public static string MakeWhiteSpaceVisible(this string s)
        {
            return s
                .Replace("\n", "↵")
                .Replace("\r", "↵")
                .Replace(" ", "␣");
        }
    }
}