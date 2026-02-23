using System;
using System.Collections.ObjectModel;
using DSLKIT.Terminals;

namespace DSLKIT.Lexer
{
    public sealed class LexerSettings : Collection<ITerminal>
    {
        public ITerminal? EofTerminal { get; set; }

        protected override void InsertItem(int index, ITerminal item)
        {
            ArgumentNullException.ThrowIfNull(item);
            base.InsertItem(index, item);
        }
    }
}
