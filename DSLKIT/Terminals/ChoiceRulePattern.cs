using System;

namespace DSLKIT.Terminals
{
    /// <summary>
    /// Inline alternatives. Each alternative is a single term or a Seq(...) pattern.
    /// </summary>
    public sealed class ChoiceRulePattern
    {
        public ChoiceRulePattern(params object[] alternatives)
        {
            if (alternatives == null)
            {
                throw new ArgumentNullException(nameof(alternatives));
            }

            if (alternatives.Length == 0 || Array.Exists(alternatives, alternative => alternative == null))
            {
                throw new ArgumentException("At least one non-null alternative is required.", nameof(alternatives));
            }

            Alternatives = alternatives;
        }

        public object[] Alternatives { get; }
    }
}
