using DSLKIT.Base;

namespace DSLKIT.Parser.ExtendedGrammar
{
    public interface IExBase
    {
        RuleSet From { get; }
        RuleSet To { get; }
        ITerm Term { get; }
    }
}