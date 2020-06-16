using DSLKIT.Terminals;

namespace DSLKIT.Parser
{
    public static class Constants
    {
        public static EmptyTerminal Empty = new EmptyTerminal();
        public static IdentifierTerminal Identifier = new IdentifierTerminal();
        public static IntegerTerminal Integer = new IntegerTerminal();
    }
}