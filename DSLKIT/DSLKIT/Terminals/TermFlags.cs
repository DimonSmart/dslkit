using System;

namespace DSLKIT.Terminals
{
    [Flags]
    public enum TermFlags
    {
        None = 0,
        Space = 1 << 0,
        OpenBrace = 1 << 1,
        CloseBrace = 1 << 2,
        Identifier = 1 << 3,
        Const = 1 << 4,
        Comment = 1 << 5,
        Brace = OpenBrace | CloseBrace
    }
}