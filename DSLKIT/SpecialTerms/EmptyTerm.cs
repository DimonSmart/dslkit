using System;
using DSLKIT.Base;

namespace DSLKIT.SpecialTerms
{
    public interface IEmptyTerm : ITerm
    {
    }

    public sealed class EmptyTerm : IEmptyTerm
    {
        private static readonly Lazy<EmptyTerm> _lazy = new Lazy<EmptyTerm>(() => new EmptyTerm());
        public static EmptyTerm Empty => _lazy.Value;

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