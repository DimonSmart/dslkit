using DSLKIT.Base;
using DSLKIT.SpecialTerms;

namespace DSLKIT.Parser.ExtendedGrammar
{
    public class ExEmptyTerm : ExBase, IExEmptyTerm
    {
        public IEmptyTerm EmptyTerm { get; }
        public override ITerm Term => EmptyTerm;

        public ExEmptyTerm(IEmptyTerm t, RuleSet from, RuleSet to) : base(from, to)
        {
            EmptyTerm = t;
        }
    }
}