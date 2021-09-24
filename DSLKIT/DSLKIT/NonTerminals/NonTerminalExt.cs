namespace DSLKIT.NonTerminals
{
    public static class NonTerminalExt
    {
        public static INonTerminal AsNonTerminal(this string nonTerminalName)
        {
            return new NonTerminal(nonTerminalName);
        }
    }
}