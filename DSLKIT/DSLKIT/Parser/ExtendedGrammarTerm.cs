using DSLKIT.Base;

namespace DSLKIT.Parser
{
    public class ExtendedGrammarTerm : ITerm
    {
        private readonly ITerm _term;
        private readonly RuleSet _from;
        private readonly RuleSet _to;

        public ExtendedGrammarTerm(ITerm term, RuleSet from, RuleSet to)
        {
            _term = term;
            _from = from;
            _to = to;
        }

        public string Name => _term.Name;
    }
}