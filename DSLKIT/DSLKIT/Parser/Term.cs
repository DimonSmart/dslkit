using DSLKIT.Terminals;

namespace DSLKIT.Parser
{
    public abstract class Term : ITerm
    {
        public abstract string Name { get; }

        public static NonTerminal operator |(Term term1, ITerm term2)
        {
            return new NonTerminal($"NT({term1.Name}|{term2.Name})",
                new Rule(term1, term2));
        }

        public static Rule operator +(Term term1, ITerm term2)
        {
            var rule = new Rule(term1);
            rule.Data.Add(term2);
            return rule;
        }

        public static Rule operator +(Term term, string keyword)
        {
            return term + KeywordTerminal.CreateKeywordTerminal(keyword);
        }

        public static Rule operator +(Rule rule, Term term2)
        {
            rule.Data.Add(term2);
            return rule;
        }
    }
}