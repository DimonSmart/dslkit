using DSLKIT.Base;
using DSLKIT.SpecialTerms;

namespace DSLKIT.Parser.ExtendedGrammar
{
    public class ExEmptyTerm : ExBase, IExEmptyTerm
    {
        public ExEmptyTerm(IEmptyTerm t, RuleSet from, RuleSet? to) : base(from, to)
        {
            EmptyTerm = t;
        }

        public IEmptyTerm EmptyTerm { get; }
        public override ITerm Term => EmptyTerm;
    }
}
