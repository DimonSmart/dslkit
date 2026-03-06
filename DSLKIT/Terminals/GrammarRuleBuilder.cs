using System;

namespace DSLKIT.Terminals
{
    public sealed class GrammarRuleBuilder
    {
        private readonly GrammarBuilder _grammarBuilder;
        private readonly string _ruleName;

        public GrammarRuleBuilder(GrammarBuilder grammarBuilder, string ruleName)
        {
            _grammarBuilder = grammarBuilder ?? throw new ArgumentNullException(nameof(grammarBuilder));
            _ruleName = string.IsNullOrWhiteSpace(ruleName)
                ? throw new ArgumentException("Rule name must be specified.", nameof(ruleName))
                : ruleName;
        }

        public GrammarRuleBuilder CanBe(params object[] terms)
        {
            AddAlternative(terms);
            return this;
        }

        public GrammarRuleBuilder Or(params object[] terms)
        {
            AddAlternative(terms);
            return this;
        }

        public GrammarRuleBuilder OneOf(params object[] alternatives)
        {
            if (alternatives == null || alternatives.Length == 0)
            {
                throw new ArgumentException("At least one alternative must be specified.", nameof(alternatives));
            }

            foreach (var alternative in alternatives)
            {
                switch (alternative)
                {
                    case null:
                        throw new ArgumentException("Alternatives must not contain null values.", nameof(alternatives));
                    case RulePattern pattern:
                        AddAlternative(pattern.Terms);
                        break;
                    default:
                        AddAlternative(alternative);
                        break;
                }
            }

            return this;
        }

        public GrammarRuleBuilder Keywords(params string[] keywords)
        {
            if (keywords == null || keywords.Length == 0)
            {
                throw new ArgumentException("At least one keyword must be specified.", nameof(keywords));
            }

            foreach (var keyword in keywords)
            {
                AddAlternative(keyword);
            }

            return this;
        }

        public GrammarRuleBuilder OrKeywords(params string[] keywords)
        {
            return Keywords(keywords);
        }

        public GrammarRuleBuilder Plus(object repeatedTerm)
        {
            _grammarBuilder.Plus(_ruleName, repeatedTerm);
            return this;
        }

        public GrammarRuleBuilder SeparatedBy(object delimiter, object repeatedTerm)
        {
            _grammarBuilder.SeparatedBy(_ruleName, repeatedTerm, delimiter);
            return this;
        }

        public GrammarBuilder Done()
        {
            return _grammarBuilder;
        }

        private void AddAlternative(params object[] terms)
        {
            if (terms == null)
            {
                throw new ArgumentNullException(nameof(terms));
            }

            _grammarBuilder.Prod(_ruleName).Is(terms);
        }
    }
}
