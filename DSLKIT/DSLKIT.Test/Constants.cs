using DSLKIT.Terminals;

namespace DSLKIT.Test
{
    public static class Constants
    {
        public static IdentifierTerminal Identifier = new IdentifierTerminal();
        public static IntegerTerminal Integer = new IntegerTerminal();
        public static StringTerminal String = new StringTerminal();
        public static EofTerminal EOF = new EofTerminal();
    }
}