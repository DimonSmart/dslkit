using System.Text.RegularExpressions;

namespace DSLKIT.Terminals
{
    public class MultiLineCommentTerminal : CommentTerminalRegexpBased
    {
        public MultiLineCommentTerminal(string start, string end) :
            base(new Regex(@"\G" + "(?<Start>" + start.Escape() + ")" +
                           "(?<CommentBody>.*?)" +
                           "(?<End>" + end.Escape() + ")", RegexOptions.Compiled | RegexOptions.Singleline), start[0])
        {
        }

        public string Name => "Multi line comment";
    }
}