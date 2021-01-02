using DSLKIT.Terminals;
using System.Collections.Generic;

namespace DSLKIT
{
    public class LexerSettings : List<ITerminal>
    {
        public ITerminal EofTerminal { get; set; }
    }
}