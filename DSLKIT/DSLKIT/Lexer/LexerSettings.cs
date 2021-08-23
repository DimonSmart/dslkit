using System.Collections.Generic;
using DSLKIT.Terminals;

namespace DSLKIT
{
    public class LexerSettings : List<ITerminal>
    {
        public ITerminal EofTerminal { get; set; }
    }
}