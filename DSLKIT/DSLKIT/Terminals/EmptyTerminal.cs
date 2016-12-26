using System;
using DSLKIT.Tokens;

namespace DSLKIT.Terminals
{
    public class EmptyTerminal : ITerminal
    {
        public TermFlags Flags => TermFlags.None;
        public string Name => "Empty";
        public TerminalPriority Priority => TerminalPriority.Low;
        public bool CanStartWith(char c)
        {
            throw new NotImplementedException();
        }

        public bool TryMatch(ISourceStream source, out Token token)
        {
            throw new NotImplementedException();
        }
    }
}