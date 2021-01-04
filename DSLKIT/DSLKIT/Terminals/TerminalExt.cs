namespace DSLKIT.Terminals
{
    public static class TerminalExt
    {
        public static ITerminal AsKeywordTerminal(this string terminalName, TermFlags flags = TermFlags.None)
        {
            return new KeywordTerminal(terminalName, flags);
        }
    }
}