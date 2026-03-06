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

        public GrammarBuilder CanBe(params object[] terms)
        {
            return _grammarBuilder.Prod(_ruleName).Is(terms);
        }

        public GrammarBuilder OneOf(params object[] alternatives)
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
                        _grammarBuilder.Prod(_ruleName).Is(pattern.Terms);
                        break;
                    default:
                        _grammarBuilder.Prod(_ruleName).Is(alternative);
                        break;
                }
            }

            return _grammarBuilder;
        }

        public GrammarBuilder Keywords(params string[] keywords)
        {
            if (keywords == null || keywords.Length == 0)
            {
                throw new ArgumentException("At least one keyword must be specified.", nameof(keywords));
            }

            foreach (var keyword in keywords)
            {
                _grammarBuilder.Prod(_ruleName).Is(keyword);
            }

            return _grammarBuilder;
        }

        public GrammarBuilder Plus(object repeatedTerm)
        {
            return _grammarBuilder.Plus(_ruleName, repeatedTerm);
        }

        public GrammarBuilder SeparatedBy(object delimiter, object repeatedTerm)
        {
            return _grammarBuilder.SeparatedBy(_ruleName, repeatedTerm, delimiter);
        }
    }
}
