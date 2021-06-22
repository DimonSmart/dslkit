using System;
using DSLKIT.Base;

namespace DSLKIT.Parser.ExtendedGrammar
{
    public interface IExBase : IEquatable<IExBase>
    {
        RuleSet From { get; }
        RuleSet To { get; }
        ITerm Term { get; }
    }
}