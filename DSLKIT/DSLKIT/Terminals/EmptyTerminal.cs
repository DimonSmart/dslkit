using DSLKIT.Tokens;
using System;

namespace DSLKIT.Terminals
{
    public sealed class EmptyTerminal : ITerminal
    {
        private static readonly Lazy<EmptyTerminal>
           lazy =
           new Lazy<EmptyTerminal>
               (() => new EmptyTerminal());
        public static EmptyTerminal Empty { get { return lazy.Value; } }

        private EmptyTerminal()
        {
        }

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