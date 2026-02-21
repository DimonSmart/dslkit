using System.Collections.Generic;
using DSLKIT.Terminals;

namespace DSLKIT.Lexer
{
    public class LexerSettings : List<ITerminal>
    {
        public ITerminal? EofTerminal { get; set; }
    }
}
