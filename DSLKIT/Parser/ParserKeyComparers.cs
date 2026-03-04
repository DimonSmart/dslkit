using System.Collections.Generic;
using System.Runtime.CompilerServices;
using DSLKIT.Base;
using DSLKIT.NonTerminals;

namespace DSLKIT.Parser
{
    internal static class ParserKeyComparers
    {
        public static IEqualityComparer<KeyValuePair<ITerm, RuleSet>> TermBySet { get; } =
            new TermBySetComparer();

        public static IEqualityComparer<KeyValuePair<INonTerminal, RuleSet>> NonTerminalBySet { get; } =
            new NonTerminalBySetComparer();

        private sealed class TermBySetComparer : IEqualityComparer<KeyValuePair<ITerm, RuleSet>>
        {
            public bool Equals(KeyValuePair<ITerm, RuleSet> x, KeyValuePair<ITerm, RuleSet> y)
            {
                return ReferenceEquals(x.Key, y.Key) && ReferenceEquals(x.Value, y.Value);
            }

            public int GetHashCode(KeyValuePair<ITerm, RuleSet> obj)
            {
                unchecked
                {
                    return (RuntimeHelpers.GetHashCode(obj.Key) * 397) ^
                        RuntimeHelpers.GetHashCode(obj.Value);
                }
            }
        }

        private sealed class NonTerminalBySetComparer : IEqualityComparer<KeyValuePair<INonTerminal, RuleSet>>
        {
            public bool Equals(KeyValuePair<INonTerminal, RuleSet> x, KeyValuePair<INonTerminal, RuleSet> y)
            {
                return ReferenceEquals(x.Key, y.Key) && ReferenceEquals(x.Value, y.Value);
            }

            public int GetHashCode(KeyValuePair<INonTerminal, RuleSet> obj)
            {
                unchecked
                {
                    return (RuntimeHelpers.GetHashCode(obj.Key) * 397) ^
                        RuntimeHelpers.GetHashCode(obj.Value);
                }
            }
        }
    }
}
