using DSLKIT.Tokens;

namespace DSLKIT.Terminals
{
    public interface ITerminal
    {
        string Name { get; }
        TermFlags Flags { get; }
        TerminalPriority Priority { get; }
        bool CanStartWith(char c);
        bool TryMatch(ISourceStream source, out IToken token);
        // Used to avoid creating duplicate terminals.
        // Duplicate terminals must have equal dictionaryKeys
        string DictionaryKey { get; }
    }
}