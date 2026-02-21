using DSLKIT.Base;

namespace DSLKIT.Parser.ExtendedGrammar
{
    public interface IExTerm
    {
        RuleSet From { get; }
        RuleSet? To { get; }
        ITerm Term { get; }
    }
}
