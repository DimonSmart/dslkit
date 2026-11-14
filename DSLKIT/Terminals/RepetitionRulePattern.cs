using System;
using System.Linq;
using DSLKIT.SpecialTerms;

namespace DSLKIT.Terminals
{
    /// <summary>
    /// Inline repetition of a non-empty sequence, with an optional separator.
    /// </summary>
    public sealed class RepetitionRulePattern
    {
        internal RepetitionRulePattern(int minimumOccurrences, object[] terms, object? delimiter = null)
        {
            if (minimumOccurrences != 0 && minimumOccurrences != 1)
            {
                throw new ArgumentOutOfRangeException(nameof(minimumOccurrences));
            }

            if (terms == null)
            {
                throw new ArgumentNullException(nameof(terms));
            }

            if (terms.Length == 0 || Array.Exists(terms, term => term == null) || CanMatchEmpty(terms))
            {
                throw new ArgumentException("Repeated terms must contain a non-empty, non-null sequence.", nameof(terms));
            }

            if (delimiter != null && CanMatchEmptyTerm(delimiter))
            {
                throw new ArgumentException("A separator cannot match an empty sequence.", nameof(delimiter));
            }

            MinimumOccurrences = minimumOccurrences;
            Terms = terms;
            Delimiter = delimiter;
        }

        public int MinimumOccurrences { get; }

        public object[] Terms { get; }

        public object? Delimiter { get; }

        private static bool CanMatchEmpty(object[] terms)
        {
            return terms.All(CanMatchEmptyTerm);
        }

        private static bool CanMatchEmptyTerm(object term)
        {
            return term switch
            {
                EmptyTerm _ => true,
                RulePattern sequence => CanMatchEmpty(sequence.Terms),
                OptionalRulePattern _ => true,
                ChoiceRulePattern choice => choice.Alternatives.Any(CanMatchEmptyTerm),
                RepetitionRulePattern repetition => repetition.MinimumOccurrences == 0 ||
                    CanMatchEmpty(repetition.Terms),
                _ => false
            };
        }
    }
}
