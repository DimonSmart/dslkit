using System.Text.RegularExpressions;

namespace DSLKIT
{
    public static class RegexHelper
    {
        public static Match Match(this Regex regex, ISourceStream sourceStream)
        {
            return regex.Match(sourceStream.GetText(), sourceStream.Position);
        }

        public static string Escape(this string s)
        {
            var r = Regex.Escape(s);
            r = r.Replace("]", @"\]");
            r = r.Replace("}", @"\}");
            return r;
        }
    }
}