using DSLKIT.Tokens;
using System;

namespace DSLKIT.Terminals
{
    public class EmptyTerminal : ITerminal
    {
        public string Name => "Empty";
        public TermFlags Flags => TermFlags.None;
        public TerminalPriority Priority => TerminalPriority.Low;

        public bool CanStartWith(char c)
        {
            throw new NotImplementedException();
        }

        public bool TryMatch(ISourceStream source, out IToken token)
        {
            throw new NotImplementedException();
        }

        public string DictionaryKey => Name;
    }
}