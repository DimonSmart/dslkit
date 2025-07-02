using System.Collections.Generic;

namespace DSLKIT.Parser
{
    public class ParseResult
    {
        public bool IsSuccess => Error == null;
        public ParseErrorDescription Error { get; set; }
        public ParseTreeNode ParseTree { get; set; }

        // TODO: May be we should delete it?
        public List<int> Productions { get; set; } = new List<int>();
    }
}
