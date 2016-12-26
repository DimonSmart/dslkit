using System.Text.RegularExpressions;

namespace DSLKIT.Terminals
{
    public class SingleLineCommentTerminal : CommentTerminalBase
    {
        public SingleLineCommentTerminal(string startWith) :
            base(new Regex(@"\G" + startWith.Escape() + "(?<CommentBody>.*)$",
                RegexOptions.Compiled | RegexOptions.Multiline), startWith[0])
        {
        }

        public string Name => "Single line comment";
    }
}