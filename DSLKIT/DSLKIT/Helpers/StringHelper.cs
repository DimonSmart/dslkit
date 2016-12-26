namespace DSLKIT
{
    public static class StringHelper
    {
        public static string DoubleQuoteIt(this string s)
        {
            return @"""" + s + @"""";
        }

        public static string SingleQuoteIt(this string s)
        {
            return @"'" + s + @"'";
        }
    }
}