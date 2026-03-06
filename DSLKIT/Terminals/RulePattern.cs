using System;

namespace DSLKIT.Terminals
{
    public sealed class RulePattern
    {
        public RulePattern(params object[] terms)
        {
            Terms = terms ?? throw new ArgumentNullException(nameof(terms));
        }

        public object[] Terms { get; }
    }
}
