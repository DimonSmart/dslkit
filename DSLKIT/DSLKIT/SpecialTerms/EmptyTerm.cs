using DSLKIT.Base;
using System;

namespace DSLKIT.SpecialTerms
{
    public sealed class EmptyTerm : ITerm
    {
        private static readonly Lazy<EmptyTerm>
           Lazy = new Lazy<EmptyTerm>(() => new EmptyTerm());
        public static EmptyTerm Empty => Lazy.Value;

        private EmptyTerm()
        {
        }

        public string Name => "Empty";

        public override string ToString()
        {
            return "Empty";
        }
    }
}