using System.Collections.Generic;

namespace DSLKIT.Parser
{
    public class ParseResult
    {
        public bool IsSuccess => Error == null;
        public ParseErrorDescription Error { get; set; }
        public ParseTreeNode ParseTree { get; set; }

        /// <summary>
        /// List of production numbers applied during parsing, in the order they were applied.
        /// Useful for debugging and analysis of the parsing process.
        /// </summary>
        public List<int> Productions { get; set; } = new List<int>();
    }
}
