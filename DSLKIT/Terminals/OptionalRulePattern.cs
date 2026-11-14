using System;

namespace DSLKIT.Terminals
{
    /// <summary>
    /// A sequence that may be omitted when expanding a grammar production.
    /// </summary>
    public sealed class OptionalRulePattern
    {
        public OptionalRulePattern(params object[] terms)
        {
            if (terms == null)
            {
                throw new ArgumentNullException(nameof(terms));
            }

            if (terms.Length == 0)
            {
                throw new ArgumentException("At least one term must be specified.", nameof(terms));
            }

            Terms = terms;
        }

        public object[] Terms { get; }
    }
}
