using DSLKIT.Base;
using System;

namespace DSLKIT.Terminals
{
    public sealed class EmptyTerm : ITerm
    {
        private static readonly Lazy<EmptyTerm>
           _lazy = new Lazy<EmptyTerm>(() => new EmptyTerm());
        public static EmptyTerm Empty => _lazy.Value;

        private EmptyTerm()
        {
        }

        public string Name => "Empty";
    }
}