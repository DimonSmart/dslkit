﻿using DSLKIT.Helpers;
using System.Text.RegularExpressions;

namespace DSLKIT.Terminals
{
    public class SingleLineCommentTerminal : CommentTerminalRegexpBased
    {
        public override string Name => "Single line comment";
        public override string DictionaryKey => Name;

        public SingleLineCommentTerminal(string startWith) :
            base(new Regex(@"\G" + startWith.Escape() + "(?<CommentBody>.*)$",
                RegexOptions.Compiled | RegexOptions.Multiline), startWith[0])
        {
        }
    }
}