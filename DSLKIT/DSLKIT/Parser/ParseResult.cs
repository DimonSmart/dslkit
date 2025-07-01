using System.Collections.Generic;

namespace DSLKIT.Parser
{
    public class ParseResult
    {
        public bool IsSuccess => Error == null;
        public ParseErrorDescription? Error { get; set; }


        // TODO: result should be a parse tree, not just productions
        public List<int> Productions { get; set; } = new List<int>();
    }
}
